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

    private static async Task MainAsync(string[] args, CancellationToken token)
    {
        foreach (var item in ..100)
        {
            await Task.Delay(100, token);
            await Console.Out.ShowProgressBarAsync(item, "正在处理中...");
        }
    }
}