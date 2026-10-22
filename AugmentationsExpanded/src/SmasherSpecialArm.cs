using System;
using System.Reflection;
using HarmonyLib;
using MGSC;
using UnityEngine;

namespace AugmentationsExpanded
{
    internal static class SmasherSpecialArm
    {
        private const string HarmonyId = "Augmentations_Expanded.SpecialArm";
        private const string TagTextKey = "ae.tooltip.specialweaponarm";

        private static bool _patched;
        private static FieldInfo _builderFactory;

        /// <summary>
        /// Applies, once per launch, the patch that adds the line "This item is tagged as Special
        /// Weapon Arm" to the item tooltip of the augmentations tagged as Special Weapon Arm
        /// (Data.ae_special_weapon_arm in their json), the weapon arms whose primary slot block
        /// follows the special weapon arm mode of the Mod Config Menu.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the game's methods through reflection.
        /// Output: Nothing is returned. Patches ItemTooltipBuilder.InitAugmentation; when the
        /// patch cannot be applied it is logged and skipped.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void Patch()
        {
            if (_patched) return;
            _patched = true;

            _builderFactory = AccessTools.Field(typeof(ItemTooltipBuilder), "_factory");
            var original = AccessTools.Method(typeof(ItemTooltipBuilder), "InitAugmentation");
            if (original == null)
            {
                Debug.LogError("[AugmentationsExpanded] special weapon arm tag: a game method was not found, "
                             + "the tooltip line is disabled.");
                return;
            }
            try
            {
                new Harmony(HarmonyId).Patch(original,
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(SmasherSpecialArm), nameof(TooltipPostfix))));
                Debug.Log("[AugmentationsExpanded] special weapon arm tag: 1/1 patches applied.");
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] special weapon arm tag: patching "
                             + original.DeclaringType.Name + "." + original.Name + " failed: " + e);
            }
        }

        /// <summary>
        /// After the augmentation part of an item tooltip is built: adds the line "This item is
        /// tagged as Special Weapon Arm" for a tagged augmentation.
        /// </summary>
        /// <param name="__instance">The tooltip builder.</param>
        /// <param name="record">The augmentation record of the tooltip.</param>
        /// <remarks>
        /// Input: The parameters, the tags and the text ae.tooltip.specialweaponarm.
        /// Output: Nothing is returned. Adds one line to the tooltip.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void TooltipPostfix(ItemTooltipBuilder __instance, AugmentationRecord record)
        {
            try
            {
                if (record == null || _builderFactory == null || !SmasherAugmentations.IsSpecialArm(record.Id)) return;
                var factory = _builderFactory.GetValue(__instance) as TooltipFactory;
                if (factory == null) return;
                factory.AddPanelToTooltip().SetMultilineName(Localization.Get(TagTextKey))
                       .SetNameColor(Colors.Yellow);
            }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] special weapon arm tooltip: " + e.Message); }
        }
    }
}
