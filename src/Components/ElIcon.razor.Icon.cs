using System;

namespace ElementPlus
{
    /// <summary>
    /// Element UI 字体图标枚举。值名使用 PascalCase，自动转换为 kebab-case CSS 类名。
    /// 例如 Icon.Close → CSS class "el-icon-close"
    /// </summary>
    public enum Icon
    {
        /// <summary>
        /// No glyph. This is the default value so that <c>&lt;ElIcon /&gt;</c> without an
        /// explicit Icon / Name renders nothing (previously the zero value was PlatformEleme,
        /// which made every unspecified icon silently render the eleme logo).
        /// </summary>
        None = 0,

        // —— 通用 ——
        PlatformEleme,
        User,
        UserFilled,
        Avatar,
        Loading,
        Loading2,
        InfoFilled,
        Info,
        SuccessFilled,
        Success,
        WarningFilled,
        Warning,
        QuestionFilled,
        Question,
        CirclePlusOutline,
        Close,
        CloseBold,
        Check,
        CheckBold,
        Plus,
        Minus,
        Remove,
        CirclePlus,
        CircleCheck,
        CircleClose,
        CircleCloseFilled,
        CircleCheckFilled,

        // —— 编辑 ——
        Edit,
        EditOutline,
        EditPen,
        DocumentAdd,
        DocumentCopy,
        DocumentDelete,
        DocumentRemove,
        Document,
        FolderAdd,
        FolderDelete,
        Folder,
        FolderOpened,

        // —— 数据 ——
        DataLine,
        DataBoard,
        DataAnalysis,
        Histogram,
        PieChart,
        TrendCharts,
        Odometer,

        // —— 媒体 ——
        VideoPlay,
        VideoPause,
        VideoCamera,
        VideoCameraFilled,
        SwitchButton,
        Picture,
        PictureFilled,
        Camera,
        CameraFilled,
        Headset,
        Microphone,
        MicrophoneFilled,
        Bell,
        BellFilled,

        // —— 方向 / 导航 ——
        ArrowLeft,
        ArrowRight,
        ArrowUp,
        ArrowDown,
        DArrowLeft,
        DArrowRight,
        DArrowUp,
        DArrowDown,
        Top,
        TopLeft,
        TopRight,
        Bottom,
        BottomLeft,
        BottomRight,
        Back,
        Right,
        Refresh,
        RefreshLeft,
        RefreshRight,

        // —— 操作 ——
        Search,
        Filter,
        FilterFilled,
        Sort,
        Menu,
        MenuFold,
        MenuUnfold,
        Expand,
        Fold,
        FullScreen,
        Aim,
        Rank,
        Select,
        SelectFilled,

        // —— 安全 / 设置 ——
        Lock,
        Unlock,
        Setting,
        Tools,
        Operation,
        Connection,
        Link,
        Unlink,
        Share,
        Promotion,
        Money,
        CreditCard,
        Wallet,

        // —— 删除 / 危险 ——
        Delete,
        DeleteFilled,
        DeleteOutline,

        // —— 时钟 / 日历 ——
        Clock,
        Timer,
        Calendar,
        CalendarFilled,
        Time,

        // —— 通讯 ——
        Message,
        MessageFilled,
        ChatDotRound,
        ChatLineRound,
        Phone,
        Iphone,
        MobilePhone,
        Postcard,
        Location,
        LocationFilled,
        Place,
        LocationInformation,

        // —— 办公 ——
        OfficeBuilding,
        House,
        HouseFilled,
        Shop,
        ShopFilled,
        Goods,
        ShoppingCart,
        ShoppingCartFull,
        ShoppingBag,
        School,
        Reading,
        ReadingActive,
        Notebooks,
        Notebook,
        Tickets,
        Trophy,
        Medal,

        // —— 其他 ——
        Cpu,
        Mouse,
        Monitor,
        MonitorFilled,
        Keyboard,
        KeyboardFilled,
        Printer,
        Scissors,
        Paperclip,
        MagicStick,
        MagicStickFilled,
        Stamp,
        StampFilled,
        PictureOutline,
        NoData,
        RemoveFilled,
        RefreshRightBold
    }
}