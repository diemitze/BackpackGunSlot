using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using UnityEngine;

namespace BackpackGunSlot
{
    // Uses the game's icon rotation so new drops and recreated items get it too.
    [HarmonyPatch(typeof(SlotView), nameof(SlotView.SetupItemView))]
    internal static class BackpackWeaponItemRotationPatch
    {
        private static readonly FieldInfo ItemViewField = AccessTools.Field(typeof(SlotView), "ItemView");

        [HarmonyPostfix]
        private static void Postfix(SlotView __instance)
        {
            if (__instance.GetComponent<BackpackSlotMarker>()?.Vertical != true) return;
            var itemView = ItemViewField.GetValue(__instance) as ItemView;
            if (itemView == null) return;
            itemView.ItemRotation = ItemRotation.Vertical;
            __instance.ScaleItem(__instance._slotPlace.rect.size);
        }
    }

    [HarmonyPatch(typeof(SlotView), nameof(SlotView.ScaleItem))]
    internal static class BackpackWeaponItemScalePatch
    {
        [HarmonyPrefix]
        private static void Prefix(SlotView __instance, ref Vector2 __0)
        {
            if (__instance.GetComponent<BackpackSlotMarker>()?.Vertical != true) return;
            // UpdateScale fits the unrotated image, so transpose the bounds for a vertical icon.
            __0 = new Vector2(__0.y, __0.x);
        }
    }
}
