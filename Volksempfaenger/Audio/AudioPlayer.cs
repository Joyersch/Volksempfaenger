using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using Discord;
using Discord.Audio;
using Discord.WebSocket;

namespace Volksempfaenger;

public class AudioPlayer
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<AudioPlayer> _logger;
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels;
    private readonly Ffmpeg _ffmpeg;

    public event Func<IGuild, Task> OnPlayerFinished;

    public AudioPlayer(ILogger<AudioPlayer> logger, DiscordSocketClient client, Ffmpeg ffmpeg)
    {
        _connectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();
        _logger = logger;
        _client = client;
        _ffmpeg = ffmpeg;
    }

    public async Task JoinChannel(IGuild guild, IVoiceChannel target)
    {
        if (_connectedChannels.TryGetValue(guild.Id, out _))
            return;

        if (target.Guild.Id != guild.Id)
            return;

        var audioClient = await target.ConnectAsync();

        if (_connectedChannels.TryAdd(guild.Id, audioClient))
            _logger.LogInformation($"Connected to voice on {guild.Name}.");
    }

    public async Task LeaveChannel(IGuild guild)
    {
        if (_connectedChannels.TryRemove(guild.Id, out IAudioClient client))
        {
            await client.StopAsync();
            _logger.LogInformation($"Disconnected from voice on {guild.Name}.");
        }
    }

    public async Task PlayAudioAsync(IGuild guild, string path)
    {
        if (_connectedChannels.TryGetValue(guild.Id, out IAudioClient client))
        {
            await PlayAudioAsync(guild, client, path);
        }
    }

    public async Task PlayAudioAsync(IGuild guild, IAudioClient client, string path)
    {
        using var ffmpeg = _ffmpeg.CreateProcess(path);

        await using var stream = client.CreatePCMStream(AudioApplication.Music);
        try
        {
            await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream);
        }
        finally
        {
            await stream.FlushAsync();
        }

        await OnPlayerFinished?.Invoke(guild);
    }
}