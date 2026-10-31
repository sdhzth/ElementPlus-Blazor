using System;
using Microsoft.AspNetCore.Components;
using ElementPlus;

namespace ElementPlus
{
    /// <summary>
    /// 图表坐标轴（Element Plus 自渲染版）。
    /// 作为 <see cref="ElChartGrow"/> 的子组件声明类别轴/数值轴，并在初始化时注册到父图表。
    /// 自身不渲染任何可见内容（轴几何由 <see cref="ElChartGrowEngine"/> 统一计算）。
    /// </summary>
    public partial class ElChartAxis : ComponentBase
    {
        /// <summary>父图表（级联参数）。</summary>
        [CascadingParameter] public ElChartGrow Chart { get; set; }

        /// <summary>坐标轴类型（Category/Value）。</summary>
        [Parameter] public ElAxisType Type { get; set; } = ElAxisType.Value;

        /// <summary>
        /// 坐标轴名称（空=主轴）。数值轴的名称若与 <c>ElChartSeries.ValueAxisName</c> 相同，
        /// 则该序列绑定到此轴并改用其量程；命名轴默认绘制在绘图区右侧（次轴）。
        /// </summary>
        [Parameter] public string Name { get; set; }

        /// <summary>坐标轴标题。</summary>
        [Parameter] public string Title { get; set; }

        /// <summary>最小值（不设置则由数据自动推断）。</summary>
        [Parameter] public double? Min { get; set; }

        /// <summary>最大值（不设置则由数据自动推断）。</summary>
        [Parameter] public double? Max { get; set; }

        /// <summary>刻度步长（不设置则由 nice 算法推断）。</summary>
        [Parameter] public double? Step { get; set; }

        /// <summary>刻度/标签格式化回调。</summary>
        [Parameter] public Func<object, string> Formatter { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            Chart?.AddAxis(this);
        }
    }
}
