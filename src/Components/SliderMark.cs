namespace ElementPlus
{
    public class SliderMark
    {
        public double Value { get; set; }

        public string Label { get; set; }

        /// <summary>
        /// EP <c>SliderMarks</c> 中每个标记可单独设置的样式（内联 style 串，如 <c>color:#f56c6c;</c>）。
        /// 会拼接在定位样式（left / bottom）之后，因此可用于覆盖颜色、字重等表现属性。
        /// </summary>
        public string Style { get; set; }
    }
}
