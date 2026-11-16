using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;

namespace BackpackGunSlot;

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class Tuner(
    TemplateTable templates,
    BotTable bots,
    ItemHelper itemHelper,
    JsonUtil jsonUtil,
    ModHelper modHelper,
    ISptLogger<Tuner> logger
) : IOnLoad
{
    private const string BackpackBaseClass = "5448e53e4bdc2d60728b4567";
    private const string GrenadeLauncherNode = "5447bedf4bdc2d87278b4568";

    // The Revolver node mixes revolvers, the MTs-255 and the M32A1, so those are listed by tpl.
    private static readonly Dictionary<string, string[]> Categories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pistol"] =
        [
            "5447b5cf4bdc2d65278b4567",
            "61a4c8884f95bc3b2c5dc96f", // Chiappa Rhino 50DS
            "624c2e8614da335f1e034d8c", // Chiappa Rhino 200DS
            "633ec7c2a6918cb895019c6c", // RSh-12
        ],
        ["SMG"] = ["5447b5e04bdc2d62278b4567"],
        ["AssaultRifle"] = ["5447b5f14bdc2d61278b4567"],
        ["AssaultCarbine"] = ["5447b5fc4bdc2d87278b4567"],
        ["Shotgun"] = ["5447b6094bdc2dc3278b4567", "60db29ce99594040e04c4a27"], // + MTs-255
        ["MarksmanRifle"] = ["5447b6194bdc2d67278b4567"],
        ["SniperRifle"] = ["5447b6254bdc2dc3278b4568"],
        ["MachineGun"] = ["5447bed64bdc2d97278b4568"],
        ["GrenadeLauncher"] = ["5447bedf4bdc2d87278b4568", "6275303a9f372d6ea97f9ec7"], // + M32A1
        ["SpecialWeapon"] = ["5447bee84bdc2dc3278b4569"],
        ["Revolver"] = ["617f1ef5e8b54b0998387733"],
        ["RocketLauncher"] = ["67446d4f04141c10630604e7"],
    };

    private readonly ConfigModel _config =
        jsonUtil.DeserializeFromFile<ConfigModel>(
            System.IO.Path.Join(
                modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()),
                "config",
                "config.json")
        ) ?? new();

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
        {
            logger.Info("[BackpackGunSlot] disabled by config");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(_config.SlotName))
        {
            logger.Error("[BackpackGunSlot] slotName cannot be empty; no templates changed");
            return Task.CompletedTask;
        }
        _config.SlotName = _config.SlotName.Trim();
        var tiers = (_config.Tiers ?? []).Where(t => t is not null && t.MaxCells > 0)
            .OrderBy(t => t.MaxCells).ToList();
        var flares = FindFlareCartridges();
        var patched = 0;
        var patchedTpls = new List<MongoId>();
        var skipped = 0;

        foreach ((var tpl, var item) in templates.Items)
        {
            if (!itemHelper.IsOfBaseclass(tpl, BackpackBaseClass))
            {
                continue;
            }

            var props = item.Properties;
            if (props is null)
            {
                continue;
            }

            var slots = props.Slots?.ToList() ?? new List<Slot>();
            if (slots.Any(s => s.Name == _config.SlotName))
            {
                continue;
            }

            var categories = ResolveCategories(tpl, CountCells(props), tiers, out var tierLabel);
            if (categories.Count == 0)
            {
                skipped++;
                continue;
            }

            slots.Add(BuildWeaponSlot(tpl, categories, flares));
            props.Slots = slots;
            patched++;
            patchedTpls.Add(tpl);

            logger.Debug($"[BackpackGunSlot] {itemHelper.GetItemName(tpl)} ({tpl}) -> tier '{tierLabel}': {string.Join("/", categories.Keys)}");
        }

        RegisterEmptyBotModPools(patchedTpls);
        logger.Success($"[BackpackGunSlot] patched {patched} backpack(s), skipped {skipped}, {flares.Count} flare cartridge(s) kept out");
        return Task.CompletedTask;
    }

    // Bot gen warns per bot without a mod pool for the slot. Empty pool means no gun.
    private void RegisterEmptyBotModPools(List<MongoId> backpackTpls)
    {
        foreach (var botType in bots.Types.Values)
        {
            var mods = botType?.BotInventory?.Mods;
            if (mods is null)
            {
                continue;
            }

            foreach (var tpl in backpackTpls)
            {
                mods.TryAdd(tpl, new Dictionary<string, HashSet<MongoId>>());
            }
        }
    }

    private static int CountCells(TemplateItemProperties props)
    {
        var total = 0;
        foreach (var grid in props.Grids ?? [])
        {
            var gp = grid.Properties;
            total += (int)((gp?.CellsH ?? 0) * (gp?.CellsV ?? 0));
        }

        return total;
    }

    private Dictionary<string, string[]> ResolveCategories(MongoId tpl, int cells, List<TierModel> tiers, out string tierLabel)
    {
        if (_config.Overrides?.TryGetValue(tpl.ToString(), out var forced) == true)
        {
            forced = forced?.Trim() ?? "none";
            tierLabel = $"override:{forced}";
            if (forced.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return new();
            }

            return MapCategories(forced.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        if (_config.AllowAnyGun)
        {
            tierLabel = "allowAnyGun";
            return MapCategories(Categories.Keys);
        }

        var tier = tiers.FirstOrDefault(t => cells <= t.MaxCells) ?? tiers.LastOrDefault();
        tierLabel = tier is null ? "none" : $"<={tier.MaxCells}c";
        return tier is null ? new() : MapCategories(tier.Categories ?? []);
    }

    private Dictionary<string, string[]> MapCategories(IEnumerable<string> names)
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            var normalized = name?.Trim();
            if (!string.IsNullOrEmpty(normalized) && Categories.TryGetValue(normalized, out var ids))
            {
                result[normalized] = ids;
            }
            else
            {
                logger.Warning($"[BackpackGunSlot] unknown weapon category '{name}' in config, ignored");
            }
        }

        return result;
    }

    // RSP-30/ROP-30 flares sit under the GrenadeLauncher node.
    private HashSet<MongoId> FindFlareCartridges()
    {
        return templates.Items
            .Where(kv => kv.Value.Parent == GrenadeLauncherNode && !(kv.Value.Properties?.Chambers?.Any() ?? false))
            .Select(kv => kv.Key)
            .ToHashSet();
    }

    private Slot BuildWeaponSlot(MongoId backpackTpl, Dictionary<string, string[]> categories, HashSet<MongoId> flares)
    {
        var filter = new SlotFilter
        {
            Filter = new HashSet<MongoId>(categories.Values.SelectMany(v => v).Select(v => new MongoId(v)))
        };
        if (categories.ContainsKey("GrenadeLauncher") && flares.Count > 0)
        {
            // The server model lacks ExcludedFilter, but the client reads the same JSON key.
            filter.ExtensionData = new Dictionary<string, object>
            {
                ["ExcludedFilter"] = flares.Select(f => f.ToString()).ToList()
            };
        }

        return new Slot
        {
            Name = _config.SlotName,
            Id = new MongoId(),
            Parent = backpackTpl,
            Required = false,
            MergeSlotWithChildren = false,
            Properties = new SlotProperties
            {
                Filters = new List<SlotFilter> { filter }
            }
        };
    }
}
