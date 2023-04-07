namespace Volksempfaenger;

public class FfmpegConfiguration
{
    public string ProcessPath { get; set; }
    public string Arguments { get; set; }
    public bool UseShellExecute { get; set; }
    public bool RedirectStandardOutput { get; set; }
}