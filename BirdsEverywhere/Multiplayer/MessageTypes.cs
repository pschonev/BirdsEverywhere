namespace BirdsEverywhere.Multiplayer;

public static class MessageTypes
{
    public const string RequestCurrentBirds = "RequestCurrentBirds";
    public const string UpdateCurrentBirds = "UpdateCurrentBirds";
    public const string SaveNewObservation = "SaveNewObservation";
    public const string GlobalObservationMessage = "GlobalObservationMessage";
    public const string BirdLocationsNewDay = "BirdLocationsForFarmhandsNewDay";
    public const string BirdLocationsOnConnect = "BirdLocationsForFarmhandsOnConnect";
    public const string UpdateFarmhandSave = "UpdateFarmhandSave";
}
