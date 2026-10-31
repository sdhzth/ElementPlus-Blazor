using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 手写 RenderTreeBuilder 的序列号稳定性扫描器（<see cref="RenderTreeSequenceGuardTests"/> 的实现）。
    ///
    /// 抓两类真实事故（都在本仓发生过）：
    /// 🔥 R1「序列漂移」：某段手写渲染树用 <c>seq++</c> 连续编号，且<b>引用捕获帧之前</b>存在
    ///    <b>条件帧</b>（写在 if/else/switch 分支里的帧）。条件帧一旦增减，其后所有帧的序列号整体平移，
    ///    捕获帧就会与旧树里同序列的异类型帧相撞，Blazor diff 抛
    ///    <c>NotImplementedException: Unexpected frame type during RemoveOldFrame: ElementReferenceCapture</c>
    ///    —— 表现是「按钮一进 loading，整页渲染失败」（2026-10-07 的 ElButton 就是这个）。
    /// 🔥 R2「捕获后加属性」：同一个元素/组件内，属性帧出现在引用捕获帧之后，直接抛
    ///    <c>InvalidOperationException: Attributes may only be added immediately after frames of type
    ///    Element or Component</c>（2026-10-07 的 ElTreeSelect 声明式 TreeContent 就是这个）。
    ///
    /// 设计约束：
    /// 1. 先剥注释与字符串内容再判定 —— 否则注释里写一句「builder.AddAttribute(seq++ ...)」就会误报
    ///    （本仓已经因为「守卫拿原文断言」误报过三条，见 TestJsScanner）。
    /// 2. 区域 = 方法体（花括号深度 1→2 进入，回到 ≤1 退出）；跨方法不互相污染。
    /// 3. <c>OpenComponent&lt;T&gt;(...)</c> 这类带泛型实参的调用必须识别成开元素，
    ///    否则元素栈是空的，R2 永远抓不到（原型阶段就漏过一次）。
    /// </summary>
    internal static class RenderTreeSequenceScanner
    {
        public struct Violation
        {
            public string Rule;
            public int Line;
            public string Message;
            public string Code;

            public override string ToString()
            {
                return string.Format("{0} L{1}: {2}  ==>  {3}", Rule, Line, Message, Code);
            }
        }

        /// <summary>扫描过的「含 builder 帧发射的方法体区域」数量，用来防止扫描器自己失效导致假绿。</summary>
        public static int LastScopeCount { get; private set; }

        private static readonly Regex FrameRegex = new Regex(
            @"\bbuilder\.(?<name>Add[A-Za-z]*|Open[A-Za-z]*|Close[A-Za-z]*)\s*(<[^;{()]*>)?\s*\(",
            RegexOptions.Compiled);

        private static readonly Regex CounterRegex = new Regex(
            @"\b\w*[Ss]eq\w*\s*\+\+|\+\+\s*\w*[Ss]eq\w*", RegexOptions.Compiled);

        private static readonly Regex CaptureRegex = new Regex(
            @"Add(?:Component|Element)ReferenceCapture", RegexOptions.Compiled);

        private static readonly Regex AttributeRegex = new Regex(
            @"^(AddAttribute|AddMultipleAttributes|AddNamedParameter)$", RegexOptions.Compiled);

        private static readonly Regex OpenRegex = new Regex(@"^Open(Component|Element)", RegexOptions.Compiled);
        private static readonly Regex CloseRegex = new Regex(@"^Close(Component|Element)", RegexOptions.Compiled);
        private static readonly Regex KeywordRegex = new Regex(@"^[\}\)\s]*([A-Za-z_]\w*)", RegexOptions.Compiled);
        /// <summary>
        /// 同一行内联的 if（含 <c>if (x) { builder.Add…(); }</c> 这种整行写完的形式）。
        /// 只要这一行同时有帧发射，就当它是条件帧：条件帧的定义是「这次渲染可能不发射」，
        /// 与它是否独占一行无关。
        /// </summary>
        private static readonly Regex IfOnThisLineRegex = new Regex(@"\b(if|else\s+if)\s*\(", RegexOptions.Compiled);

        public static List<Violation> Scan(string text)
        {
            var violations = new List<Violation>();
            var codeLines = StripCommentsAndLiterals(text ?? string.Empty);
            int scopes = 0;

            int depth = 0;
            Region region = null;

            foreach (var pair in codeLines)
            {
                int lineNo = pair.Item1;
                string code = pair.Item2;
                string trimmed = code.Trim();

                int opens = Count(code, '{');
                int closes = Count(code, '}');
                int before = depth;
                int after = depth + opens - closes;

                if (string.IsNullOrEmpty(trimmed))
                {
                    depth = after;
                    continue;
                }

                // 区域（方法体）边界：深度 1 → 2 且本行开了花括号
                if (region == null && before == 1 && opens > 0)
                {
                    region = new Region();
                    scopes++;
                }

                if (region != null)
                {
                    AnalyzeLine(region, lineNo, code, trimmed, before, after, opens, violations);
                }

                depth = after;
                if (region != null && depth <= 1)
                {
                    region = null;
                }
            }

            LastScopeCount = scopes;
            return violations;
        }

        private static void AnalyzeLine(Region region, int lineNo, string code, string trimmed,
                                        int before, int after, int opens, List<Violation> violations)
        {
            var match = KeywordRegex.Match(trimmed);
            string keyword = match.Success ? match.Groups[1].Value : string.Empty;
            bool conditionalKeyword = keyword == "if" || keyword == "else" || keyword == "switch" || keyword == "case" || keyword == "default";

            bool insideConditional = false;
            foreach (var entry in region.Conditionals)
            {
                if (entry.Depth >= after)
                {
                    continue;
                }
                insideConditional = true;
            }

            var frames = FrameRegex.Matches(code);
            if (frames.Count == 0)
            {
                TrackRegionControl(region, conditionalKeyword, before, after);
                return;
            }

            bool lineIsConditional = insideConditional
                                   || (conditionalKeyword && opens == 0)
                                   || IfOnThisLineRegex.IsMatch(code);

            if (CounterRegex.IsMatch(code))
            {
                region.UsesCounter = true;
            }
            if (lineIsConditional && region.FirstConditionalFrameLine == 0)
            {
                region.FirstConditionalFrameLine = lineNo;
            }

            foreach (Match frame in frames)
            {
                string name = frame.Groups["name"].Value;

                if (OpenRegex.IsMatch(name))
                {
                    region.Elements.Push(0);
                }
                else if (CloseRegex.IsMatch(name))
                {
                    if (region.Elements.Count > 0)
                    {
                        region.Elements.Pop();
                    }
                }
                else if (CaptureRegex.IsMatch(name))
                {
                    if (region.Elements.Count > 0)
                    {
                        region.Elements.Pop();
                        region.Elements.Push(lineNo);
                    }
                    if (region.UsesCounter && region.FirstConditionalFrameLine > 0
                        && region.FirstConditionalFrameLine < lineNo)
                    {
                        violations.Add(new Violation
                        {
                            Rule = "R1",
                            Line = lineNo,
                            Message = "seq++ 编号 + 捕获帧之前存在条件帧（首处 L" + region.FirstConditionalFrameLine
                                    + "）：条件帧增减会让捕获帧序列漂移，diff 会抛 Unexpected frame type during RemoveOldFrame",
                            Code = Trimmed(trimmed),
                        });
                        region.FirstConditionalFrameLine = 0;
                    }
                }
                else if (AttributeRegex.IsMatch(name))
                {
                    if (region.Elements.Count > 0 && region.Elements.Peek() > 0)
                    {
                        violations.Add(new Violation
                        {
                            Rule = "R2",
                            Line = lineNo,
                            Message = "同一元素内在引用捕获帧（L" + region.Elements.Peek() + "）之后又追加属性帧",
                            Code = Trimmed(trimmed),
                        });
                    }
                }
            }

            TrackRegionControl(region, conditionalKeyword, before, after);
        }

        private static void TrackRegionControl(Region region, bool conditionalKeyword, int before, int after)
        {
            if (conditionalKeyword && before >= region.ConditionalDepthFloor)
            {
                region.Conditionals.Add(new Conditional { Depth = before });
            }
            region.Conditionals.RemoveAll(c => c.Depth >= after);
        }

        private static string Trimmed(string text)
        {
            return text.Length <= 140 ? text : text.Substring(0, 140) + "...";
        }

        private static int Count(string text, char value)
        {
            int total = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == value)
                {
                    total++;
                }
            }
            return total;
        }

        /// <summary>剥掉注释与字符串/字符内容，返回 (1 基行号, 剩余代码)。</summary>
        public static List<Tuple<int, string>> StripCommentsAndLiterals(string source)
        {
            var result = new List<Tuple<int, string>>();
            var line = new StringBuilder();
            int lineNo = 1;
            int i = 0;
            char state = ' ';   // ' ' 普通, 'l' 行注释, 'b' 块注释, 'd' 普通串, 'v' 逐字串, 'c' 字符

            while (i < source.Length)
            {
                char c = source[i];
                char next = i + 1 < source.Length ? source[i + 1] : '\0';

                if (c == '\n')
                {
                    result.Add(Tuple.Create(lineNo, line.ToString()));
                    line.Clear();
                    lineNo++;
                    if (state == 'l')
                    {
                        state = ' ';
                    }
                    i++;
                    continue;
                }

                switch (state)
                {
                    case 'l':
                        i++;
                        continue;
                    case 'b':
                        if (c == '*' && next == '/')
                        {
                            state = ' ';
                            i += 2;
                            continue;
                        }
                        i++;
                        continue;
                    case 'd':
                        if (c == '\\')
                        {
                            i += 2;
                            continue;
                        }
                        if (c == '"')
                        {
                            state = ' ';
                        }
                        i++;
                        continue;
                    case 'v':
                        if (c == '"' && next == '"')
                        {
                            i += 2;
                            continue;
                        }
                        if (c == '"')
                        {
                            state = ' ';
                        }
                        i++;
                        continue;
                    case 'c':
                        if (c == '\\')
                        {
                            i += 2;
                            continue;
                        }
                        if (c == '\'')
                        {
                            state = ' ';
                        }
                        i++;
                        continue;
                }

                if (c == '/' && next == '/')
                {
                    state = 'l';
                    i += 2;
                    continue;
                }
                if (c == '/' && next == '*')
                {
                    state = 'b';
                    i += 2;
                    continue;
                }
                if (c == '@' && next == '"')
                {
                    state = 'v';
                    i += 2;
                    continue;
                }
                if (c == '$' && (next == '"' || next == '@' || next == '\''))
                {
                    i++;
                    continue;
                }
                if (c == '"')
                {
                    state = 'd';
                    i++;
                    continue;
                }
                if (c == '\'')
                {
                    state = 'c';
                    i++;
                    continue;
                }

                line.Append(c);
                i++;
            }

            result.Add(Tuple.Create(lineNo, line.ToString()));
            return result;
        }

        private class Region
        {
            public bool UsesCounter;
            public int FirstConditionalFrameLine;
            public int ConditionalDepthFloor = -1;
            public readonly List<Conditional> Conditionals = new List<Conditional>();
            public readonly Stack<int> Elements = new Stack<int>();
        }

        private struct Conditional
        {
            public int Depth;
        }
    }
}
