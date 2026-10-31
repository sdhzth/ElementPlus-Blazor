using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>时间滚轮的三列之一。</summary>
    internal enum TimeSpinnerPart
    {
        Hour,
        Minute,
        Second
    }

    /// <summary>
    /// <c>el-time-spinner</c>（时 / 分 / 秒滚轮列）与 <c>el-time-panel__footer</c>（取消 / 确认）的共享渲染器。
    /// <para>
    /// <see cref="ElTimePicker"/> 与 <see cref="ElDateTimePicker"/> 的面板共用同一份标记，
    /// 因此样式、<c>data-time-*</c> 测试钩子、键盘/无障碍属性只在一处维护。
    /// 列单元固定带 <c>data-time-panel</c> / <c>data-time-part</c> / <c>data-time-value</c> 三个属性，
    /// 单测可直接按它们定位：<c>li[data-time-panel='single'][data-time-part='hour'][data-time-value='9']</c>。
    /// </para>
    /// </summary>
    internal static class TimeSpinnerRenderer
    {
        internal static string PartName(TimeSpinnerPart part)
        {
            return part switch
            {
                TimeSpinnerPart.Hour => "hour",
                TimeSpinnerPart.Minute => "minute",
                _ => "second"
            };
        }

        internal static int PartCount(TimeSpinnerPart part)
        {
            return part == TimeSpinnerPart.Hour ? 24 : 60;
        }

        internal static int ReadPart(TimeSpan value, TimeSpinnerPart part)
        {
            return part switch
            {
                TimeSpinnerPart.Hour => value.Hours,
                TimeSpinnerPart.Minute => value.Minutes,
                _ => value.Seconds
            };
        }

        internal static TimeSpan UpdatePart(TimeSpan source, TimeSpinnerPart part, int value)
        {
            return part switch
            {
                TimeSpinnerPart.Hour => new TimeSpan(value, source.Minutes, source.Seconds),
                TimeSpinnerPart.Minute => new TimeSpan(source.Hours, value, source.Seconds),
                _ => new TimeSpan(source.Hours, source.Minutes, value)
            };
        }

        /// <summary>
        /// 渲染 EP TimePicker 的时分秒滚轮（DOM 结构与 element-plus 一致）。
        /// <code>
        /// div.el-time-spinner(.has-seconds)
        ///   └ div.el-time-spinner__wrapper        ← 可滚动容器（data-part=hour|minute|second）
        ///       └ ul.el-time-spinner__list        ← ::before/::after 各 80px 占位（index.css）
        ///           └ li.el-time-spinner__item(.active)
        /// </code>
        /// <para>
        /// 关键点：滚动容器必须是 <c>wrapper</c> 而不是 <c>list</c>——EP 的
        /// <c>.el-time-panel__content::before/::after</c> 画的是中间那条选中带，
        /// 只有 wrapper 滚动时，选中带才固定不动、被选中的项才能停在同一条带上（三列上下对齐）。
        /// </para>
        /// </summary>
        internal static void BuildSpinner(
            RenderTreeBuilder builder,
            ref int seq,
            object receiver,
            string panel,
            TimeSpan value,
            bool showMinutes,
            bool showSeconds,
            Func<TimeSpinnerPart, int, Task> onSelect)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", showSeconds ? "el-time-spinner has-seconds" : "el-time-spinner");
            builder.AddAttribute(seq++, "role", "group");
            builder.AddAttribute(seq++, "aria-label", panel);
            builder.AddAttribute(seq++, "style", "display:flex;");
            BuildColumn(builder, ref seq, receiver, panel, TimeSpinnerPart.Hour, value.Hours, onSelect);
            if (showMinutes)
            {
                BuildColumn(builder, ref seq, receiver, panel, TimeSpinnerPart.Minute, value.Minutes, onSelect);
            }
            if (showSeconds)
            {
                BuildColumn(builder, ref seq, receiver, panel, TimeSpinnerPart.Second, value.Seconds, onSelect);
            }
            builder.CloseElement();
        }

        /// <summary>渲染 <c>div.el-time-panel__footer</c>（取消 / 确认）。</summary>
        internal static void BuildFooter(
            RenderTreeBuilder builder,
            ref int seq,
            string cancelText,
            string confirmText,
            bool confirmDisabled,
            EventCallback<MouseEventArgs> onCancel,
            EventCallback<MouseEventArgs> onConfirm)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "el-time-panel__footer");
            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, "type", "button");
            builder.AddAttribute(seq++, "class", "el-time-panel__btn cancel");
            builder.AddAttribute(seq++, "onclick", onCancel);
            builder.AddContent(seq++, cancelText);
            builder.CloseElement();
            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, "type", "button");
            builder.AddAttribute(seq++, "class", "el-time-panel__btn confirm");
            builder.AddAttribute(seq++, "disabled", confirmDisabled);
            builder.AddAttribute(seq++, "onclick", onConfirm);
            builder.AddContent(seq++, confirmText);
            builder.CloseElement();
            builder.CloseElement();
        }

        private static void BuildColumn(
            RenderTreeBuilder builder,
            ref int seq,
            object receiver,
            string panel,
            TimeSpinnerPart part,
            int selectedValue,
            Func<TimeSpinnerPart, int, Task> onSelect)
        {
            var count = PartCount(part);
            var partName = PartName(part);

            // 滚动容器（EP：.el-time-spinner__wrapper）
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "el-time-spinner__wrapper");
            builder.AddAttribute(seq++, "data-time-panel", panel);
            builder.AddAttribute(seq++, "data-part", partName);
            builder.AddAttribute(seq++, "role", "listbox");
            builder.AddAttribute(seq++, "aria-label", part.ToString());
            builder.AddAttribute(seq++, "style", "position:relative;flex:1 1 0;max-height:190px;overflow:auto;");

            // 列表（EP：.el-time-spinner__list，上下占位由 ::before/::after 提供）
            builder.OpenElement(seq++, "ul");
            builder.AddAttribute(seq++, "class", "el-time-spinner__list");
            for (var value = 0; value < count; value++)
            {
                var current = value;
                var itemClass = HtmlPropertyBuilder.CreateCssClassBuilder()
                    .Add("el-time-spinner__item")
                    .AddIf(current == selectedValue, "active")
                    .ToString();
                builder.OpenElement(seq++, "li");
                builder.AddAttribute(seq++, "class", itemClass);
                builder.AddAttribute(seq++, "role", "option");
                builder.AddAttribute(seq++, "aria-selected", current == selectedValue ? "true" : "false");
                builder.AddAttribute(seq++, "data-time-panel", panel);
                builder.AddAttribute(seq++, "data-time-part", partName);
                builder.AddAttribute(seq++, "data-time-value", current);
                builder.AddAttribute(seq++, "style", "height:32px;line-height:32px;text-align:center;cursor:pointer;");
                builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(receiver, () => onSelect(part, current)));
                builder.AddContent(seq++, current.ToString("00", CultureInfo.InvariantCulture));
                builder.CloseElement();
            }
            builder.CloseElement(); // ul
            builder.CloseElement(); // div.el-time-spinner__wrapper
        }
    }
}
