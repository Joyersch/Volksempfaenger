using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace Volksempfaenger;

public class Ffmpeg
{
    private string _programName;
    private FfmpegConfiguration _configuration;

    public Ffmpeg(IOptions<FfmpegConfiguration> settings)
    {
        _configuration = settings.Value;
        _programName = "ffmpeg";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _programName += ".exe";
    }

    public Process CreateProcess(string path)
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = _programName,
            Arguments = string.Format(_configuration.Arguments, path),
            UseShellExecute = _configuration.UseShellExecute,
            RedirectStandardOutput = _configuration.RedirectStandardOutput
        });
    }
}