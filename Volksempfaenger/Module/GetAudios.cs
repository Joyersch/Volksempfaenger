using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Options;
using Volksempfaenger.Configurations;

namespace Volksempfaenger.Module;

public class GetAudios : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<GetAudios> _logger;
    private readonly AudioLibrary _library;
    private readonly PermissionSettings _settings;

    public GetAudios(ILogger<GetAudios> logger, AudioLibrary library, IOptions<PermissionSettings> settings)
    {
        _logger = logger;
        _library = library;
        _settings = settings.Value;
    }

    [SlashCommand("list", "List of audios for this server")]
    public async Task ListAsync()
    {
        IGuildUser user = (IGuildUser) Context.User;
        
        if (!_settings.Roles.Any(r => user.RoleIds.Contains(r)))
        {
            _logger.LogInformation(
                $"UserId:{0}, Name:{1} tried to use /play without the special role. Server: {Context.Guild.Id}"
                , user.Id
                , user.Nickname);
            return;
        }
        
        // ToDo: Write data directly into a steam and give that as a response (no interaction with the file system) if possible
        string fileName = Context.Guild.Id + ".txt";
        await using StreamWriter streamWriter = new StreamWriter(_library.GetFullFilePath(Context.Guild, fileName));
        var lib = _library.GetAudios(Context.Guild);

        foreach (var l in lib)
            await streamWriter.WriteLineAsync(_library.GetFileName(Context.Guild, l));

        await RespondWithFileAsync(fileName);
    }
}