using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Options;
using Volksempfaenger.Configurations;

namespace Volksempfaenger.Module;

public class Play : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<Play> _logger;
    private readonly AudioPlayer _player;
    private readonly AudioLibrary _library;
    private readonly Random _random;
    private readonly PermissionConfiguration _permissionConfiguration;

    public Play(ILogger<Play> logger, AudioPlayer player, AudioLibrary library, Random random,
        IOptions<PermissionConfiguration> settings)
    {
        _logger = logger;
        _player = player;
        _library = library;
        _random = random;
        _permissionConfiguration = settings.Value;
    }

    [SlashCommand("play", "play random audio for this server")]
    public async Task PlayAsync()
    {
        IGuildUser user = (IGuildUser) Context.User;

        if (!_permissionConfiguration.Roles.Any(r => user.RoleIds.Contains(r)))
        {
            _logger.LogInformation(
                $"UserId:{0}, Name:{1} tried to use /play without the special role. Server: {Context.Guild.Id}"
                , user.Id
                , user.Nickname);
            return;
        }

        var context = ((IVoiceState) Context.User).VoiceChannel;

        if (context is null)
        {
            await RespondAsync("No, you are not in a voice!");
            return;
        }
        
        _logger.LogInformation("Running play");

        _library.CheckAndCreateDirectory(Context.Guild);

        var allAudios = _library.GetAudios(Context.Guild);

        if (allAudios.Length == 0)
            await RespondAsync("No audios for this server!");

        string audioPath = allAudios[_random.Next(allAudios.Length)];

        _logger.LogInformation("Playing: " + audioPath);

        await _player.LeaveChannel(Context.Guild);
        await RespondAsync($"Playing: `{_library.GetFileName(Context.Guild, audioPath)}`");
        await _player.JoinChannel(Context.Guild, context);

        await _player.PlayAudioAsync(Context.Guild, audioPath);
    }

    [SlashCommand("play_specific", "play a specific audio")]
    public async Task PlaySpeficicAsync(string audio)
    {
        IGuildUser user = (IGuildUser) Context.User;

        if (!_permissionConfiguration.Roles.Any(r => user.RoleIds.Contains(r)))
        {
            _logger.LogInformation(
                "UserId:{0}, Name:{1} tried to use /play_specific without the special role"
                , user.Id
                , user.Nickname);
            return;
        }

        _logger.LogInformation("Running play");

        _library.CheckAndCreateDirectory(Context.Guild);

        if (!File.Exists(_library.GetFullFilePath(Context.Guild, audio)))
        {
            _logger.LogInformation($"Audio: '{audio}' does not exist!");
            await RespondAsync($"That audio does not exists!");
            return;
        }

        await _player.LeaveChannel(Context.Guild);
        await RespondAsync($"Playing: `{audio}`");
        await _player.JoinChannel(Context.Guild, ((IVoiceState) Context.User).VoiceChannel);

        await _player.PlayAudioAsync(Context.Guild, _library.GetFullFilePath(Context.Guild, audio));
    }
}