namespace ElementPlus.Blazor.Demo;

/// <summary>角色管理页的示例模型（实际项目换成你的角色 DTO / API 返回）。</summary>
public class RoleModel
{
    public string Name { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public bool Enabled { get; set; } = true;
}
