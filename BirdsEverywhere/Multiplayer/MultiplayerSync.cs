using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BirdsEverywhere.Multiplayer;

/// <summary>Sends and reads versioned mod messages; unsupported versions and malformed payloads are logged and dropped.</summary>
public sealed class MultiplayerSync
{
    private readonly IMultiplayerHelper _multiplayer;
    private readonly IMonitor _monitor;
    private readonly string[] _modIds;

    public MultiplayerSync(IMultiplayerHelper multiplayer, IMonitor monitor, string modId)
    {
        _multiplayer = multiplayer;
        _monitor = monitor;
        _modIds = new[] { modId };
    }

    public void SendToAll<T>(T message)
        where T : IMessage =>
        _multiplayer.SendMessage(message, typeof(T).Name, modIDs: _modIds);

    public void SendToPeer<T>(T message, long playerId)
        where T : IMessage =>
        _multiplayer.SendMessage(message, typeof(T).Name, modIDs: _modIds, playerIDs: new[] { playerId });

    public T? Read<T>(ModMessageReceivedEventArgs e)
        where T : class, IMessage
    {
        T? message;
        try
        {
            message = e.ReadAs<T>();
        }
        catch (JsonException ex)
        {
            _monitor.Log($"Dropped malformed {e.Type} from player {e.FromPlayerID}: {ex.Message}", LogLevel.Warn);
            return null;
        }

        if (message is null || message.Version != IMessage.CurrentVersion)
        {
            _monitor.Log($"Dropped {e.Type} from player {e.FromPlayerID}: unsupported version {message?.Version.ToString() ?? "<null>"}, expected {IMessage.CurrentVersion}.", LogLevel.Warn);
            return null;
        }

        return message;
    }
}
