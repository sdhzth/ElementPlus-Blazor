using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElSliderTests : BunitContext
    {
        public ElSliderTests()
        {
            Services.AddElementServices();
        }

        [Fact]
        public void RangeSliderRendersTwoThumbsAndRangeBar()
        {
            var range = new List<double> { 20, 80 };
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, range)
                .Add(x => x.Id, "range-slider"));

            var inputs = cut.FindAll(".el-slider__input--range");
            Assert.Equal(2, inputs.Count);
            Assert.Equal("slider", inputs[0].GetAttribute("role"));
            Assert.Equal("range-slider", inputs[0].Id);
            Assert.Equal("range-slider-2", inputs[1].Id);
            Assert.Equal("20", inputs[0].GetAttribute("aria-valuenow"));
            Assert.Equal("80", inputs[1].GetAttribute("aria-valuenow"));
            Assert.Contains("left:20", cut.Find(".el-slider__bar").GetAttribute("style"));
            Assert.Contains("width:60", cut.Find(".el-slider__bar").GetAttribute("style"));
        }

        [Fact]
        public void RangeSliderOrdersChangedValues()
        {
            IList<double> range = new List<double> { 20, 80 };
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, range)
                .Add(x => x.RangeValueChanged, next => range = next));

            cut.FindAll(".el-slider__input--range").First().Input(90);

            Assert.Equal(new[] { 80d, 90d }, range);
            Assert.Contains("left:80", cut.Find(".el-slider__bar").GetAttribute("style"));
            Assert.Contains("width:10", cut.Find(".el-slider__bar").GetAttribute("style"));
        }

        [Fact]
        public void SliderFormatsTooltip()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 30)
                .Add(x => x.FormatTooltip, value => $"{value}%"));

            Assert.Contains("30%", cut.Markup);
            Assert.NotEmpty(cut.FindAll(".el-slider__tooltip"));
        }

        [Fact]
        public void SliderCanHideTooltip()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 30)
                .Add(x => x.ShowTooltip, false));

            Assert.Empty(cut.FindAll(".el-slider__tooltip"));
            Assert.Equal("30", cut.Find(".el-slider__input").GetAttribute("aria-valuetext"));
        }

        [Fact]
        public void RangeSliderFormatsBothTooltips()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, new List<double> { 10, 40 })
                .Add(x => x.FormatTooltip, value => $"Level {value}"));

            var tooltips = cut.FindAll(".el-slider__tooltip");
            var inputs = cut.FindAll(".el-slider__input--range");

            Assert.Equal(2, tooltips.Count);
            Assert.Equal("Level 10", tooltips[0].TextContent);
            Assert.Equal("Level 40", tooltips[1].TextContent);
            Assert.Equal("Level 10", inputs[0].GetAttribute("aria-valuetext"));
            Assert.Equal("Level 40", inputs[1].GetAttribute("aria-valuetext"));
        }

        [Fact]
        public void FocusShowsTooltipForActiveThumb()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, new List<double> { 10, 40 }));

            cut.FindAll(".el-slider__input--range")[1].Focus();

            Assert.DoesNotContain("is-hover", cut.FindAll(".el-slider__button-wrapper")[0].ClassList);
            Assert.Contains("is-hover", cut.FindAll(".el-slider__button-wrapper")[1].ClassList);

            cut.FindAll(".el-slider__input--range")[1].Blur();

            Assert.DoesNotContain("is-hover", cut.FindAll(".el-slider__button-wrapper")[1].ClassList);
        }

        [Fact]
        public void RangeSliderNormalizesBoundsAndStep()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.Min, 10)
                .Add(x => x.Max, 50)
                .Add(x => x.Step, 5)
                .Add(x => x.RangeValue, new List<double> { 52, 11 }));

            var inputs = cut.FindAll(".el-slider__input--range");

            Assert.Equal("10", inputs[0].GetAttribute("value"));
            Assert.Equal("50", inputs[1].GetAttribute("value"));
            Assert.Contains("left:0", cut.Find(".el-slider__bar").GetAttribute("style"));
            Assert.Contains("width:100", cut.Find(".el-slider__bar").GetAttribute("style"));
        }

        [Fact]
        public void SliderRendersStopsMarksAndInput()
        {
            double value = 25;
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Step, 25)
                .Add(x => x.ShowStops, true)
                .Add(x => x.ShowInput, true)
                .Add(x => x.Marks, new[]
                {
                    new SliderMark { Value = 0, Label = "Low" },
                    new SliderMark { Value = 100, Label = "High" }
                }));

            // EP useStops：只渲染「已选区间之外」的断点（step=25 → 50/75，25 已被 bar 覆盖）
            // marks 会额外产出 .el-slider__marks-stop，需排除后才是真正的 step 断点
            Assert.Equal(2, cut.FindAll(".el-slider__stop:not(.el-slider__marks-stop)").Count);
            Assert.Contains("Low", cut.Markup);
            Assert.Contains("High", cut.Markup);
            Assert.Contains("el-slider--with-input", cut.Find(".el-slider").ClassList);

            cut.Find(".el-input-number input").Change("75");

            Assert.Equal(75, value);
            Assert.Equal("75", cut.Find(".el-slider__input").GetAttribute("value"));
        }

        [Fact]
        public void DisabledSliderDoesNotCommitInputs()
        {
            double value = 20;
            IList<double> range = new List<double> { 10, 40 };
            var single = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Disabled, true));
            var ranged = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, range)
                .Add(x => x.RangeValueChanged, next => range = next)
                .Add(x => x.Disabled, true));

            single.Find(".el-slider__input").Input(80);
            ranged.FindAll(".el-slider__input--range")[1].Input(80);

            Assert.Equal(20, value);
            Assert.Equal(new[] { 10d, 40d }, range);
            Assert.Equal("true", single.Find(".el-slider").GetAttribute("aria-disabled"));
            Assert.Equal("true", single.Find(".el-slider__input").GetAttribute("aria-disabled"));
        }

        [Fact]
        public void SliderExposesAccessibleNameOnThumbs()
        {
            var named = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 30)
                .Add(x => x.AriaLabel, "volume")
                .Add(x => x.AriaLabelledBy, "volume-label"));

            var input = named.Find(".el-slider__input");
            Assert.Equal("volume", input.GetAttribute("aria-label"));
            Assert.Equal("volume-label", input.GetAttribute("aria-labelledby"));

            var ranged = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, new List<double> { 20, 80 })
                .Add(x => x.AriaLabel, "span")
                .Add(x => x.AriaLabelledBy, "span-label"));

            foreach (var thumb in ranged.FindAll(".el-slider__input--range"))
            {
                Assert.Equal("span", thumb.GetAttribute("aria-label"));
                Assert.Equal("span-label", thumb.GetAttribute("aria-labelledby"));
            }
        }

        [Fact]
        public void SliderOmitsAccessibleNameWhenUnset()
        {
            var cut = Render<ElSlider>(parameters => parameters.Add(x => x.Value, 30));

            Assert.Null(cut.Find(".el-slider__input").GetAttribute("aria-label"));
            Assert.Null(cut.Find(".el-slider__input").GetAttribute("aria-labelledby"));
        }

        [Fact]
        public void VerticalSliderUsesHeightParameter()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Vertical, true)
                .Add(x => x.Height, 120));

            Assert.Contains("height:120px", cut.Find(".el-slider__runway").GetAttribute("style"));
            Assert.Contains("is-vertical", cut.Find(".el-slider").ClassList);
        }

        [Fact]
        public void SizeRendersWrapperModifierClass()
        {
            var large = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 10)
                .Add(x => x.Size, InputSize.Large));

            Assert.Contains("el-slider--large", large.Find(".el-slider").ClassList);
            Assert.DoesNotContain("el-slider--default", large.Find(".el-slider").ClassList);

            var defaulted = Render<ElSlider>(parameters => parameters.Add(x => x.Value, 10));

            Assert.Contains("el-slider--default", defaulted.Find(".el-slider").ClassList);
        }

        [Fact]
        public void InputSizeFallsBackToSizeWhenNotSpecified()
        {
            var inherited = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 10)
                .Add(x => x.ShowInput, true)
                .Add(x => x.Size, InputSize.Small));

            Assert.Contains("el-input-number--small", inherited.Find(".el-slider .el-input-number").ClassList);

            var explicitSize = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 10)
                .Add(x => x.ShowInput, true)
                .Add(x => x.Size, InputSize.Small)
                .Add(x => x.InputSize, InputSize.Large));

            Assert.Contains("el-input-number--large", explicitSize.Find(".el-slider .el-input-number").ClassList);
        }

        [Fact]
        public void PlacementDrivesTooltipModifierAndSupportsLegacyAlias()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 30)
                .Add(x => x.Placement, "bottom")
                .Add(x => x.TooltipClass, "demo-slider-tip"));

            var tooltip = cut.Find(".el-slider__tooltip");
            Assert.Contains("el-tooltip--bottom", tooltip.ClassList);
            Assert.Contains("demo-slider-tip", tooltip.ClassList);

            var aliased = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 30)
                .Add(x => x.TooltipPlacement, "right"));

            Assert.Contains("el-tooltip--right", aliased.Find(".el-slider__tooltip").ClassList);
        }

        [Fact]
        public void PersistentFalseDestroysInactiveTooltip()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 30)
                .Add(x => x.Persistent, false));

            Assert.Empty(cut.FindAll(".el-slider__tooltip"));

            cut.Find(".el-slider__input").Focus();

            Assert.Single(cut.FindAll(".el-slider__tooltip"));

            cut.Find(".el-slider__input").Blur();

            Assert.Empty(cut.FindAll(".el-slider__tooltip"));
        }

        [Fact]
        public void ShowTooltipFalseKeepsTooltipOutEvenWhenPersistent()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 30)
                .Add(x => x.ShowTooltip, false)
                .Add(x => x.Persistent, true));

            Assert.Empty(cut.FindAll(".el-slider__tooltip"));
        }

        [Fact]
        public void RangeThumbsUseStartAndEndLabels()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, new List<double> { 20, 80 })
                .Add(x => x.RangeStartLabel, "minimum")
                .Add(x => x.RangeEndLabel, "maximum")
                .Add(x => x.AriaLabel, "span"));

            var inputs = cut.FindAll(".el-slider__input--range");
            Assert.Equal("minimum", inputs[0].GetAttribute("aria-label"));
            Assert.Equal("maximum", inputs[1].GetAttribute("aria-label"));
            Assert.Equal("span", cut.Find(".el-slider").GetAttribute("aria-label"));
            Assert.Equal("group", cut.Find(".el-slider").GetAttribute("role"));
        }

        [Fact]
        public void RangeThumbsConstraintAriaBoundsToNeighbour()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.Min, 0)
                .Add(x => x.Max, 100)
                .Add(x => x.RangeValue, new List<double> { 20, 80 }));

            var inputs = cut.FindAll(".el-slider__input--range");

            Assert.Equal("80", inputs[0].GetAttribute("aria-valuemax"));
            Assert.Equal("0", inputs[0].GetAttribute("aria-valuemin"));
            Assert.Equal("20", inputs[1].GetAttribute("aria-valuemin"));
            Assert.Equal("100", inputs[1].GetAttribute("aria-valuemax"));
        }

        [Fact]
        public void FormatValueTextOverridesAriaValueTextOnly()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 30)
                .Add(x => x.FormatTooltip, value => $"{value}%")
                .Add(x => x.FormatValueText, value => $"Level {value}"));

            Assert.Equal("Level 30", cut.Find(".el-slider__input").GetAttribute("aria-valuetext"));
            Assert.Contains("30%", cut.Find(".el-slider__tooltip").TextContent);
        }

        [Fact]
        public void StepToMarkRestrictsValuesToMarksAndDisablesInput()
        {
            double value = 20;
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.StepToMark, true)
                .Add(x => x.ShowInput, true)
                .Add(x => x.ShowStops, true)
                .Add(x => x.Marks, new[]
                {
                    new SliderMark { Value = 0 },
                    new SliderMark { Value = 25 },
                    new SliderMark { Value = 100 }
                }));

            // step="mark"：EP 的 renderInput 判定为 false，输入框与断点都不渲染
            Assert.DoesNotContain("el-slider--with-input", cut.Find(".el-slider").ClassList);
            Assert.Empty(cut.FindAll(".el-input-number"));
            Assert.Empty(cut.FindAll(".el-slider__stop:not(.el-slider__marks-stop)"));
            Assert.Equal(3, cut.FindAll(".el-slider__marks-stop").Count);

            cut.Find(".el-slider__input").Input(30);

            Assert.Equal(25, value);
            Assert.Equal("25", cut.Find(".el-slider__input").GetAttribute("value"));
        }

        [Fact]
        public void ShowInputIsIgnoredForRange()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, new List<double> { 20, 80 })
                .Add(x => x.ShowInput, true));

            Assert.DoesNotContain("el-slider--with-input", cut.Find(".el-slider").ClassList);
            Assert.Empty(cut.FindAll(".el-input-number"));
        }

        [Fact]
        public void VerticalSliderPositionsWithBottom()
        {
            var single = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 40)
                .Add(x => x.Vertical, true)
                .Add(x => x.Height, 150));

            Assert.Equal("bottom:0;height:40%;", single.Find(".el-slider__bar").GetAttribute("style"));
            Assert.Equal("bottom:40%;", single.Find(".el-slider__button-wrapper").GetAttribute("style"));
            Assert.Equal("vertical", single.Find(".el-slider__input").GetAttribute("aria-orientation"));

            var ranged = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.Vertical, true)
                .Add(x => x.Height, 150)
                .Add(x => x.RangeValue, new List<double> { 20, 80 }));

            Assert.Equal("bottom:20%;height:60%;", ranged.Find(".el-slider__bar").GetAttribute("style"));
        }

        [Fact]
        public void VerticalStopsAndMarksUseBottom()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 10)
                .Add(x => x.Vertical, true)
                .Add(x => x.Height, 150)
                .Add(x => x.Step, 25)
                .Add(x => x.ShowStops, true)
                .Add(x => x.Marks, new[]
                {
                    new SliderMark { Value = 50, Label = "Half" }
                }));

            foreach (var stop in cut.FindAll(".el-slider__stop"))
            {
                Assert.Contains("bottom:", stop.GetAttribute("style"));
            }

            Assert.Contains("bottom:50%;", cut.Find(".el-slider__marks-text").GetAttribute("style"));
        }

        [Fact]
        public void MarksRenderInsideMarksContainerWithCustomStyle()
        {
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, 10)
                .Add(x => x.Marks, new[]
                {
                    new SliderMark { Value = 0, Label = "Low" },
                    new SliderMark { Value = 100, Label = "High", Style = "color:#f56c6c;" }
                }));

            Assert.Single(cut.FindAll(".el-slider__marks"));
            Assert.Equal(2, cut.FindAll(".el-slider__marks .el-slider__marks-text").Count);
            Assert.Equal(2, cut.FindAll(".el-slider__marks-stop").Count);
            Assert.Contains("color:#f56c6c;", cut.FindAll(".el-slider__marks-text")[1].GetAttribute("style"));
        }

        // ==== 批次⑨ #57：range 拖动（原生钮交叉 / 跑道按住拖）====================
        // EP 口径实抓：钮各拿各的值（initData.firstValue / secondValue 从不排序），
        // 只有「对外发布」和「条形 / 断点」取 minValue / maxValue
        // （Temp/ep214_slider_use-slide.ts.txt:34-40、114-122）；
        // 松手时 watch(dragging=false) → setValues() 把有序的 modelValue 回灌给两个钮
        // （Temp/ep214_slider_use-watch.ts.txt:76-83）。

        /// <summary>bUnit 没有布局引擎，跑道的视口矩形只能桩一个：这里桩成 left=0、宽 100，
        /// 于是 clientX 数值上就等于百分位，用例里的坐标可以直接读成「拖到百分之几」。</summary>
        private void StubRunwayGeometry(double left = 0, double width = 100, double top = 40, double height = 6)
        {
            JSInterop.Mode = JSRuntimeMode.Loose;
            JSInterop.Setup<ElementViewportRect>("elementViewportRect", _ => true)
                .SetResult(new ElementViewportRect
                {
                    Left = left,
                    Top = top,
                    Right = left + width,
                    Bottom = top + height,
                    Width = width,
                    Height = height,
                    Vw = 1440,
                    Vh = 1000
                });
            JSInterop.Setup<bool>("elementPointerCapture", _ => true).SetResult(true);
        }

        private static PointerEventArgs Pointer(double clientX, double clientY = 43, long pointerId = 7)
            => new PointerEventArgs { ClientX = clientX, ClientY = clientY, PointerId = pointerId };

        private int CaptureCalls(bool? capture)
            => JSInterop.Invocations.Count(i => i.Identifier == "elementPointerCapture"
                && (capture == null || System.Convert.ToBoolean(i.Arguments[i.Arguments.Count - 1]) == capture.Value));

        [Fact]
        public void RangeThumbsKeepOwnValuesAcrossNeighbour()
        {
            IList<double> range = new List<double> { 20, 80 };
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, range)
                .Add(x => x.RangeValueChanged, next => range = next));

            cut.FindAll(".el-slider__input--range")[0].Input(90);

            var inputs = cut.FindAll(".el-slider__input--range");
            // 钮自己：正跨越（第一颗 90 已经越过第二颗 80），画面上谁都不该被掰回去
            Assert.Equal("90", inputs[0].GetAttribute("value"));
            Assert.Equal("80", inputs[1].GetAttribute("value"));
            Assert.Equal("left:90%;", cut.FindAll(".el-slider__button-wrapper")[0].GetAttribute("style"));
            Assert.Equal("left:80%;", cut.FindAll(".el-slider__button-wrapper")[1].GetAttribute("style"));
            // 对外：区间仍有序，条形取 min/max（EP minValue / maxValue 同构）
            Assert.Equal(new[] { 80d, 90d }, range);
            Assert.Contains("left:80", cut.Find(".el-slider__bar").GetAttribute("style"));
            Assert.Contains("width:10", cut.Find(".el-slider__bar").GetAttribute("style"));
        }

        [Fact]
        public void DraggingGateKeepsPerThumbValuesWhenParentFeedsBackSortedPair()
        {
            var thumbs = new List<double> { 20, 60 };
            IList<double> published = thumbs;
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, thumbs)
                .Add(x => x.RangeValueChanged, next => published = next));

            cut.FindAll(".el-slider__input--range")[0].Input(80);

            Assert.Equal(new[] { 60d, 80d }, published);
            // 浏览器里塌成 [0,1] 的那一步就发生在「父级把有序区间当参数灌回来、而用户还按着钮」时：
            // 少了 thumbsDragging 这道闸，回灌会把两颗钮一起掰成 [60,80]，正按着的那颗被抢位。
            cut.Render(parameters => parameters.Add(x => x.RangeValue, published));

            var inputs = cut.FindAll(".el-slider__input--range");
            Assert.Equal("80", inputs[0].GetAttribute("value"));
            Assert.Equal("60", inputs[1].GetAttribute("value"));
            Assert.Equal("left:80%;", cut.FindAll(".el-slider__button-wrapper")[0].GetAttribute("style"));
        }

        [Fact]
        public void DragEndRestoresOrderedThumbsAndEmitsChangeOnce()
        {
            IList<double> range = new List<double> { 20, 60 };
            double changed = double.NaN;
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, range)
                .Add(x => x.RangeValueChanged, next => range = next)
                .Add(x => x.OnChange, value => changed = value));

            var thumbs = cut.FindAll(".el-slider__input--range");
            thumbs[0].Input(80);
            // 重新取节点：Input 触发的这次重渲染把 tooltip 的有无改了，旧 INode 在 bUnit 里已经作废
            //（浏览器里同一根 input 不会重建，这条真实拖动由 diag-batch9-slider-runway.cjs 的 B 段量）
            cut.FindAll(".el-slider__input--range")[0].Change(80);

            var after = cut.FindAll(".el-slider__input--range");
            // 松手后回到有序（EP setValues），两颗钮的画面对调序号但位置集合不变：仍是 60 与 80 这两处
            Assert.Equal("60", after[0].GetAttribute("value"));
            Assert.Equal("80", after[1].GetAttribute("value"));
            Assert.Equal(new[] { 60d, 80d }, range);
            Assert.Equal(80d, changed);
        }

        [Fact]
        public async Task RunwayPressDragsNearestThumbAndCachesGeometryForTheGesture()
        {
            StubRunwayGeometry();
            IList<double> range = new List<double> { 20, 60 };
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, range)
                .Add(x => x.RangeValueChanged, next => range = next));

            var runway = cut.Find(".el-slider__runway");
            await runway.PointerDownAsync(Pointer(70));    // 目标值 70 → 离第二颗（60）更近
            await runway.PointerMoveAsync(Pointer(90));
            await runway.PointerUpAsync(Pointer(90));

            var inputs = cut.FindAll(".el-slider__input--range");
            Assert.Equal("20", inputs[0].GetAttribute("value"));
            Assert.Equal("90", inputs[1].GetAttribute("value"));
            Assert.Equal(new[] { 20d, 90d }, range);
            // 一次手势只量一次矩形：EP 每次 onDragging 都 resetSize()，那是同进程取值；
            // 这里是一次 WASM 往返，每次移动都量就等于把「停滞」又请回来。
            Assert.Equal(1, JSInterop.Invocations.Count(i => i.Identifier == "elementViewportRect"));
            Assert.Equal(1, CaptureCalls(true));
            Assert.Equal(1, CaptureCalls(false));
        }

        [Fact]
        public async Task RunwayPressHandsTieToHigherThumb()
        {
            StubRunwayGeometry();
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, new List<double> { 0, 100 }));

            // EP getButtonRefByPercent 用严格 < 判据：等距时交给拿较大值的那颗（第一颗保持 0）
            await cut.Find(".el-slider__runway").PointerDownAsync(Pointer(50));

            var inputs = cut.FindAll(".el-slider__input--range");
            Assert.Equal("0", inputs[0].GetAttribute("value"));
            Assert.Equal("50", inputs[1].GetAttribute("value"));
        }

        [Fact]
        public async Task RunwayPressIsRangeOnlyAndIgnoresDisabledAndOutOfBounds()
        {
            StubRunwayGeometry();
            double single = 30;
            IList<double> disabledRange = new List<double> { 20, 60 };
            IList<double> gatedRange = new List<double> { 20, 60 };

            var plain = Render<ElSlider>(parameters => parameters
                .Add(x => x.Value, single)
                .Add(x => x.ValueChanged, next => single = next));
            var disabled = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, disabledRange)
                .Add(x => x.RangeValueChanged, next => disabledRange = next)
                .Add(x => x.Disabled, true));
            var outOfRange = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, gatedRange)
                .Add(x => x.RangeValueChanged, next => gatedRange = next));

            await plain.Find(".el-slider__runway").PointerDownAsync(Pointer(70));
            await disabled.Find(".el-slider__runway").PointerDownAsync(Pointer(70));
            await outOfRange.Find(".el-slider__runway").PointerDownAsync(Pointer(-10));

            Assert.Equal(30d, single);
            Assert.Equal(new[] { 20d, 60d }, disabledRange);
            Assert.Equal(new[] { 20d, 60d }, gatedRange);
            // 三条负向共用一个「捕获一次都没发生」的口径；越界那条确实量到了矩形，
            // 说明它是在「算出百分位之后」被 EP 的越界判据挡掉的，不是压根没接线。
            Assert.Equal(0, CaptureCalls(null));
            Assert.Equal(1, JSInterop.Invocations.Count(i => i.Identifier == "elementViewportRect"));

            // 正向对照：同一根跑道、同一个桩，落在区间内就必须动 —— 否则上面三条红不说明任何事
            await outOfRange.Find(".el-slider__runway").PointerDownAsync(Pointer(80));
            Assert.Equal("80", outOfRange.FindAll(".el-slider__input--range")[1].GetAttribute("value"));
            Assert.Equal(1, CaptureCalls(true));
        }

        [Fact]
        public async Task RunwayPointerCancelReleasesCaptureWithoutChange()
        {
            StubRunwayGeometry();
            IList<double> range = new List<double> { 20, 60 };
            var changeCount = 0;
            var cut = Render<ElSlider>(parameters => parameters
                .Add(x => x.Range, true)
                .Add(x => x.RangeValue, range)
                .Add(x => x.RangeValueChanged, next => range = next)
                .Add(x => x.OnChange, value => changeCount++));

            var runway = cut.Find(".el-slider__runway");
            await runway.PointerDownAsync(Pointer(35));
            await runway.PointerCancelAsync(Pointer(35));

            Assert.Equal(0, changeCount);
            Assert.Equal(1, CaptureCalls(true));
            Assert.Equal(1, CaptureCalls(false));
            // 取消也要结束「正在拖」，两颗钮回到有序
            var inputs = cut.FindAll(".el-slider__input--range");
            Assert.Equal("35", inputs[0].GetAttribute("value"));
            Assert.Equal("60", inputs[1].GetAttribute("value"));
        }
    }
}
