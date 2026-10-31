using Bunit;
using ElementPlus;
using ElementPlus.Lang;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    /// <summary>
    /// 批次⑨：引导按钮（el.tour.*）与上传文件项（el.upload.*）的取词契约。
    /// <para>
    /// EP 2.14.0 取证（<c>packages/components/upload/src/upload-list.vue</c>）：上传族只有两个消费点 ——
    /// 删除图标 <c>&lt;el-icon class="el-icon-close" :aria-label="t('el.upload.delete')" role="button" tabindex="0"&gt;</c>，
    /// 以及 <c>&lt;i v-if="!disabled" class="el-icon-close-tip"&gt;{{ t('el.upload.deleteTip') }}&lt;/i&gt;</c>。
    /// <c>upload-content.vue</c> / <c>upload.vue</c> 里<b>没有任何 t() 调用</b>：触发按钮与拖拽区文案属宿主插槽，
    /// 所以本仓不给它们接词条（locale 里的 preview / continue 在本仓无消费方，已不收录）。
    /// </para>
    /// <para>
    /// 引导按钮必须<b>每次渲染现取</b>，不能在 OnInitialized 里 <c>??=</c> 缓存进参数：缓存会把文字冻在
    /// 首次取到的语言上，切语言后整页都换了、面板里三个按钮不换
    /// （<see cref="TourButtonsFollowLanguageSwitchWhilePanelOpen"/> 钉住这一点）。
    /// </para>
    /// </summary>
    public class LangTourUploadTextTests : BunitContext
    {
        public LangTourUploadTextTests()
        {
            // ElTour 渲染后会调 elementScrollIntoView；没配 Loose 会抛 JSRuntimeUnhandledInvocationException
            JSInterop.Mode = JSRuntimeMode.Loose;
            Services.AddElementServices();
        }

        private const string ZhJson = @"{
  ""el"": {
    ""tour"": { ""next"": ""下一步"", ""previous"": ""上一步"", ""finish"": ""结束导览"", ""close"": ""关闭"" },
    ""upload"": { ""deleteTip"": ""按 Delete 键可删除"", ""delete"": ""删除"" }
  }
}";

        private const string EnJson = @"{
  ""el"": {
    ""tour"": { ""next"": ""Next"", ""previous"": ""Previous"", ""finish"": ""Finish"", ""close"": ""Close"" },
    ""upload"": { ""deleteTip"": ""Press Delete to remove"", ""delete"": ""Delete"" }
  }
}";

        private ElementLang UseLocales()
        {
            var lang = TestLang.Loaded(new Dictionary<string, string>
            {
                { "zh-CN", ZhJson },
                { "en-US", EnJson }
            }, "zh-CN");
            Services.AddSingleton<ILang>(lang);
            return lang;
        }

        private static List<TourStep> TwoSteps() => new List<TourStep>
        {
            new TourStep { Title = "Step A", Description = "First" },
            new TourStep { Title = "Step B", Description = "Second" }
        };

        private IRenderedComponent<ElTour> RenderTour(string nextText = null, string prevText = null, string closeAriaLabel = null)
        {
            return Render<ElTour>(p =>
            {
                p.Add(x => x.ModelValue, true);
                p.Add(x => x.Steps, TwoSteps());
                if (nextText != null)
                {
                    p.Add(x => x.NextText, nextText);
                }
                if (prevText != null)
                {
                    p.Add(x => x.PrevText, prevText);
                }
                if (closeAriaLabel != null)
                {
                    p.Add(x => x.CloseAriaLabel, closeAriaLabel);
                }
            });
        }

        private string[] ActionTexts(IRenderedComponent<ElTour> cut)
        {
            return cut.FindAll(".el-tour__actions button").Select(b => b.TextContent.Trim()).ToArray();
        }

        private string CloseAria(IRenderedComponent<ElTour> cut)
        {
            return cut.Find(".el-tour__close").GetAttribute("aria-label");
        }

        // ===== ElTour：三个按钮 =====

        [Fact]
        public void TourButtonsUseFallbackWithoutEntries()
        {
            var cut = RenderTour();

            // 词条没接通时必须是接入 i18n 之前就有的英文字面量，不能是空按钮
            Assert.Equal(new[] { "Previous", "Next" }, ActionTexts(cut));
        }

        [Fact]
        public void TourButtonsUseChineseEntriesAndFinishOnLastStep()
        {
            UseLocales();
            var cut = RenderTour();

            Assert.Equal(new[] { "上一步", "下一步" }, ActionTexts(cut));

            cut.Find(".el-tour__actions button:last-child").Click();
            cut.WaitForAssertion(() => Assert.Equal(new[] { "上一步", "结束导览" }, ActionTexts(cut)), timeout: TestWait.Timeout);
        }

        [Fact]
        public void ExplicitTourTextsBeatEntries()
        {
            UseLocales();
            var cut = RenderTour("继续", "回到上一步");

            Assert.Equal(new[] { "回到上一步", "继续" }, ActionTexts(cut));
        }

        [Fact]
        public async Task TourButtonsFollowLanguageSwitchWhilePanelOpen()
        {
            var lang = UseLocales();
            var cut = RenderTour();
            Assert.Equal(new[] { "上一步", "下一步" }, ActionTexts(cut));

            await lang.SetLangAsync("en-US");

            // 面板开着切语言：按钮要跟着换（OnInitialized 里 ??= 缓存的写法会在这条上红）
            cut.WaitForAssertion(() => Assert.Equal(new[] { "Previous", "Next" }, ActionTexts(cut)), timeout: TestWait.Timeout);
        }

        // ===== ElTour：关闭按钮的可访问名称 =====

        [Fact]
        public void TourCloseButtonUsesOriginalLiteralWithoutEntries()
        {
            var cut = RenderTour();

            // 兜底必须等于接入 i18n 之前的硬编码值：没词条时 DOM 一字不变
            Assert.Equal("关闭", CloseAria(cut));
        }

        [Fact]
        public async Task TourCloseButtonFollowsLanguageSwitch()
        {
            var lang = UseLocales();
            var cut = RenderTour();
            Assert.Equal("关闭", CloseAria(cut));

            await lang.SetLangAsync("en-US");

            cut.WaitForAssertion(() => Assert.Equal("Close", CloseAria(cut)), timeout: TestWait.Timeout);
        }

        [Fact]
        public void ExplicitTourCloseAriaLabelBeatsEntries()
        {
            UseLocales();
            var cut = RenderTour(closeAriaLabel: "关掉引导");

            Assert.Equal("关掉引导", CloseAria(cut));
        }

        // ===== ElUpload：删除图标 aria-label 与 close-tip =====


        private IRenderedComponent<ElUpload> RenderUploadWithFile(
            string deleteAriaLabel = null,
            string deleteTipText = null,
            bool disabled = false,
            UploadListType listType = UploadListType.Text)
        {
            var cut = Render<ElUpload>(p =>
            {
                p.Add(x => x.Url, "/upload");
                p.Add(x => x.AutoUpload, false);
                p.Add(x => x.EnablePasteUpload, false);
                p.Add(x => x.ListType, listType);
                p.Add(x => x.Disabled, disabled);
                if (deleteAriaLabel != null)
                {
                    p.Add(x => x.DeleteAriaLabel, deleteAriaLabel);
                }
                if (deleteTipText != null)
                {
                    p.Add(x => x.DeleteTipText, deleteTipText);
                }
            });

            cut.InvokeAsync(() => cut.Instance.PasteUploadFiles(new[] { new[] { "report.txt", "100" } })).Wait();
            return cut;
        }

        [Fact]
        public void UploadDeleteAriaLabelAndTipUseFallbackWithoutEntries()
        {
            var cut = RenderUploadWithFile();

            var icon = cut.Find(".el-upload-list__item .el-icon-close");
            Assert.Equal("Delete", icon.GetAttribute("aria-label"));
            Assert.Equal("button", icon.GetAttribute("role"));
            Assert.Equal("Press Delete to remove", cut.Find(".el-icon-close-tip").TextContent);
        }

        [Fact]
        public void UploadDeleteAriaLabelAndTipUseChineseEntries()
        {
            UseLocales();
            var cut = RenderUploadWithFile();

            Assert.Equal("删除", cut.Find(".el-upload-list__item .el-icon-close").GetAttribute("aria-label"));
            Assert.Equal("按 Delete 键可删除", cut.Find(".el-icon-close-tip").TextContent);
        }

        [Fact]
        public void ExplicitUploadTextsBeatEntries()
        {
            UseLocales();
            var cut = RenderUploadWithFile("移除这个文件", "删！");

            Assert.Equal("移除这个文件", cut.Find(".el-upload-list__item .el-icon-close").GetAttribute("aria-label"));
            Assert.Equal("删！", cut.Find(".el-icon-close-tip").TextContent);
        }

        [Fact]
        public void DisabledUploadKeepsCloseTipEmpty()
        {
            UseLocales();
            var cut = RenderUploadWithFile(disabled: true);

            // EP：<i v-if="!disabled"> —— 禁用时提示不留文字
            Assert.Equal("", cut.Find(".el-icon-close-tip").TextContent);
        }

        [Fact]
        public void UploadDeleteTipFollowsLanguageSwitch()
        {
            var lang = UseLocales();
            var cut = RenderUploadWithFile();
            Assert.Equal("按 Delete 键可删除", cut.Find(".el-icon-close-tip").TextContent);

            lang.SetLangAsync("en-US").Wait();

            cut.WaitForAssertion(() => Assert.Equal("Press Delete to remove", cut.Find(".el-icon-close-tip").TextContent), timeout: TestWait.Timeout);
        }

        [Fact]
        public void PictureCardDeleteIconGetsAriaLabel()
        {
            UseLocales();
            var cut = Render<ElUpload>(p =>
            {
                p.Add(x => x.Url, "/upload");
                p.Add(x => x.AutoUpload, false);
                p.Add(x => x.EnablePasteUpload, false);
                p.Add(x => x.ListType, UploadListType.PictureCard);
            });

            cut.InvokeAsync(() => cut.Instance.PasteUploadFiles(new[] { new[] { "scan.png", "100" } })).Wait();

            Assert.Equal("删除", cut.Find(".el-upload-list__item-delete i").GetAttribute("aria-label"));
        }
    }
}
