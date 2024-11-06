using System.Security.Principal;

namespace WpfAutoDISample.Common;

internal static class SysHelper
{
    internal static bool IsCurrentUserAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}