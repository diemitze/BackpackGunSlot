using System;
using System.Linq;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackGunSlot
{
    // A pack opened in its own window (nested in another pack, in the stash, on the ground)
    // only shows grids. Add the gun slot there too so it stays reachable.
    [HarmonyPatch(typeof(GridWindow), nameof(GridWindow.Show))]
    internal static class GridWindowSlotPatch
    {
        private const string InjectedName = "BackpackGunSlot_Window";
        private static SlotView _template;

        [HarmonyPostfix]
        private static void Postfix(GridWindow __instance, CompoundItem __0, ItemController __2, ItemUiContext __3)
        {
            try { Attach(__instance, __0, __2, __3); }
            catch (Exception e) { Plugin.Log.LogError("Backpack window slot failed: " + e); }
        }

        private static void Attach(GridWindow window, CompoundItem pack, ItemController controller, ItemUiContext uiContext)
        {
            if (!Plugin.ModEnabled.Value || pack?.Slots == null) return;
            var slot = pack.Slots.FirstOrDefault(BackpackWeaponSlot.Matches);
            if (slot == null) return;
            // The equipped pack already has the big ON PACK slot beside it.
            if (pack.CurrentAddress?.Container is Slot { ParentItem: InventoryEquipment }) return;
            var template = FindTemplate();
            if (template == null || window._containedGrids == null) return;

            var view = UnityEngine.Object.Instantiate(template, window.transform, false);
            view.gameObject.name = InjectedName;
            view.gameObject.AddComponent<BackpackSlotMarker>();
            view.gameObject.AddComponent<BackpackSlotTooltip>().Bind(slot);
            view.Show(slot, window._itemContext, controller, uiContext, null, null);
            if (view._headerText != null) view._headerText.text = Plugin.Caption.Value;
            if (view._slotHeader?._headerText != null) view._slotHeader._headerText.text = Plugin.Caption.Value;

            var rect = (RectTransform)view.transform;
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();
            bool stacked = window.GetComponent<VerticalLayoutGroup>() != null;
            if (!stacked)
            {
                // No layout group to stack into: pin it under the grids by hand.
                var layout = view.GetComponent<LayoutElement>() ?? view.gameObject.AddComponent<LayoutElement>();
                layout.ignoreLayout = true;
            }
            var sync = window.gameObject.AddComponent<WindowSlotSync>();
            sync.Bind(view, window._containedGrids, stacked);
            window.UI.AddDisposable(() =>
            {
                if (sync != null) UnityEngine.Object.Destroy(sync);
                if (view == null) return;
                view.Close();
                view.gameObject.SetActive(false);
                view.transform.SetParent(null, false);
                UnityEngine.Object.Destroy(view.gameObject);
            });
            if (Plugin.DebugLogging.Value)
                Plugin.Log.LogDebug($"Window slot on {pack.TemplateId}, stacked={stacked}");
        }

        // Vanilla's small SPEC slot prefab, the one the equipped pack would otherwise use.
        private static SlotView FindTemplate()
        {
            if (_template != null) return _template;
            foreach (var view in Resources.FindObjectsOfTypeAll<SearchableSlotView>())
                if (view._specSlotTemplate != null && view.GetComponent<BackpackSlotMarker>() == null)
                    return _template = view._specSlotTemplate;
            return null;
        }
    }

    // Follows the grids: hidden until the pack is searched or while its contents are hidden.
    internal sealed class WindowSlotSync : MonoBehaviour
    {
        private SlotView _view;
        private ContainedGridsView _grids;
        private bool _stacked;
        private static readonly Vector3[] Corners = new Vector3[4];

        internal void Bind(SlotView view, ContainedGridsView grids, bool stacked)
        {
            _view = view;
            _grids = grids;
            _stacked = stacked;
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (_view == null || _grids == null) return;
            bool show = _grids.gameObject.activeSelf;
            if (_view.gameObject.activeSelf != show) _view.gameObject.SetActive(show);
            if (!show || _stacked) return;
            var rect = (RectTransform)_view.transform;
            var parent = (RectTransform)rect.parent;
            ((RectTransform)_grids.transform).GetWorldCorners(Corners);
            Vector2 bottomLeft = parent.InverseTransformPoint(Corners[0]);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(0, 1);
            if (((Vector2)rect.localPosition - bottomLeft).sqrMagnitude > .25f)
                rect.localPosition = bottomLeft;
        }
    }
}
