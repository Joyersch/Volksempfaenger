using System.Security.Cryptography;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Volksempfaenger.Configuration;
using Volksempfaenger;
using Volksempfaenger.Configurations;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

        services.Configure<DiscordConfiguration>(configuration.GetSection(nameof(DiscordConfiguration)));
        services.Configure<DiscordSocketConfig>(configuration.GetSection(nameof(DiscordSocketConfig)));
        services.Configure<FfmpegConfiguration>(configuration.GetSection(nameof(FfmpegConfiguration)));
        services.Configure<PermissionConfiguration>(configuration.GetSection(nameof(PermissionConfiguration)));
        services.Configure<AudioLibraryConfiguration>(configuration.GetSection(nameof(AudioLibraryConfiguration)));
        services.Configure<BotBehaviourConfiguration>(configuration.GetSection(nameof(BotBehaviourConfiguration)));

        services.AddSingleton<DiscordSocketClient>();
        services.AddSingleton(serviceProvider =>
            new InteractionService(serviceProvider.GetRequiredService<DiscordSocketClient>()));

        services.AddSingleton<Ffmpeg>();
        services.AddSingleton<AudioLibrary>();
        services.AddSingleton<AudioPlayer>();
        services.AddSingleton<Random>();

        services.AddHostedService<Bot>();
    })
    .Build();
await host.RunAsync();