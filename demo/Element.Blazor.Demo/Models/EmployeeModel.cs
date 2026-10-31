namespace ElementPlus.Blazor.Demo;

/// <summary>表单对话框回传的示例模型。</summary>
public class EmployeeModel
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Department { get; set; }
    public bool OnDuty { get; set; } = true;

    public override string ToString()
        => $"{Name}（{Age}岁，{(string.IsNullOrWhiteSpace(Department) ? "未选部门" : Department)}，{(OnDuty ? "在职" : "离职")}）";
}
