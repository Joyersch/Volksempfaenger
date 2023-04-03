using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Volksempfaenger.Configuration;
using Volksempfaenger.Configurations;
using Volksempfaenger.Module;

namespace Volksempfaenger;

public class Bot : BackgroundService
{
    private readonly ILogger<Bot> _logger;
    private readonly DiscordSocketClient _client;
    private readonly DiscordConfiguration _configuration;
    private readonly InteractionService _commands;
    private readonly IServiceProvider _services;
    private readonly PermissionSettings _settings;
    private readonly AudioPlayer _player;
    private readonly AudioLibrary _library;
    private readonly Random _random;
    private readonly ConcurrentDictionary<ulong, IDisposable> _connectedChannels;

    public Bot(ILogger<Bot> logger, DiscordSocketClient client,
        IOptions<DiscordConfiguration> configuration,
        InteractionService commands, IServiceProvider services, IOptions<PermissionSettings> settings,
        AudioLibrary library, AudioPlayer player, Random random)
    {
        _logger = logger;
        _client = client;
        _commands = commands;
        _configuration = configuration.Value;
        _commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        _connectedChannels = new ConcurrentDictionary<ulong, IDisposable>();
        _services = services;
        _settings = settings.Value;
        _library = library;
        _player = player;
        _random = random;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Log += ClientLog;
        _client.InteractionCreated += InteractionCreated;
        _client.SlashCommandExecuted += SlashCommandExecuted;
        _client.UserVoiceStateUpdated += UserJoinedChannel;

        await _client.LoginAsync(TokenType.Bot, _configuration.Token);
        await _client.StartAsync();
        _client.Ready += RegisterGuilds;

        while (!stoppingToken.IsCancellationRequested)
        {
            await _client.SetStatusAsync(UserStatus.DoNotDisturb);

            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task UserJoinedChannel(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        if (user.IsBot)
            return;

        if (after.VoiceChannel is null)
            return;

        IGuildUser u = (IGuildUser) user;
        if (!_settings.Roles.Any(r => u.RoleIds.Contains(r)))
            return;
        
        ulong voiceId = after.VoiceChannel.Id;
        if (_connectedChannels.TryGetValue(voiceId, out _))
            return;

        // this will register an observer to the ConnectAsync Task
        IObservable<IAudioClient> observable = after.VoiceChannel
            .ConnectAsync()
            .ToObservable(Scheduler.CurrentThread);

        // the collection is to keep a reference of disposable subscription of the observer
        _connectedChannels.TryAdd(voiceId,
            observable.Subscribe(client => JoinedChannel(client, after.VoiceChannel.Guild, after.VoiceChannel.Id))
        );
    }

    private async void JoinedChannel(IAudioClient audioClient, IGuild guild, ulong channel)
    {
        if (audioClient is not null)
        {
            var allAudios = _library.GetAudios(guild);

            if (allAudios.Length > 0)
            {
                string audioPath = allAudios[_random.Next(allAudios.Length)];
                await _player.PlayAudioAsync(guild, audioClient, audioPath);
            }
            await audioClient.StopAsync();
            audioClient.Dispose();
        }

        if (_connectedChannels.TryGetValue(channel, out var observer))
            observer.Dispose();

        _connectedChannels.TryRemove(channel, out _);
    }

    private async Task RegisterGuilds()
    {
        foreach (ulong guildId in _settings.Guilds)
        {
            try
            {
                await _commands.RegisterCommandsToGuildAsync(guildId);
                // Logging after registration in case of failure
                _logger.LogInformation("Registering for server: {0}", guildId);

                // Create directory for guild in case it is not created yet
                _library.CheckAndCreateDirectory(guildId);
            }
            catch (Exception ex)
            {
                // The exeption is most likely a "Missing Access" therefor ex.Message suffices
                _logger.LogWarning("Unable to register server: {0}\n{1}", guildId, ex.Message);
            }
        }
    }

    private Task SlashCommandExecuted(SocketSlashCommand arg)
        => Task.CompletedTask;

    private async Task InteractionCreated(SocketInteraction arg)
        => await _commands.ExecuteCommandAsync(new SocketInteractionContext(_client, arg), _services);

    private async Task ClientLog(LogMessage info)
    {
        switch (info.Severity)
        {
            case LogSeverity.Critical:
                _logger.LogCritical(info.Exception, info.Message);
                break;
            case LogSeverity.Error:
                _logger.LogError(info.Exception, info.Message);
                break;
            case LogSeverity.Warning:
                _logger.LogWarning(info.Message);
                break;
            case LogSeverity.Info:
                _logger.LogInformation(info.Message);
                break;
            case LogSeverity.Verbose:
                _logger.LogTrace(info.Message + Environment.NewLine + info.Source);
                break;
            case LogSeverity.Debug:
                _logger.LogDebug(info.Message);
                break;
        }
    }
}
