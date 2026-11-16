using SPTarkov.Server.Core.Models.Spt.Mod;

namespace BackpackGunSlot;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.20fpsguy.backpackgunslot";
    public string Name { get; init; } = "BackpackGunSlot";
    public string Author { get; init; } = "20fpsguy";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } =
        new(typeof(ModMetadata).Assembly.GetName().Version!.ToString(3));
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "";
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; }
}
