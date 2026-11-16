using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace BackpackGunSlot
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("EscapeFromTarkov.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.20fpsguy.backpackgunslot";
        public const string PluginName = "BackpackGunSlot";
        public const string PluginVersion = "1.1.2";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> ModEnabled;
        internal static ConfigEntry<string> SlotName;
        internal static ConfigEntry<string> Caption;
        internal static ConfigEntry<bool> DebugLogging;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            ModEnabled = Config.Bind("General", "Enabled", true,
                "Draw the backpack gun slot in the gear tab.");

            // Must match the slot name the server mod injects onto the backpack template.
            SlotName = Config.Bind("General", "Slot name", "mod_backpack_weapon",
                "Internal name of the backpack child slot. Keep in sync with the server mod config.");

            Caption = Config.Bind("General", "Slot caption", "ON PACK",
                "Label shown above the injected slot on the gear doll.");

            DebugLogging = Config.Bind("General", "Debug logging", false,
                "Verbose log of slot injection.");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo($"{PluginName} {PluginVersion} loaded");
        }

        private void OnDestroy()
        {
            foreach (var binding in UnityEngine.Resources.FindObjectsOfTypeAll<BackpackSlotBinding>())
                binding.Shutdown();
            _harmony?.UnpatchSelf();
            SlotIcons.Clear();
        }
    }
}
