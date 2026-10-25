using System;
using System.Reflection;
using HarmonyLib;
using MGSC;
using UnityEngine;
using Mod = AugmentationsExpanded.AugmentationsExpandedMod;

namespace AugmentationsExpanded
{
    internal static class SmasherRankLock
    {
        private const string HarmonyId = "Augmentations_Expanded.RankLock";

        private static bool _patched;
        private static Mercenary _menuMerc;
        private static FieldInfo _screenMerc;
        private static FieldInfo _builderFactory;
        private static FieldInfo _factoryMerc;

        /// <summary>
        /// Applies, once per launch, the patches of the clone rank restriction: while it is
        /// switched on, a clone of the player can only install a piece of a set once its rank
        /// reaches the minimum chosen for that set. The game decides whether an augmentation can be
        /// installed only from global data (quasi classes and the station upgrade), with no hook
        /// for a single clone, so the mod uses Harmony, which ships with the game. The context
        /// menu of the ship screens remembers which clone it is open for; the game's "can this be
        /// installed" check then answers no for a piece above that clone's rank, so the Augment
        /// command is greyed out like a quasi augmentation without the station upgrade; installing
        /// such a piece is refused in case another path reaches it; and the item tooltip of a
        /// piece shows the minimum clone rank. Every patch adds to the game method instead of
        /// replacing it, acts only while the restriction is on, and is applied on its own, so a
        /// game update that renames one method only disables that part. Enemies are never
        /// affected: they get their augmentations through
        /// AugmentationSystem.TryApplyGeneratedAugmentation.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the game's methods through reflection.
        /// Output: Nothing is returned. Patches the game methods; every patch that cannot be
        /// applied is logged and skipped.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void Patch()
        {
            if (_patched) return;
            _patched = true;

            _screenMerc     = AccessTools.Field(typeof(ScreenWithShipCargo), "_merc");
            _builderFactory = AccessTools.Field(typeof(ItemTooltipBuilder), "_factory");
            _factoryMerc    = AccessTools.Field(typeof(TooltipFactory), "_currentMercenary");

            var harmony = new Harmony(HarmonyId);
            int ok = 0;
            ok += Apply(harmony, AccessTools.Method(typeof(ScreenWithShipCargo), "DragControllerShowContextMenuCallback"),
                        prefix: nameof(MenuPrefix), finalizer: nameof(MenuFinalizer));
            ok += Apply(harmony, AccessTools.Method(typeof(AugmentationSystem), nameof(AugmentationSystem.HasProgressToInstall)),
                        postfix: nameof(CanInstallPostfix));
            ok += Apply(harmony, AccessTools.Method(typeof(AugmentationSystem), nameof(AugmentationSystem.Augment)),
                        prefix: nameof(AugmentPrefix));
            ok += Apply(harmony, AccessTools.Method(typeof(ItemTooltipBuilder), "InitAugmentation"),
                        postfix: nameof(TooltipPostfix));
            Debug.Log("[AugmentationsExpanded] clone rank restriction: " + ok + "/4 patches applied.");
        }

