using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using UnityEngine;

namespace BackpackGunSlot
{
    [HarmonyPatch(typeof(EquipItemWindow), nameof(EquipItemWindow.Show),
        typeof(Slot), typeof(InventoryController), typeof(IEftSession), typeof(SkillManager), typeof(Vector3))]
    internal static class BackpackEquipWindowPatch
    {
        // The enum controls window layout and dog-tag handling only. Keep the real
        // Slot and ItemAddress, so selection still uses the backpack's own filters.
        internal static bool TryParseSlot(string id, out EquipmentSlot equipmentSlot)
        {
            if (string.Equals(id, Plugin.SlotName.Value?.Trim(), StringComparison.Ordinal))
            {
                equipmentSlot = EquipmentSlot.FirstPrimaryWeapon;
                return true;
            }
            return Enum.TryParse(id, out equipmentSlot);
        }

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var parse = typeof(Enum).GetMethods().Single(m => m.Name == nameof(Enum.TryParse) &&
                m.IsGenericMethodDefinition && m.GetParameters().Length == 2 &&
                m.GetParameters()[0].ParameterType == typeof(string)).MakeGenericMethod(typeof(EquipmentSlot));
            var replacement = AccessTools.Method(typeof(BackpackEquipWindowPatch), nameof(TryParseSlot));
            int replaced = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(parse))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                    replaced++;
                }
                yield return instruction;
            }
            if (replaced != 1)
                throw new InvalidOperationException("Backpack equip window: expected one EquipmentSlot parser.");
        }
    }

    [HarmonyPatch(typeof(ObjectsFactory), nameof(ObjectsFactory.AttachMods))]
    internal static class BackpackAttachmentContainersPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref ContainerCollection __0)
        {
            __0 = FilterForRendering(__0);
        }

        internal static ContainerCollection FilterForRendering(ContainerCollection collection)
        {
            if (collection == null) return null;
            var containers = collection.Containers.ToArray();
            if (!containers.Any(c => c is Slot slot && BackpackWeaponSlot.Matches(slot)))
                return collection;
            // Substitute only this method's argument. Do not patch async MoveNext:
            // detouring the struct state machine on Mono breaks task completion.
            return new AttachmentCollection(collection, containers
                .Where(c => c is not Slot slot || !BackpackWeaponSlot.Matches(slot)).ToArray());
        }

        private sealed class AttachmentCollection : ContainerCollection
        {
            private readonly ContainerCollection _original;
            private readonly IContainer[] _containers;

            internal AttachmentCollection(ContainerCollection original, IContainer[] containers)
                : base(original.Id, original.Template)
            {
                _original = original;
                _containers = containers;
            }

            public override IEnumerable<IContainer> Containers => _containers;
            public override IContainer GetContainer(string id) => _original.GetContainer(id);
            public override bool TryFindItem(string id, out Item item) => _original.TryFindItem(id, out item);
            public override bool TryFindItem(Func<Item, bool> filter, out Item item) => _original.TryFindItem(filter, out item);
        }
    }
}
