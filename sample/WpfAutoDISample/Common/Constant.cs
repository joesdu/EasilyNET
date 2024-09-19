using System.IO;

namespace WpfAutoDISample.Common;

internal static class Constant
{
    /// <summary>
    /// UI配置服务Key
    /// </summary>
    internal const string UiConfigServiceKey = "UiConfig";

    /// <summary>
    /// AppCache服务Key
    /// </summary>
    internal const string AppCacheServiceKey = "AppCache";

    /// <summary>
    /// UI配置数据库
    /// </summary>
    internal const string UiConfigDB = "uiconfig.db";

    /// <summary>
    /// AppCache数据库
    /// </summary>
    internal const string AppCacheDB = "appcache.db";

    /// <summary>
    /// 获取用户数据目录
    /// </summary>
    /// <returns></returns>
    internal static string GetUserDataPath()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var appFolderPath = Path.Combine(documentsPath, "WpfAutoDISample");
        // 如果应用程序文件夹不存在，则创建它
        if (!Directory.Exists(appFolderPath)) Directory.CreateDirectory(appFolderPath);
        return appFolderPath;
    }
}