#if Windows
using System.Runtime.Versioning;
using System.Security.Principal;
#endif

namespace WebApi.Test.Unit.Common;

internal static class SysHelper
{
#if Windows
    [SupportedOSPlatform("windows")]
    internal static bool IsCurrentUserAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
#endif
}