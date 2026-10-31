using System.Collections.Generic;
using System.Text;

namespace ElementPlus
{
    /// <summary>Markdown 文档根节点。</summary>
    public class MdDocument
    {
        /// <summary>块级子节点（按文档顺序）。</summary>
        public List<MdBlock> Blocks { get; } = new List<MdBlock>();

        /// <summary>文档中出现的全部块级节点数量。</summary>
        public int BlockCount => Blocks.Count;
    }

    /// <summary>块级节点基类。</summary>
    public abstract class MdBlock
    {
    }

    /// <summary>可包含子块的容器基类。</summary>
    public abstract class MdContainerBlock : MdBlock
    {
        /// <summary>子块集合。</summary>
        public List<MdBlock> Blocks { get; } = new List<MdBlock>();
    }

    /// <summary>标题（ATX <c>#</c> 或 setext <c>===</c>/<c>---</c>）。</summary>
    public class MdHeading : MdBlock
    {
        /// <summary>层级 1-6。</summary>
        public int Level { get; set; }

        /// <summary>标题内的行内节点。</summary>
        public List<MdInline> Inlines { get; } = new List<MdInline>();

        /// <summary>标题纯文本（不含标记），用于生成锚点。</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>自动生成的锚点 id；未启用自动锚点时为 <c>null</c>。</summary>
        public string Anchor { get; set; }
    }

    /// <summary>段落。</summary>
    public class MdParagraph : MdBlock
    {
        /// <summary>段落内的行内节点。</summary>
        public List<MdInline> Inlines { get; } = new List<MdInline>();
    }

    /// <summary>代码块（围栏或缩进）。</summary>
    public class MdCodeBlock : MdBlock
    {
        /// <summary>语言/信息串（围栏代码块 <c>```csharp</c> 中的 <c>csharp</c>）。</summary>
        public string Info { get; set; } = string.Empty;

        /// <summary>代码原文（未转义）。</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>是否为围栏代码块。</summary>
        public bool Fenced { get; set; }
    }

    /// <summary>引用块。</summary>
    public class MdBlockQuote : MdContainerBlock
    {
    }

    /// <summary>列表。</summary>
    public class MdList : MdContainerBlock
    {
        /// <summary>是否为有序列表。</summary>
        public bool Ordered { get; set; }

        /// <summary>有序列表起始序号。</summary>
        public int Start { get; set; } = 1;

        /// <summary>是否紧凑（列表项之间无空行）。</summary>
        public bool Tight { get; set; } = true;
    }

    /// <summary>列表项。</summary>
    public class MdListItem : MdContainerBlock
    {
        /// <summary>任务列表勾选状态：<c>null</c> 表示非任务列表项。</summary>
        public bool? Checked { get; set; }
    }

    /// <summary>表格单元格对齐方式（GFM）。</summary>
    public enum MdTableAlign
    {
        /// <summary>默认（无对齐声明）。</summary>
        None,
        /// <summary>左对齐。</summary>
        Left,
        /// <summary>居中。</summary>
        Center,
        /// <summary>右对齐。</summary>
        Right
    }

    /// <summary>表格行。</summary>
    public class MdTableRow
    {
        /// <summary>是否为表头行。</summary>
        public bool IsHeader { get; set; }

        /// <summary>单元格集合，每个单元格是一组行内节点。</summary>
        public List<List<MdInline>> Cells { get; } = new List<List<MdInline>>();
    }

    /// <summary>GFM 管道表格。</summary>
    public class MdTable : MdBlock
    {
        /// <summary>各列对齐方式。</summary>
        public List<MdTableAlign> Aligns { get; } = new List<MdTableAlign>();

        /// <summary>全部行（第一行为表头）。</summary>
        public List<MdTableRow> Rows { get; } = new List<MdTableRow>();
    }

    /// <summary>分隔线。</summary>
    public class MdThematicBreak : MdBlock
    {
    }

    /// <summary>原始 HTML 块（仅在 <see cref="ElMarkdownGrowOptions.AllowHtml"/> 为 true 时产生）。</summary>
    public class MdHtmlBlock : MdBlock
    {
        /// <summary>原始 HTML 文本（渲染时会经过消毒）。</summary>
        public string Html { get; set; } = string.Empty;
    }

