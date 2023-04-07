using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace Volksempfaenger;

public class Ffmpeg
{
    private FfmpegConfiguration _configuration;

    public Ffmpeg(IOptions<FfmpegConfiguration> settings)
    {
        _configuration = settings.Value;
    }

    public Process CreateProcess(string path)
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = _configuration.ProcessPath,
            Arguments = string.Format(_configuration.Arguments, path),
            UseShellExecute = _configuration.UseShellExecute,
            RedirectStandardOutput = _configuration.RedirectStandardOutput
        });
    }
}