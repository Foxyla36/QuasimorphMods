using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using MGSC;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class AugmentationsExpandedMod
    {
        internal const string ChipId    = "smasher_chip";
        internal const string BoxId     = SmasherTrash.BoxId;
        internal const string ModName   = "Augmentations Expanded";
        internal const string IconsModName = "Augmentations Expanded Icons";
        internal const string KeyFaction = "chip_faction";
        internal const string KeyTech       = "chip_techlevel";
        internal const string KeyBoxFaction = "box_faction";
        internal const string KeyBoxTech    = "box_techlevel";
        internal const string KeyEnabled    = "set_enabled";
        internal const string KeyMerc       = "merc_enabled";

        internal const string MercId        = "ae_merc";
        internal const string MercFaction   = "Dilthey";
        internal const int    MercTech      = 3;
        internal const string VanillaChipId = "augment_chip";
        internal const string KeyCraftChip  = "smasher_craft_chip";
        internal const string KeyAugTech    = "smasher_aug_techlevel";
        internal const string KeyPieceFaction = "smasher_piece_faction";

        internal const string NachtChipId    = "nacht_chip";
        internal const string KeyNachtFaction = "nacht_faction";
        internal const string KeyNachtTech    = "nacht_techlevel";
        internal const string KeyNacht        = "nacht_enabled";
        internal const string NachtBoxId          = SmasherTrash.NachtBoxId;
        internal const string KeyNachtBoxFaction  = "nacht_box_faction";
        internal const string KeyNachtBoxTech     = "nacht_box_techlevel";
        internal const string KeyNachtCraftChip   = "nacht_craft_chip";
        internal const string KeyNachtAugTech     = "nacht_aug_techlevel";
        internal const string KeyNachtPieceFaction = "nacht_piece_faction";

        internal const string Scout22ChipId         = "scout22_chip";
        internal const string KeyScoutFaction       = "scout22_faction";
        internal const string KeyScoutTech          = "scout22_techlevel";
        internal const string KeyScout              = "scout22_enabled";
        internal const string Scout22BoxId          = SmasherTrash.Scout22BoxId;
        internal const string KeyScoutBoxFaction    = "scout22_box_faction";
        internal const string KeyScoutBoxTech       = "scout22_box_techlevel";
        internal const string KeyScoutCraftChip     = "scout22_craft_chip";
        internal const string KeyScoutAugTech       = "scout22_aug_techlevel";
        internal const string KeyScoutPieceFaction  = "scout22_piece_faction";

        internal const string AndrasBoxId           = SmasherTrash.AndrasBoxId;
        internal const string KeyAndras             = "andras_enabled";
        internal const string KeyAndrasAugTech      = "andras_aug_techlevel";
        internal const string KeyAndrasPieceFaction = "andras_piece_faction";
        internal const string KeyAndrasBoxFaction   = "andras_box_faction";
        internal const string KeyAndrasBoxTech      = "andras_box_techlevel";

        internal const string NemesisChipId          = "nemesis_chip";
        internal const string NemesisBoxId           = SmasherTrash.NemesisBoxId;
        internal const string KeyNemesis             = "nemesis_enabled";
        internal const string KeyNemesisAugTech      = "nemesis_aug_techlevel";
        internal const string KeyNemesisPieceFaction = "nemesis_piece_faction";
        internal const string KeyNemesisFaction      = "nemesis_faction";
        internal const string KeyNemesisTech         = "nemesis_techlevel";
        internal const string KeyNemesisBoxFaction   = "nemesis_box_faction";
        internal const string KeyNemesisBoxTech      = "nemesis_box_techlevel";
        internal const string KeyNemesisCraftChip    = "nemesis_craft_chip";

        internal const string OpiChipId              = "opi_chip";
        internal const string OpiId                  = "ae_opi";
        internal const string KeyOpi                 = "opi_enabled";
        internal const string KeyOpiAugTech          = "opi_aug_techlevel";
        internal const string KeyOpiPieceFaction     = "opi_piece_faction";
        internal const string KeyOpiFaction          = "opi_faction";
        internal const string KeyOpiTech             = "opi_techlevel";
        internal const string KeyOpiCraftChip        = "opi_craft_chip";
        internal const string KeyOpiRewardFaction    = "opi_reward_faction";

        internal const string KeyHandFootRecipe   = "craft_hand_foot_required";
        internal const string KeyHandRecipeOld    = "craft_hand_required";
        internal const string KeyRankRestriction  = "rank_restriction";
        internal const string KeyMultiSlotLoss    = "multislot_amputation_loss";
        internal const string KeyVAClipping      = "enable_va_clipping";
        internal const string KeyMinRank          = "smasher_min_rank";
        internal const string KeyNachtMinRank     = "nacht_min_rank";
        internal const string KeyScoutMinRank     = "scout22_min_rank";
        internal const string KeyAndrasMinRank    = "andras_min_rank";
        internal const string KeyNemesisMinRank   = "nemesis_min_rank";
        internal const string KeyOpiMinRank       = "opi_min_rank";
        internal const string KeyPmc              = "smasher_pmc";
        internal const string KeyNachtPmc         = "nacht_pmc";
        internal const string KeyScoutPmc         = "scout22_pmc";
        internal const string KeyAndrasPmc        = "andras_pmc";
        internal const string KeyNemesisPmc       = "nemesis_pmc";
        internal const string KeyOpiPmc           = "opi_pmc";

        internal const string ConfigHeader  = "ae.cfg.header.modconfig";
        internal const string GeneralHeader = "ae.cfg.header.general";
        internal const string Header        = "ae.cfg.header.smasher";
        internal const string NachtHeader   = "ae.cfg.header.nacht";
        internal const string Scout22Header = "ae.cfg.header.scout22";
        internal const string AndrasHeader  = "ae.cfg.header.andras";
        internal const string NemesisHeader = "ae.cfg.header.nemesis";
        internal const string OpiHeader     = "ae.cfg.header.opi";
        internal const string SetlessHeader = "ae.cfg.header.setless";

        internal const string DefaultFaction = "RealWare";
        internal const int    DefaultTech    = 7;

        internal const int    DefaultBoxTech        = DefaultTech + 1;
        internal const int    NachtDefaultBoxTech   = NachtDefaultTech + 1;
        internal const int    Scout22DefaultBoxTech = Scout22DefaultTech + 1;

        internal const int    DefaultAugTech        = 7;
        internal const int    NachtDefaultAugTech   = 6;
        internal const int    Scout22DefaultAugTech = 3;

        internal const string NoFaction = "none";

        internal const string NachtDefaultFaction = "AnCom";
        internal const int    NachtDefaultTech    = 6;

        internal const string Scout22DefaultFaction = "FrancheComte";
        internal const int    Scout22DefaultTech    = 3;

        internal const string AndrasDefaultFaction  = "Tezctlan";
        internal const int    AndrasDefaultAugTech  = 5;
        internal const int    AndrasDefaultBoxTech  = 6;
        internal const string NemesisDefaultFaction = "DaydreamChems";
        internal const int    NemesisDefaultTech    = 6;
        internal const int    NemesisDefaultAugTech = 6;
        internal const int    NemesisDefaultBoxTech = 7;
        internal const string OpiDefaultFaction     = "UnchainedBelt";
        internal const int    OpiDefaultTech        = 10;
        internal const int    OpiDefaultAugTech     = 9;
        internal const bool   OpiDefaultCraftChip   = false;

        internal const int    DefaultMinRank        = 4;
        internal const int    NachtDefaultMinRank   = 4;
        internal const int    Scout22DefaultMinRank = 2;
        internal const int    AndrasDefaultMinRank  = 3;
        internal const int    NemesisDefaultMinRank = 2;
        internal const int    OpiDefaultMinRank     = 4;

        internal const bool   DefaultPmc        = false;
        internal const bool   NachtDefaultPmc   = false;
        internal const bool   Scout22DefaultPmc = true;
        internal const bool   AndrasDefaultPmc  = false;
        internal const bool   NemesisDefaultPmc = false;
        internal const bool   OpiDefaultPmc     = false;

        internal static string _faction    = DefaultFaction;
        internal static string _pieceFaction = DefaultFaction;
        internal static int    _techLevel  = DefaultTech;
        internal static string _boxFaction = DefaultFaction;
        internal static int    _boxTech    = DefaultBoxTech;
        internal static bool   _enabled    = true;
        internal static bool   _merc       = true;
        internal static bool   _craftChip  = true;
        internal static int    _augTech    = DefaultAugTech;
        internal static string _nFaction   = NachtDefaultFaction;
        internal static string _nPieceFaction = NachtDefaultFaction;
        internal static int    _nTech      = NachtDefaultTech;
        internal static bool   _nacht      = true;
        internal static string _nBoxFaction = NachtDefaultFaction;
        internal static int    _nBoxTech    = NachtDefaultBoxTech;
        internal static bool   _nCraftChip  = true;
        internal static int    _nAugTech    = NachtDefaultAugTech;
        internal static string _scFaction    = Scout22DefaultFaction;
        internal static string _scPieceFaction = Scout22DefaultFaction;
        internal static int    _scTech       = Scout22DefaultTech;
        internal static bool   _scout        = true;
        internal static string _scBoxFaction = Scout22DefaultFaction;
        internal static int    _scBoxTech    = Scout22DefaultBoxTech;
        internal static bool   _scCraftChip  = true;
        internal static int    _scAugTech    = Scout22DefaultAugTech;
        internal static bool   _andras          = true;
        internal static int    _anAugTech       = AndrasDefaultAugTech;
        internal static string _anPieceFaction  = AndrasDefaultFaction;
        internal static string _anBoxFaction    = AndrasDefaultFaction;
        internal static int    _anBoxTech       = AndrasDefaultBoxTech;
        internal static bool   _nemesis         = true;
        internal static int    _neAugTech       = NemesisDefaultAugTech;
        internal static string _nePieceFaction  = NemesisDefaultFaction;
        internal static string _neFaction       = NemesisDefaultFaction;
        internal static int    _neTech          = NemesisDefaultTech;
        internal static string _neBoxFaction    = NemesisDefaultFaction;
        internal static int    _neBoxTech       = NemesisDefaultBoxTech;
        internal static bool   _neCraftChip     = true;
        internal static bool   _opi             = true;
        internal static int    _opAugTech       = OpiDefaultAugTech;
        internal static string _opPieceFaction  = OpiDefaultFaction;
        internal static string _opFaction       = OpiDefaultFaction;
        internal static int    _opTech          = OpiDefaultTech;
        internal static bool   _opCraftChip     = OpiDefaultCraftChip;
        internal static string _opRewardFaction = OpiDefaultFaction;
        internal static bool   _handFootRecipe  = true;
        internal static bool   _rankRestriction = false;
        internal static bool   _multiSlotLoss   = false;
        internal static bool   _vaClipping      = false;
        internal static int    _minRank         = DefaultMinRank;
        internal static int    _nMinRank        = NachtDefaultMinRank;
        internal static int    _scMinRank       = Scout22DefaultMinRank;
        internal static int    _anMinRank       = AndrasDefaultMinRank;
        internal static int    _neMinRank       = NemesisDefaultMinRank;
        internal static int    _opMinRank       = OpiDefaultMinRank;
        internal static bool   _pmc             = DefaultPmc;
        internal static bool   _nPmc            = NachtDefaultPmc;
        internal static bool   _scPmc           = Scout22DefaultPmc;
        internal static bool   _anPmc           = AndrasDefaultPmc;
        internal static bool   _nePmc           = NemesisDefaultPmc;
        internal static bool   _opPmc           = OpiDefaultPmc;
        private static bool   _registered;

        internal static string ConfigDir => Path.Combine(
            Application.persistentDataPath + "/../Quasimorph_ModConfigs", "Augmentations_Expanded");

        private static string ConfigFile => Path.Combine(ConfigDir, "config.json");

        /// <summary>
        /// Entry point of the mod, called by the game once its config tables are loaded. Reads
        /// config.json and finds the icon packs, then registers the content in dependency order:
        /// wound slots first (an augmentation item cannot be created without them), then the
        /// firemodes, the ammo and the limb weapons that use them, the augmentations, the crates,
        /// the datadisks, the crafting recipes (every output and ingredient must already exist),
        /// the hand and foot ingredient switch, the localization and the wound slot images; every
        /// sprite follows the icon pack chosen for it. Afterwards it adds M.E.R.C., the gun arms
        /// and the UHMG to the vanilla augmentation chip, applies the chip, crate, PMC and reward
        /// settings, unlocks the chip conversion recipes, applies the spawn switches and patches
        /// the game for the clone rank restriction, the hidden effect lists (ae_kill_long_stats),
        /// the Multi-WoundSlot augmentations (ae_lose_augmentation_on_amputate) and the weapon
        /// crit bonus (ae_crit_chance_bonus). Every step has its own try/catch, so a failing step
        /// does not skip the others.
        /// </summary>
        /// <param name="context">The context given by the game. Its ModContentPath must be the
        /// folder of this mod. With null, no content is loaded.</param>
        /// <remarks>
        /// Input: The context, the json and png files under Assets, the icon packs, and
        /// config.json.
        /// Output: Nothing is returned. Adds the mod's records to the game data (wound slots,
        /// items, recipes, texts, wound slot images), applies the settings loaded from
        /// config.json or saved in the Mod Config Menu to them and to the reward and loot tables,
        /// and patches the augmentation install, its tooltip, the loss of amputated augmentations
        /// after a mission and the crit chance of the weapons with a bonus.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        [Hook(ModHookType.AfterConfigsLoaded)]
        public static void AfterConfigsLoaded(IModContext context)
        {
            try { LoadSettings(); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] LoadSettings: " + e); }
            // The optional bridge may receive the config hook before this class does.
            VisibleAugments.ModMain.RefreshClippingPolicy();

            string path = context == null ? null : context.ModContentPath;

            try { SmasherIcons.Init(path, ConfigDir); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] icon packs: " + e); }

            try { SmasherRecords.RegisterWoundSlots(path); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] wound slots: " + e); }

            try { SmasherFiremodes.Register(path); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] firemodes: " + e); }

            try { SmasherAmmo.Register(path); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] ammo: " + e); }

            try { SmasherWeapons.Register(path); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] weapons: " + e); }

            try { SmasherAugmentations.Register(path); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] augmentations: " + e); }

            try { SmasherTrash.Register(path); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] crate: " + e); }

            try { SmasherContent.RegisterDatadisks(path); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] datadisks: " + e); }

            try { SmasherContent.RegisterRecipes(path); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] recipes: " + e); }

            try { SmasherCrafting.SetHandFootRequired(_handFootRecipe); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] hand and foot in recipes: " + e); }

            try { SmasherContent.RegisterLocalization(path); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] localization: " + e); }

            try { SmasherWoundSlotIcons.Register(path); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] wound slot icons: " + e); }

            try
            {
                SmasherAugmentations.AddToChip(VanillaChipId, MercId);
                SmasherGunArms.AddToChip(VanillaChipId);
                SmasherUhmg.AddToChip(VanillaChipId);
            }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] chip pool: " + e); }

            try { ApplyToChip(); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] ApplyToChip: " + e); }

            try
            {
                SmasherCrafting.AddToStartReceipts(SmasherCrafting.SmasherChipOutput);
                SmasherCrafting.AddToStartReceipts(SmasherCrafting.NachtChipOutput);
                SmasherCrafting.AddToStartReceipts(SmasherCrafting.Scout22ChipOutput);
                SmasherCrafting.AddToStartReceipts(SmasherCrafting.NemesisChipOutput);
                SmasherCrafting.AddToStartReceipts(SmasherCrafting.OpiChipOutput);
            }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] starting recipes: " + e); }

            try { ApplySpawnToggle(); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] set toggle: " + e); }

            try { SmasherRankLock.Patch(); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] clone rank restriction: " + e); }

            try { SmasherLongStats.Patch(); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] long effect lists: " + e); }

            try { SmasherMultiSlot.Patch(); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] multi-wound slot augmentations: " + e); }

            try { SmasherWeapons.PatchCritBonus(); }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] weapon crit bonus: " + e); }
        }

        /// <summary>
        /// Applies every on/off switch to the loaded records: whether each set, M.E.R.C., each
        /// gun arm and the UHMG can spawn on enemies, the gun arm mode, whether the crates keep
        /// their loot tags, whether each chip conversion recipe is available (it needs both the
        /// set switch and the "can craft chip" switch) and whether each chip is added to the map
        /// containers (only when its set is on and its faction is not "none"). The OP-I chip is
        /// never added to the map containers: besides its own conversion recipe, off by default,
        /// it only comes from its faction's rewards.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the settings loaded from config.json or saved in the Mod
        /// Config Menu.
        /// Output: Nothing is returned. Changes the Categories of pieces and crates, the hand wound
        /// slots of the gun arms, the unlocked recipes and the map container loot tables, and
        /// writes a summary line in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void ApplySpawnToggle()
        {
            SmasherAugmentations.SetSpawnEnabled(SmasherAugmentations.SetSmasher, _enabled);
            SmasherAugmentations.SetSpawnEnabled(SmasherAugmentations.SetNachtreiher, _nacht);
            SmasherAugmentations.SetSpawnEnabled(SmasherAugmentations.SetScout22, _scout);
            SmasherAugmentations.SetSpawnEnabled(SmasherAugmentations.SetAndras, _andras);
            SmasherAugmentations.SetSpawnEnabled(SmasherAugmentations.SetNemesis, _nemesis);
            SmasherAugmentations.SetSpawnEnabled(SmasherAugmentations.SetOpi, _opi);
            SmasherAugmentations.SetSpawnEnabled(SmasherAugmentations.SetSetless, _merc);
            SmasherGunArms.ApplySpawn();
            SmasherGunArms.ApplyMode();
            SmasherUhmg.ApplySpawn();
            SmasherTrash.SetSpawnEnabled(SmasherAugmentations.SetSmasher, _enabled);
            SmasherTrash.SetSpawnEnabled(SmasherAugmentations.SetNachtreiher, _nacht);
            SmasherTrash.SetSpawnEnabled(SmasherAugmentations.SetScout22, _scout);
            SmasherTrash.SetSpawnEnabled(SmasherAugmentations.SetAndras, _andras);
            SmasherTrash.SetSpawnEnabled(SmasherAugmentations.SetNemesis, _nemesis);
            SmasherCrafting.SetEnabled(SmasherCrafting.SmasherChipOutput, _enabled && _craftChip);
            SmasherCrafting.SetEnabled(SmasherCrafting.NachtChipOutput,   _nacht   && _nCraftChip);
            SmasherCrafting.SetEnabled(SmasherCrafting.Scout22ChipOutput, _scout   && _scCraftChip);
            SmasherCrafting.SetEnabled(SmasherCrafting.NemesisChipOutput, _nemesis && _neCraftChip);
            SmasherCrafting.SetEnabled(SmasherCrafting.OpiChipOutput,     _opi     && _opCraftChip);

            ApplyMapDrop(ChipId,        _enabled && _faction   != NoFaction);
            ApplyMapDrop(NachtChipId,   _nacht   && _nFaction  != NoFaction);
            ApplyMapDrop(Scout22ChipId, _scout   && _scFaction != NoFaction);
            ApplyMapDrop(NemesisChipId, _nemesis && _neFaction != NoFaction);

            Debug.Log("[AugmentationsExpanded] Adam Smasher: " + (_enabled ? "ON" : "OFF")
                    + " (chip crafting " + (_enabled && _craftChip ? "ON" : "OFF") + ")"
                    + "   Nachtreiher: " + (_nacht ? "ON" : "OFF")
                    + " (chip crafting " + (_nacht && _nCraftChip ? "ON" : "OFF") + ")"
                    + "   SCOUT-22: " + (_scout ? "ON" : "OFF")
                    + " (chip crafting " + (_scout && _scCraftChip ? "ON" : "OFF") + ")"
                    + "   ANDRAS: " + (_andras ? "ON" : "OFF")
                    + "   Nemesis: " + (_nemesis ? "ON" : "OFF")
                    + " (chip crafting " + (_nemesis && _neCraftChip ? "ON" : "OFF") + ")"
                    + "   OP-I: " + (_opi ? "ON" : "OFF")
                    + " (chip crafting " + (_opi && _opCraftChip ? "ON" : "OFF") + ")"
                    + "   M.E.R.C.: " + (_merc ? "ON" : "OFF")
                    + "   Gun arms: " + SmasherGunArms.Summary()
                    + "   UHMG: " + SmasherUhmg.Summary());
        }

        /// <summary>
        /// Adds a set chip to the map container loot tables, or removes it from them. Both
        /// operations can be repeated safely.
        /// </summary>
        /// <param name="chipId">Id of a registered set chip, e.g. "smasher_chip".</param>
        /// <param name="wanted">True to add the chip, false to remove it.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Adds or removes the chip's rows in the map container loot
        /// tables.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void ApplyMapDrop(string chipId, bool wanted)
        {
            if (wanted) SmasherMapDrop.Apply(chipId);
            else        SmasherMapDrop.Remove(chipId);
        }

        /// <summary>
        /// Called when the ship view starts. Applies the chip conversion recipes to the campaign in
        /// progress, so a save started before the mod was installed gets them too, following the
        /// set and chip crafting switches.
        /// </summary>
        /// <param name="context">The context given by the game. Not used.</param>
        /// <remarks>
        /// Input: Reads the settings loaded from config.json or saved in the Mod Config Menu.
        /// Output: Nothing is returned. Adds or removes the five conversion recipes in the starting
        /// recipes and in the campaign in progress.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        [Hook(ModHookType.SpaceStarted)]
        public static void SpaceStarted(IModContext context)
        {
            SmasherCrafting.SetEnabled(SmasherCrafting.SmasherChipOutput, _enabled && _craftChip);
            SmasherCrafting.SetEnabled(SmasherCrafting.NachtChipOutput,   _nacht   && _nCraftChip);
            SmasherCrafting.SetEnabled(SmasherCrafting.Scout22ChipOutput, _scout   && _scCraftChip);
            SmasherCrafting.SetEnabled(SmasherCrafting.NemesisChipOutput, _nemesis && _neCraftChip);
            SmasherCrafting.SetEnabled(SmasherCrafting.OpiChipOutput,     _opi     && _opCraftChip);
        }

        /// <summary>
        /// Called when the main menu opens. Registers the two Mod Config Menu panels (settings and
        /// icons), once. When MCM is missing, disabled or incompatible the error is logged and the
        /// mod keeps running on the values of config.json.
        /// </summary>
        /// <param name="context">The context given by the game. Not used.</param>
        /// <remarks>
        /// Input: Reads the settings loaded from config.json or saved in the Mod Config Menu to
        /// fill the panels.
        /// Output: Nothing is returned. The first call registers the panels in MCM; later calls do
        /// nothing.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        [Hook(ModHookType.MainMenuStarted)]
        public static void MainMenuStarted(IModContext context)
        {
            if (_registered) return;
            _registered = true;

            try { RegisterConfigMenu(); }
            catch (Exception e)
            {
                Debug.Log("[AugmentationsExpanded] MCM not available, using config.json: "
                        + e.Message);
            }
        }

        /// <summary>
        /// The only call from this class into the Mod Config Menu code. It is a separate method
        /// that cannot be inlined, so the MCM types are resolved only when it runs: without MCM.dll
        /// the failure happens inside the try/catch of the caller instead of when the hook is
        /// compiled.
        /// </summary>
        /// <remarks>
        /// Input: No parameters.
        /// Output: Nothing is returned. Registers the panels in MCM; throws when MCM is missing or
        /// incompatible.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterConfigMenu()
        {
            ConfigMenu.Register();
        }

        /// <summary>
        /// Ids of the enabled factions in the game data, sorted, including factions added by other
        /// mods. Disabled factions (the feral ones and the unused ones) are left out because an
        /// item assigned to them would never drop. Used to fill the faction dropdowns.
        /// </summary>
        /// <returns>The faction ids, e.g. "AnCom", "CoreWard", ..., sorted ignoring case. Never
        /// null; empty when the factions cannot be read.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the factions in the game data.
        /// Output: The return value, plus one line in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static List<string> AllFactionTags()
        {
            var tags = new List<string>();
            int skipped = 0;
            try
            {
                foreach (var f in Data.Factions.Records)
                {
                    if (f == null || string.IsNullOrEmpty(f.Id)) continue;
                    if (!f.Enabled) { skipped++; continue; }
                    if (!tags.Contains(f.Id)) tags.Add(f.Id);
                }
            }
            catch (Exception e)
            {
                Debug.Log("[AugmentationsExpanded] reading factions failed: " + e.Message);
            }
            tags.Sort(StringComparer.OrdinalIgnoreCase);
            Debug.Log("[AugmentationsExpanded] selectable factions: " + tags.Count
                    + " (" + skipped + " disabled ones left out).");
            return tags;
        }

        /// <summary>
        /// Looks up a datadisk record by id. Unlike Data.Items.GetSimpleRecord, which throws on an
        /// unknown id, it returns null.
        /// </summary>
        /// <param name="chipId">Id of a datadisk, e.g. "nacht_chip". Any string is
        /// accepted.</param>
        /// <returns>The datadisk record, or null when the id is unknown, is not a datadisk or the
        /// lookup fails.</returns>
        /// <remarks>
        /// Input: The parameter and the items in the game data.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static DatadiskRecord FindChip(string chipId)
        {
            try
            {
                if (Data.Items == null) return null;
                var composite = Data.Items.GetRecord(chipId) as CompositeItemRecord;
                if (composite == null) return null;
                return composite.GetRecord<DatadiskRecord>();
            }
            catch (Exception e)
            {
                Debug.Log("[AugmentationsExpanded] reading record " + chipId + " failed: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Applies faction and tech level to a set chip: sets its TechLevel (clamped to 1-10),
        /// replaces its Categories ("Chip", "Science" and the faction, or only "none" when the set
        /// is off or has no faction) and rebuilds its row in the faction's chip reward table.
        /// </summary>
        /// <param name="chipId">Id of a set chip, e.g. "smasher_chip".</param>
        /// <param name="faction">A faction id from the game data, e.g. "RealWare", or "none" for no
        /// faction.</param>
        /// <param name="techLevel">Tech level, expected 1 to 10; the chip is clamped to 1-10, the
        /// reward row to the game's range.</param>
        /// <param name="enabled">Whether the chip's set is switched on.</param>
        /// <returns>False when the chip is not registered (nothing is changed); otherwise
        /// true.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. Changes the chip's TechLevel and Categories and its reward
        /// row, and writes a line in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool ApplyChip(string chipId, string faction, int techLevel, bool enabled)
        {
            var chip = FindChip(chipId);
            if (chip == null)
            {
                Debug.Log("[AugmentationsExpanded] record " + chipId + " not found: "
                        + "its Assets were not loaded.");
                return false;
            }

            chip.TechLevel = Mathf.Clamp(techLevel, 1, 10);
            chip.Categories = (!enabled || faction == NoFaction)
                ? new List<string> { NoFaction }
                : new List<string> { "Chip", "Science", faction };

            SmasherChipDrop.Apply(chipId, faction, chip.TechLevel, !enabled || faction == NoFaction);
            Debug.Log("[AugmentationsExpanded] " + chipId + " -> faction=" + faction
                    + " techlevel=" + chip.TechLevel + (enabled ? "" : "  (set off)"));
            return true;
        }

        /// <summary>
        /// Sets the tech level of a crate, the one shown on its tooltip, and rebuilds its row in
        /// the faction's equipment reward table. The crate stays out of the table when its set is
        /// off or its faction is "none".
        /// </summary>
        /// <param name="boxId">Id of a crate, e.g. "smasher_box".</param>
        /// <param name="faction">A faction id from the game data, e.g. "RealWare", or "none" for no
        /// faction.</param>
        /// <param name="techLevel">Tech level, expected 1 to 10; the crate is clamped to 1-10, the
        /// reward row to the game's range.</param>
        /// <param name="enabled">Whether the crate's set is switched on.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the crate's TechLevel and its reward row.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void ApplyBox(string boxId, string faction, int techLevel, bool enabled)
        {
            SmasherTrash.SetTechLevel(boxId, Mathf.Clamp(techLevel, 1, 10));
            SmasherChipDrop.ApplyBox(boxId, faction, techLevel, !enabled || faction == NoFaction);
        }

        /// <summary>
        /// Applies the Mod Config Menu values to the records: the chips, the tech level, preferred
        /// faction and PMC tag of the pieces of every set, the crates, and the reward rows of
        /// M.E.R.C., of the gun arms and of the UHMG. ANDRAS has no chip because it cannot be
        /// crafted; OP-I has no crate because it is a single augmentation, so the augmentation
        /// itself gets the crate's place in its reward faction's equipment table, from its own
        /// tech level.
        /// </summary>
        /// <returns>True when all five chip records exist and were updated; false when at least one
        /// is missing.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the settings loaded from config.json or saved in the Mod
        /// Config Menu.
        /// Output: The return value. Changes the chips, the pieces (tech level and tags), the
        /// crates and the faction reward tables.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool ApplyToChip()
        {
            bool ok = ApplyChip(ChipId, _faction, _techLevel, _enabled);
            ok &= ApplyChip(NachtChipId, _nFaction, _nTech, _nacht);
            ok &= ApplyChip(Scout22ChipId, _scFaction, _scTech, _scout);
            ok &= ApplyChip(NemesisChipId, _neFaction, _neTech, _nemesis);
            ok &= ApplyChip(OpiChipId, _opFaction, _opTech, _opi);

            SmasherAugmentations.SetTechLevel(SmasherAugmentations.SetSmasher, _augTech);
            SmasherAugmentations.SetTechLevel(SmasherAugmentations.SetNachtreiher, _nAugTech);
            SmasherAugmentations.SetTechLevel(SmasherAugmentations.SetScout22, _scAugTech);
            SmasherAugmentations.SetTechLevel(SmasherAugmentations.SetAndras, _anAugTech);
            SmasherAugmentations.SetTechLevel(SmasherAugmentations.SetNemesis, _neAugTech);
            SmasherAugmentations.SetTechLevel(SmasherAugmentations.SetOpi, _opAugTech);

            SmasherAugmentations.SetFaction(SmasherAugmentations.SetSmasher, _pieceFaction);
            SmasherAugmentations.SetFaction(SmasherAugmentations.SetNachtreiher, _nPieceFaction);
            SmasherAugmentations.SetFaction(SmasherAugmentations.SetScout22, _scPieceFaction);
            SmasherAugmentations.SetFaction(SmasherAugmentations.SetAndras, _anPieceFaction);
            SmasherAugmentations.SetFaction(SmasherAugmentations.SetNemesis, _nePieceFaction);
            SmasherAugmentations.SetFaction(SmasherAugmentations.SetOpi, _opPieceFaction);

            SmasherAugmentations.SetPmc(SmasherAugmentations.SetSmasher, _pmc);
            SmasherAugmentations.SetPmc(SmasherAugmentations.SetNachtreiher, _nPmc);
            SmasherAugmentations.SetPmc(SmasherAugmentations.SetScout22, _scPmc);
            SmasherAugmentations.SetPmc(SmasherAugmentations.SetAndras, _anPmc);
            SmasherAugmentations.SetPmc(SmasherAugmentations.SetNemesis, _nePmc);
            SmasherAugmentations.SetPmc(SmasherAugmentations.SetOpi, _opPmc);

            ApplyBox(BoxId,        _boxFaction,   _boxTech,   _enabled);
            ApplyBox(NachtBoxId,   _nBoxFaction,  _nBoxTech,  _nacht);
            ApplyBox(Scout22BoxId, _scBoxFaction, _scBoxTech, _scout);
            ApplyBox(AndrasBoxId,  _anBoxFaction, _anBoxTech, _andras);
            ApplyBox(NemesisBoxId, _neBoxFaction, _neBoxTech, _nemesis);
            SmasherChipDrop.ApplyBox(OpiId, _opRewardFaction, _opAugTech, !_opi || _opRewardFaction == NoFaction);

            SmasherChipDrop.ApplyAugmentation(MercId, MercFaction, MercTech, !_merc);
            SmasherGunArms.ApplyRewards();
            SmasherUhmg.ApplyRewards();
            return ok;
        }

        /// <summary>
        /// Called by the Mod Config Menu when Save is pressed. Copies the saved values into the
        /// settings, updates which fields the panel shows, applies everything to the records and
        /// writes config.json, because MCM does not store the values of this panel itself.
        /// </summary>
        /// <param name="config">The panel values keyed by config key, e.g. "chip_faction":
        /// "RealWare", "chip_techlevel": 7, "set_enabled": true. Missing keys keep their current
        /// value; null skips the reading.</param>
        /// <param name="feedback">Set by the method: the message MCM shows after saving.</param>
        /// <returns>True when the settings were saved and applied, also when a chip is not loaded
        /// yet (the message says so); false when an error occurred, with the error in
        /// feedback.</returns>
        /// <remarks>
        /// Input: The parameters and the current settings.
        /// Output: The return value and feedback. Changes the settings, the fields shown by the
        /// panel, the records and the reward and loot tables, and writes config.json.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static bool OnConfigSaved(Dictionary<string, object> config, out string feedback)
        {
            feedback = string.Empty;
            try
            {
                if (config != null)
                {
                    if (config.TryGetValue(KeyVAClipping, out var clipping) && clipping != null)
                        _vaClipping = Convert.ToBoolean(clipping);
                    object f;
                    if (config.TryGetValue(KeyFaction, out f) && f != null)
                        _faction = f.ToString();

                    object t;
                    if (config.TryGetValue(KeyTech, out t) && t != null)
                        _techLevel = Convert.ToInt32(t);

                    object bf;
                    if (config.TryGetValue(KeyBoxFaction, out bf) && bf != null)
                        _boxFaction = bf.ToString();

                    object bt;
                    if (config.TryGetValue(KeyBoxTech, out bt) && bt != null)
                        _boxTech = Convert.ToInt32(bt);

                    object e2;
                    if (config.TryGetValue(KeyEnabled, out e2) && e2 != null)
                        _enabled = Convert.ToBoolean(e2);

                    object lb;
                    if (config.TryGetValue(KeyMerc, out lb) && lb != null)
                        _merc = Convert.ToBoolean(lb);

                    object cc;
                    if (config.TryGetValue(KeyCraftChip, out cc) && cc != null)
                        _craftChip = Convert.ToBoolean(cc);

                    object at;
                    if (config.TryGetValue(KeyAugTech, out at) && at != null)
                        _augTech = Convert.ToInt32(at);

                    object pf;
                    if (config.TryGetValue(KeyPieceFaction, out pf) && pf != null)
                        _pieceFaction = pf.ToString();

                    object nf;
                    if (config.TryGetValue(KeyNachtFaction, out nf) && nf != null)
                        _nFaction = nf.ToString();

                    object nt;
                    if (config.TryGetValue(KeyNachtTech, out nt) && nt != null)
                        _nTech = Convert.ToInt32(nt);

                    object ne;
                    if (config.TryGetValue(KeyNacht, out ne) && ne != null)
                        _nacht = Convert.ToBoolean(ne);

                    object nbf;
                    if (config.TryGetValue(KeyNachtBoxFaction, out nbf) && nbf != null)
                        _nBoxFaction = nbf.ToString();

                    object nbt;
                    if (config.TryGetValue(KeyNachtBoxTech, out nbt) && nbt != null)
                        _nBoxTech = Convert.ToInt32(nbt);

                    object ncc;
                    if (config.TryGetValue(KeyNachtCraftChip, out ncc) && ncc != null)
                        _nCraftChip = Convert.ToBoolean(ncc);

                    object nat;
                    if (config.TryGetValue(KeyNachtAugTech, out nat) && nat != null)
                        _nAugTech = Convert.ToInt32(nat);

                    object sf;
                    if (config.TryGetValue(KeyScoutFaction, out sf) && sf != null)
                        _scFaction = sf.ToString();

                    object st;
                    if (config.TryGetValue(KeyScoutTech, out st) && st != null)
                        _scTech = Convert.ToInt32(st);

                    object se;
                    if (config.TryGetValue(KeyScout, out se) && se != null)
                        _scout = Convert.ToBoolean(se);

                    object sbf;
                    if (config.TryGetValue(KeyScoutBoxFaction, out sbf) && sbf != null)
                        _scBoxFaction = sbf.ToString();

                    object sbt;
                    if (config.TryGetValue(KeyScoutBoxTech, out sbt) && sbt != null)
                        _scBoxTech = Convert.ToInt32(sbt);

                    object scc;
                    if (config.TryGetValue(KeyScoutCraftChip, out scc) && scc != null)
                        _scCraftChip = Convert.ToBoolean(scc);

                    object sat;
                    if (config.TryGetValue(KeyScoutAugTech, out sat) && sat != null)
                        _scAugTech = Convert.ToInt32(sat);

                    object npf;
                    if (config.TryGetValue(KeyNachtPieceFaction, out npf) && npf != null)
                        _nPieceFaction = npf.ToString();

                    object spf;
                    if (config.TryGetValue(KeyScoutPieceFaction, out spf) && spf != null)
                        _scPieceFaction = spf.ToString();

                    object ae; if (config.TryGetValue(KeyAndras, out ae) && ae != null) _andras = Convert.ToBoolean(ae);
                    object aat; if (config.TryGetValue(KeyAndrasAugTech, out aat) && aat != null) _anAugTech = Convert.ToInt32(aat);
                    object apf; if (config.TryGetValue(KeyAndrasPieceFaction, out apf) && apf != null) _anPieceFaction = apf.ToString();
                    object abf; if (config.TryGetValue(KeyAndrasBoxFaction, out abf) && abf != null) _anBoxFaction = abf.ToString();
                    object abt; if (config.TryGetValue(KeyAndrasBoxTech, out abt) && abt != null) _anBoxTech = Convert.ToInt32(abt);

                    object nee; if (config.TryGetValue(KeyNemesis, out nee) && nee != null) _nemesis = Convert.ToBoolean(nee);
                    object neat; if (config.TryGetValue(KeyNemesisAugTech, out neat) && neat != null) _neAugTech = Convert.ToInt32(neat);
                    object nepf; if (config.TryGetValue(KeyNemesisPieceFaction, out nepf) && nepf != null) _nePieceFaction = nepf.ToString();
                    object nef; if (config.TryGetValue(KeyNemesisFaction, out nef) && nef != null) _neFaction = nef.ToString();
                    object net; if (config.TryGetValue(KeyNemesisTech, out net) && net != null) _neTech = Convert.ToInt32(net);
                    object nebf; if (config.TryGetValue(KeyNemesisBoxFaction, out nebf) && nebf != null) _neBoxFaction = nebf.ToString();
                    object nebt; if (config.TryGetValue(KeyNemesisBoxTech, out nebt) && nebt != null) _neBoxTech = Convert.ToInt32(nebt);
                    object necc; if (config.TryGetValue(KeyNemesisCraftChip, out necc) && necc != null) _neCraftChip = Convert.ToBoolean(necc);

                    object ope; if (config.TryGetValue(KeyOpi, out ope) && ope != null) _opi = Convert.ToBoolean(ope);
                    object opat; if (config.TryGetValue(KeyOpiAugTech, out opat) && opat != null) _opAugTech = Convert.ToInt32(opat);
                    object oppf; if (config.TryGetValue(KeyOpiPieceFaction, out oppf) && oppf != null) _opPieceFaction = oppf.ToString();
                    object opf; if (config.TryGetValue(KeyOpiFaction, out opf) && opf != null) _opFaction = opf.ToString();
                    object opt; if (config.TryGetValue(KeyOpiTech, out opt) && opt != null) _opTech = Convert.ToInt32(opt);
                    object opcc; if (config.TryGetValue(KeyOpiCraftChip, out opcc) && opcc != null) _opCraftChip = Convert.ToBoolean(opcc);
                    object oprf; if (config.TryGetValue(KeyOpiRewardFaction, out oprf) && oprf != null) _opRewardFaction = oprf.ToString();

                    object hr; if (config.TryGetValue(KeyHandFootRecipe, out hr) && hr != null) _handFootRecipe = Convert.ToBoolean(hr);
                    object rr; if (config.TryGetValue(KeyRankRestriction, out rr) && rr != null) _rankRestriction = Convert.ToBoolean(rr);
                    object msl; if (config.TryGetValue(KeyMultiSlotLoss, out msl) && msl != null) _multiSlotLoss = Convert.ToBoolean(msl);

                    object mr;   if (config.TryGetValue(KeyMinRank, out mr))          _minRank   = SmasherRanks.Parse(mr, _minRank);
                    object nmr;  if (config.TryGetValue(KeyNachtMinRank, out nmr))    _nMinRank  = SmasherRanks.Parse(nmr, _nMinRank);
                    object smr;  if (config.TryGetValue(KeyScoutMinRank, out smr))    _scMinRank = SmasherRanks.Parse(smr, _scMinRank);
                    object amr;  if (config.TryGetValue(KeyAndrasMinRank, out amr))   _anMinRank = SmasherRanks.Parse(amr, _anMinRank);
                    object nemr; if (config.TryGetValue(KeyNemesisMinRank, out nemr)) _neMinRank = SmasherRanks.Parse(nemr, _neMinRank);
                    object opmr; if (config.TryGetValue(KeyOpiMinRank, out opmr))     _opMinRank = SmasherRanks.Parse(opmr, _opMinRank);

                    object pm;   if (config.TryGetValue(KeyPmc, out pm) && pm != null)            _pmc   = Convert.ToBoolean(pm);
                    object npm;  if (config.TryGetValue(KeyNachtPmc, out npm) && npm != null)     _nPmc  = Convert.ToBoolean(npm);
                    object spm;  if (config.TryGetValue(KeyScoutPmc, out spm) && spm != null)     _scPmc = Convert.ToBoolean(spm);
                    object apm;  if (config.TryGetValue(KeyAndrasPmc, out apm) && apm != null)    _anPmc = Convert.ToBoolean(apm);
                    object nepm; if (config.TryGetValue(KeyNemesisPmc, out nepm) && nepm != null) _nePmc = Convert.ToBoolean(nepm);
                    object oppm; if (config.TryGetValue(KeyOpiPmc, out oppm) && oppm != null)     _opPmc = Convert.ToBoolean(oppm);

                    SmasherGunArms.ReadConfig(config);
                    SmasherUhmg.ReadConfig(config);
                }

                ConfigMenu.RefreshFields();
                VisibleAugments.ModMain.RefreshClippingPolicy();

                bool applied = ApplyToChip();
                ApplySpawnToggle();
                SmasherCrafting.SetHandFootRequired(_handFootRecipe);

                SaveSettings();

                if (!applied)
                {
                    feedback = "Saved, but a chip record is not loaded yet. "
                             + "It will be applied on the next start.";
                    return true;
                }

                string limbs = "   Nachtreiher: "
                             + (_nacht ? _nFaction + " TL" + _nTech
                                       + " / crate " + _nBoxFaction + " TL" + _nBoxTech : "off")
                             + ".   SCOUT-22: "
                             + (_scout ? _scFaction + " TL" + _scTech
                                       + " / crate " + _scBoxFaction + " TL" + _scBoxTech : "off")
                             + ".   ANDRAS: "
                             + (_andras ? "crate " + _anBoxFaction + " TL" + _anBoxTech : "off")
                             + ".   Nemesis: "
                             + (_nemesis ? _neFaction + " TL" + _neTech
                                         + " / crate " + _neBoxFaction + " TL" + _neBoxTech : "off")
                             + ".   OP-I: " + (_opi ? _opFaction + " TL" + _opTech
                                             + " / augmentation " + _opRewardFaction + " TL" + _opAugTech : "off")
                             + ".   M.E.R.C.: " + (_merc ? "on" : "off")
                             + ".   Gun arms: " + SmasherGunArms.Summary()
                             + ".   UHMG: " + SmasherUhmg.Summary() + ".";
                feedback = (!_enabled
                    ? "Smasher set disabled. Chip and crate settings come back after a restart."
                    : "Chip: " + _faction + " TL" + _techLevel
                    + "   Crate: " + _boxFaction + " TL" + _boxTech + ".") + limbs;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] saving config: " + e);
                feedback = "Error: " + e.Message;
                return false;
            }
        }

        /// <summary>
        /// Called by the Mod Config Menu when Save is pressed in the Augmentations Expanded Icons
        /// panel. Copies the chosen icon packs and the adaptive inventory size switch into the
        /// settings and writes config.json; sprites and item sizes are only set at startup, so
        /// the changes show after a restart.
        /// </summary>
        /// <param name="config">The panel values keyed by config key, e.g.
        /// "icons_smasher_items": "Default - Foxyla36", "adaptive_inventory_size": false. Null
        /// skips the reading.</param>
        /// <param name="feedback">Set by the method: the message MCM shows after saving.</param>
        /// <returns>True when the choices were saved; false when an error occurred, with the
        /// error in feedback.</returns>
        /// <remarks>
        /// Input: The parameters and the current settings.
        /// Output: The return value and feedback. Changes the icon pack choices and the adaptive
        /// size switch, and writes config.json.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static bool OnIconsSaved(Dictionary<string, object> config, out string feedback)
        {
            try
            {
                SmasherIcons.ReadConfig(config);
                SaveSettings();
                feedback = "Icon choices saved. Restart the game to apply them.";
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] saving icon choices: " + e);
                feedback = "Error: " + e.Message;
                return false;
            }
        }

        /// <summary>
        /// Minimum clone rank chosen for a set, used by the clone rank restriction.
        /// </summary>
        /// <param name="set">Set name, i.e. the subfolder name under Assets: "Smasher",
        /// "Nachtreiher", "Scout22", "Andras", "Nemesis" or "OPI". Null is accepted.</param>
        /// <returns>The rank number as saved (not clamped), or -1 for any other set, setless
        /// items included.</returns>
        /// <remarks>
        /// Input: The parameter and the rank settings.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static int MinRankOf(string set)
        {
            switch (set)
            {
                case SmasherAugmentations.SetSmasher:     return _minRank;
                case SmasherAugmentations.SetNachtreiher: return _nMinRank;
                case SmasherAugmentations.SetScout22:     return _scMinRank;
                case SmasherAugmentations.SetAndras:      return _anMinRank;
                case SmasherAugmentations.SetNemesis:     return _neMinRank;
                case SmasherAugmentations.SetOpi:         return _opMinRank;
                default:                                  return -1;
            }
        }

        /// <summary>
        /// Reads config.json into the settings. The file sits in
        /// Quasimorph_ModConfigs/Augmentations_Expanded, next to the game's persistent data folder,
        /// and is the one written by SaveSettings. Keys this class does not know are passed to
        /// SmasherGunArms, then to SmasherUhmg, then to SmasherIcons.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads config.json, one "key": value pair per line. A missing or
        /// unreadable file is not an error.
        /// Output: Nothing is returned. Changes the settings found in the file; the others keep
        /// their defaults.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void LoadSettings()
        {
            try
            {
                if (!File.Exists(ConfigFile)) return;
                foreach (var raw in File.ReadAllLines(ConfigFile))
                {
                    var line = raw.Trim().Trim(',', '{', '}');
                    var i = line.IndexOf(':');
                    if (i <= 0) continue;
                    var k = line.Substring(0, i).Trim().Trim('"');
                    var v = line.Substring(i + 1).Trim().Trim('"');
                    if (k == KeyFaction && v.Length > 0) _faction = v;
                    else if (k == KeyVAClipping) _vaClipping = v.Equals("true", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyTech && int.TryParse(v, out var n)) _techLevel = n;
                    else if (k == KeyBoxFaction && v.Length > 0) _boxFaction = v;
                    else if (k == KeyBoxTech && int.TryParse(v, out var bn)) _boxTech = bn;
                    else if (k == KeyEnabled) _enabled = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyMerc)    _merc    = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyCraftChip) _craftChip = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyAugTech && int.TryParse(v, out var an)) _augTech = an;
                    else if (k == KeyNachtAugTech && int.TryParse(v, out var nan)) _nAugTech = nan;
                    else if (k == KeyNachtCraftChip) _nCraftChip = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyNachtFaction && v.Length > 0) _nFaction = v;
                    else if (k == KeyNachtTech && int.TryParse(v, out var nn)) _nTech = nn;
                    else if (k == KeyNacht)   _nacht   = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyNachtBoxFaction && v.Length > 0) _nBoxFaction = v;
                    else if (k == KeyNachtBoxTech && int.TryParse(v, out var nbn)) _nBoxTech = nbn;
                    else if (k == KeyScoutFaction && v.Length > 0) _scFaction = v;
                    else if (k == KeyScoutTech && int.TryParse(v, out var sn)) _scTech = sn;
                    else if (k == KeyScout)   _scout   = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyScoutBoxFaction && v.Length > 0) _scBoxFaction = v;
                    else if (k == KeyScoutBoxTech && int.TryParse(v, out var sbn)) _scBoxTech = sbn;
                    else if (k == KeyScoutCraftChip) _scCraftChip = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyScoutAugTech && int.TryParse(v, out var san)) _scAugTech = san;
                    else if (k == KeyPieceFaction && v.Length > 0) _pieceFaction = v;
                    else if (k == KeyNachtPieceFaction && v.Length > 0) _nPieceFaction = v;
                    else if (k == KeyScoutPieceFaction && v.Length > 0) _scPieceFaction = v;
                    else if (k == KeyAndras) _andras = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyAndrasAugTech && int.TryParse(v, out var aan)) _anAugTech = aan;
                    else if (k == KeyAndrasPieceFaction && v.Length > 0) _anPieceFaction = v;
                    else if (k == KeyAndrasBoxFaction && v.Length > 0) _anBoxFaction = v;
                    else if (k == KeyAndrasBoxTech && int.TryParse(v, out var abn)) _anBoxTech = abn;
                    else if (k == KeyNemesis) _nemesis = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyNemesisAugTech && int.TryParse(v, out var nean)) _neAugTech = nean;
                    else if (k == KeyNemesisPieceFaction && v.Length > 0) _nePieceFaction = v;
                    else if (k == KeyNemesisFaction && v.Length > 0) _neFaction = v;
                    else if (k == KeyNemesisTech && int.TryParse(v, out var netn)) _neTech = netn;
                    else if (k == KeyNemesisBoxFaction && v.Length > 0) _neBoxFaction = v;
                    else if (k == KeyNemesisBoxTech && int.TryParse(v, out var nebn)) _neBoxTech = nebn;
                    else if (k == KeyNemesisCraftChip) _neCraftChip = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyOpi) _opi = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyOpiAugTech && int.TryParse(v, out var opan)) _opAugTech = opan;
                    else if (k == KeyOpiPieceFaction && v.Length > 0) _opPieceFaction = v;
                    else if (k == KeyOpiFaction && v.Length > 0) _opFaction = v;
                    else if (k == KeyOpiTech && int.TryParse(v, out var optn)) _opTech = optn;
                    else if (k == KeyOpiCraftChip) _opCraftChip = v.Equals("true", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyOpiRewardFaction && v.Length > 0) _opRewardFaction = v;
                    else if (k == KeyHandFootRecipe || k == KeyHandRecipeOld)
                        _handFootRecipe = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyRankRestriction) _rankRestriction = v.Equals("true", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyMultiSlotLoss) _multiSlotLoss = v.Equals("true", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyMinRank)        _minRank   = SmasherRanks.Parse(v, _minRank);
                    else if (k == KeyNachtMinRank)   _nMinRank  = SmasherRanks.Parse(v, _nMinRank);
                    else if (k == KeyScoutMinRank)   _scMinRank = SmasherRanks.Parse(v, _scMinRank);
                    else if (k == KeyAndrasMinRank)  _anMinRank = SmasherRanks.Parse(v, _anMinRank);
                    else if (k == KeyNemesisMinRank) _neMinRank = SmasherRanks.Parse(v, _neMinRank);
                    else if (k == KeyOpiMinRank)     _opMinRank = SmasherRanks.Parse(v, _opMinRank);
                    else if (k == KeyPmc)        _pmc   = v.Equals("true", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyNachtPmc)   _nPmc  = v.Equals("true", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyScoutPmc)   _scPmc = v.Equals("true", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyAndrasPmc)  _anPmc = v.Equals("true", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyNemesisPmc) _nePmc = v.Equals("true", StringComparison.OrdinalIgnoreCase);
                    else if (k == KeyOpiPmc)     _opPmc = v.Equals("true", StringComparison.OrdinalIgnoreCase);
                    else if (!SmasherGunArms.TryLoad(k, v) && !SmasherUhmg.TryLoad(k, v)) SmasherIcons.TryLoad(k, v);
                }
            }
            catch (Exception e)
            {
                Debug.Log("[AugmentationsExpanded] config.json is unreadable: " + e.Message);
            }
        }

        /// <summary>
        /// Writes every setting to config.json as flat JSON with one key per line, so LoadSettings
        /// can read it without a JSON parser and without MCM. The gun arm entries, the UHMG entry
        /// and then the icons panel entries (adaptive size switch and icon pack choices) come last,
        /// from SmasherGunArms.ConfigJsonTail, SmasherUhmg.ConfigJsonTail and
        /// SmasherIcons.ConfigJsonTail.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the current settings.
        /// Output: Nothing is returned. Creates the folder if needed and overwrites config.json; a
        /// failure is logged, not thrown.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                File.WriteAllText(ConfigFile,
                    "{\n"
                  + "  \"" + KeyVAClipping + "\": " + (_vaClipping ? "true" : "false") + ",\n"
                  + "  \"" + KeyFaction    + "\": \"" + _faction    + "\",\n"
                  + "  \"" + KeyTech       + "\": "   + _techLevel  + ",\n"
                  + "  \"" + KeyBoxFaction + "\": \"" + _boxFaction + "\",\n"
                  + "  \"" + KeyBoxTech    + "\": "   + _boxTech    + ",\n"
                  + "  \"" + KeyEnabled    + "\": "   + (_enabled ? "true" : "false") + ",\n"
                  + "  \"" + KeyMerc       + "\": "   + (_merc    ? "true" : "false") + ",\n"
                  + "  \"" + KeyCraftChip  + "\": "   + (_craftChip ? "true" : "false") + ",\n"
                  + "  \"" + KeyAugTech    + "\": "   + _augTech    + ",\n"
                  + "  \"" + KeyNachtFaction + "\": \"" + _nFaction + "\",\n"
                  + "  \"" + KeyNachtTech    + "\": "   + _nTech     + ",\n"
                  + "  \"" + KeyNacht        + "\": "   + (_nacht    ? "true" : "false") + ",\n"
                  + "  \"" + KeyNachtBoxFaction + "\": \"" + _nBoxFaction + "\",\n"
                  + "  \"" + KeyNachtBoxTech    + "\": "   + _nBoxTech     + ",\n"
                  + "  \"" + KeyNachtCraftChip  + "\": "   + (_nCraftChip ? "true" : "false") + ",\n"
                  + "  \"" + KeyNachtAugTech    + "\": "   + _nAugTech + ",\n"
                  + "  \"" + KeyScoutFaction    + "\": \"" + _scFaction + "\",\n"
                  + "  \"" + KeyScoutTech       + "\": "   + _scTech     + ",\n"
                  + "  \"" + KeyScout           + "\": "   + (_scout    ? "true" : "false") + ",\n"
                  + "  \"" + KeyScoutBoxFaction + "\": \"" + _scBoxFaction + "\",\n"
                  + "  \"" + KeyScoutBoxTech    + "\": "   + _scBoxTech     + ",\n"
                  + "  \"" + KeyScoutCraftChip  + "\": "   + (_scCraftChip ? "true" : "false") + ",\n"
                  + "  \"" + KeyScoutAugTech    + "\": "   + _scAugTech + ",\n"
                  + "  \"" + KeyPieceFaction      + "\": \"" + _pieceFaction   + "\",\n"
                  + "  \"" + KeyNachtPieceFaction + "\": \"" + _nPieceFaction  + "\",\n"
                  + "  \"" + KeyScoutPieceFaction + "\": \"" + _scPieceFaction + "\",\n"
                  + "  \"" + KeyAndras              + "\": "   + (_andras ? "true" : "false") + ",\n"
                  + "  \"" + KeyAndrasAugTech       + "\": "   + _anAugTech      + ",\n"
                  + "  \"" + KeyAndrasPieceFaction  + "\": \"" + _anPieceFaction + "\",\n"
                  + "  \"" + KeyAndrasBoxFaction    + "\": \"" + _anBoxFaction   + "\",\n"
                  + "  \"" + KeyAndrasBoxTech       + "\": "   + _anBoxTech      + ",\n"
                  + "  \"" + KeyNemesis             + "\": "   + (_nemesis ? "true" : "false") + ",\n"
                  + "  \"" + KeyNemesisAugTech      + "\": "   + _neAugTech      + ",\n"
                  + "  \"" + KeyNemesisPieceFaction + "\": \"" + _nePieceFaction + "\",\n"
                  + "  \"" + KeyNemesisFaction      + "\": \"" + _neFaction      + "\",\n"
                  + "  \"" + KeyNemesisTech         + "\": "   + _neTech         + ",\n"
                  + "  \"" + KeyNemesisBoxFaction   + "\": \"" + _neBoxFaction   + "\",\n"
                  + "  \"" + KeyNemesisBoxTech      + "\": "   + _neBoxTech      + ",\n"
                  + "  \"" + KeyNemesisCraftChip    + "\": "   + (_neCraftChip ? "true" : "false") + ",\n"
                  + "  \"" + KeyOpi                 + "\": "   + (_opi ? "true" : "false") + ",\n"
                  + "  \"" + KeyOpiAugTech          + "\": "   + _opAugTech      + ",\n"
                  + "  \"" + KeyOpiPieceFaction     + "\": \"" + _opPieceFaction + "\",\n"
                  + "  \"" + KeyOpiFaction          + "\": \"" + _opFaction      + "\",\n"
                  + "  \"" + KeyOpiTech             + "\": "   + _opTech         + ",\n"
                  + "  \"" + KeyOpiCraftChip        + "\": "   + (_opCraftChip ? "true" : "false") + ",\n"
                  + "  \"" + KeyOpiRewardFaction    + "\": \"" + _opRewardFaction + "\",\n"
                  + "  \"" + KeyHandFootRecipe      + "\": "   + (_handFootRecipe ? "true" : "false") + ",\n"
                  + "  \"" + KeyRankRestriction     + "\": "   + (_rankRestriction ? "true" : "false") + ",\n"
                  + "  \"" + KeyMultiSlotLoss       + "\": "   + (_multiSlotLoss ? "true" : "false") + ",\n"
                  + "  \"" + KeyMinRank             + "\": "   + _minRank   + ",\n"
                  + "  \"" + KeyNachtMinRank        + "\": "   + _nMinRank  + ",\n"
                  + "  \"" + KeyScoutMinRank        + "\": "   + _scMinRank + ",\n"
                  + "  \"" + KeyAndrasMinRank       + "\": "   + _anMinRank + ",\n"
                  + "  \"" + KeyNemesisMinRank      + "\": "   + _neMinRank + ",\n"
                  + "  \"" + KeyOpiMinRank          + "\": "   + _opMinRank + ",\n"
                  + "  \"" + KeyPmc                 + "\": "   + (_pmc   ? "true" : "false") + ",\n"
                  + "  \"" + KeyNachtPmc            + "\": "   + (_nPmc  ? "true" : "false") + ",\n"
                  + "  \"" + KeyScoutPmc            + "\": "   + (_scPmc ? "true" : "false") + ",\n"
                  + "  \"" + KeyAndrasPmc           + "\": "   + (_anPmc ? "true" : "false") + ",\n"
                  + "  \"" + KeyNemesisPmc          + "\": "   + (_nePmc ? "true" : "false") + ",\n"
                  + "  \"" + KeyOpiPmc              + "\": "   + (_opPmc ? "true" : "false")
                  + SmasherGunArms.ConfigJsonTail() + SmasherUhmg.ConfigJsonTail()
                  + SmasherIcons.ConfigJsonTail() + "\n}\n");
            }
            catch (Exception e)
            {
                Debug.Log("[AugmentationsExpanded] saving config failed: " + e.Message);
            }
        }
    }
}
