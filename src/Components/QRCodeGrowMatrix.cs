using System;
using System.Collections.Generic;

namespace ElementPlus
{
    /// <summary>
    /// 二维码符号矩阵。包含最终的模块明暗结果，以及在编码过程中确定的版本、纠错等级、
    /// 掩码图样与数据段划分，便于调用方做容量核算与问题排查。
    /// </summary>
    public sealed class QRCodeGrowMatrix
    {
        private static readonly IReadOnlyList<QRCodeGrowSegment> EmptySegments = Array.Empty<QRCodeGrowSegment>();

        /// <summary>
        /// 初始化二维码符号矩阵。
        /// </summary>
        /// <param name="modules">模块明暗矩阵，<c>true</c> 表示深色。</param>
        /// <param name="version">符号版本（1-40）。</param>
        /// <param name="ecc">纠错等级。</param>
        /// <param name="maskPattern">实际使用的掩码图样编号（0-7）。</param>
        /// <param name="segments">数据段划分。</param>
        public QRCodeGrowMatrix(
            bool[,] modules,
            int version,
            ElQRCodeGrowEcc ecc,
            int maskPattern,
            IReadOnlyList<QRCodeGrowSegment> segments)
        {
            Modules = modules ?? throw new ArgumentNullException(nameof(modules));
            Version = version;
            Ecc = ecc;
            MaskPattern = maskPattern;
            Segments = segments ?? EmptySegments;
        }

        /// <summary>
        /// 模块明暗矩阵，<c>true</c> 表示深色。索引顺序为 <c>[行, 列]</c>，不含静区。
        /// </summary>
        public bool[,] Modules { get; }

        /// <summary>
        /// 符号版本（1-40）。版本 n 的边长为 <c>17 + 4 * n</c> 个模块。
        /// </summary>
        public int Version { get; }

        /// <summary>
        /// 纠错等级。
        /// </summary>
        public ElQRCodeGrowEcc Ecc { get; }

        /// <summary>
        /// 实际使用的掩码图样编号（0-7）。
        /// </summary>
        public int MaskPattern { get; }

        /// <summary>
        /// 数据段划分。
        /// </summary>
        public IReadOnlyList<QRCodeGrowSegment> Segments { get; }

        /// <summary>
        /// 矩阵边长（模块数，不含静区）。
        /// </summary>
        public int Size => Modules.GetLength(0);

        /// <summary>
        /// 读取指定行列的模块明暗值。越界返回 <c>false</c>。
        /// </summary>
        /// <param name="row">行索引。</param>
        /// <param name="column">列索引。</param>
        /// <returns>深色返回 <c>true</c>。</returns>
        public bool IsDark(int row, int column)
        {
            if (row < 0 || column < 0 || row >= Size || column >= Size)
            {
                return false;
            }

            return Modules[row, column];
        }

        /// <summary>
        /// 深色模块数量。
        /// </summary>
        /// <returns>深色模块数量。</returns>
        public int CountDarkModules()
        {
            var count = 0;
            var n = Size;

            for (var r = 0; r < n; r++)
            {
                for (var c = 0; c < n; c++)
                {
                    if (Modules[r, c])
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// 数据段摘要，形如 <c>Numeric[0,4) + Byte[4,9)</c>；无数据段时为 <c>(empty)</c>。
        /// </summary>
        public string SegmentText
        {
            get
            {
                if (Segments.Count == 0)
                {
                    return "(empty)";
                }

                var parts = new List<string>(Segments.Count);
                foreach (var segment in Segments)
                {
                    parts.Add(segment.ToString());
                }

                return string.Join(" + ", parts);
            }
        }

        /// <summary>
        /// 返回符号摘要，形如 <c>版本 2（25×25）· M 级 · 掩码 5 · Numeric[0,4)</c>。
        /// </summary>
        /// <returns>符号摘要文本。</returns>
        public override string ToString()
        {
            var eccName = Ecc switch
            {
                ElQRCodeGrowEcc.Low => "L",
                ElQRCodeGrowEcc.Medium => "M",
                ElQRCodeGrowEcc.Quartile => "Q",
                _ => "H"
            };

            return $"版本 {Version}（{Size}×{Size}）· {eccName} 级 · 掩码 {MaskPattern} · {SegmentText}";
        }
    }
}
