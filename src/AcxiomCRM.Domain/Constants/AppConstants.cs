namespace AcxiomCRM.Domain.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesExecutive = "SalesExecutive";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Manager, SalesExecutive };
}

public static class AuditActions
{
    public const string Login = "Login";
    public const string FailedLogin = "FailedLogin";
    public const string Logout = "Logout";
    public const string Lockout = "Lockout";
    public const string Unlock = "Unlock";
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string Deactivate = "Deactivate";
    public const string Convert = "Convert";
    public const string StatusChange = "StatusChange";
    public const string RoleChange = "RoleChange";
    public const string PasswordReset = "PasswordReset";
    public const string SecurityAction = "SecurityAction";
}
