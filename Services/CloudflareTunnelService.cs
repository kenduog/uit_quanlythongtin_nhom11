using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Nhom11.Services;

/// <summary>
/// Dev-only: tự bật Cloudflare Tunnel khi app khởi động và in link public ra console
/// (chạy app 1 lần là có link để copy). Tự tắt cloudflared khi app dừng.
/// Console output để ASCII thuần để không bị mojibake.
/// </summary>
public class CloudflareTunnelService : IHostedService
{
    private static readonly Regex LinkRegex =
        new(@"https://[a-z0-9-]+\.trycloudflare\.com", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IServer _server;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<CloudflareTunnelService> _logger;
    private Process? _proc;
    private bool _printed;

    public CloudflareTunnelService(IServer server, IHostApplicationLifetime lifetime, ILogger<CloudflareTunnelService> logger)
    {
        _server = server;
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Chạy sau khi Kestrel đã bind xong (mới biết địa chỉ thật).
        _lifetime.ApplicationStarted.Register(StartTunnel);
        _lifetime.ApplicationStopping.Register(StopTunnel);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopTunnel();
        return Task.CompletedTask;
    }

    private void StartTunnel()
    {
        Note("[TUNNEL] Dang tim cloudflared...");
        var exe = ResolveCloudflared();
        if (exe is null)
        {
            Note("[TUNNEL] Khong tim thay cloudflared - bo qua tunnel. Cai bang: winget install Cloudflare.cloudflared");
            return;
        }
        Note($"[TUNNEL] Dung cloudflared: {exe}");

        var url = GetHttpAddress();

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("tunnel");
        psi.ArgumentList.Add("--url");
        psi.ArgumentList.Add(url);

        _proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _proc.OutputDataReceived += OnOutput;
        _proc.ErrorDataReceived += OnOutput; // cloudflared in banner ra stderr

        try
        {
            _proc.Start();
            _proc.BeginOutputReadLine();
            _proc.BeginErrorReadLine();
            Note($"[TUNNEL] Da khoi dong cloudflared -> {url}. Dang cho link cong khai...");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[TUNNEL] Khong khoi dong duoc cloudflared.");
            Note("[TUNNEL] Khong khoi dong duoc cloudflared: " + ex.Message);
        }
    }

    // In ra ca Console (stdout - chac chan hien o Debug Console) lan ILogger.
    private void Note(string message)
    {
        Console.WriteLine(message);
        _logger.LogInformation("{Message}", message);
    }

    private void OnOutput(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data) || _printed) return;
        var match = LinkRegex.Match(e.Data);
        if (!match.Success) return;

        _printed = true;
        var line = new string('=', 70);
        Console.WriteLine();
        Console.WriteLine(line);
        Console.WriteLine($"  LINK CHIA SE (copy gui moi nguoi): {match.Value}");
        Console.WriteLine("  Mo la xem duoc ngay - khong dang nhap, khong trang chan.");
        Console.WriteLine(line);
        Console.WriteLine();
    }

    private void StopTunnel()
    {
        try
        {
            if (_proc is { HasExited: false })
            {
                _proc.Kill(entireProcessTree: true);
            }
        }
        catch { /* ignore */ }
    }

    private string GetHttpAddress()
    {
        var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses;
        var http = addresses?.FirstOrDefault(a => a.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(http)) return "http://localhost:5255";
        return http.Replace("0.0.0.0", "localhost").Replace("[::]", "localhost");
    }

    private static string? ResolveCloudflared()
    {
        // 1) Trong PATH cua tien trinh
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var f = Path.Combine(dir.Trim(), "cloudflared.exe");
                if (File.Exists(f)) return f;
            }
            catch { /* duong dan loi - bo qua */ }
        }

        // 2) Vi tri cai pho bien (cloudflared msi cai vao Program Files (x86)\cloudflared)
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (var c in new[]
        {
            Path.Combine(programFilesX86, @"cloudflared\cloudflared.exe"),
            Path.Combine(programFiles, @"cloudflared\cloudflared.exe"),
            Path.Combine(programFiles, @"Cloudflare\Cloudflared\cloudflared.exe"),
            Path.Combine(local, @"Microsoft\WinGet\Links\cloudflared.exe"),
            @"C:\ProgramData\chocolatey\bin\cloudflared.exe",
            Path.Combine(userProfile, @"scoop\shims\cloudflared.exe"),
        })
        {
            if (!string.IsNullOrEmpty(c) && File.Exists(c)) return c;
        }

        // 3) Quet thu muc Packages cua winget
        var pkgRoot = Path.Combine(local, @"Microsoft\WinGet\Packages");
        if (Directory.Exists(pkgRoot))
        {
            try
            {
                var f = Directory.EnumerateFiles(pkgRoot, "cloudflared.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (f != null) return f;
            }
            catch { /* ignore */ }
        }

        // 4) Hoi 'where.exe' (theo PATH thuc cua he thong)
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = "cloudflared",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                var outp = p.StandardOutput.ReadToEnd();
                p.WaitForExit(3000);
                var first = outp.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.EndsWith("cloudflared.exe", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(first) && File.Exists(first)) return first;
            }
        }
        catch { /* ignore */ }

        return null;
    }
}
