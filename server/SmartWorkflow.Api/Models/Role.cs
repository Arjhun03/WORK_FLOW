namespace SmartWorkflow.Api.Models;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";
    public const string Manager = "Manager";
    public const string HR = "HR";
    public const string IT = "IT";
    public const string Finance = "Finance";

    public static readonly string[] All = { Admin, Employee, Manager, HR, IT, Finance };
}
