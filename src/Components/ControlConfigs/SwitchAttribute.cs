namespace ElementPlus.ControlConfigs
{
    public class SwitchAttribute : BaseAttribute
    {
        public bool IsDisabled { get; set; }

        public string ActiveText { get; set; }

        public string InactiveText { get; set; }

        public string ActiveValue { get; set; }

        public string InactiveValue { get; set; }

        public bool Loading { get; set; }

        public string LoadingIcon { get; set; } = "el-icon-loading";
    }
}
