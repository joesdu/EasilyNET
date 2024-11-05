using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace WebApi.Test.Unit.Common;

internal static class SysHelper
{
    [SupportedOSPlatform(nameof(OSPlatform.Windows))]
    internal static bool IsCurrentUserAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}