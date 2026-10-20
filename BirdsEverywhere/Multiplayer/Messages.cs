namespace BirdsEverywhere.Multiplayer;

/// <summary>Every mod message carries a version; the record type name is the SMAPI message type.</summary>
public interface IMessage
{
    const int CurrentVersion = 1;

    int Version { get; }
}

/// <summary>Farmhand to host: "I saw this bird."</summary>
public sealed record ObservationRequest(int Version, string BirdId, ObservationData Details) : IMessage;

/// <summary>Host to all: the observation was recorded as log version <paramref name="LogVersion"/>.</summary>
public sealed record ObservationAccepted(int Version, int LogVersion, string BirdId, ObservationData Details) : IMessage;

/// <summary>Farmhand to host: send the full observation log.</summary>
public sealed record ObservationLogRequest(int Version) : IMessage;

/// <summary>Host to peer: the full observation log.</summary>
public sealed record ObservationLogSnapshot(int Version, SaveData Log) : IMessage;

/// <summary>Host to farmhands: today's spawn plan serialized with <c>MessageJson</c>.</summary>
public sealed record SpawnPlanMessage(int Version, string PlanJson) : IMessage;

/// <summary>Entering peer to a peer already in the location: send me your live birds.</summary>
public sealed record LocationBirdsRequest(int Version, string LocationName) : IMessage;

/// <summary>Responder to requester: the live birds of <paramref name="LocationName"/> serialized with <c>MessageJson</c>.</summary>
public sealed record LocationBirdsState(int Version, string LocationName, string BirdsJson) : IMessage;
