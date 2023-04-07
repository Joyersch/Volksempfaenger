using System.Collections.Concurrent;
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

    public event Func<IGuild, bool, Task> PlayerFinished;
    public event Func<IGuild, Task> Disconnect;

    public event Func<IGuild, IAudioClient, Task> Connect;


    public AudioPlayer(ILogger<AudioPlayer> logger, DiscordSocketClient client, Ffmpeg ffmpeg)
    {
        _connectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();
        _logger = logger;
        _client = client;
        _ffmpeg = ffmpeg;
    }

    public async Task JoinChannel(IGuild guild, IVoiceChannel target, bool invoiceConnectEvent = true)
    {
        if (_connectedChannels.TryGetValue(guild.Id, out _))
            return;

        if (target.Guild.Id != guild.Id)
            return;

        var audioClient = await target.ConnectAsync();

        if (_connectedChannels.TryAdd(guild.Id, audioClient))
            _logger.LogInformation($"Connected to voice on {guild.Name}.");

        if (invoiceConnectEvent)
            await Connect?.Invoke(guild, audioClient);
    }

    public async Task LeaveChannel(IGuild guild, bool invokeDisconnectEvent = true)
    {
        if (_connectedChannels.TryRemove(guild.Id, out IAudioClient client))
        {
            try
            {
                await client.StopAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    "Something when wrong while trying to leave a channel on {0}.\n{1}"
                    , guild.Id, exception.Message);
            }

            _logger.LogInformation($"Disconnected from voice on {guild.Name}.");
            if (invokeDisconnectEvent)
                Disconnect?.Invoke(guild);
        }
    }

    public async Task PlayAudioAsync(IGuild guild, string audioPath, bool invokePlayerFinishedEvent = true)
    {
        if (_connectedChannels.TryGetValue(guild.Id, out IAudioClient client))
        {
            await PlayAudioAsync(guild, client, audioPath, invokePlayerFinishedEvent);
        }
    }

    public async Task PlayAudioAsync(IGuild guild, IAudioClient client, string audioPath,
        bool invokePlayerFinishedEvent = true)
    {
        using var ffmpeg = _ffmpeg.CreateProcess(audioPath);

        await using var stream = client.CreatePCMStream(AudioApplication.Music);
        try
        {
            await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream);
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Something when wrong while playing.\n {0}", exception.Message);
            return;
        }
        finally
        {
            await stream.FlushAsync();
        }

        _logger.LogInformation("Finished playing audio!\nExit Code:{0}", ffmpeg.ExitCode);

        if (invokePlayerFinishedEvent)
            await PlayerFinished?.Invoke(guild, true);
    }
}