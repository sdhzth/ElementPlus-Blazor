using AngleSharp.Dom;
using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// ElHtmlEditorGrow 组件测试（仅覆盖本组件，不跑全量）。
    /// <para>
    /// 覆盖：外壳渲染与模式切换、工具栏数据与图标、公开命令映射、状态 / 表格选区 / 表格参数的 JSON 解析、
    /// 纯 C# 的 HTML 构建器（表格 / 图片 / 链接，单一真源）、粘贴消毒、上传载荷解析，
    /// 以及三条守卫：浏览器兼容、零第三方依赖、自写 JS 通道契约。
    /// </para>
    /// <para>
    /// 交互一律通过 DOM 事件派发（<c>TriggerEventAsync</c> / <c>Click()</c>）驱动，
    /// 因为工具栏与编辑区的事件处理器是 <c>protected/private</c>，只有走真实事件链路才能覆盖到接线本身。
    /// </para>
    /// </summary>
    public class ElHtmlEditorGrowTests : BunitContext
    {
        public ElHtmlEditorGrowTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        #region 辅助

        private IRenderedComponent<ElHtmlEditorGrow> Editor(
            Action<ComponentParameterCollectionBuilder<ElHtmlEditorGrow>> configure = null)
        {
            if (configure == null)
            {
                // 注意：不能写成 Render<ElHtmlEditorGrow>(p => { }) —— 空 lambda 会被解析成
                // RenderFragment 重载，渲染出一个空片段，导致 Instance/Find 抛 ComponentNotFoundException。
                return Render<ElHtmlEditorGrow>();
            }

            return Render<ElHtmlEditorGrow>(configure);
        }

        private static string RepoRoot([CallerFilePath] string file = null)
        {
            // .../test/Element.ComponentTests/ElHtmlEditorGrowTests.cs → 上溯三层
            var directory = new FileInfo(file).Directory;

            return directory.Parent.Parent.FullName;
        }

        /// <summary>读取 index.css 补丁段（原 fix.css，2026-09-26 并入）中 ElHtmlEditorGrow 的样式段（已剥离注释）。</summary>
        private static string CssSection()
        {
            var path = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "css", "index.css");
            var text = File.ReadAllText(path);
            var marker = text.IndexOf("ElHtmlEditorGrow：Element Plus 自渲染富文本编辑器", StringComparison.Ordinal);

            Assert.True(marker >= 0, "index.css 中未找到 ElHtmlEditorGrow 样式段");

            var start = text.LastIndexOf("/*", marker, StringComparison.Ordinal);

            // 只取本组件的段落：截到下一个「段分隔注释」为止。
            // 否则会把样式表里后面追加的其它组件样式也算进来，作用域守卫会误报。
            var next = text.IndexOf("\n/* =", marker + 10, StringComparison.Ordinal);
            var section = next >= 0 ? text.Substring(start, next - start) : text.Substring(start);

            return Regex.Replace(section, @"/\*[\s\S]*?\*/", string.Empty);
        }

        /// <summary>读取 dom.js 中 ElHtmlEditorGrow 的自写通道段。</summary>
        private static string JsSection()
        {
            var path = Path.Combine(RepoRoot(), "src", "Components", "wwwroot", "js", "dom.js");
            var text = File.ReadAllText(path);
            var marker = text.IndexOf("ElHtmlEditorGrow：自写富文本编辑器浏览器通道", StringComparison.Ordinal);

            Assert.True(marker >= 0, "dom.js 中未找到 ElHtmlEditorGrow 通道");

            return text.Substring(marker);
        }

        /// <summary>
        /// 剥离注释（块注释 + 行注释 + 文档注释），只留可执行代码。
        /// 实现收敛到 <see cref="TestJsScanner"/>：必须是引号感知的，
        /// 否则字符串里的 <c>"https://…"</c> 会被当成行注释，把后面的真代码整段剥没、守卫失去牙齿。
        /// </summary>
        private static string StripComments(string text) => TestJsScanner.StripComments(text);

        private static ElHtmlEditorGrowTableArgs TableArgs(Action<ElHtmlEditorGrowTableArgs> configure = null)
        {
            var args = new ElHtmlEditorGrowTableArgs();
            configure?.Invoke(args);

            return args;
        }

        private static ElHtmlEditorGrowState State(string json) => ElHtmlEditorGrowState.Parse(json);

        private static IElement FindToolButton(IRenderedComponent<ElHtmlEditorGrow> cut, string title)
            => cut.FindAll(".el-html-editor-grow__btn").First(b => b.GetAttribute("title") == title);

        private static IElement FindSelect(IRenderedComponent<ElHtmlEditorGrow> cut, string title)
            => cut.FindAll("select.el-html-editor-grow__select").First(s => s.GetAttribute("title") == title);

        #endregion

        #region 外壳渲染

        [Fact]
        public void Renders_WrapperWithRootClass()
        {
            var cut = Editor();

            var wrapper = cut.Find("div.el-html-editor-grow");

            Assert.NotNull(wrapper);
            Assert.Equal("el-html-editor-grow", wrapper.ClassList.First());
        }

        [Fact]
        public void Renders_ToolbarWithRoleAndLabel()
        {
            var cut = Editor();

            var toolbar = cut.Find("div.el-html-editor-grow__toolbar");

            Assert.Equal("toolbar", toolbar.GetAttribute("role"));
            Assert.False(string.IsNullOrWhiteSpace(toolbar.GetAttribute("aria-label")));
        }

        [Fact]
        public void ShowToolbarFalse_OmitsToolbar()
        {
            var cut = Editor(p => p.Add(x => x.ShowToolbar, false));

            Assert.Empty(cut.FindAll(".el-html-editor-grow__toolbar"));
        }

        [Fact]
        public void Renders_ContentEditableContainer()
        {
            var cut = Editor();

            var content = cut.Find("div.el-html-editor-grow__content");

            Assert.Equal("true", content.GetAttribute("contenteditable"));
            Assert.Equal("textbox", content.GetAttribute("role"));
            Assert.Equal("true", content.GetAttribute("aria-multiline"));
            Assert.Equal("false", content.GetAttribute("spellcheck"));
        }

        [Fact]
        public void ContentEditable_ExposesPlaceholder()
        {
            var cut = Editor(p => p.Add(x => x.Placeholder, "请输入病情描述"));

            var content = cut.Find(".el-html-editor-grow__content");

            Assert.Equal("请输入病情描述", content.GetAttribute("data-placeholder"));
            Assert.Equal("请输入病情描述", content.GetAttribute("aria-label"));
        }

        [Fact]
        public void Renders_SourceTextareaHiddenInDesignMode()
        {
            var cut = Editor();

            var source = cut.Find("textarea.el-html-editor-grow__source");

            Assert.True(source.HasAttribute("hidden"));
            Assert.False(cut.Find(".el-html-editor-grow__content").HasAttribute("hidden"));
        }

        [Fact]
        public void RendersHiddenFileInput_WithAccept()
        {
            var cut = Editor(p => p.Add(x => x.Accept, "image/png"));

            var input = cut.Find("input.el-html-editor-grow__file");

            Assert.Equal("file", input.GetAttribute("type"));
            Assert.Equal("image/png", input.GetAttribute("accept"));
            Assert.Equal("-1", input.GetAttribute("tabindex"));
        }

        [Fact]
        public void Disabled_AddsClassAndDisablesEveryTool()
        {
            var cut = Editor(p => p.Add(x => x.Disabled, true));

            Assert.Contains("is-disabled", cut.Find("div.el-html-editor-grow").ClassList);
            Assert.False(cut.Instance.CanEdit);
            Assert.All(cut.FindAll(".el-html-editor-grow__btn"), b => Assert.True(b.HasAttribute("disabled")));
        }

        [Fact]
        public void ReadOnly_AddsClassAndBlocksEditing()
        {
            var cut = Editor(p => p.Add(x => x.ReadOnly, true));

            Assert.Contains("is-readonly", cut.Find("div.el-html-editor-grow").ClassList);
            Assert.Equal("false", cut.Find(".el-html-editor-grow__content").GetAttribute("contenteditable"));
            Assert.True(cut.Find("textarea.el-html-editor-grow__source").HasAttribute("readonly"));
        }

        [Fact]
        public void ClsAndStyle_AreAppliedToWrapper()
        {
            var cut = Editor(p => p
                .Add(x => x.Cls, "my-editor")
                .Add(x => x.Style, "margin-top:4px"));

            var wrapper = cut.Find("div.el-html-editor-grow");

            Assert.Contains("my-editor", wrapper.ClassList);
            Assert.Equal("margin-top:4px", wrapper.GetAttribute("style"));
        }

        [Fact]
        public void HeightAndMinHeight_AreAppliedToBody()
        {
            var cut = Editor(p => p
                .Add(x => x.Height, "300px")
                .Add(x => x.MinHeight, "180px"));

            var style = cut.Find(".el-html-editor-grow__body").GetAttribute("style");

            Assert.Contains("300px", style);
            Assert.Contains("180px", style);
        }

        [Fact]
        public void Wrapper_HasStableEditorId()
        {
            var cut = Editor();

            var id = cut.Find("div.el-html-editor-grow").GetAttribute("id");

            Assert.StartsWith("el-html-editor-grow-", id, StringComparison.Ordinal);
            Assert.Equal(id, cut.Instance.EditorId);
        }

        #endregion

        #region 值与事件绑定

        [Fact]
        public void Value_IsLoadedIntoState()
        {
            var cut = Editor(p => p.Add(x => x.Value, "<p>体温 36.5℃</p>"));

            Assert.Equal("<p>体温 36.5℃</p>", cut.Instance.Html);
        }

        [Fact]
        public void NullValue_IsNormalisedToEmpty()
        {
            var cut = Editor(p => p.Add(x => x.Value, (string)null));

            Assert.Equal(string.Empty, cut.Instance.Html);
        }

        [Fact]
        public void ChangedValueParameter_PushesIntoEditor()
        {
            var cut = Editor(p => p.Add(x => x.Value, "<p>a</p>"));

            cut.Render(p => p.Add(x => x.Value, "<p>b</p>"));

            Assert.Equal("<p>b</p>", cut.Instance.Html);
            Assert.Equal("<p>b</p>", cut.Find("textarea.el-html-editor-grow__source").GetAttribute("value"));
        }

        [Fact]
        public async Task UserInput_RaisesInputAndValueChanged()
        {
            string value = null;
            string input = null;

            var cut = Editor(p => p
                .Add(x => x.ValueChanged, v => value = v)
                .Add(x => x.Input, v => input = v));

            await cut.InvokeAsync(() => cut.Instance.OnEditorInputAsync("<p>脉搏 80</p>"));

            Assert.Equal("<p>脉搏 80</p>", cut.Instance.Html);
            Assert.Equal("<p>脉搏 80</p>", value);
            Assert.Equal("<p>脉搏 80</p>", input);
        }

        [Fact]
        public async Task SameInput_IsIgnored()
        {
            var raised = 0;

            var cut = Editor(p => p.Add(x => x.Input, _ => raised++));

            await cut.InvokeAsync(() => cut.Instance.OnEditorInputAsync(string.Empty));
            await cut.InvokeAsync(() => cut.Instance.OnEditorInputAsync(string.Empty));

            Assert.Equal(0, raised);
        }

        [Fact]
        public async Task Blur_RaisesChange()
        {
            string changed = null;

            var cut = Editor(p => p
                .Add(x => x.Value, "<p>x</p>")
                .Add(x => x.Change, v => changed = v));

            await cut.InvokeAsync(() => cut.Instance.OnEditorBlurAsync());

            Assert.Equal("<p>x</p>", changed);
        }

        [Fact]
        public async Task GetHtmlAsync_FallsBackToStateWithoutJs()
        {
            var cut = Editor(p => p.Add(x => x.Value, "<p>ok</p>"));

            var html = await cut.InvokeAsync(() => cut.Instance.GetHtmlAsync());

            Assert.Equal("<p>ok</p>", html);
        }

        [Fact]
        public async Task SetHtmlAsync_InvokesJsAndUpdatesState()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => cut.Instance.SetHtmlAsync("<p>写回</p>"));

            Assert.Equal("<p>写回</p>", cut.Instance.Html);
            Assert.Contains(JSInterop.Invocations, i => i.Identifier == "elementHtmlEditorSetHtml");
        }

        #endregion

        #region 模式切换

        [Fact]
        public void ModeSource_ShowsSourceTextarea()
        {
            var cut = Editor(p => p
                .Add(x => x.Mode, ElHtmlEditorGrowMode.Source)
                .Add(x => x.Value, "<p>hi</p>"));

            Assert.True(cut.Find(".el-html-editor-grow__content").HasAttribute("hidden"));
            Assert.False(cut.Find("textarea.el-html-editor-grow__source").HasAttribute("hidden"));
            Assert.Equal(ElHtmlEditorGrowMode.Source, cut.Instance.CurrentMode);
            Assert.Equal("<p>hi</p>", cut.Find("textarea.el-html-editor-grow__source").GetAttribute("value"));
        }

        [Fact]
        public async Task ToggleMode_SwitchesAndRaisesModeChanged()
        {
            ElHtmlEditorGrowMode? next = null;

            var cut = Editor(p => p.Add(x => x.ModeChanged, v => next = v));

            await cut.InvokeAsync(() => cut.Instance.ToggleModeAsync());

            Assert.Equal(ElHtmlEditorGrowMode.Source, cut.Instance.CurrentMode);
            Assert.Equal(ElHtmlEditorGrowMode.Source, next);

            await cut.InvokeAsync(() => cut.Instance.ToggleModeAsync());

            Assert.Equal(ElHtmlEditorGrowMode.Design, cut.Instance.CurrentMode);
        }

        [Fact]
        public async Task SetMode_SameValue_DoesNothing()
        {
            var raised = 0;

            var cut = Editor(p => p.Add(x => x.ModeChanged, _ => raised++));

            await cut.InvokeAsync(() => cut.Instance.SetModeAsync(ElHtmlEditorGrowMode.Design));

            Assert.Equal(0, raised);
        }

        [Fact]
        public void ModeFlags_ComposeAsExpected()
        {
            Assert.Equal(ElHtmlEditorGrowMode.Design, ElHtmlEditorGrowMode.All & ElHtmlEditorGrowMode.Design);
            Assert.Equal(ElHtmlEditorGrowMode.Source, ElHtmlEditorGrowMode.All & ElHtmlEditorGrowMode.Source);
            Assert.Equal(0, (int)(ElHtmlEditorGrowMode.Design & ElHtmlEditorGrowMode.Source));
        }

        [Fact]
        public async Task SourceTextarea_InputUpdatesHtml()
        {
            string value = null;

            var cut = Render<ElHtmlEditorGrow>(p => p
                .Add(x => x.Mode, ElHtmlEditorGrowMode.Source)
                .Add(x => x.ValueChanged, v => value = v));

            await cut.Find("textarea.el-html-editor-grow__source")
                .TriggerEventAsync("oninput", new ChangeEventArgs { Value = "<p>源码</p>" });

            Assert.Equal("<p>源码</p>", cut.Instance.Html);
            Assert.Equal("<p>源码</p>", value);
        }

        [Fact]
        public async Task SourceToolClick_TogglesMode()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => FindToolButton(cut, "源码 / 设计视图").Click());

            Assert.Equal(ElHtmlEditorGrowMode.Source, cut.Instance.CurrentMode);

            await cut.InvokeAsync(() => FindToolButton(cut, "源码 / 设计视图").Click());

            Assert.Equal(ElHtmlEditorGrowMode.Design, cut.Instance.CurrentMode);
        }

        #endregion

        #region 工具栏数据

        [Fact]
        public void DefaultToolbar_HasExpectedShape()
        {
            var tools = ElHtmlEditorGrowToolbar.Default;

            Assert.Equal(45, tools.Count);
            Assert.Equal(8, tools.Count(t => t.Kind == ElHtmlEditorGrowToolKind.Separator));
            Assert.Equal(3, tools.Count(t => t.Kind == ElHtmlEditorGrowToolKind.Select));
            Assert.Equal(2, tools.Count(t => t.Kind == ElHtmlEditorGrowToolKind.Color));
            Assert.Equal(10, tools.Count(t => t.Kind == ElHtmlEditorGrowToolKind.TableCommand));
        }

        [Fact]
        public void DefaultToolbar_StartsWithUndoRedo()
        {
            var tools = ElHtmlEditorGrowToolbar.Default;

            Assert.Equal("undo", tools[0].Name);
            Assert.Equal("redo", tools[1].Name);
            Assert.Equal(ElHtmlEditorGrowCommands.Undo, tools[0].Command);
        }

        [Fact]
        public void DefaultToolbar_LastToolIsSource()
        {
            var tools = ElHtmlEditorGrowToolbar.Default;

            Assert.Equal("source", tools[tools.Count - 1].Name);
            Assert.Equal(ElHtmlEditorGrowMode.All, tools[tools.Count - 1].EnabledModes);
        }

        [Fact]
        public void EveryVisibleTool_HasNameTitleAndIcon()
        {
            foreach (var tool in ElHtmlEditorGrowToolbar.Default.Where(t => t.Kind != ElHtmlEditorGrowToolKind.Separator))
            {
                Assert.False(string.IsNullOrWhiteSpace(tool.Name));
                Assert.False(string.IsNullOrWhiteSpace(tool.Title));

                // 下拉工具用原生 select 呈现，不带图标；其余工具必须引用一个真实存在的图标。
                if (tool.Kind == ElHtmlEditorGrowToolKind.Select)
                {
                    Assert.True(string.IsNullOrEmpty(tool.Icon) || ElHtmlEditorGrowIcons.Has(tool.Icon));
                    continue;
                }

                Assert.True(ElHtmlEditorGrowIcons.Has(tool.Icon), "未知图标：" + tool.Icon);
            }
        }

        [Fact]
        public void IconTools_AllReferenceDeclaredIcons()
        {
            var iconTools = ElHtmlEditorGrowToolbar.Default
                .Where(t => t.Kind == ElHtmlEditorGrowToolKind.Button
                    || t.Kind == ElHtmlEditorGrowToolKind.Color
                    || t.Kind == ElHtmlEditorGrowToolKind.TableCommand)
                .ToList();

            Assert.Equal(34, iconTools.Count);
            Assert.All(iconTools, t => Assert.True(ElHtmlEditorGrowIcons.Has(t.Icon), "未知图标：" + t.Icon));
        }

        [Fact]
        public void ToolNames_AreUnique()
        {
            var names = ElHtmlEditorGrowToolbar.Default
                .Where(t => t.Kind != ElHtmlEditorGrowToolKind.Separator)
                .Select(t => t.Name)
                .ToList();

            Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Fact]
        public void SelectTools_DeclareStatePropertyAndOptions()
        {
            foreach (var tool in ElHtmlEditorGrowToolbar.Default.Where(t => t.Kind == ElHtmlEditorGrowToolKind.Select))
            {
                Assert.False(string.IsNullOrWhiteSpace(tool.StateProperty));
                Assert.NotNull(tool.Options);
                Assert.NotEmpty(tool.Options);
                Assert.False(string.IsNullOrWhiteSpace(tool.Width));
            }
        }

        [Fact]
        public void TableCommandTools_RequireTable()
        {
            var tableTools = ElHtmlEditorGrowToolbar.Default
                .Where(t => t.Kind == ElHtmlEditorGrowToolKind.TableCommand)
                .ToList();

            Assert.NotEmpty(tableTools);
            Assert.All(tableTools, t => Assert.True(t.RequiresTable));
        }

        [Fact]
        public void Find_IsCaseInsensitiveAndSafe()
        {
            Assert.NotNull(ElHtmlEditorGrowToolbar.Find("bold"));
            Assert.NotNull(ElHtmlEditorGrowToolbar.Find("BOLD"));
            Assert.Null(ElHtmlEditorGrowToolbar.Find("nope"));
            Assert.Null(ElHtmlEditorGrowToolbar.Find(null));
            Assert.Null(ElHtmlEditorGrowToolbar.Find(string.Empty));
        }

        [Fact]
        public void Shortcuts_AreUniqueWhenPresent()
        {
            var shortcuts = ElHtmlEditorGrowToolbar.Default
                .Where(t => !string.IsNullOrWhiteSpace(t.Shortcut))
                .Select(t => t.Shortcut)
                .ToList();

            Assert.NotEmpty(shortcuts);
            Assert.Equal(shortcuts.Count, shortcuts.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Fact]
        public void Palette_HasFiftyOpaqueColors()
        {
            Assert.Equal(50, ElHtmlEditorGrowToolbar.Palette.Count);
            Assert.All(ElHtmlEditorGrowToolbar.Palette, c => Assert.StartsWith("#", c, StringComparison.Ordinal));
        }

        [Fact]
        public void FontSizeOptions_MatchNativeScale()
        {
            var options = ElHtmlEditorGrowToolbar.FontSizeOptions;

            Assert.Equal(7, options.Count);
            Assert.Equal(new[] { "1", "2", "3", "4", "5", "6", "7" }, options.Select(o => o.Value).ToArray());
        }

        [Fact]
        public void FontNameOptions_IncludeChineseFonts()
        {
            Assert.Contains(ElHtmlEditorGrowToolbar.FontNameOptions, o => o.Text == "宋体");
            Assert.Contains(ElHtmlEditorGrowToolbar.FontNameOptions, o => o.Text == "微软雅黑");
        }

        [Fact]
        public void FormatBlockOptions_CoverParagraphAndHeadings()
        {
            var values = ElHtmlEditorGrowToolbar.FormatBlockOptions.Select(o => o.Value).ToList();

            Assert.Equal(7, values.Count);
            Assert.Contains("p", values);
            Assert.Contains("h1", values);
            Assert.Contains("h6", values);
        }

        [Fact]
        public void Buttons_DefaultToDesignModeOnly()
        {
            Assert.Equal(ElHtmlEditorGrowMode.Design, ElHtmlEditorGrowToolbar.Find("bold").EnabledModes);
        }

        #endregion

        #region 工具栏渲染与交互

        [Fact]
        public void DefaultToolbar_RendersButtonSelectAndSeparatorCounts()
        {
            var cut = Editor();

            Assert.Equal(34, cut.FindAll(".el-html-editor-grow__btn").Count);
            Assert.Equal(8, cut.FindAll(".el-html-editor-grow__separator").Count);
            Assert.Equal(3, cut.FindAll("select.el-html-editor-grow__select").Count);
        }

        [Fact]
        public void ColorTools_RenderSwatchBar()
        {
            var cut = Editor();

            Assert.Equal(2, cut.FindAll(".el-html-editor-grow__color-bar").Count);
        }

        [Fact]
        public void ToolButtons_AreLabelledAndUnpressed()
        {
            var cut = Editor();

            var bold = FindToolButton(cut, "加粗");

            Assert.Equal("加粗", bold.GetAttribute("aria-label"));
            Assert.Equal("false", bold.GetAttribute("aria-pressed"));
            Assert.False(bold.HasAttribute("disabled"));
        }

        [Fact]
        public async Task ClickingBold_InvokesExecCommand()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => FindToolButton(cut, "加粗").Click());

            var invocation = JSInterop.Invocations.First(i => i.Identifier == "elementHtmlEditorExec");

            Assert.Equal(ElHtmlEditorGrowCommands.Bold, (string)invocation.Arguments[1]);
        }

        [Fact]
        public async Task Bold_BecomesPressedAfterStateRoundTrip()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => FindToolButton(cut, "加粗").Click());

            cut.Instance.State.Bold = true;
            cut.Render();

            Assert.Equal("true", FindToolButton(cut, "加粗").GetAttribute("aria-pressed"));
        }

        [Fact]
        public async Task InsertHtml_MapsToNativeInsertHtmlCommand()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => cut.Instance.ExecuteCommandAsync(
                ElHtmlEditorGrowCommands.InsertHtml, "<p>x</p>"));

            var invocation = JSInterop.Invocations.First(i => i.Identifier == "elementHtmlEditorExec");

            Assert.Equal("insertHTML", (string)invocation.Arguments[1]);
            Assert.Equal("<p>x</p>", (string)invocation.Arguments[2]);
        }

        [Fact]
        public async Task ExecuteCommand_RaisesExecuteEvent()
        {
            ElHtmlEditorGrowExecuteArgs args = null;

            var cut = Editor(p => p.Add(x => x.Execute, a => args = a));

            await cut.InvokeAsync(() => cut.Instance.ExecuteCommandAsync(ElHtmlEditorGrowCommands.Bold));

            Assert.NotNull(args);
            Assert.Equal(ElHtmlEditorGrowCommands.Bold, args.CommandName);
        }

        [Fact]
        public async Task ExecuteCommand_WhenDisabled_DoesNothing()
        {
            var cut = Editor(p => p.Add(x => x.Disabled, true));

            await cut.InvokeAsync(() => cut.Instance.ExecuteCommandAsync(ElHtmlEditorGrowCommands.Bold));

            Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "elementHtmlEditorExec");
        }

        [Fact]
        public void TableCommands_AreDisabledOutsideTable()
        {
            var cut = Editor();

            Assert.False(cut.Instance.IsToolEnabled(ElHtmlEditorGrowToolbar.Find("row-above")));
            Assert.False(cut.Instance.TableSelection.InTable);
            Assert.True(FindToolButton(cut, "上方插入行").HasAttribute("disabled"));
        }

        [Fact]
        public void TableCommands_EnableWhenSelectionIsInsideTable()
        {
            var cut = Editor();

            cut.Instance.TableSelection.InTable = true;
            cut.Render();

            Assert.True(cut.Instance.IsToolEnabled(ElHtmlEditorGrowToolbar.Find("row-above")));
            Assert.False(FindToolButton(cut, "上方插入行").HasAttribute("disabled"));
        }

        [Fact]
        public void SourceTool_IsActiveInSourceMode()
        {
            var cut = Editor(p => p.Add(x => x.Mode, ElHtmlEditorGrowMode.Source));

            Assert.True(cut.Instance.IsToolActive(ElHtmlEditorGrowToolbar.Find("source")));

            var button = FindToolButton(cut, "源码 / 设计视图");

            Assert.Contains("is-active", button.ClassList);
        }

        [Fact]
        public void HiddenTools_AreFilteredOut()
        {
            var cut = Editor(p => p.Add(x => x.HiddenTools, new[] { "bold", "italic" }));

            Assert.DoesNotContain(cut.Instance.VisibleTools, t => t.Name == "bold");
            Assert.DoesNotContain(cut.Instance.VisibleTools, t => t.Name == "italic");
            Assert.Contains(cut.Instance.VisibleTools, t => t.Name == "underline");
            Assert.Equal(32, cut.FindAll(".el-html-editor-grow__btn").Count);

            // 被隐藏的工具前后各有分隔符时，分隔符本身仍成立（8 个都在）。
            Assert.Equal(8, cut.FindAll(".el-html-editor-grow__separator").Count);
        }

        [Fact]
        public void DesignOnlyTool_IsRenderedButDisabledInSourceMode()
        {
            var cut = Editor(p => p.Add(
                x => x.Tools,
                new[]
                {
                    new ElHtmlEditorGrowToolDescriptor
                    {
                        Name = "bold",
                        Kind = ElHtmlEditorGrowToolKind.Button,
                        Command = ElHtmlEditorGrowCommands.Bold,
                        Title = "加粗",
                        Icon = "bold",
                        EnabledModes = ElHtmlEditorGrowMode.Design
                    }
                }));

            Assert.Single(cut.Instance.VisibleTools);

            cut.Render(p => p.Add(x => x.Mode, ElHtmlEditorGrowMode.Source));

            // EnabledModes 只决定「可点 / 置灰」，不把工具从工具栏里抽走
            // （参考实现的 EditorButton 同样只置灰）；要彻底不出现请用 HiddenTools。
            Assert.Single(cut.Instance.VisibleTools);

            var button = Assert.Single(cut.FindAll(".el-html-editor-grow__btn"));

            Assert.True(button.HasAttribute("disabled"));
            Assert.Contains("is-disabled", button.ClassList);
        }

        [Fact]
        public void SourceMode_KeepsFullToolbarWithIconsAndDisablesDesignTools()
        {
            var cut = Editor(p => p.Add(x => x.Mode, ElHtmlEditorGrowMode.Source));

            // 工具栏不因模式塌掉：图标按钮 / 分隔符 / 下拉都还在，布局与设计视图一致。
            // 此前按模式过滤工具，源码视图里只剩下一排孤立的分隔符——看上去像一排没有图标的空按钮。
            Assert.Equal(34, cut.FindAll(".el-html-editor-grow__btn").Count);
            Assert.Equal(8, cut.FindAll(".el-html-editor-grow__separator").Count);
            Assert.Equal(3, cut.FindAll("select.el-html-editor-grow__select").Count);

            // 每个按钮都带图标，不存在「有按钮、没图标」的空壳。
            foreach (var button in cut.FindAll(".el-html-editor-grow__btn"))
            {
                Assert.NotEmpty(button.QuerySelectorAll("svg.el-html-editor-grow__icon"));
            }

            // 设计类命令一律置灰；只有「源码 / 设计视图」可点且处于激活态。
            Assert.True(FindToolButton(cut, "加粗").HasAttribute("disabled"));
            Assert.Contains("is-disabled", FindToolButton(cut, "加粗").ClassList);
            Assert.False(cut.Instance.IsToolEnabled(ElHtmlEditorGrowToolbar.Find("bold")));

            var sourceButton = FindToolButton(cut, "源码 / 设计视图");

            Assert.False(sourceButton.HasAttribute("disabled"));
            Assert.Contains("is-active", sourceButton.ClassList);
        }

        [Fact]
        public void HiddenTools_CollapseTheSeparatorsTheyLeaveBehind()
        {
            // 隐藏「加粗 / 斜体 / 下划线 / 删除线」整段后，两侧的分隔符会贴在一起，必须合并成一个。
            var cut = Editor(p => p.Add(x => x.HiddenTools, new[] { "bold", "italic", "underline", "strike" }));

            Assert.Equal(30, cut.FindAll(".el-html-editor-grow__btn").Count);
            Assert.Equal(7, cut.FindAll(".el-html-editor-grow__separator").Count);
        }

        [Fact]
        public void VisibleTools_NeverDangleSeparators()
        {
            foreach (var mode in new[]
            {
                ElHtmlEditorGrowMode.Design, ElHtmlEditorGrowMode.Source, ElHtmlEditorGrowMode.All
            })
            {
                var tools = Editor(p => p.Add(x => x.Mode, mode)).Instance.VisibleTools;

                Assert.NotEqual(ElHtmlEditorGrowToolKind.Separator, tools[0].Kind);
                Assert.NotEqual(ElHtmlEditorGrowToolKind.Separator, tools[tools.Count - 1].Kind);

                for (var i = 1; i < tools.Count; i++)
                {
                    Assert.False(
                        tools[i].Kind == ElHtmlEditorGrowToolKind.Separator
                        && tools[i - 1].Kind == ElHtmlEditorGrowToolKind.Separator);
                }
            }
        }

        [Fact]
        public void CustomTools_ReplaceDefaultToolbar()
        {
            var cut = Editor(p => p.Add(x => x.Tools, new[]
            {
                new ElHtmlEditorGrowToolDescriptor
                {
                    Name = "only",
                    Kind = ElHtmlEditorGrowToolKind.Button,
                    Command = ElHtmlEditorGrowCommands.Bold,
                    Title = "仅加粗",
                    Icon = "bold"
                }
            }));

            Assert.Single(cut.Instance.VisibleTools);
            Assert.Single(cut.FindAll(".el-html-editor-grow__btn"));
        }

        [Fact]
        public void ChildContent_RendersInsteadOfDefaultToolbar()
        {
            var cut = Render<ElHtmlEditorGrow>(p => p
                .AddChildContent("<span class=\"custom-bar\">自定义</span>"));

            Assert.Single(cut.FindAll(".custom-bar"));
            Assert.Empty(cut.FindAll(".el-html-editor-grow__btn"));
        }

        [Fact]
        public void ToolbarCls_IsAppended()
        {
            var cut = Editor(p => p.Add(x => x.ToolbarCls, "my-bar"));

            Assert.Contains("my-bar", cut.Find(".el-html-editor-grow__toolbar").ClassList);
        }

        #endregion

        #region 面板

        [Fact]
        public async Task LinkTool_OpensLinkPanel()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => FindToolButton(cut, "插入链接").Click());

            Assert.Equal("link", cut.Instance.ActivePanel);
            Assert.Single(cut.FindAll(".el-html-editor-grow__panel"));
            Assert.Contains("插入链接", cut.Markup);
        }

        [Fact]
        public async Task SubmitLinkPanel_InsertsBuiltLink()
        {
            var cut = Editor(p => p.Add(x => x.Value, "<p>选中文本</p>"));

            await cut.InvokeAsync(() => FindToolButton(cut, "插入链接").Click());

            cut.Instance.LinkUrl = "https://example.com/a?b=1&c=2";
            cut.Instance.LinkText = "病历系统";
            cut.Instance.LinkNewWindow = true;

            await cut.InvokeAsync(() => cut.Instance.SubmitLinkPanelAsync());

            var markup = (string)JSInterop.Invocations
                .First(i => i.Identifier == "elementHtmlEditorExec").Arguments[2];

            Assert.Contains("href=\"https://example.com/a?b=1&amp;c=2\"", markup);
            Assert.Contains("target=\"_blank\"", markup);
            Assert.Contains("rel=\"noopener noreferrer\"", markup);
            Assert.Contains("病历系统", markup);
            Assert.Null(cut.Instance.ActivePanel);
        }

        [Fact]
        public async Task SubmitLinkPanel_WithDangerousUrl_InsertsNothing()
        {
            var cut = Editor();

            cut.Instance.LinkUrl = "javascript:alert(1)";

            await cut.InvokeAsync(() => cut.Instance.SubmitLinkPanelAsync());

            Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "elementHtmlEditorExec");
        }

        [Fact]
        public async Task ImageTool_OpensImagePanel()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => FindToolButton(cut, "插入图片").Click());

            Assert.Equal("image", cut.Instance.ActivePanel);
            Assert.Contains("插入图片", cut.Markup);
        }

        [Fact]
        public async Task SubmitImagePanel_InsertsBuiltImage()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => FindToolButton(cut, "插入图片").Click());

            cut.Instance.ImageUrl = "https://cdn.example.com/a.png";
            cut.Instance.ImageAlt = "示意图";
            cut.Instance.ImageWidth = "120";

            await cut.InvokeAsync(() => cut.Instance.SubmitImagePanelAsync());

            var markup = (string)JSInterop.Invocations
                .First(i => i.Identifier == "elementHtmlEditorExec").Arguments[2];

            Assert.Contains("<img", markup);
            Assert.Contains("alt=\"示意图\"", markup);
            Assert.Contains("width=\"120px\"", markup);
        }

        [Fact]
        public async Task SubmitImagePanel_WithDangerousUrl_InsertsNothing()
        {
            var cut = Editor();

            cut.Instance.ImageUrl = "javascript:alert(1)";

            await cut.InvokeAsync(() => cut.Instance.SubmitImagePanelAsync());

            Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "elementHtmlEditorExec");
        }

        [Fact]
        public async Task TableTool_OpensInsertPanelWithDefaults()
        {
            var cut = Editor(p => p
                .Add(x => x.DefaultTableRows, 4)
                .Add(x => x.DefaultTableColumns, 3));

            await cut.InvokeAsync(() => FindToolButton(cut, "插入表格").Click());

            Assert.Equal("table", cut.Instance.ActivePanel);
            Assert.False(cut.Instance.TableUpdating);
            Assert.Equal(4, cut.Instance.TableDraft.Rows);
            Assert.Equal(3, cut.Instance.TableDraft.Columns);
            Assert.Contains("插入表格", cut.Markup);
        }

        [Fact]
        public async Task SubmitTablePanel_InvokesTableCommand()
        {
            var cut = Editor();

            cut.Instance.TableDraft.Rows = 2;
            cut.Instance.TableDraft.Columns = 2;
            cut.Instance.TableDraft.HeaderRow = true;

            await cut.InvokeAsync(() => cut.Instance.SubmitTablePanelAsync());

            var invocation = JSInterop.Invocations.First(i => i.Identifier == "elementHtmlEditorTableCommand");
            var payload = ElHtmlEditorGrowTableArgs.Parse((string)invocation.Arguments[2]);

            Assert.Equal(ElHtmlEditorGrowCommands.InsertTable, (string)invocation.Arguments[1]);
            Assert.Contains("<table", payload.Html);
            Assert.Contains("<thead>", payload.Html);
            Assert.Null(cut.Instance.ActivePanel);
        }

        [Fact]
        public void BuildInsertTableHtml_HonoursHeaderFormat()
        {
            var cut = Editor();

            var html = cut.Instance.BuildInsertTableHtml(TableArgs(a =>
            {
                a.Rows = 1;
                a.Columns = 2;
                a.HeaderRow = true;
                a.DefaultColumnHeader = "{0} 号列";
            }));

            Assert.Contains("1 号列", html);
            Assert.Contains("2 号列", html);
        }

        [Fact]
        public async Task ColorTool_OpensPalettePanel()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => FindToolButton(cut, "文字颜色").Click());

            Assert.Equal("color:color", cut.Instance.ActivePanel);
            Assert.Equal(50, cut.FindAll(".el-html-editor-grow__swatch").Count);
            Assert.False(string.IsNullOrWhiteSpace(cut.Instance.CustomColor));
        }

        [Fact]
        public async Task ApplyColor_InvokesForeColor()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => cut.Instance.ApplyColorAsync("color", "#f56c6c"));

            var invocation = JSInterop.Invocations.First(i => i.Identifier == "elementHtmlEditorExec");

            Assert.Equal(ElHtmlEditorGrowCommands.Color, (string)invocation.Arguments[1]);
            Assert.Equal("#f56c6c", (string)invocation.Arguments[2]);
        }

        [Fact]
        public async Task ApplyColor_ForHighlight_InvokesBackColor()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => cut.Instance.ApplyColorAsync("highlight", "#f7ba2a"));

            var invocation = JSInterop.Invocations.First(i => i.Identifier == "elementHtmlEditorExec");

            Assert.Equal(ElHtmlEditorGrowCommands.Background, (string)invocation.Arguments[1]);
        }

        [Fact]
        public async Task ApplyColor_WithEmptyColor_DoesNothing()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => cut.Instance.ApplyColorAsync("color", "   "));

            Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "elementHtmlEditorExec");
        }

        [Fact]
        public async Task ClickingSwatch_AppliesThatColor()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => FindToolButton(cut, "文字颜色").Click());
            await cut.InvokeAsync(() => cut.FindAll(".el-html-editor-grow__swatch").First().Click());

            var invocation = JSInterop.Invocations.First(i => i.Identifier == "elementHtmlEditorExec");

            Assert.Equal("#f56c6c", (string)invocation.Arguments[2]);
            Assert.Null(cut.Instance.ActivePanel);
        }

        [Fact]
        public async Task ClosePanel_ClearsActivePanel()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => FindToolButton(cut, "插入链接").Click());
            await cut.InvokeAsync(() => cut.Instance.ClosePanelAsync());

            Assert.Null(cut.Instance.ActivePanel);
            Assert.Empty(cut.FindAll(".el-html-editor-grow__panel"));
        }

        [Fact]
        public async Task SelectingFormatBlock_InvokesCommand()
        {
            var cut = Editor();

            await FindSelect(cut, "段落样式")
                .TriggerEventAsync("onchange", new ChangeEventArgs { Value = "h2" });

            var invocation = JSInterop.Invocations.First(i => i.Identifier == "elementHtmlEditorExec");

            Assert.Equal(ElHtmlEditorGrowCommands.FormatBlock, (string)invocation.Arguments[1]);
            Assert.Equal("h2", (string)invocation.Arguments[2]);
        }

        [Fact]
        public async Task SelectingFontName_UsesQuotedValue()
        {
            var cut = Editor();

            await FindSelect(cut, "字体")
                .TriggerEventAsync("onchange", new ChangeEventArgs { Value = "SimSun" });

            var invocation = JSInterop.Invocations.First(i => i.Identifier == "elementHtmlEditorExec");

            Assert.Equal(ElHtmlEditorGrowCommands.FontName, (string)invocation.Arguments[1]);
            Assert.Equal("SimSun", (string)invocation.Arguments[2]);
        }

        [Fact]
        public async Task SelectingFontSize_IsPassedThrough()
        {
            var cut = Editor();

            await FindSelect(cut, "字号")
                .TriggerEventAsync("onchange", new ChangeEventArgs { Value = "4" });

            var invocation = JSInterop.Invocations.First(i => i.Identifier == "elementHtmlEditorExec");

            Assert.Equal(ElHtmlEditorGrowCommands.FontSize, (string)invocation.Arguments[1]);
            Assert.Equal("4", (string)invocation.Arguments[2]);
        }

        [Fact]
        public async Task SelectingEmptyValue_IsIgnored()
        {
            var cut = Editor();

            await FindSelect(cut, "段落样式")
                .TriggerEventAsync("onchange", new ChangeEventArgs { Value = string.Empty });

            Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "elementHtmlEditorExec");
        }

        [Fact]
        public void GetSelectValue_StripsQuotesFromFontName()
        {
            var cut = Editor();

            cut.Instance.State.FontName = "\"Segoe UI\"";

            var tool = ElHtmlEditorGrowToolbar.Find("font-name");

            Assert.Equal("Segoe UI", cut.Instance.GetSelectValue(tool));
        }

        [Fact]
        public void GetSelectValue_NormalisesFormatBlock()
        {
            var cut = Editor();

            cut.Instance.State.FormatBlock = "<H3>";

            Assert.Equal("h3", cut.Instance.GetSelectValue(ElHtmlEditorGrowToolbar.Find("format-block")));
        }

        [Fact]
        public void GetColorValue_PrefersStoredValue()
        {
            var cut = Editor();
            var tool = ElHtmlEditorGrowToolbar.Find("color");

            Assert.Equal("#f56c6c", cut.Instance.GetColorValue(tool));
            Assert.Equal("#000000", cut.Instance.GetColorValue(null));
        }

        [Fact]
        public async Task PickFile_WithoutUploadUrl_DoesNothing()
        {
            var cut = Editor();

            await cut.InvokeAsync(() => cut.Instance.PickFileAsync());

            Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "elementHtmlEditorPickFile");
        }

        [Fact]
        public async Task PickFile_WithUploadUrl_InvokesJs()
        {
            var cut = Editor(p => p.Add(x => x.UploadUrl, "https://api.example.com/upload"));

            await cut.InvokeAsync(() => cut.Instance.PickFileAsync());

            Assert.Contains(JSInterop.Invocations, i => i.Identifier == "elementHtmlEditorPickFile");
        }

        #endregion

        #region 快捷键

        [Fact]
        public async Task CtrlB_RunsBoldCommand()
        {
            var cut = Editor();

            await cut.Find(".el-html-editor-grow__content")
                .TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "b", CtrlKey = true });

            var invocation = JSInterop.Invocations.First(i => i.Identifier == "elementHtmlEditorExec");

            Assert.Equal(ElHtmlEditorGrowCommands.Bold, (string)invocation.Arguments[1]);
        }

        [Fact]
        public async Task CtrlZ_RunsUndoCommand()
        {
            var cut = Editor();

            await cut.Find(".el-html-editor-grow__content")
                .TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "z", CtrlKey = true });

            var invocation = JSInterop.Invocations.First(i => i.Identifier == "elementHtmlEditorExec");

            Assert.Equal(ElHtmlEditorGrowCommands.Undo, (string)invocation.Arguments[1]);
        }

        [Fact]
        public async Task UnmappedKey_DoesNothing()
        {
            var cut = Editor();

            await cut.Find(".el-html-editor-grow__content")
                .TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "q", CtrlKey = true });

            Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "elementHtmlEditorExec");
        }

        [Fact]
        public async Task CustomShortcut_CanBeRegisteredAndRemoved()
        {
            var cut = Editor();
            var runs = 0;

            cut.Instance.RegisterShortcut("Ctrl+Shift+P", () =>
            {
                runs++;
                return Task.CompletedTask;
            });

            var key = new KeyboardEventArgs { Key = "P", CtrlKey = true, ShiftKey = true };

            await cut.Find(".el-html-editor-grow__content").TriggerEventAsync("onkeydown", key);

            Assert.Equal(1, runs);

            cut.Instance.UnregisterShortcut("Ctrl+Shift+P");

            await cut.Find(".el-html-editor-grow__content").TriggerEventAsync("onkeydown", key);

            Assert.Equal(1, runs);
        }

        [Fact]
        public void RegisterShortcut_RejectsEmptyKey()
        {
            var cut = Editor();

            cut.Instance.RegisterShortcut(null, () => Task.CompletedTask);
            cut.Instance.RegisterShortcut(string.Empty, () => Task.CompletedTask);

            // 无异常即通过；空键不会写入快捷键表。
            Assert.NotNull(cut.Instance);
        }

        [Fact]
        public async Task Shortcut_IsIgnoredWhenNotEditable()
        {
            var cut = Editor(p => p.Add(x => x.ReadOnly, true));

            await cut.Find(".el-html-editor-grow__content")
                .TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "b", CtrlKey = true });

            Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "elementHtmlEditorExec");
        }

        #endregion

        #region 命令映射

        [Fact]
        public void ToExecCommand_MapsInsertHtmlToNativeCasing()
        {
            Assert.Equal("insertHTML", ElHtmlEditorGrowCommands.ToExecCommand(ElHtmlEditorGrowCommands.InsertHtml));
            Assert.Equal("insertHorizontalRule", ElHtmlEditorGrowCommands.ToExecCommand(ElHtmlEditorGrowCommands.HorizontalRule));
        }

        [Fact]
        public void ToExecCommand_PassesThroughNativeCommands()
        {
            Assert.Equal("bold", ElHtmlEditorGrowCommands.ToExecCommand(ElHtmlEditorGrowCommands.Bold));
            Assert.Equal("foreColor", ElHtmlEditorGrowCommands.ToExecCommand(ElHtmlEditorGrowCommands.Color));
        }

        [Fact]
        public void ToExecCommand_ReturnsNullForTableCommands()
        {
            Assert.Null(ElHtmlEditorGrowCommands.ToExecCommand(ElHtmlEditorGrowCommands.InsertTable));
            Assert.Null(ElHtmlEditorGrowCommands.ToExecCommand(ElHtmlEditorGrowCommands.DeleteTable));
            Assert.Null(ElHtmlEditorGrowCommands.ToExecCommand(null));
        }

        [Fact]
        public void IsTableCommand_ClassifiesTableOnly()
        {
            Assert.True(ElHtmlEditorGrowCommands.IsTableCommand(ElHtmlEditorGrowCommands.AddRowAfter));
            Assert.True(ElHtmlEditorGrowCommands.IsTableCommand(ElHtmlEditorGrowCommands.MergeCellRight));
            Assert.True(ElHtmlEditorGrowCommands.IsTableCommand(ElHtmlEditorGrowCommands.SplitCell));
            Assert.False(ElHtmlEditorGrowCommands.IsTableCommand(ElHtmlEditorGrowCommands.Bold));
            Assert.False(ElHtmlEditorGrowCommands.IsTableCommand(null));
        }

        [Fact]
        public void RequiresValue_OnlyForParameterisedCommands()
        {
            Assert.True(ElHtmlEditorGrowCommands.RequiresValue(ElHtmlEditorGrowCommands.InsertHtml));
            Assert.True(ElHtmlEditorGrowCommands.RequiresValue(ElHtmlEditorGrowCommands.Color));
            Assert.True(ElHtmlEditorGrowCommands.RequiresValue(ElHtmlEditorGrowCommands.Background));
            Assert.True(ElHtmlEditorGrowCommands.RequiresValue(ElHtmlEditorGrowCommands.FontName));
            Assert.True(ElHtmlEditorGrowCommands.RequiresValue(ElHtmlEditorGrowCommands.FontSize));
            Assert.True(ElHtmlEditorGrowCommands.RequiresValue(ElHtmlEditorGrowCommands.FormatBlock));
            Assert.False(ElHtmlEditorGrowCommands.RequiresValue(ElHtmlEditorGrowCommands.Bold));
            Assert.False(ElHtmlEditorGrowCommands.RequiresValue(null));
        }

        [Fact]
        public void IsToggle_CoversFormattingCommands()
        {
            Assert.True(ElHtmlEditorGrowCommands.IsToggle("bold"));
            Assert.True(ElHtmlEditorGrowCommands.IsToggle("StrikeThrough"));
            Assert.True(ElHtmlEditorGrowCommands.IsToggle("justifyCenter"));
            Assert.False(ElHtmlEditorGrowCommands.IsToggle(ElHtmlEditorGrowCommands.RemoveFormat));
            Assert.False(ElHtmlEditorGrowCommands.IsToggle(null));
        }

        [Fact]
        public void IsActive_ReadsMatchingStateFields()
        {
            var state = new ElHtmlEditorGrowState
            {
                Bold = true,
                Italic = true,
                Underline = true,
                StrikeThrough = true,
                Subscript = true,
                Superscript = true,
                JustifyLeft = true,
                JustifyCenter = true,
                JustifyRight = true,
                JustifyFull = true,
                Unlink = true
            };

            Assert.True(ElHtmlEditorGrowCommands.IsActive(state, "bold"));
            Assert.True(ElHtmlEditorGrowCommands.IsActive(state, "italic"));
            Assert.True(ElHtmlEditorGrowCommands.IsActive(state, "underline"));
            Assert.True(ElHtmlEditorGrowCommands.IsActive(state, "strikeThrough"));
            Assert.True(ElHtmlEditorGrowCommands.IsActive(state, "subscript"));
            Assert.True(ElHtmlEditorGrowCommands.IsActive(state, "superscript"));
            Assert.True(ElHtmlEditorGrowCommands.IsActive(state, "justifyLeft"));
            Assert.True(ElHtmlEditorGrowCommands.IsActive(state, "justifyCenter"));
            Assert.True(ElHtmlEditorGrowCommands.IsActive(state, "justifyRight"));
            Assert.True(ElHtmlEditorGrowCommands.IsActive(state, "justifyFull"));
            Assert.True(ElHtmlEditorGrowCommands.IsActive(state, "unlink"));
            Assert.False(ElHtmlEditorGrowCommands.IsActive(state, "removeFormat"));
            Assert.False(ElHtmlEditorGrowCommands.IsActive(null, "bold"));
        }

        [Fact]
        public void NormalizeBlockFormat_StripsBracketsAndCase()
        {
            Assert.Equal("h1", ElHtmlEditorGrow.NormalizeBlockFormat("<H1>"));
            Assert.Equal("p", ElHtmlEditorGrow.NormalizeBlockFormat("P"));
            Assert.Equal(string.Empty, ElHtmlEditorGrow.NormalizeBlockFormat(null));
            Assert.Equal(string.Empty, ElHtmlEditorGrow.NormalizeBlockFormat("   "));
        }

        #endregion

        #region 状态 JSON

        [Fact]
        public void State_Parse_NullOrInvalidFallsBackToDefault()
        {
            Assert.True(State(null).Success);
            Assert.True(State(string.Empty).Success);
            Assert.True(State("not-json").Success);
            Assert.True(State("{").Success);
            Assert.True(State(null).IsDefault);
        }

        [Fact]
        public void State_Parse_ReadsPascalCaseJson()
        {
            var state = State("{\"Html\":\"<b>x</b>\",\"Bold\":true,\"InTable\":true,\"FormatBlock\":\"h2\",\"Success\":false,\"Message\":\"boom\"}");

            Assert.Equal("<b>x</b>", state.Html);
            Assert.True(state.Bold);
            Assert.True(state.InTable);
            Assert.Equal("h2", state.FormatBlock);
            Assert.False(state.Success);
            Assert.Equal("boom", state.Message);
        }

        [Fact]
        public void State_Parse_ReadsCamelCaseJson()
        {
            var state = State("{\"html\":\"<i>y</i>\",\"italic\":true,\"fontSize\":\"4\"}");

            Assert.Equal("<i>y</i>", state.Html);
            Assert.True(state.Italic);
            Assert.Equal("4", state.FontSize);
        }

        [Fact]
        public void State_Parse_ReadsJsonBooleans()
        {
            // System.Text.Json 不会把数字 1/0 读成 bool（AllowReadingFromString 只影响字符串→数字），
            // 因此这里只约束真正的 JSON 布尔值。
            var state = State("{\"Bold\":true,\"Underline\":false}");

            Assert.True(state.Bold);
            Assert.False(state.Underline);
        }

        [Fact]
        public void State_Parse_ToleratesUnknownProperties()
        {
            var state = State("{\"Html\":\"x\",\"SomethingNew\":123,\"Nested\":{\"a\":1}}");

            Assert.Equal("x", state.Html);
            Assert.True(state.Success);
        }

        [Fact]
        public void State_ToJson_RoundTrips()
        {
            var source = new ElHtmlEditorGrowState { Html = "<p>a</p>", Underline = true, FontName = "SimSun" };

            var parsed = State(source.ToJson());

            Assert.Equal("<p>a</p>", parsed.Html);
            Assert.True(parsed.Underline);
            Assert.Equal("SimSun", parsed.FontName);
        }

        [Fact]
        public void State_IsDefault_DetectsPristineState()
        {
            Assert.True(new ElHtmlEditorGrowState().IsDefault);
            Assert.False(new ElHtmlEditorGrowState { Bold = true }.IsDefault);
            Assert.False(new ElHtmlEditorGrowState { Html = "x" }.IsDefault);
        }

        #endregion

        #region 表格选区与参数

        [Fact]
        public void TableSelection_Parse_NullGivesOutsideTable()
        {
            var selection = ElHtmlEditorGrowTableSelection.Parse(null);

            Assert.False(selection.InTable);
            Assert.Equal(-1, selection.RowIndex);
            Assert.Equal(-1, selection.ColumnIndex);
            Assert.Equal(1, selection.ColSpan);
            Assert.Equal(1, selection.RowSpan);
        }

        [Fact]
        public void TableSelection_Parse_ReadsBrowserPayload()
        {
            var selection = ElHtmlEditorGrowTableSelection.Parse(
                "{\"InTable\":true,\"InHeader\":true,\"RowIndex\":1,\"ColumnIndex\":2,\"Rows\":3,\"Columns\":4," +
                "\"ColSpan\":2,\"RowSpan\":1,\"CanMergeRight\":true,\"CanMergeDown\":false,\"CanSplit\":true," +
                "\"CellBackground\":\"#ffffff\",\"CellTextAlign\":\"center\",\"BorderWidthPx\":1}");

            Assert.True(selection.InTable);
            Assert.True(selection.InHeader);
            Assert.Equal(1, selection.RowIndex);
            Assert.Equal(2, selection.ColumnIndex);
            Assert.Equal(3, selection.Rows);
            Assert.Equal(4, selection.Columns);
            Assert.True(selection.CanMergeRight);
            Assert.False(selection.CanMergeDown);
            Assert.True(selection.CanSplit);
            Assert.Equal("#ffffff", selection.CellBackground);
            Assert.Equal("center", selection.CellTextAlign);
            Assert.Equal(1, selection.BorderWidthPx);
        }

        [Fact]
        public void TableSelection_CanExecute_BlocksOutsideTable()
        {
            var selection = new ElHtmlEditorGrowTableSelection();

            Assert.False(selection.CanExecute(ElHtmlEditorGrowCommands.AddRowAfter));
            Assert.False(selection.CanExecute(ElHtmlEditorGrowCommands.DeleteTable));
        }

        [Fact]
        public void TableSelection_CanExecute_FollowsMergeFlags()
        {
            var selection = new ElHtmlEditorGrowTableSelection
            {
                InTable = true,
                CanMergeRight = false,
                CanMergeDown = true,
                CanSplit = false
            };

            Assert.False(selection.CanExecute(ElHtmlEditorGrowCommands.MergeCellRight));
            Assert.True(selection.CanExecute(ElHtmlEditorGrowCommands.MergeCellDown));
            Assert.False(selection.CanExecute(ElHtmlEditorGrowCommands.SplitCell));
            Assert.True(selection.CanExecute(ElHtmlEditorGrowCommands.CopyCells));
            Assert.True(selection.CanExecute(ElHtmlEditorGrowCommands.PasteCells));
            Assert.True(selection.CanExecute(ElHtmlEditorGrowCommands.AddColumnBefore));
        }

        [Fact]
        public void TableArgs_Parse_DefaultsBorderToOne()
        {
            var args = ElHtmlEditorGrowTableArgs.Parse(null);

            Assert.Equal(1, args.Border);
            Assert.True(args.BorderTop);
            Assert.True(args.BorderLeft);
            Assert.Equal(-1, args.RowIndex);
        }

        [Fact]
        public void TableArgs_ToJson_RoundTrips()
        {
            var args = TableArgs(a =>
            {
                a.Rows = 3;
                a.Columns = 4;
                a.HeaderRow = true;
                a.Width = "80%";
                a.CellBackground = "#f5f7fa";
                a.CellTextAlign = "center";
                a.BorderStyle = "dashed";
            });

            var parsed = ElHtmlEditorGrowTableArgs.Parse(args.ToJson());

            Assert.Equal(3, parsed.Rows);
            Assert.Equal(4, parsed.Columns);
            Assert.True(parsed.HeaderRow);
            Assert.Equal("80%", parsed.Width);
            Assert.Equal("#f5f7fa", parsed.CellBackground);
            Assert.Equal("dashed", parsed.BorderStyle);
        }

        [Fact]
        public void TableArgs_Parse_InvalidJsonFallsBack()
        {
            var args = ElHtmlEditorGrowTableArgs.Parse("{oops");

            Assert.Equal(1, args.Border);
            Assert.Equal(0, args.Rows);
        }

        [Fact]
        public void TableStrings_Resolve_TranslatesKnownCodes()
        {
            var strings = new ElHtmlEditorGrowTableStrings();

            Assert.Equal("请先把光标放入表格内", strings.Resolve("table.action.requiresTable"));
            Assert.NotEqual("table.copy.invalidSelection", strings.Resolve("table.copy.invalidSelection"));
            Assert.Equal("custom", strings.Resolve("custom"));
            Assert.Null(strings.Resolve(null));
        }

        [Fact]
        public void TableStrings_Resolve_HonoursOverrides()
        {
            var strings = new ElHtmlEditorGrowTableStrings { ActionRequiresTable = "先选中表格" };

            Assert.Equal("先选中表格", strings.Resolve("table.action.requiresTable"));
        }

        #endregion

        #region HTML 构建器

        [Theory]
        [InlineData("100", "100px")]
        [InlineData("100%", "100%")]
        [InlineData("12em", "12em")]
        [InlineData("1.5rem", "1.5rem")]
        [InlineData("50vw", "50vw")]
        [InlineData("  ", null)]
        [InlineData(null, null)]
        [InlineData("auto", "auto")]
        public void NormalizeLength_Behaves(string input, string expected)
        {
            Assert.Equal(expected, ElHtmlEditorGrowHtmlBuilder.NormalizeLength(input));
        }

        [Fact]
        public void ResolveBorder_UsesDefaults()
        {
            Assert.Equal("1px solid #dcdfe6", ElHtmlEditorGrowHtmlBuilder.ResolveBorder(new ElHtmlEditorGrowTableArgs()));
        }

        [Fact]
        public void ResolveBorder_HonoursExplicitValues()
        {
            var value = ElHtmlEditorGrowHtmlBuilder.ResolveBorder(new ElHtmlEditorGrowTableArgs
            {
                Border = 2,
                BorderStyle = "dashed",
                BorderColor = "#000000"
            });

            Assert.Equal("2px dashed #000000", value);
        }

        [Fact]
        public void ResolveBorder_PrefersPixelWidth()
        {
            var value = ElHtmlEditorGrowHtmlBuilder.ResolveBorder(new ElHtmlEditorGrowTableArgs
            {
                Border = 1,
                BorderWidthPx = 5
            });

            Assert.StartsWith("5px ", value, StringComparison.Ordinal);
        }

        [Fact]
        public void ResolveCellPadding_DefaultsAndOverrides()
        {
            Assert.Equal("4px 8px", ElHtmlEditorGrowHtmlBuilder.ResolveCellPadding(new ElHtmlEditorGrowTableArgs()));
            Assert.Equal("6px", ElHtmlEditorGrowHtmlBuilder.ResolveCellPadding(new ElHtmlEditorGrowTableArgs { CellPaddingPx = 6 }));
            Assert.Equal("0 4px", ElHtmlEditorGrowHtmlBuilder.ResolveCellPadding(new ElHtmlEditorGrowTableArgs { CellPadding = "0 4px" }));
        }

        [Fact]
        public void BuildCellStyle_HeaderGetsBackgroundAndAlign()
        {
            var header = ElHtmlEditorGrowHtmlBuilder.BuildCellStyle(new ElHtmlEditorGrowTableArgs(), true);
            var body = ElHtmlEditorGrowHtmlBuilder.BuildCellStyle(new ElHtmlEditorGrowTableArgs(), false);

            Assert.Contains("background:#f5f7fa", header);
            Assert.Contains("text-align:left", header);
            Assert.DoesNotContain("background:", body);
        }

        [Fact]
        public void BuildCellStyle_IncludesVerticalAlignWhenSet()
        {
            var style = ElHtmlEditorGrowHtmlBuilder.BuildCellStyle(
                new ElHtmlEditorGrowTableArgs { CellVerticalAlign = "middle" }, false);

            Assert.Contains("vertical-align:middle", style);
        }

        [Fact]
        public void BuildTable_ProducesWellFormedMarkup()
        {
            var html = ElHtmlEditorGrowHtmlBuilder.BuildTable(TableArgs(a =>
            {
                a.Rows = 2;
                a.Columns = 3;
                a.HeaderRow = true;
            }));

            Assert.StartsWith("<table ", html, StringComparison.Ordinal);
            Assert.EndsWith("</table><p><br></p>", html, StringComparison.Ordinal);
            Assert.Contains("<thead><tr>", html);
            Assert.Contains("<th ", html);
            Assert.Equal(2, Regex.Matches(html, "<tr>").Count - 1);
            Assert.Equal(6, Regex.Matches(html, "<td ").Count);
            Assert.Equal(3, Regex.Matches(html, "<th ").Count);
            Assert.Contains("列 1", html);
            Assert.Contains("列 3", html);
        }

        [Fact]
        public void BuildTable_OmitsHeaderRowWhenDisabled()
        {
            var html = ElHtmlEditorGrowHtmlBuilder.BuildTable(TableArgs(a =>
            {
                a.Rows = 1;
                a.Columns = 2;
                a.HeaderRow = false;
            }));

            Assert.DoesNotContain("<thead", html);
            Assert.DoesNotContain("<th ", html);
        }

        [Fact]
        public void BuildTable_FallsBackToTwoByTwo()
        {
            var html = ElHtmlEditorGrowHtmlBuilder.BuildTable(new ElHtmlEditorGrowTableArgs());

            Assert.Equal(4, Regex.Matches(html, "<td ").Count);
            Assert.Contains("width:100%", html);
        }

        [Fact]
        public void BuildTable_CapsOversizedRequests()
        {
            var html = ElHtmlEditorGrowHtmlBuilder.BuildTable(TableArgs(a =>
            {
                a.Rows = 500;
                a.Columns = 200;
            }));

            Assert.Equal(100 * 50, Regex.Matches(html, "<td ").Count);
        }

        [Fact]
        public void BuildTable_UsesTableStringsFormat()
        {
            var html = ElHtmlEditorGrowHtmlBuilder.BuildTable(
                TableArgs(a =>
                {
                    a.Rows = 1;
                    a.Columns = 1;
                    a.HeaderRow = true;
                }),
                new ElHtmlEditorGrowTableStrings { DefaultColumnHeader = "第{0}列" });

            Assert.Contains("第1列", html);
        }

        [Fact]
        public void BuildTable_ExposesWidthBorderAndBackground()
        {
            var html = ElHtmlEditorGrowHtmlBuilder.BuildTable(TableArgs(a =>
            {
                a.Rows = 1;
                a.Columns = 1;
                a.BorderWidthPx = 2;
                a.BorderColor = "#ff0000";
                a.HeaderRow = true;
                a.CellBackground = "#eeeeee";
                a.Width = "600";
            }));

            Assert.Contains("width:600px", html);
            Assert.Contains("border:2px solid #ff0000", html);
            Assert.Contains("background:#eeeeee", html);
        }

        [Fact]
        public void BuildTable_HandlesNullArgs()
        {
            var html = ElHtmlEditorGrowHtmlBuilder.BuildTable(null);

            Assert.Contains("<table", html);
            Assert.Equal(4, Regex.Matches(html, "<td ").Count);
        }

        [Fact]
        public void BuildImage_RejectsEmptyAndDangerousSources()
        {
            Assert.Null(ElHtmlEditorGrowHtmlBuilder.BuildImage(null));
            Assert.Null(ElHtmlEditorGrowHtmlBuilder.BuildImage("   "));
            Assert.Null(ElHtmlEditorGrowHtmlBuilder.BuildImage("javascript:alert(1)"));
            Assert.Null(ElHtmlEditorGrowHtmlBuilder.BuildImage("vbscript:msgbox(1)"));
        }

        [Fact]
        public void BuildImage_BuildsTagWithAltAndSize()
        {
            var html = ElHtmlEditorGrowHtmlBuilder.BuildImage("https://x.example/a.png", "影像", "100", "50");

            Assert.Contains("src=\"https://x.example/a.png\"", html);
            Assert.Contains("alt=\"影像\"", html);
            Assert.Contains("width=\"100px\"", html);
            Assert.Contains("height=\"50px\"", html);
        }

        [Fact]
        public void BuildImage_EscapesQuotedAlt()
        {
            var html = ElHtmlEditorGrowHtmlBuilder.BuildImage("https://x.example/a.png", "a\"b");

            Assert.Contains("alt=\"a&quot;b\"", html);
        }

        [Fact]
        public void BuildLink_RejectsEmptyAndDangerousHref()
        {
            Assert.Null(ElHtmlEditorGrowHtmlBuilder.BuildLink(null, "x"));
            Assert.Null(ElHtmlEditorGrowHtmlBuilder.BuildLink("javascript:alert(1)", "x"));
            Assert.Null(ElHtmlEditorGrowHtmlBuilder.BuildLink("data:text/html;base64,AAA", "x"));
        }

        [Fact]
        public void BuildLink_UsesHrefAsTextWhenTextMissing()
        {
            var html = ElHtmlEditorGrowHtmlBuilder.BuildLink("https://a.example/x", null);

            Assert.Equal("<a href=\"https://a.example/x\">https://a.example/x</a>", html);
        }

        [Fact]
        public void BuildLink_AddsRelForNewWindow()
        {
            var html = ElHtmlEditorGrowHtmlBuilder.BuildLink("https://a.example/x", "打开", true);

            Assert.Contains("target=\"_blank\"", html);
            Assert.Contains("rel=\"noopener noreferrer\"", html);
        }

        [Fact]
        public void BuildLink_WithoutNewWindow_HasNoTarget()
        {
            var html = ElHtmlEditorGrowHtmlBuilder.BuildLink("https://a.example/x", "打开", false);

            Assert.DoesNotContain("target=", html);
        }

        [Fact]
        public void BuildLink_EscapesDisplayText()
        {
            var html = ElHtmlEditorGrowHtmlBuilder.BuildLink("https://a.example/x", "<b>粗</b>");

            Assert.Contains("&lt;b&gt;粗&lt;/b&gt;", html);
        }

        [Fact]
        public void BuildHorizontalRule_IsEmptyElement()
        {
            Assert.Equal("<hr>", ElHtmlEditorGrowHtmlBuilder.BuildHorizontalRule());
        }

        [Fact]
        public void EscapeAttribute_CoversAllFiveEntities()
        {
            Assert.Equal("&amp;&lt;&gt;&quot;&#39;", ElHtmlEditorGrowHtmlBuilder.EscapeAttribute("&<>\"'"));
            Assert.Equal(string.Empty, ElHtmlEditorGrowHtmlBuilder.EscapeAttribute(null));
        }

        [Fact]
        public void EscapeText_LeavesQuotesIntact()
        {
            Assert.Equal("&amp;&lt;&gt;\"'", ElHtmlEditorGrowHtmlBuilder.EscapeText("&<>\"'"));
            Assert.Equal(string.Empty, ElHtmlEditorGrowHtmlBuilder.EscapeText(null));
        }

        #endregion

        #region 粘贴消毒

        [Fact]
        public void Sanitize_RemovesDangerousContainersWithContent()
        {
            var html = ElHtmlEditorGrowSanitizer.Sanitize("<p>a</p><script>alert(1)</script><p>b</p>");

            Assert.DoesNotContain("script", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("alert", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<p>a</p>", html);
            Assert.Contains("<p>b</p>", html);
        }

        [Fact]
        public void Sanitize_RemovesIframeAndObjectAndStyle()
        {
            Assert.DoesNotContain("iframe", ElHtmlEditorGrowSanitizer.Sanitize("<iframe src=\"http://x\"></iframe>"), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("object", ElHtmlEditorGrowSanitizer.Sanitize("<object data=\"x\"></object>"), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("style", ElHtmlEditorGrowSanitizer.Sanitize("<style>p{color:red}</style>"), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Sanitize_StripsEventHandlers()
        {
            var html = ElHtmlEditorGrowSanitizer.Sanitize("<p onclick=\"evil()\" onmouseover=\"evil()\">x</p>");

            Assert.DoesNotContain("onclick", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onmouseover", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("x", html);
        }

        [Fact]
        public void Sanitize_BlocksDangerousUrls()
        {
            var html = ElHtmlEditorGrowSanitizer.Sanitize("<a href=\"javascript:alert(1)\">x</a>");

            Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("x", html);
        }

        [Fact]
        public void Sanitize_BlocksDangerousStyleExpressions()
        {
            var html = ElHtmlEditorGrowSanitizer.Sanitize("<div style=\"width:expression(alert(1))\">x</div>");

            Assert.DoesNotContain("expression", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Sanitize_KeepsTableStructure()
        {
            var html = ElHtmlEditorGrowSanitizer.Sanitize("<table><thead><tr><th>h</th></tr></thead><tbody><tr><td>1</td></tr></tbody></table>");

            Assert.Contains("<table>", html);
            Assert.Contains("<th>", html);
            Assert.Contains("<td>", html);
        }

        [Fact]
        public void Sanitize_DropsUnknownTagsButKeepsText()
        {
            var html = ElHtmlEditorGrowSanitizer.Sanitize("<custom>内容</custom>");

            Assert.DoesNotContain("custom", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("内容", html);
        }

        [Fact]
        public void Sanitize_KeepsInlineFormatting()
        {
            var html = ElHtmlEditorGrowSanitizer.Sanitize("<p><b>粗</b><i>斜</i><u>下划线</u></p>");

            Assert.Contains("<b>粗</b>", html);
            Assert.Contains("<i>斜</i>", html);
            Assert.Contains("<u>下划线</u>", html);
        }

        [Fact]
        public void Sanitize_HonoursTagWhitelistOverride()
        {
            var html = ElHtmlEditorGrowSanitizer.Sanitize(
                "<p>a</p><b>b</b>",
                new ElHtmlEditorGrowSanitizeOptions { AllowedTags = new[] { "p" } });

            Assert.Contains("<p>", html);
            Assert.DoesNotContain("<b>", html);
            Assert.Contains("b", html);
        }

        [Fact]
        public void Sanitize_CanDisableImagesAndLinks()
        {
            var options = new ElHtmlEditorGrowSanitizeOptions { AllowImages = false, AllowLinks = false };

            var html = ElHtmlEditorGrowSanitizer.Sanitize("<img src=\"https://x/a.png\"><a href=\"https://x\">t</a>", options);

            Assert.DoesNotContain("<img", html);
            Assert.DoesNotContain("<a ", html);
        }

        [Fact]
        public void Sanitize_KeepsOrDropsDataAttributes()
        {
            var kept = ElHtmlEditorGrowSanitizer.Sanitize("<p data-code=\"A01\">x</p>");
            var dropped = ElHtmlEditorGrowSanitizer.Sanitize(
                "<p data-code=\"A01\">x</p>",
                new ElHtmlEditorGrowSanitizeOptions { AllowDataAttributes = false });

            Assert.Contains("data-code", kept);
            Assert.DoesNotContain("data-code", dropped);
        }

        [Fact]
        public void Sanitize_KeepsSafeStyleAndDropsUnsafe()
        {
            var html = ElHtmlEditorGrowSanitizer.Sanitize("<p style=\"color:#f00\">x</p>");

            Assert.Contains("style=", html);
            Assert.Contains("color:#f00", html);
        }

        [Fact]
        public void Sanitize_StripsComments()
        {
            var html = ElHtmlEditorGrowSanitizer.Sanitize("<p>a</p><!-- 注释 --><p>b</p>");

            Assert.DoesNotContain("注释", html);
            Assert.Contains("<p>a</p>", html);
        }

        [Fact]
        public void Sanitize_StripFormatting_ReturnsPlainText()
        {
            var html = ElHtmlEditorGrowSanitizer.Sanitize(
                "<p>第一行</p><p>第二行</p>",
                new ElHtmlEditorGrowSanitizeOptions { StripFormatting = true });

            Assert.DoesNotContain("<p>", html);
            Assert.Contains("第一行", html);
            Assert.Contains("第二行", html);
        }

        [Fact]
        public void Sanitize_EmptyInputGivesEmptyOutput()
        {
            Assert.Equal(string.Empty, ElHtmlEditorGrowSanitizer.Sanitize(null));
            Assert.Equal(string.Empty, ElHtmlEditorGrowSanitizer.Sanitize(string.Empty));
        }

        [Fact]
        public void Sanitize_NormalisesClosingTags()
        {
            Assert.Contains("</p>", ElHtmlEditorGrowSanitizer.Sanitize("<p>a</p>"));
        }

        [Fact]
        public void Sanitize_DropsVoidClosingTags()
        {
            var html = ElHtmlEditorGrowSanitizer.Sanitize("<p>a<br></br>b</p>");

            Assert.DoesNotContain("</br>", html);
            Assert.Contains("<br>", html);
        }

        [Fact]
        public void ToPlainText_ConvertsBlocksToNewlines()
        {
            var text = ElHtmlEditorGrowSanitizer.ToPlainText("<p>a</p><p>b</p>");

            Assert.Contains("\n", text);
            Assert.Contains("a", text);
            Assert.Contains("b", text);
        }

        [Fact]
        public void ToPlainText_EmptyInput()
        {
            Assert.Equal(string.Empty, ElHtmlEditorGrowSanitizer.ToPlainText(null));
            Assert.Equal(string.Empty, ElHtmlEditorGrowSanitizer.ToPlainText(string.Empty));
        }

        [Fact]
        public void IsSafeUrl_AllowsCommonProtocolsAndBlocksScript()
        {
            Assert.True(ElHtmlEditorGrowSanitizer.IsSafeUrl("https://a.example"));
            Assert.True(ElHtmlEditorGrowSanitizer.IsSafeUrl("http://a.example"));
            Assert.True(ElHtmlEditorGrowSanitizer.IsSafeUrl("/relative/path"));
            Assert.True(ElHtmlEditorGrowSanitizer.IsSafeUrl("mailto:a@b.c"));
            Assert.False(ElHtmlEditorGrowSanitizer.IsSafeUrl("javascript:alert(1)"));
            Assert.False(ElHtmlEditorGrowSanitizer.IsSafeUrl("data:text/html;base64,AA"));
        }

        [Fact]
        public void IsDangerousUrlAndStyle_DetectScripting()
        {
            Assert.True(ElHtmlEditorGrowSanitizer.IsDangerousUrl("JavaScript:alert(1)"));
            Assert.False(ElHtmlEditorGrowSanitizer.IsDangerousUrl("https://a.example"));
            Assert.True(ElHtmlEditorGrowSanitizer.IsDangerousStyle("width:expression(alert(1))"));
            Assert.False(ElHtmlEditorGrowSanitizer.IsDangerousStyle("color:#f00"));
        }

        [Fact]
        public void DecodeEntities_HandlesNamedAndNumeric()
        {
            Assert.Equal("&", ElHtmlEditorGrowSanitizer.DecodeEntities("&amp;"));
            Assert.Equal("<", ElHtmlEditorGrowSanitizer.DecodeEntities("&lt;"));
            Assert.Equal("A", ElHtmlEditorGrowSanitizer.DecodeEntities("&#65;"));
        }

        [Fact]
        public void Sanitizer_DefaultWhitelists_AreAlignedWithMarkdown()
        {
            Assert.Contains("table", ElHtmlEditorGrowSanitizer.DefaultAllowedTags);
            Assert.Contains("href", ElHtmlEditorGrowSanitizer.DefaultAllowedAttributes);
            Assert.Contains("src", ElHtmlEditorGrowSanitizer.UriAttributes);
            Assert.Contains("br", ElHtmlEditorGrowSanitizer.VoidElements);
        }

        [Fact]
        public async Task PasteFilter_SanitizesIncomingHtml()
        {
            var cut = Editor();

            var filtered = await cut.InvokeAsync(() => cut.Instance.OnPasteFilterAsync("<script>bad()</script><p>好</p>"));

            Assert.DoesNotContain("script", filtered, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<p>好</p>", filtered);
        }

        [Fact]
        public async Task PasteFilter_CanBeDisabled()
        {
            var cut = Editor(p => p.Add(x => x.SanitizePaste, false));

            var filtered = await cut.InvokeAsync(() => cut.Instance.OnPasteFilterAsync("<em>raw</em>"));

            Assert.Equal("<em>raw</em>", filtered);
        }

        [Fact]
        public async Task Paste_RaisesPasteEvent()
        {
            ElHtmlEditorGrowPasteArgs args = null;

            var cut = Editor(p => p.Add(x => x.Paste, a => args = a));

            await cut.InvokeAsync(() => cut.Instance.OnPasteFilterAsync("<p>x</p>"));

            Assert.NotNull(args);
            Assert.Equal("<p>x</p>", args.Html);
        }

        [Fact]
        public async Task Paste_EventCanRewriteContent()
        {
            var cut = Editor(p => p.Add(x => x.Paste, a => a.Html = "<p>被改写</p>"));

            var filtered = await cut.InvokeAsync(() => cut.Instance.OnPasteFilterAsync("<p>x</p>"));

            Assert.Equal("<p>被改写</p>", filtered);
        }

        [Fact]
        public async Task PasteFilter_NullInputGivesEmpty()
        {
            var cut = Editor();

            Assert.Equal(string.Empty, await cut.InvokeAsync(() => cut.Instance.OnPasteFilterAsync(null)));
        }

        #endregion

        #region 上传

        [Fact]
        public void ParseUploadPayload_ReadsJsonUrl()
        {
            var args = ElHtmlEditorGrow.ParseUploadPayload("{\"url\":\"https://cdn.example/a.png\"}");

            Assert.True(args.Success);
            Assert.Equal("https://cdn.example/a.png", args.Url);
        }

        [Fact]
        public void ParseUploadPayload_ReadsFailureJson()
        {
            var args = ElHtmlEditorGrow.ParseUploadPayload("{\"success\":false,\"error\":\"too large\"}");

            Assert.False(args.Success);
            Assert.Equal("too large", args.Error);
        }

        [Fact]
        public void ParseUploadPayload_AcceptsPlainUrlText()
        {
            var args = ElHtmlEditorGrow.ParseUploadPayload("\"https://cdn.example/b.png\"");

            Assert.True(args.Success);
            Assert.Equal("https://cdn.example/b.png", args.Url);
        }

        [Fact]
        public void ParseUploadPayload_KeepsRawResponse()
        {
            var args = ElHtmlEditorGrow.ParseUploadPayload("{\"url\":\"https://x/a.png\"}");

            Assert.Equal("{\"url\":\"https://x/a.png\"}", args.RawResponse);
        }

        [Fact]
        public void ParseUploadPayload_RejectsEmptyAndUnsafe()
        {
            Assert.False(ElHtmlEditorGrow.ParseUploadPayload(null).Success);
            Assert.False(ElHtmlEditorGrow.ParseUploadPayload("javascript:alert(1)").Success);
        }

        [Fact]
        public async Task UploadCompleted_InsertsImageAndRaisesEvent()
        {
            ElHtmlEditorGrowUploadArgs completed = null;

            var cut = Editor(p => p.Add(x => x.UploadComplete, a => completed = a));

            cut.Instance.ImageAlt = "上传图";

            await cut.InvokeAsync(() => cut.Instance.OnUploadCompletedAsync("{\"url\":\"https://cdn.example/c.png\"}"));

            var markup = (string)JSInterop.Invocations
                .First(i => i.Identifier == "elementHtmlEditorExec").Arguments[2];

            Assert.Contains("https://cdn.example/c.png", markup);
            Assert.Contains("alt=\"上传图\"", markup);
            Assert.NotNull(completed);
            Assert.True(completed.Success);
        }

        [Fact]
        public async Task UploadFailed_RaisesUploadErrorOnly()
        {
            ElHtmlEditorGrowUploadArgs failed = null;
            var completed = 0;

            var cut = Editor(p => p
                .Add(x => x.UploadError, a => failed = a)
                .Add(x => x.UploadComplete, _ => completed++));

            await cut.InvokeAsync(() => cut.Instance.OnUploadCompletedAsync("{\"success\":false,\"error\":\"boom\"}"));

            Assert.Equal(0, completed);
            Assert.NotNull(failed);
            Assert.Equal("boom", failed.Error);
            Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "elementHtmlEditorExec");
        }

        #endregion

        #region 图标

        [Fact]
        public void Icons_ReturnInlineSvgForKnownNames()
        {
            foreach (var name in ElHtmlEditorGrowIcons.Names)
            {
                var svg = ElHtmlEditorGrowIcons.Get(name);

                Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
                Assert.Contains("</svg>", svg);
                Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Icons_NeverContainTextElement()
        {
            // 本仓库既有坑：Razor 无法直接输出 SVG 的 <text>，因此图标必须只用 path/circle/rect/line。
            foreach (var name in ElHtmlEditorGrowIcons.Names)
            {
                Assert.DoesNotContain("<text", ElHtmlEditorGrowIcons.Get(name), StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void Icons_ExposeInnerMarkupWithoutWrapper()
        {
            var inner = ElHtmlEditorGrowIcons.GetInner("bold");

            Assert.False(string.IsNullOrWhiteSpace(inner));
            Assert.DoesNotContain("<svg", inner, StringComparison.OrdinalIgnoreCase);
            Assert.Null(ElHtmlEditorGrowIcons.GetInner("nope"));
        }

        [Fact]
        public void Icons_UnknownNameFallsBack()
        {
            Assert.Equal(ElHtmlEditorGrowIcons.Get(ElHtmlEditorGrowIcons.Fallback), ElHtmlEditorGrowIcons.Get("definitely-not-an-icon"));
        }

        [Fact]
        public void Icons_NullAndEmptyFallBack()
        {
            Assert.Equal(ElHtmlEditorGrowIcons.Get(ElHtmlEditorGrowIcons.Fallback), ElHtmlEditorGrowIcons.Get(null));
            Assert.Equal(ElHtmlEditorGrowIcons.Get(ElHtmlEditorGrowIcons.Fallback), ElHtmlEditorGrowIcons.Get(string.Empty));
        }

        [Fact]
        public void Icons_HasReportsMembership()
        {
            Assert.True(ElHtmlEditorGrowIcons.Has("bold"));
            Assert.False(ElHtmlEditorGrowIcons.Has("nope"));
            Assert.False(ElHtmlEditorGrowIcons.Has(null));
        }

        [Fact]
        public void Icons_CoverWholeToolbarVocabulary()
        {
            Assert.True(ElHtmlEditorGrowIcons.Names.Count >= 40);
            Assert.Contains("dot", ElHtmlEditorGrowIcons.Names);
            Assert.Contains("table", ElHtmlEditorGrowIcons.Names);
            Assert.Contains("source", ElHtmlEditorGrowIcons.Names);
        }

        [Fact]
        public void Icons_SvgUsesCurrentColorAndAccessibleDefaults()
        {
            var svg = ElHtmlEditorGrowIcons.Get("bold");

            Assert.Contains("currentColor", svg);
            Assert.Contains("aria-hidden=\"true\"", svg);
        }

        #endregion

        #region 守卫：零第三方依赖

        [Fact]
        public void ComponentSources_DoNotReferenceThirdPartyLibraries()
        {
            var directory = Path.Combine(RepoRoot(), "src", "Components");
            var files = Directory.GetFiles(directory, "ElHtmlEditorGrow*", SearchOption.TopDirectoryOnly);

            Assert.NotEmpty(files);

            var offenders = new List<string>();

            foreach (var file in files)
            {
                var code = StripComments(File.ReadAllText(file));

                foreach (var token in new[] { "Radzen", "Markdig", "System.Web" })
                {
                    if (code.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        offenders.Add(Path.GetFileName(file) + " → " + token);
                    }
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void ComponentSources_DoNotDeclareUsingForThirdPartyNamespaces()
        {
            var directory = Path.Combine(RepoRoot(), "src", "Components");
            var offenders = new List<string>();

            foreach (var file in Directory.GetFiles(directory, "ElHtmlEditorGrow*", SearchOption.TopDirectoryOnly))
            {
                foreach (var line in File.ReadAllLines(file))
                {
                    var trimmed = line.TrimStart();

                    if (trimmed.StartsWith("using ", StringComparison.Ordinal)
                        && (trimmed.Contains("Radzen", StringComparison.OrdinalIgnoreCase)
                            || trimmed.Contains("Markdig", StringComparison.OrdinalIgnoreCase)))
                    {
                        offenders.Add(Path.GetFileName(file) + " → " + trimmed);
                    }
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void HtmlBuilding_StaysInCSharp()
        {
            // 结构拼装（表格 / 图片 / 链接）必须留在纯 C# 侧，JS 只负责插入与选区，保证可单测。
            var source = File.ReadAllText(Path.Combine(
                RepoRoot(), "src", "Components", "ElHtmlEditorGrowHtmlBuilder.cs"));

            Assert.Contains("public static string BuildTable", source);
            Assert.Contains("public static string BuildImage", source);
            Assert.Contains("public static string BuildLink", source);
        }

        [Fact]
        public void ComponentProject_DoesNotAddNewPackageDependencies()
        {
            var csproj = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Components", "Element.csproj"));

            Assert.DoesNotContain("Radzen", csproj, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Markdig", csproj, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region 守卫：自写 JS 通道

        [Theory]
        [InlineData("elementHtmlEditorAttach")]
        [InlineData("elementHtmlEditorDetach")]
        [InlineData("elementHtmlEditorPickFile")]
        [InlineData("elementHtmlEditorGetHtml")]
        [InlineData("elementHtmlEditorSetHtml")]
        [InlineData("elementHtmlEditorExec")]
        [InlineData("elementHtmlEditorQueryState")]
        [InlineData("elementHtmlEditorSaveSelection")]
        [InlineData("elementHtmlEditorRestoreSelection")]
        [InlineData("elementHtmlEditorSelectionText")]
        [InlineData("elementHtmlEditorGetTableSelection")]
        [InlineData("elementHtmlEditorTableCommand")]
        public void JsChannel_ExportsEveryFunctionTheComponentCalls(string export)
        {
            Assert.Contains("window." + export + " =", JsSection());
        }

        [Fact]
        public void JsChannel_DeclaresEveryDotNetCallback()
        {
            var js = JsSection();

            foreach (var callback in new[]
            {
                "OnEditorInputAsync", "OnSelectionChangedAsync", "OnEditorBlurAsync",
                "OnPasteFilterAsync", "OnUploadCompletedAsync"
            })
            {
                Assert.Contains(callback, js);
            }
        }

        [Fact]
        public void JsChannel_UsesOnlyBrowserBuiltIns()
        {
            // 只查代码：注释里写「对齐 Radzen 的某某能力」是文档说明，不是引用第三方库
            var js = StripComments(JsSection());

            Assert.Contains("document.execCommand", js);
            Assert.Contains("window.getSelection", js);
            Assert.Contains("XMLHttpRequest", js);
            Assert.DoesNotContain("Radzen", js, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Markdig", js, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void JsChannel_IsWrittenInEs5Syntax()
        {
            // 只看代码：通道里的中文注释习惯用反引号包裹标识号（`scrollHeight-clientHeight`），
            // 拿原文做断言会把注释里的反引号当成模板字符串误报。
            var js = StripComments(JsSection());

            Assert.DoesNotContain("=>", js);
            Assert.DoesNotContain("`", js);
            Assert.DoesNotMatch(new Regex(@"\b(let|const)\s"), js);
            Assert.DoesNotMatch(new Regex(@"\?\."), js);
        }

        [Fact]
        public void JsChannel_ImplementsAllTableCommands()
        {
            var js = JsSection();

            // insertTable 在进 switch 之前单独处理（它不需要当前单元格）。
            Assert.Contains("\"inserttable\"", js);

            foreach (var command in new[]
            {
                "updatetable", "addrowbefore", "addrowafter",
                "addcolumnbefore", "addcolumnafter", "deleterow", "deletecolumn",
                "deletetable", "mergecellright", "mergecelldown", "splitcell",
                "copycells", "pastecells"
            })
            {
                Assert.Contains("case \"" + command + "\"", js);
            }
        }

        [Fact]
        public void JsChannel_ReportsTableFailuresWithLocalizableCodes()
        {
            var js = JsSection();

            foreach (var code in new[]
            {
                "table.action.requiresTable", "table.merge.invalidSelection", "table.paste.blocked"
            })
            {
                Assert.Contains(code, js);
            }
        }

        [Fact]
        public void JsChannel_GuardsAgainstMissingDom()
        {
            var js = JsSection();

            Assert.Contains("if (!el)", js);
            Assert.Contains("context(el)", js);
        }

        #endregion

        #region 守卫：浏览器兼容

        [Fact]
        public void CssSection_AvoidsModernOnlySyntax()
        {
            var css = CssSection();

            foreach (var forbidden in new[]
            {
                ":has(", ":is(", ":where(", ":focus-visible", "accent-color",
                "clamp(", "inset-", "margin-inline", "padding-inline", "gap:"
            })
            {
                Assert.DoesNotContain(forbidden, css, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void CssSection_IsFullyScoped()
        {
            var css = CssSection();
            var offenders = new List<string>();

            foreach (var chunk in css.Split('}'))
            {
                var brace = chunk.IndexOf('{');

                if (brace < 0)
                {
                    continue;
                }

                var selector = chunk.Substring(0, brace).Trim();

                if (selector.Length == 0)
                {
                    continue;
                }

                foreach (var part in selector.Split(','))
                {
                    var single = part.Trim();

                    if (single.Length > 0 && !single.StartsWith(".el-html-editor-grow", StringComparison.Ordinal))
                    {
                        offenders.Add(single);
                    }
                }
            }

            Assert.Empty(offenders);
        }

        [Fact]
        public void CssSection_AvoidsImportant()
        {
            Assert.DoesNotContain("!important", CssSection(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CssSection_CoversKeyParts()
        {
            var css = CssSection();

            foreach (var part in new[]
            {
                ".el-html-editor-grow__toolbar", ".el-html-editor-grow__btn",
                ".el-html-editor-grow__content", ".el-html-editor-grow__source",
                ".el-html-editor-grow__panel", ".el-html-editor-grow__swatch",
                ".el-html-editor-grow__separator", ".el-html-editor-grow__select"
            })
            {
                Assert.Contains(part, css);
            }
        }

        [Fact]
        public void CssSection_HasNoGlobalElementSelectors()
        {
            var css = CssSection();

            Assert.DoesNotMatch(new Regex(@"(?<![-\w.])body\s*\{"), css);
            Assert.DoesNotMatch(new Regex(@"(?<![-\w.])html\s*\{"), css);
        }

        [Fact]
        public void CssSection_UsesElementPlusVariablesWithFallbacks()
        {
            var css = CssSection();

            Assert.Contains("var(--el-", css);
            Assert.DoesNotMatch(new Regex(@"var\(--el-[a-z-]+\)"), css);
        }

        #endregion
    }
}
