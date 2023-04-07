using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Options;
using Volksempfaenger.Configurations;

namespace Volksempfaenger.Module;

public class Cancel : InteractionModuleBase<SocketInteractionContext>
{
    
    private readonly ILogger<Cancel> _logger;
    private readonly AudioPlayer _player;
    private readonly AudioLibrary _library;
    private readonly Random _random;
    private readonly PermissionConfiguration _permissionConfiguration;

    public Cancel(ILogger<Cancel> logger, AudioPlayer player, AudioLibrary library, Random random, IOptions<PermissionConfiguration> settings)
    {
        _logger = logger;
        _player = player;
        _player.PlayerFinished += _player.LeaveChannel;
        _library = library;
        _random = random;
        _permissionConfiguration = settings.Value;
    }

    [SlashCommand("cancel", "cancel audio")]
    public async Task PlayAsync()
    {
        IGuildUser user = (IGuildUser) Context.User;

        if (!_permissionConfiguration.Roles.Any(r => user.RoleIds.Contains(r)))
        {
            _logger.LogInformation(
                "UserId:{0}, Name:{1} tried to use /cancel without the special role"
                , user.Id
                , user.Nickname);
            return;
        }
        
        _logger.LogInformation("Running cancel");
        
        await RespondAsync($"Canceling audio!");
        await _player.LeaveChannel(user.Guild);
    }
}