using System.Reflection;
using Discord;
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

        await _client.LoginAsync(TokenType.Bot, _configuration.Token);
        await _client.StartAsync();
        _client.Ready += async () =>
        {
            foreach (ulong guildId in _settings.Guilds)
            {
                _logger.LogInformation("Registering for server: {0}", guildId);
                await _commands.RegisterCommandsToGuildAsync(guildId);
            }
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            await _client.SetStatusAsync(UserStatus.DoNotDisturb);

            await Task.Delay(1000, stoppingToken);
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