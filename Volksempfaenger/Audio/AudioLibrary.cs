using Discord;
using Microsoft.Extensions.Options;
using Volksempfaenger.Configurations;

namespace Volksempfaenger;

public class AudioLibrary
{
    private AudioLibraryConfiguration _configuration;

    public AudioLibrary(IOptions<AudioLibraryConfiguration> configuration)
    {
        _configuration = configuration.Value;
    }

    public void CheckAndCreateDirectory(IGuild guild)
        => CheckAndCreateDirectory(guild.Id);

    public void CheckAndCreateDirectory(ulong guild)
    {
        if (!Directory.Exists(_configuration.Path + $"/{guild}/"))
            Directory.CreateDirectory(_configuration.Path + $"/{guild}/");
    }

    public string GetFileName(IGuild guild, string pathToFile)
        => pathToFile.Substring(_configuration.Path.Length + 2 + guild.Id.ToString().Length);

    public string GetFullFilePath(IGuild guild, string fileName)
        => _configuration.Path + $"/{guild.Id}/" + fileName;

    public string[] FindAudios(IGuild guild, string pattern)
        => Directory.GetFiles(_configuration.Path + $"/{guild.Id}/", pattern, SearchOption.AllDirectories);

    public string[] GetAudios(IGuild guild)
        => Directory.GetFiles(_configuration.Path + $"/{guild.Id}/", "*.*", SearchOption.AllDirectories);
}