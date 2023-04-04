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

        await RespondAsync("Preparing file!");
        string fileName = _library.GetFullFilePath(Context.Guild, Context.Guild.Id + ".txt");
        try
        {
            // get files first otherwise the file will be listed as well
            var files = _library.GetAudios(Context.Guild);
            await using StreamWriter streamWriter = new StreamWriter(fileName);

            foreach (var file in files)
                await streamWriter.WriteLineAsync(_library.GetFileName(Context.Guild, file));

            // required to allow others to read the file
            streamWriter.Close();

            await FollowupWithFileAsync(fileName);
        }
        catch (Exception ex)
        {
            await FollowupAsync(ex.Message);
        }
        finally
        {
            // clean up
            if (File.Exists(fileName))
                File.Delete(fileName);
        }
    }
}