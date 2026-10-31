using System;
using System.Collections.Generic;
using System.Text;

namespace ElementPlus
{
    /// <summary>
    /// 验证码服务契约：生成一道「算术题」验证码（SVG 图片 + 题面 + 答案），并支持按 captchaId 校验用户输入。
    /// <para>答案默认保存在内存字典中（按 captchaId 索引），校验成功后即销毁该条目，避免重复提交。</para>
    /// </summary>
    public interface ICaptchaService
    {
        /// <summary>生成一道算术验证码。</summary>
        /// <param name="complexity">噪点复杂度（默认 5，即基准噪点量；0 = 无噪点）。</param>
        /// <param name="width">SVG 宽度（像素）。</param>
        /// <param name="height">SVG 高度（像素）。</param>
        System.Threading.Tasks.Task<CaptchaResult> GenerateMathCaptchaAsync(int complexity = 5, int width = 200, int height = 60);

        /// <summary>校验用户输入是否与指定 captchaId 的答案一致；一致则销毁该验证码。</summary>
        System.Threading.Tasks.Task<bool> ValidateCaptchaAsync(string userInput, string captchaId);
    }

    /// <summary>一次验证码生成的产物。</summary>
    public class CaptchaResult
    {
        /// <summary>本次验证码的唯一标识，用于后续校验。</summary>
        public string CaptchaId { get; set; }

        /// <summary>可直接用于 <c>&lt;img src&gt;</c> 的 SVG data URL（<c>data:image/svg+xml;base64,...</c>）。</summary>
        public string ImageData { get; set; }

        /// <summary>题面文本（如 <c>12 加 3 = </c>）。</summary>
        public string Question { get; set; }

        /// <summary>正确答案（数字字符串）。</summary>
        public string Answer { get; set; }
    }

    /// <summary>
    /// 自包含的算术验证码服务（零第三方依赖）。
    /// <para>
    /// 题目为「加 / 减 / 乘 / 除」四则运算（中文运算符），答案恒为非负整数；
    /// 图片为程序生成的 SVG（渐变底 + 逐个字符随机变形 + 多类干扰线 / 点 / 几何图形）。
    /// <b>注意</b>：答案保存在进程内字典中，仅适用于演示与前端自校验；生产环境请把生成与校验放到服务端并替换为不可逆存储。
    /// </para>
    /// </summary>
    public class VerifyCodeService : ICaptchaService
    {
        private readonly Random _random = new Random();
        private readonly Dictionary<string, string> _captchaStore = new Dictionary<string, string>();

        /// <inheritdoc/>
        public System.Threading.Tasks.Task<CaptchaResult> GenerateMathCaptchaAsync(int complexity = 5, int width = 200, int height = 60)
        {
            var (question, answer) = GenerateMathQuestion();
            var imageData = GenerateSvgCaptcha(question, complexity, width, height);
            var captchaId = Guid.NewGuid().ToString();

            _captchaStore[captchaId] = answer;

            return System.Threading.Tasks.Task.FromResult(new CaptchaResult
            {
                CaptchaId = captchaId,
                ImageData = imageData,
                Question = question,
                Answer = answer
            });
        }

        /// <inheritdoc/>
        public System.Threading.Tasks.Task<bool> ValidateCaptchaAsync(string userInput, string captchaId)
        {
            if (_captchaStore.TryGetValue(captchaId, out var correctAnswer))
            {
                _captchaStore.Remove(captchaId);
                return System.Threading.Tasks.Task.FromResult(
                    string.Equals((userInput ?? string.Empty).Trim(), correctAnswer, StringComparison.OrdinalIgnoreCase));
            }

            return System.Threading.Tasks.Task.FromResult(false);
        }

        /// <summary>把基准噪点数量按复杂度换算：complexity=5 时保持基准，0 时归零。</summary>
        private int Scale(int baseCount, int complexity) =>
            (int)Math.Round(baseCount * Math.Max(0, complexity) / 5.0);

        /// <summary>随机出一道四则运算题，返回（题面, 答案）。</summary>
        public (string Question, string Answer) GenerateMathQuestion()
        {
            var operators = new[] { "加", "减", "乘", "除" };
            var operatorChosen = operators[_random.Next(operators.Length)];

            int num1 = _random.Next(1, 21);
            int num2 = _random.Next(1, 21);

            string question = string.Empty;
            string answer = string.Empty;

            switch (operatorChosen)
            {
                case "加":
                    question = $"{num1} 加 {num2} = ";
                    answer = (num1 + num2).ToString();
                    break;
                case "减":
                    while (num1 < num2)
                    {
                        num1 = _random.Next(1, 100);
                        num2 = _random.Next(1, 100);
                    }
                    question = $"{num1} 减 {num2} = ";
                    answer = (num1 - num2).ToString();
                    break;
                case "乘":
                    question = $"{num1} 乘 {num2} = ";
                    answer = (num1 * num2).ToString();
                    break;
                case "除":
                    while (num1 % num2 != 0)
                    {
                        num1 = _random.Next(1, 21);
                        num2 = _random.Next(1, 21);
                    }
                    question = $"{num1} 除 {num2} = ";
                    answer = (num1 / num2).ToString();
                    break;
            }

            return (question, answer);
        }