    /// <summary>占位块：对应 <c>ChildContent</c> 中的元素/组件帧，渲染时原样输出（不包裹段落）。</summary>
    public class MdOutletBlock : MdBlock
    {
        /// <summary>占位编号。</summary>
        public int Id { get; set; }
    }

    /// <summary>行内节点基类。</summary>
    public abstract class MdInline
    {
    }

    /// <summary>纯文本。</summary>
    public class MdText : MdInline
    {
        /// <summary>文本内容（未转义）。</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>构造。</summary>
        public MdText() { }

        /// <summary>以指定文本构造。</summary>
        public MdText(string text) { Text = text ?? string.Empty; }
    }

    /// <summary>行内代码。</summary>
    public class MdCodeSpan : MdInline
    {
        /// <summary>代码内容（未转义）。</summary>
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>含子行内节点的容器基类。</summary>
    public abstract class MdInlineContainer : MdInline
    {
        /// <summary>子行内节点。</summary>
        public List<MdInline> Inlines { get; } = new List<MdInline>();
    }

    /// <summary>斜体（<c>*em*</c> / <c>_em_</c>）。</summary>
    public class MdEmphasis : MdInlineContainer
    {
    }

    /// <summary>加粗（<c>**strong**</c> / <c>__strong__</c>）。</summary>
    public class MdStrong : MdInlineContainer
    {
    }

    /// <summary>删除线（<c>~~del~~</c>）。</summary>
    public class MdStrike : MdInlineContainer
    {
    }

    /// <summary>链接。</summary>
    public class MdLink : MdInlineContainer
    {
        /// <summary>目标地址。</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>标题（title 属性），可为空。</summary>
        public string Title { get; set; }
    }

    /// <summary>图片。</summary>
    public class MdImage : MdInline
    {
        /// <summary>图片地址。</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>替代文本（纯文本）。</summary>
        public string Alt { get; set; } = string.Empty;

        /// <summary>标题（title 属性），可为空。</summary>
        public string Title { get; set; }
    }

    /// <summary>原始 HTML 行内片段（仅在 <see cref="ElMarkdownGrowOptions.AllowHtml"/> 为 true 时产生）。</summary>
    public class MdHtmlInline : MdInline
    {
        /// <summary>原始 HTML 文本（渲染时会经过消毒）。</summary>
        public string Html { get; set; } = string.Empty;
    }

    /// <summary>换行。</summary>
    public class MdLineBreak : MdInline
    {
        /// <summary>是否为硬换行（行尾两个空格或反斜杠）。</summary>
        public bool Hard { get; set; }
    }

    /// <summary>emoji 短代码（<c>:smile:</c>）。</summary>
    public class MdEmoji : MdInline
    {
        /// <summary>短代码名（不含冒号）。</summary>
        public string Shortcode { get; set; } = string.Empty;

        /// <summary>对应字符。</summary>
        public string Glyph { get; set; } = string.Empty;
    }

    /// <summary>行内占位：对应 <c>ChildContent</c> 中出现在文本中间的元素/组件帧。</summary>
    public class MdInlineOutlet : MdInline
    {
        /// <summary>占位编号。</summary>
        public int Id { get; set; }
    }

    /// <summary>AST 工具方法。</summary>
    public static class MdAst
    {
        /// <summary>取出节点集合中的纯文本（用于标题锚点、图片 alt）。</summary>
        public static string ToPlainText(IEnumerable<MdInline> inlines)
        {
            var sb = new StringBuilder();
            AppendPlainText(sb, inlines);
            return sb.ToString();
        }

        private static void AppendPlainText(StringBuilder sb, IEnumerable<MdInline> inlines)
        {
            if (inlines == null)
            {
                return;
            }

            foreach (var inline in inlines)
            {
                switch (inline)
                {
                    case MdText text:
                        sb.Append(text.Text);
                        break;
                    case MdCodeSpan code:
                        sb.Append(code.Code);
                        break;
                    case MdEmoji emoji:
                        sb.Append(emoji.Glyph);
                        break;
                    case MdInlineContainer container:
                        AppendPlainText(sb, container.Inlines);
                        break;
                }
            }
        }
    }
}
