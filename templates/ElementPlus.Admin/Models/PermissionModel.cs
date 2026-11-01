namespace ElementPlus.Blazor.Demo;

/// <summary>权限管理页的示例模型（实际项目换成你的权限 DTO / API 返回）。</summary>
public class PermissionModel
{
    /// <summary>权限类型：菜单 / 按钮 / 接口。</summary>
    public string Type { get; set; } = "菜单";

    public string Name { get; set; }
    public string Code { get; set; }
    public bool Enabled { get; set; } = true;
}