        private string GenerateSvgCaptcha(string text, int complexity, int width, int height)
        {
            var svgContent = $@"
<svg width='{width}' height='{height}' xmlns='http://www.w3.org/2000/svg'>
  <defs>
    <linearGradient id='bg' x1='0%' y1='0%' x2='100%' y2='100%'>
      <stop offset='0%' style='stop-color:#f0f8ff;stop-opacity:1' />
      <stop offset='100%' style='stop-color:#e6f3ff;stop-opacity:1' />
    </linearGradient>
  </defs>
  <rect width='100%' height='100%' fill='url(#bg)'/>
  {GenerateDistortedText(text, width, height)}
  {GenerateInterferenceLines(complexity, width, height)}
  {GenerateComplexInterferenceLines(complexity, width, height)}
  {GenerateCurvedLines(complexity, width, height)}
  {GenerateDottedLines(complexity, width, height)}
  {GenerateRandomShapes(complexity, width, height)}
  {GenerateColorfulInterferenceLines(complexity, width, height)}
  {GenerateComplexCurvedLines(complexity, width, height)}
  {GenerateRandomDots(complexity, width, height)}
  {GenerateGeometricShapes(complexity, width, height)}
  {GenerateWaveLines(complexity, width, height)}
</svg>";

            return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svgContent));
        }

        /// <summary>
        /// 生成逐个字符随机变形的题面文本。
        /// <para>
        /// 整行文本<b>动态居中</b>：先按每个字符的实际占位宽度（含字间距、运算符额外间距）求出总宽，
        /// 令其中心落在 SVG 水平中线（水平居中）；再按 SVG 高度与最大字号计算基线位置
        /// （数字/大写字母的视觉中心约在基线上方 0.35em），令其落在 SVG 垂直中线（垂直居中）。
        /// 因此无论 <c>CaptchaWidth</c> / <c>CaptchaHeight</c> 如何调整，题面都稳定居中，不会靠下。
        /// </para>
        /// </summary>
        private string GenerateDistortedText(string text, int width, int height)
        {
            double CharWidth(char c, double fontSize)
            {
                if (c >= '0' && c <= '9') return fontSize * 0.58;
                if (c == '=') return fontSize * 0.62;
                if (c == ' ') return fontSize * 0.28;
                return fontSize * 0.95;
            }

            // 第一遍：确定每个字符的随机字形参数，并累加整行总宽（含运算符额外间距）。
            var glyphs = new List<(char Ch, double W, double FontSize, double Sx, double Sy, int Rot, int OffY, bool IsOp, double LeadGap)>();
            double totalWidth = 0;
            double maxFontSize = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                bool isOp = ch == '加' || ch == '减' || ch == '乘' || ch == '除' || ch == '=';
                double fontSize = _random.Next(18, 23);
                double sx = 0.9 + _random.NextDouble() * 0.2;
                double sy = 0.9 + _random.NextDouble() * 0.2;
                int rot = _random.Next(-12, 13);
                int offY = _random.Next(-3, 4);
                double leadGap = isOp ? 7 : 0;
                double w = CharWidth(ch, fontSize) * sx;

                glyphs.Add((ch, w, fontSize, sx, sy, rot, offY, isOp, leadGap));
                totalWidth += leadGap + w + (isOp ? 11 : 3);
                if (fontSize > maxFontSize)
                {
                    maxFontSize = fontSize;
                }
            }

            // 水平居中：让整行占位宽度的中心与 SVG 水平中线重合。
            double x = Math.Max(0, (width - totalWidth) / 2.0);

            // 垂直居中：数字/大写字母视觉中心 ≈ 基线上方 0.35em，令其等于 SVG 垂直中线。
            double baseY = height / 2.0 + maxFontSize * 0.35;

            var sb = new StringBuilder();
            foreach (var g in glyphs)
            {
                x += g.LeadGap;
                double cx = x + g.W / 2;
                int cy = (int)Math.Round(baseY + g.OffY);

                sb.AppendLine($@"
<text x='{cx:F1}' y='{cy}' font-family='Arial, sans-serif'
      font-size='{g.FontSize}' font-weight='bold' text-anchor='middle'
      transform='translate({cx:F1},{cy}) rotate({g.Rot}) scale({g.Sx:F2},{g.Sy:F2}) translate({-cx:F1},{-cy})'
      fill='#2c3e50'>{g.Ch}</text>");

                x += g.W + (g.IsOp ? 11 : 3);
            }

            return sb.ToString();
        }

        private string GenerateColorfulInterferenceLines(int complexity, int width, int height)
        {
            var lines = new StringBuilder();
            var colorPalettes = new[]
            {
                "#e74c3c", "#3498db", "#2ecc71", "#f39c12", "#9b59b6", "#1abc9c",
                "#e67e22", "#d35400", "#c0392b", "#8e44ad", "#2980b9", "#27ae60"
            };

            int count = Scale(12, complexity);
            for (int i = 0; i < count; i++)
            {
                var x1 = _random.Next(width);
                var y1 = _random.Next(height);
                var x2 = _random.Next(width);
                var y2 = _random.Next(height);
                var strokeWidth = _random.Next(1, 4);
                var opacity = _random.Next(15, 50) / 100.0;
                var color = colorPalettes[_random.Next(colorPalettes.Length)];

                lines.AppendLine($@"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='{color}' stroke-width='{strokeWidth}' opacity='{opacity}'/>");
            }

            return lines.ToString();
        }

        private string GenerateComplexCurvedLines(int complexity, int width, int height)
        {
            var curves = new StringBuilder();
            var colors = new[] { "#e74c3c", "#3498db", "#2ecc71", "#f39c12", "#9b59b6" };

            int count = Scale(6, complexity);
            for (int i = 0; i < count; i++)
            {
                var startX = _random.Next(width);
                var startY = _random.Next(height);
                var controlX1 = _random.Next(width);
                var controlY1 = _random.Next(height);
                var controlX2 = _random.Next(width);
                var controlY2 = _random.Next(height);
                var endX = _random.Next(width);
                var endY = _random.Next(height);
                var opacity = _random.Next(10, 35) / 100.0;
                var color = colors[_random.Next(colors.Length)];

                curves.AppendLine($@"<path d='M {startX} {startY} C {controlX1} {controlY1}, {controlX2} {controlY2}, {endX} {endY}' fill='none' stroke='{color}' stroke-width='1.5' opacity='{opacity}'/>");
            }

            return curves.ToString();
        }

        private string GenerateRandomDots(int complexity, int width, int height)
        {
            var dots = new StringBuilder();

            int count = Scale(25, complexity);
            for (int i = 0; i < count; i++)
            {
                var x = _random.Next(width);
                var y = _random.Next(height);
                var size = _random.Next(1, 3);
                var opacity = _random.Next(5, 20) / 100.0;
                var colors = new[] { "#e74c3c", "#3498db", "#2ecc71", "#f39c12", "#9b59b6" };
                var color = colors[_random.Next(colors.Length)];

                dots.AppendLine($@"<circle cx='{x}' cy='{y}' r='{size}' fill='{color}' opacity='{opacity}'/>");
            }

            return dots.ToString();
        }

        private string GenerateGeometricShapes(int complexity, int width, int height)
        {
            var shapes = new StringBuilder();
            var colors = new[] { "#e74c3c", "#3498db", "#2ecc71", "#f39c12", "#9b59b6", "#1abc9c" };

            int count = Scale(15, complexity);
            for (int i = 0; i < count; i++)
            {
                var x = _random.Next(width);
                var y = _random.Next(height);
                var size = _random.Next(2, 6);
                var opacity = _random.Next(8, 25) / 100.0;
                var color = colors[_random.Next(colors.Length)];

                switch (_random.Next(4))
                {
                    case 0:
                        shapes.AppendLine($@"<circle cx='{x}' cy='{y}' r='{size}' fill='{color}' opacity='{opacity}'/>");
                        break;
                    case 1:
                        shapes.AppendLine($@"<rect x='{x}' y='{y}' width='{size}' height='{size}' fill='{color}' opacity='{opacity}'/>");
                        break;
                    case 2:
                        shapes.AppendLine($@"<polygon points='{x},{y} {x + size},{y + size} {x - size},{y + size}' fill='{color}' opacity='{opacity}'/>");
                        break;
                    case 3:
                        var halfSize = size / 2.0;
                        shapes.AppendLine($@"<polygon points='{x},{y - halfSize} {x + halfSize},{y} {x},{y + halfSize} {x - halfSize},{y}' fill='{color}' opacity='{opacity}'/>");
                        break;
                }
            }

            return shapes.ToString();
        }

        private string GenerateWaveLines(int complexity, int width, int height)
        {
            var waves = new StringBuilder();
            var colors = new[] { "#e74c3c", "#3498db", "#2ecc71", "#f39c12" };

            int count = Scale(4, complexity);
            for (int i = 0; i < count; i++)
            {
                var startY = _random.Next(height);
                var amplitude = _random.Next(5, 15);
                var frequency = _random.Next(2, 6);
                var color = colors[_random.Next(colors.Length)];
                var opacity = _random.Next(10, 30) / 100.0;

                var pathData = new StringBuilder();
                pathData.Append($"M 0 {startY}");

                for (int x = 5; x < width; x += 5)
                {
                    var y = startY + (int)(amplitude * Math.Sin(x * frequency * 0.1));
                    pathData.Append($" L {x} {y}");
                }

                waves.AppendLine($@"<path d='{pathData}' fill='none' stroke='{color}' stroke-width='1' opacity='{opacity}'/>");
            }

            return waves.ToString();
        }

        private string GenerateInterferenceLines(int complexity, int width, int height)
        {
            var lines = new StringBuilder();

            int count = Scale(4, complexity);
            for (int i = 0; i < count; i++)
            {
                var x1 = _random.Next(width);
                var y1 = _random.Next(height);
                var x2 = _random.Next(width);
                var y2 = _random.Next(height);
                var opacity = _random.Next(30, 70) / 100.0;
                lines.AppendLine($@"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='#adb5bd' stroke-width='1' opacity='{opacity}'/>");
            }
            return lines.ToString();
        }

        private string GenerateComplexInterferenceLines(int complexity, int width, int height)
        {
            var lines = new StringBuilder();

            int count = Scale(6, complexity);
            for (int i = 0; i < count; i++)
            {
                var x1 = _random.Next(width);
                var y1 = _random.Next(height);
                var x2 = _random.Next(width);
                var y2 = _random.Next(height);
                var strokeWidth = _random.Next(1, 3);
                var opacity = _random.Next(20, 60) / 100.0;

                var colors = new[] { "#6c757d", "#495057", "#adb5bd", "#343a40" };
                var color = colors[_random.Next(colors.Length)];

                lines.AppendLine($@"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='{color}' stroke-width='{strokeWidth}' opacity='{opacity}'/>");
            }

            return lines.ToString();
        }

        private string GenerateCurvedLines(int complexity, int width, int height)
        {
            var curves = new StringBuilder();

            int count = Scale(3, complexity);
            for (int i = 0; i < count; i++)
            {
                var startX = _random.Next(width);
                var startY = _random.Next(height);
                var controlX1 = _random.Next(width);
                var controlY1 = _random.Next(height);
                var controlX2 = _random.Next(width);
                var controlY2 = _random.Next(height);
                var endX = _random.Next(width);
                var endY = _random.Next(height);
                var opacity = _random.Next(15, 45) / 100.0;

                curves.AppendLine($@"<path d='M {startX} {startY} C {controlX1} {controlY1}, {controlX2} {controlY2}, {endX} {endY}' fill='none' stroke='#868e96' stroke-width='1' opacity='{opacity}'/>");
            }

            return curves.ToString();
        }

        private string GenerateDottedLines(int complexity, int width, int height)
        {
            var dots = new StringBuilder();

            int count = Scale(4, complexity);
            for (int i = 0; i < count; i++)
            {
                var x1 = _random.Next(width);
                var y1 = _random.Next(height);
                var x2 = _random.Next(width);
                var y2 = _random.Next(height);
                var opacity = _random.Next(10, 30) / 100.0;

                dots.AppendLine($@"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='#495057' stroke-width='1' stroke-dasharray='2,2' opacity='{opacity}'/>");
            }

            return dots.ToString();
        }

        private string GenerateRandomShapes(int complexity, int width, int height)
        {
            var shapes = new StringBuilder();

            int count = Scale(8, complexity);
            for (int i = 0; i < count; i++)
            {
                var x = _random.Next(width);
                var y = _random.Next(height);
                var size = _random.Next(3, 8);
                var opacity = _random.Next(10, 25) / 100.0;

                if (_random.Next(2) == 0)
                {
                    shapes.AppendLine($@"<circle cx='{x}' cy='{y}' r='{size}' fill='#6c757d' opacity='{opacity}'/>");
                }
                else
                {
                    shapes.AppendLine($@"<rect x='{x}' y='{y}' width='{size}' height='{size / 2}' fill='#495057' opacity='{opacity}'/>");
                }
            }

            return shapes.ToString();
        }
    }
}
