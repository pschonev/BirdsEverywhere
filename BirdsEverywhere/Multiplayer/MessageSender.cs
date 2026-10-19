using StardewModdingAPI;

namespace BirdsEverywhere.Multiplayer;

public sealed class MessageSender
{
    private readonly IMultiplayerHelper _multiplayer;
    private readonly string[] _modIds;

    public MessageSender(IMultiplayerHelper multiplayer, string modId)
    {
        _multiplayer = multiplayer;
        _modIds = new[] { modId };
    }

    public void SendToAll<T>(T message, string type) =>
        _multiplayer.SendMessage(message, type, modIDs: _modIds);

    public void SendToPeer<T>(T message, string type, long playerId) =>
        _multiplayer.SendMessage(message, type, modIDs: _modIds, playerIDs: new[] { playerId });
}
