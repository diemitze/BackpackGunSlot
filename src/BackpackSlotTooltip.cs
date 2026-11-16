using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using EFT.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BackpackGunSlot
{
    // The ghost only shows the biggest class, so the tooltip lists them all.
    internal sealed class BackpackSlotTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly (string Id, string Name)[] Classes =
        {
            ("5447b5cf4bdc2d65278b4567", "pistols"),
            ("5447b5e04bdc2d62278b4567", "SMGs"),
            ("5447b6094bdc2dc3278b4567", "shotguns"),
            ("5447b5fc4bdc2d87278b4567", "carbines"),
            ("5447b5f14bdc2d61278b4567", "assault rifles"),
            ("5447b6194bdc2d67278b4567", "marksman rifles"),
            ("5447b6254bdc2dc3278b4568", "sniper rifles"),
            ("5447bed64bdc2d97278b4568", "machine guns"),
            ("5447bedf4bdc2d87278b4568", "grenade launchers"),
            ("5447bee84bdc2dc3278b4569", "special weapons"),
            ("617f1ef5e8b54b0998387733", "revolvers"),
            ("67446d4f04141c10630604e7", "rocket launchers"),
        };

        private Slot _slot;
        private string _text;
        private bool _shown;

        internal void Bind(Slot slot)
        {
            _slot = slot;
            var excluded = new HashSet<string>(slot.Filters.Where(f => f.ExcludedFilter != null)
                .SelectMany(f => f.ExcludedFilter).Select(id => id.ToString()));
            var allowed = new HashSet<string>(slot.Filters.SelectMany(f => f.Filter)
                .Select(id => id.ToString()).Where(id => !excluded.Contains(id)));
            var names = Classes.Where(c => allowed.Contains(c.Id)).Select(c => c.Name).ToList();
            _text = names.Count == 0 ? null : "Fits: " + string.Join(", ", names);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_text == null || _slot?.ContainedItem != null) return;
            var tooltip = ItemUiContext.Instance?.Tooltip;
            if (tooltip == null) return;
            tooltip.Show(_text, null, .5f);
            _shown = true;
        }

        public void OnPointerExit(PointerEventData eventData) => Hide();

        private void OnDisable() => Hide();

        private void Hide()
        {
            if (!_shown) return;
            _shown = false;
            var tooltip = ItemUiContext.Instance?.Tooltip;
            if (tooltip != null && tooltip.isActiveAndEnabled) tooltip.Close();
        }
    }
}
