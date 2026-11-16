using System.Text.Json.Serialization;

namespace BackpackGunSlot;

public record ConfigModel
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("slotName")]
    public string SlotName { get; set; } = "mod_backpack_weapon";

    // Every backpack slot takes every weapon class, ignoring tiers. A "none" override still skips a pack.
    [JsonPropertyName("allowAnyGun")]
    public bool AllowAnyGun { get; set; }

    // Backpacks are sorted into the first tier whose maxCells >= the pack's total grid cells.
    // Categories are weapon-class names from the map in Tuner (Pistol, SMG, Shotgun, AssaultRifle,
    // AssaultCarbine, MarksmanRifle, SniperRifle, MachineGun, GrenadeLauncher, SpecialWeapon, Revolver, RocketLauncher).
    [JsonPropertyName("tiers")]
    public List<TierModel> Tiers { get; set; } = new()
    {
        new() { MaxCells = 12, Categories = new() { "Pistol" } },
        new() { MaxCells = 18, Categories = new() { "Pistol", "SMG" } },
        new() { MaxCells = 24, Categories = new() { "Pistol", "SMG", "Shotgun" } },
        new() { MaxCells = 35, Categories = new() { "Pistol", "SMG", "Shotgun", "AssaultRifle", "AssaultCarbine", "MarksmanRifle" } },
        new() { MaxCells = 9999, Categories = new() { "Pistol", "SMG", "Shotgun", "AssaultRifle", "AssaultCarbine", "MarksmanRifle", "SniperRifle", "MachineGun", "GrenadeLauncher" } },
    };

    // Optional: force specific backpack tpls into a named tier regardless of size, or "none" to skip.
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
