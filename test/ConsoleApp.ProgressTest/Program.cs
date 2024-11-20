// See https://aka.ms/new-console-template for more information

using EasilyNET.Core.Language;
using EasilyNET.Core.Misc;

/// <summary>
/// Program
/// </summary>
internal static class Program
{
    private static CancellationTokenSource? _cts;

    // ReSharper disable once UnusedMember.Global
    /// <summary>
    /// Main
    /// </summary>
    /// <param name="args"></param>
    public static async Task Main(string[] args)
    {
        _cts = new();
        Console.CancelKeyPress += (_, _) => _cts.Cancel();
        await MainAsync(args, _cts.Token);
    }

    private static async Task MainAsync(string[] _, CancellationToken token)
    {
        Console.WriteLine(Console.WindowWidth);
        await Task.Delay(3000, token);
        const double totalSize = 10240; // 总大小，单位：KB
        const int totalCount = 100;     // 总文件数量
        double downloadSize = 0;        // 已下载大小，单位：KB
        var _downloadCount = 0;         // 已下载文件数量
        foreach (var item in ..100)
        {
            await Task.Delay(100, token);
            downloadSize += totalSize / 100; // 每次循环增加已下载大小
            _downloadCount++;                // 每次循环增加已下载文件数量
            var msg = $"{Math.Round(downloadSize / 1024.0, 3, MidpointRounding.AwayFromZero)}/{Math.Round(totalSize / 1024.0, 3, MidpointRounding.AwayFromZero)} MB, 剩余文件数量: {totalCount - _downloadCount}";
            await Console.Out.ShowProgressBarAsync(item, msg, 50, isFixedBarWidth: true);
        }
        Console.ReadKey();
    }
}