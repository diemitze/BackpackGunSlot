using System.Text.Json.Serialization;

namespace BackpackGunSlot;

public record ConfigModel
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("slotName")]
    public string SlotName { get; set; } = "mod_backpack_weapon";

    // Ignore tiers, every slot takes every class. "none" overrides still skip.
    [JsonPropertyName("allowAnyGun")]
    public bool AllowAnyGun { get; set; }

    // First tier with maxCells >= the pack's grid cells wins. Category names are the keys in Tuner.Categories.
    [JsonPropertyName("tiers")]
    public List<TierModel> Tiers { get; set; } = new()
    {
        new() { MaxCells = 12, Categories = new() { "Pistol" } },
        new() { MaxCells = 18, Categories = new() { "Pistol", "SMG" } },
        new() { MaxCells = 24, Categories = new() { "Pistol", "SMG", "Shotgun" } },
        new() { MaxCells = 35, Categories = new() { "Pistol", "SMG", "Shotgun", "AssaultRifle", "AssaultCarbine", "MarksmanRifle" } },
        new() { MaxCells = 9999, Categories = new() { "Pistol", "SMG", "Shotgun", "AssaultRifle", "AssaultCarbine", "MarksmanRifle", "SniperRifle", "MachineGun", "GrenadeLauncher" } },
    };

    // Pin tpls to a tier, or "none" to skip them.
    [JsonPropertyName("overrides")]
    public Dictionary<string, string> Overrides { get; set; } = new();
}

public record TierModel
{
    [JsonPropertyName("maxCells")]
    public int MaxCells { get; set; }

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = new();
}
