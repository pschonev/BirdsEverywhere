using Newtonsoft.Json;

namespace BirdsEverywhere;

public class BirdTemplate
{
    public string template { get; set; } = "";
    public SpawnData defaultSpawnData { get; set; } = null;
    public Dictionary<string, List<SpawnData>> allSpawnData { get; set; } = new Dictionary<string, List<SpawnData>>() {
        { "spring", new List<SpawnData>() {new SpawnData() } }
    }; // advanced spawn patters in the form season : SpawnData

    [JsonConstructor]
    public BirdTemplate(SpawnData defaultSpawnData, Dictionary<string, List<SpawnData>> allSpawnData, string template)
    {
        if (allSpawnData == null)
        {
            allSpawnData = new Dictionary<string, List<SpawnData>>();
        }
        if (defaultSpawnData == null)
        {
            defaultSpawnData = SpawnData.getDefaultSpawnData();
        }
        this.defaultSpawnData = defaultSpawnData;
        this.allSpawnData = allSpawnData;
        this.template = template ?? "";
    }

    public void ApplyTemplate(Func<string, BirdTemplate> resolvedTemplate)
    {
        if (string.IsNullOrEmpty(template))
        {
            return;
        }

        BirdTemplate templateData = resolvedTemplate(template);
        templateData.defaultSpawnData.CopyProperties(defaultSpawnData);
        allSpawnData.MergeDictionaries(templateData.allSpawnData);
    }

}