        /// <summary>
        /// Applies one patch.
        /// </summary>
        /// <param name="harmony">The Harmony instance of the mod.</param>
        /// <param name="original">The game method to patch; null when it was not found.</param>
        /// <param name="prefix">Name of the prefix method of this class, or null.</param>
        /// <param name="postfix">Name of the postfix method of this class, or null.</param>
        /// <param name="finalizer">Name of the finalizer method of this class, or null.</param>
        /// <returns>1 when the patch was applied, 0 otherwise (logged).</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. Patches the method.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static int Apply(Harmony harmony, MethodBase original, string prefix = null,
                                 string postfix = null, string finalizer = null)
        {
            if (original == null)
            {
                Debug.LogError("[AugmentationsExpanded] rank restriction: a game method was not found, "
                             + "that part is disabled.");
                return 0;
            }
            try
            {
                harmony.Patch(original,
                              prefix:    Own(prefix),
                              postfix:   Own(postfix),
                              finalizer: Own(finalizer));
                return 1;
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] rank restriction: patching "
                             + original.DeclaringType.Name + "." + original.Name + " failed: " + e);
                return 0;
            }
        }

        /// <summary>
        /// A patch method of this class, for Harmony.
        /// </summary>
        /// <param name="name">Method name, or null.</param>
        /// <returns>The Harmony wrapper, or null for a null name.</returns>
        /// <remarks>
        /// Input: The parameter only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static HarmonyMethod Own(string name)
        {
            return name == null ? null : new HarmonyMethod(AccessTools.Method(typeof(SmasherRankLock), name));
        }

        /// <summary>
        /// Minimum rank needed to install an item.
        /// </summary>
        /// <param name="itemId">Item id, e.g. "smasher_head". Null is accepted.</param>
        /// <returns>The rank number, or -1 when the item is not a piece of one of the seven
        /// sets (OP-I and CMEDIC-32 included).</returns>
        /// <remarks>
        /// Input: The parameter, the sets of the mod's items and the rank settings.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static int RequiredRank(string itemId)
        {
            int rank = Mod.MinRankOf(SmasherAugmentations.SetOfItem(itemId));
            return rank < 0 ? -1 : SmasherRanks.Clamp(rank);
        }

        /// <summary>
        /// Whether the restriction stops a clone from installing an item: the restriction is on,
        /// the item is a set piece and the clone's rank is below the set's minimum. A clone whose
        /// rank cannot be read is never stopped.
        /// </summary>
        /// <param name="merc">The clone. Null gives false.</param>
        /// <param name="item">The item. Null gives false.</param>
        /// <returns>True when the install must be refused.</returns>
        /// <remarks>
        /// Input: The parameters and the rank settings.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static bool IsBlocked(Mercenary merc, BasePickupItem item)
        {
            if (!Mod._rankRestriction || merc == null || item == null) return false;
            int need = RequiredRank(item.Id);
            if (need <= 0) return false;
            int have = SmasherRanks.Of(merc);
            return have >= 0 && have < need;
        }

        /// <summary>
        /// Before the item context menu of a ship screen is built: remembers the clone the screen
        /// shows.
        /// </summary>
        /// <param name="__instance">The ship screen opening the menu.</param>
        /// <remarks>
        /// Input: The parameter.
        /// Output: Nothing is returned. Sets the remembered clone.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void MenuPrefix(ScreenWithShipCargo __instance)
        {
            try { _menuMerc = _screenMerc == null ? null : _screenMerc.GetValue(__instance) as Mercenary; }
            catch (Exception) { _menuMerc = null; }
        }

        /// <summary>
        /// After the item context menu is built, also when it failed: forgets the clone.
        /// </summary>
        /// <param name="__exception">The exception thrown by the game method, if any.</param>
        /// <returns>The same exception, so the game handles it as before.</returns>
        /// <remarks>
        /// Input: The parameter.
        /// Output: The return value. Clears the remembered clone.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static Exception MenuFinalizer(Exception __exception)
        {
            _menuMerc = null;
            return __exception;
        }

        /// <summary>
        /// After the game decides whether an augmentation can be installed (only asked while the
        /// context menu is built): answers no for a piece above the clone's rank, which greys out
        /// the Augment command.
        /// </summary>
        /// <param name="item">The item of the menu.</param>
        /// <param name="__result">The game's answer, changed to false when blocked.</param>
        /// <remarks>
        /// Input: The parameters, the remembered clone and the rank settings.
        /// Output: Changes __result.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void CanInstallPostfix(BasePickupItem item, ref bool __result)
        {
            try
            {
                if (__result && _menuMerc != null && IsBlocked(_menuMerc, item)) __result = false;
            }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] rank check: " + e.Message); }
        }

        /// <summary>
        /// Before an augmentation is installed on a clone: refuses a piece above the clone's rank,
        /// leaving the item where it is.
        /// </summary>
        /// <param name="mercenary">The clone.</param>
        /// <param name="item">The augmentation item.</param>
        /// <returns>False to skip the installation, true to let the game install it.</returns>
        /// <remarks>
        /// Input: The parameters and the rank settings.
        /// Output: The return value, plus a line in the log when refused.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool AugmentPrefix(Mercenary mercenary, BasePickupItem item)
        {
            try
            {
                if (!IsBlocked(mercenary, item)) return true;
                Debug.Log("[AugmentationsExpanded] " + item.Id + " not installed: it needs clone rank "
                        + SmasherRanks.Label(RequiredRank(item.Id)) + ".");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] rank check: " + e.Message);
                return true;
            }
        }

        /// <summary>
        /// After the augmentation part of an item tooltip is built: adds the minimum clone rank of
        /// a set piece, in yellow, or in red when the clone the screen shows is below it.
        /// </summary>
        /// <param name="__instance">The tooltip builder.</param>
        /// <param name="record">The augmentation record of the tooltip.</param>
        /// <remarks>
        /// Input: The parameters, the clone the tooltips are built for and the rank settings.
        /// Output: Nothing is returned. Adds one line to the tooltip.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void TooltipPostfix(ItemTooltipBuilder __instance, AugmentationRecord record)
        {
            try
            {
                if (!Mod._rankRestriction || record == null || _builderFactory == null) return;
                int need = RequiredRank(record.Id);
                if (need < 0) return;
                var factory = _builderFactory.GetValue(__instance) as TooltipFactory;
                if (factory == null) return;

                var merc = _factoryMerc == null ? null : _factoryMerc.GetValue(factory) as Mercenary;
                int have = SmasherRanks.Of(merc);
                bool belowRank = merc != null && have >= 0 && have < need;
                factory.AddPanelToTooltip().SetIcon("common_star").LocalizeName("ae.tooltip.minrank")
                       .SetValue(SmasherRanks.Label(need), false)
                       .SetValueColor(belowRank ? Colors.LightRed : Colors.Yellow);
            }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] rank tooltip: " + e.Message); }
        }
    }
}
