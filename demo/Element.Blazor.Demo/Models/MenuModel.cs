namespace ElementPlus.Blazor.Demo;

/// <summary>菜单管理页的示例模型（实际项目换成你的菜单树节点 DTO / API 返回）。</summary>
public class MenuModel
{
    public string Title { get; set; }
    public string Path { get; set; }
    public string Icon { get; set; }
    public string Parent { get; set; } = "顶级菜单";
    public int Sort { get; set; }
    public bool Enabled { get; set; } = true;
}
