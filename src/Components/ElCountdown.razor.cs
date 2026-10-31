using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Timers;

namespace ElementPlus
{
    public partial class ElCountdown : ElementComponentBase
    {
        private Timer timer;
        private TimeSpan remain;

        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public RenderFragment TitleContent { get; set; }

        [Parameter]
        public DateTime Value { get; set; }

        [Parameter]
        public string Format { get; set; } = "HH:mm:ss";

        [Parameter]
        public string PrefixText { get; set; }

        [Parameter]
        public RenderFragment Prefix { get; set; }

        [Parameter]
        public string SuffixText { get; set; }

        [Parameter]
        public RenderFragment Suffix { get; set; }

        /// <summary>数值区 style（对齐 EP 官方 value-style；EP 的 ElCountdown 是把它透传给内层 ElStatistic）</summary>
        [Parameter]
        public string ValueStyle { get; set; }

        [Parameter]
        public EventCallback OnFinish { get; set; }

        protected string DisplayValue => FormatRemain();

        protected string CountdownClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-statistic", "el-countdown", Cls)
            .ToString();

        protected override void OnInitialized()
        {
            base.OnInitialized();
            UpdateRemain();
            timer = new Timer(1000);
            timer.Elapsed += OnTimerElapsed;
            timer.AutoReset = true;
            if (remain > TimeSpan.Zero)
            {
                timer.Start();
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            UpdateRemain();
            if (timer != null && remain > TimeSpan.Zero && !timer.Enabled)
            {
                timer.Start();
            }
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            UpdateRemain();
            await InvokeAsync(StateHasChanged);
            if (remain <= TimeSpan.Zero)
            {
                timer?.Stop();
                if (OnFinish.HasDelegate)
                {
                    await OnFinish.InvokeAsync(null);
                }
            }
        }

        private void UpdateRemain()
        {
            remain = Value - DateTime.Now;
            if (remain < TimeSpan.Zero)
            {
                remain = TimeSpan.Zero;
            }
        }

        private string FormatRemain()
        {
            if (string.IsNullOrWhiteSpace(Format))
            {
                return remain.ToString();
            }

            // EP 口径（实抓 packages/components/countdown/src/utils.ts 的 formatTime）：
            // ① 单位从 Y/M/D/H/m/s/S 依次「吃掉余数」，所以同时写 DD 与 HH 时 HH 是去掉整天后的余下小时；
            // ② 替换值按 token 连续长度补零（"DD"→03、"HHH"→007）；
            // ③ [ ] 包住的文字是字面量，不参与替换，最后统一剥掉方括号。
            var text = Format;
            var left = (long)remain.TotalMilliseconds;

            foreach (var (token, unit) in TimeUnits)
            {
                var runs = FindUnbracketedRuns(text, token);
                if (runs.Count == 0) continue;

                var value = left / unit;
                left -= value * unit;
                var rendered = value.ToString(CultureInfo.InvariantCulture);

                // 从后往前替换，避免前面的下标被改动过的串错位。
                for (var i = runs.Count - 1; i >= 0; i--)
                {
                    var (start, length) = runs[i];
                    text = text.Remove(start, length).Insert(start, rendered.PadLeft(length, '0'));
                }
            }

            return LiteralBracket.Replace(text, "$1");
        }

        // 与 EP 的 escapeRegex 同一条：/\[([^\]]*)]/g
        private static readonly Regex LiteralBracket = new(@"\[([^\]]*)]", RegexOptions.Compiled);

        /// <summary>
        /// EP 的 timeUnits 表原样搬过来（年按 365 天、月按 30 天，是官方自己的近似口径）。
        /// </summary>
        private static readonly (char Token, long UnitMs)[] TimeUnits =
        {
            ('Y', 365L * 24 * 60 * 60 * 1000),
            ('M', 30L * 24 * 60 * 60 * 1000),
            ('D', 24L * 60 * 60 * 1000),
            ('H', 60L * 60 * 1000),
            ('m', 60L * 1000),
            ('s', 1000L),
            ('S', 1L),
        };

        private static List<(int Start, int Length)> FindUnbracketedRuns(string text, char token)
        {
            var runs = new List<(int, int)>();
            var inside = false;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '[')
                {
                    inside = true;
                    continue;
                }
                if (c == ']')
                {
                    inside = false;
                    continue;
                }
                if (inside || c != token) continue;

                var start = i;
                while (i + 1 < text.Length && text[i + 1] == token) i++;
                runs.Add((start, i - start + 1));
            }
            return runs;
        }

        public override void Dispose()
        {
            if (timer != null)
            {
                timer.Elapsed -= OnTimerElapsed;
                timer.Dispose();
            }

            base.Dispose();
        }
    }
}
