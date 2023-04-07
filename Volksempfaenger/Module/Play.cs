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
    private readonly BotBehaviourConfiguration _botBehaviourConfiguration;
    private readonly PermissionConfiguration _permissionConfiguration;

    public Play(ILogger<Play> logger, AudioPlayer player, AudioLibrary library, Random random,
        IOptions<PermissionConfiguration> settings, IOptions<BotBehaviourConfiguration> botBehaviourConfiguration)
    {
        _logger = logger;
        _player = player;
        _library = library;
        _random = random;
        _botBehaviourConfiguration = botBehaviourConfiguration.Value;
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
        
        _logger.LogDebug("Preparing audio to play.");

        _library.CheckAndCreateDirectory(Context.Guild);

        var allAudios = _library.GetAudios(Context.Guild);

        if (allAudios.Length == 0)
            await RespondAsync("No audios for this server!");

        string audioPath = allAudios[_random.Next(allAudios.Length)];

        _logger.LogDebug("prepared audio file: {0}", audioPath);
        
        _logger.LogInformation("Playing: " + audioPath);

        await RespondAsync($"Playing: `{_library.GetFileName(Context.Guild, audioPath)}`");
        await PlayAudio(context, audioPath);
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
        
        var context = ((IVoiceState) Context.User).VoiceChannel;

        if (context is null)
        {
            await RespondAsync("No, you are not in a voice!");
            return;
        }

        _logger.LogDebug("Preparing audio to play.");

        _library.CheckAndCreateDirectory(Context.Guild);
        
        var allAudios = _library.GetAudios(Context.Guild);

        if (allAudios.All(a => a != _library.GetFullFilePath(Context.Guild, audio)))
        {
            _logger.LogInformation("Audio: '{audio}' does not exist!", audio);
            await RespondAsync("That audio does not exists!");
            return;
        }

        _logger.LogDebug("prepared audio file: {0}", audio);
        await RespondAsync($"Playing: `{audio}`");
        await PlayAudio(context, _library.GetFullFilePath(Context.Guild, audio));
    }

    private async Task PlayAudio(IVoiceChannel context, string audioPath)
    {
        _logger.LogDebug("leave previous voice if in voice!");
        await _player.LeaveChannel(Context.Guild);
        
        _logger.LogDebug("Joining Channel");
        await _player.JoinChannel(Context.Guild, context, false);
        _logger.LogDebug("Joined Channel");
        
        _logger.LogDebug("Playing Audio");
        await _player.PlayAudioAsync(Context.Guild, audioPath, false);
        _logger.LogDebug("Played Audio");

        if (_botBehaviourConfiguration.Debug.DisableLeave)
            return;
        
        _logger.LogDebug("Disconnecting from Audio");
        await _player.LeaveChannel(Context.Guild, false);
        _logger.LogDebug("Disconnected from Audio");
    }
}