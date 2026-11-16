using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;

namespace BackpackGunSlot
{
    internal static class BackpackWeaponSlot
    {
        internal static bool Matches(Slot slot) => slot != null &&
            slot.Name == Plugin.SlotName.Value?.Trim();
    }

    [HarmonyPatch(typeof(SearchableSlotView), nameof(SearchableSlotView.CreateSlots))]
    internal static class BackpackSpecialSlotsPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(SearchableSlotView __instance, Item __0)
        {
            if (__instance._specSlotHeader != null)
                __instance._specSlotHeader.gameObject.SetActive(true);
            if (__0 is not CompoundItem pack || !pack.Slots.Any(BackpackWeaponSlot.Matches))
                return true;
            // The equipped pack draws this slot with the ON BACK prefab, so skip vanilla's SPEC slot.
            if (pack.Slots.All(BackpackWeaponSlot.Matches))
            {
                if (__instance._specSlotsPanel != null)
                    __instance._specSlotsPanel.gameObject.SetActive(false);
                if (__instance._specSlotHeader != null)
                    __instance._specSlotHeader.gameObject.SetActive(false);
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        private static void Postfix(SearchableSlotView __instance, Item __0)
        {
            if (__0 is not CompoundItem pack || !pack.Slots.Any(BackpackWeaponSlot.Matches) ||
                __instance._specSlotsPanel == null) return;
            foreach (var view in __instance._specSlotsPanel.GetComponentsInChildren<SlotView>(true))
            {
                if (!BackpackWeaponSlot.Matches(view.Slot) ||
                    view.GetComponent<BackpackSlotMarker>() != null) continue;
                // Keep the object for vanilla's registered disposal callback.
                view.Close();
                view.gameObject.SetActive(false);
            }
        }
    }

    [HarmonyPatch(typeof(ContainedGridsView), nameof(ContainedGridsView.Show), typeof(CompoundItem),
        typeof(ItemContext), typeof(GridView[]), typeof(SlotView[]), typeof(ItemController),
        typeof(FilterPanel), typeof(ItemUiContext), typeof(bool))]
    internal static class LayoutPresetSlotsPatch
    {
        // Terraframe ships a preset slot view bound to nothing, ours would pair with it and throw.
        [HarmonyPrefix]
        private static void Prefix(ref SlotView[] __3)
        {
            if (__3 != null && __3.Any(v => v == null))
                __3 = __3.Where(v => v != null).ToArray();
        }
    }

    [HarmonyPatch(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.CreateModSlots))]
    internal static class BackpackInspectionSlotsPatch
    {
        private static bool IsGunPack(CompoundItem item) => item != null &&
            item.Slots.Any(BackpackWeaponSlot.Matches) && item.Slots.All(BackpackWeaponSlot.Matches);

        [HarmonyPrefix]
        private static bool Prefix(ItemSpecificationPanel __instance, CompoundItem __0)
        {
            if (!IsGunPack(__0)) return true;
            // The empty custom slot renders as a white tile, so only show the panel with a gun in it.
            bool hasGun = __0.Slots.Any(s => s.ContainedItem != null);
            __instance._modsPanel.gameObject.SetActive(hasGun);
            return hasGun;
        }

        // AllSlots recurses into the mounted gun's parts, list only the pack's own slot.
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var allSlots = AccessTools.PropertyGetter(typeof(CompoundItem), nameof(CompoundItem.AllSlots));
            var replacement = AccessTools.Method(typeof(BackpackInspectionSlotsPatch), nameof(ListedSlots));
            foreach (var instruction in instructions)
                yield return instruction.Calls(allSlots)
                    ? new CodeInstruction(OpCodes.Call, replacement).MoveLabelsFrom(instruction)
                    : instruction;
        }

        private static IEnumerable<Slot> ListedSlots(CompoundItem item) =>
            IsGunPack(item) ? item.Slots : item.AllSlots;

        [HarmonyPostfix]
        private static void Postfix(ItemSpecificationPanel __instance, CompoundItem __0)
        {
            if (!IsGunPack(__0) || __instance._modsContainer == null) return;
            foreach (var view in __instance._modsContainer.GetComponentsInChildren<ModSlotView>(true))
                if (BackpackWeaponSlot.Matches(view.Slot) && view._slotName != null)
                    view._slotName.text = Plugin.Caption.Value;
        }
    }
}
