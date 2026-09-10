namespace NuevoMMO.Server.Admin;

public sealed class AdminService
{
    public bool Has(AdminPermission required, AdminPermission actual) => actual >= required;
}
