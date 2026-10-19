using System;
using System.Collections.Generic;
using System.Linq;
using MGSC;
using ModConfigMenu;
using ModConfigMenu.Contracts;
using ModConfigMenu.Implementations;
using ModConfigMenu.Objects;
using UnityEngine;
using Mod = AugmentationsExpanded.AugmentationsExpandedMod;

namespace AugmentationsExpanded
{
    internal static class ConfigMenu
    {
        private const string RestartColor = "#FF0000";
        private const string IconsInfoHeader = "ae.icons.header.info";

        private static List<IConfigValue> _config;
        private static IConfigValue _configNote;
        private static IConfigValue _restartNote;
        private static readonly List<IConfigValue> _generalFields = new List<IConfigValue>();
        private static readonly List<IConfigValue> _chipAndBoxFields = new List<IConfigValue>();
        private static readonly List<IConfigValue> _nachtFields = new List<IConfigValue>();
        private static readonly List<IConfigValue> _scoutFields = new List<IConfigValue>();
        private static readonly List<IConfigValue> _andrasFields = new List<IConfigValue>();
        private static readonly List<IConfigValue> _nemesisFields = new List<IConfigValue>();
        private static readonly List<IConfigValue> _opiFields = new List<IConfigValue>();
        private static IConfigValue _setToggle;
        private static IConfigValue _nachtToggle;
        private static IConfigValue _scoutToggle;
        private static IConfigValue _andrasToggle;
        private static IConfigValue _nemesisToggle;
        private static IConfigValue _opiToggle;
        private static IConfigValue _mercToggle;
        private static readonly List<IConfigValue> _gunArmToggles = new List<IConfigValue>();
        private static IConfigValue _uhmgToggle;

        /// <summary>
        /// Registers the two Mod Config Menu panels of the mod: "Augmentations Expanded" (the
        /// settings) and "Augmentations Expanded Icons" (the icon packs). MCM lists one entry per
        /// registered name, so they show as two separate panels, next to each other. Every use of
        /// an MCM type is kept inside this class, so the rest of the mod loads even when MCM.dll
        /// is missing; the caller wraps this call in a try/catch. MCM refuses a name registered
        /// twice, so this must run once per launch.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the settings loaded from config.json or saved in the Mod
        /// Config Menu, the factions, the clone ranks, the icon packs and the localized texts.
        /// Output: Nothing is returned. Both panels are registered in MCM with their save
        /// callbacks; throws when MCM is missing or its API changed. A failure of the icons panel
        /// alone is logged and leaves the settings panel in place.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void Register()
        {
            ModConfigMenuAPI.RegisterModConfig(Mod.ModName, BuildConfig(), Mod.OnConfigSaved);
            RefreshFields();

            try { ModConfigMenuAPI.RegisterModConfig(Mod.IconsModName, BuildIconsConfig(), Mod.OnIconsSaved); }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] icons panel not registered: " + e);
            }
        }

        /// <summary>
        /// Builds every entry of the settings panel: the MOD CONFIG introduction with the red
        /// restart reminder; the general switches (hand and foot in the recipes, clone rank
        /// restriction, loss of Multi-WoundSlot augmentations on amputation, gun arm mode); one
        /// switch per set, per gun arm and for the UHMG; and for each set the minimum clone rank,
        /// the preferred faction, the tech level of the pieces, the PMC switch, the crate faction
        /// and tech level, and, except for ANDRAS which has no chip, the chip crafting switch, the
        /// chip faction and the chip tech level. OP-I, a single augmentation, has no crate: in its
        /// place it has the reward faction of the augmentation itself, and its chip crafting switch
        /// is off by default. Labels are passed as localization keys, which MCM translates;
        /// tooltips are translated here because MCM does not do it.
        /// </summary>
        /// <returns>The list that MCM keeps and draws. It holds every entry, so MCM watches all of
        /// them for unsaved changes; RefreshFields then hides the fields of the sets that are off.
        /// Never null.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the settings loaded from config.json or saved in the Mod
        /// Config Menu as the values shown, the factions, the clone ranks and the localized
        /// texts.
        /// Output: The return value. Also fills the field lists kept by this class.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static List<IConfigValue> BuildConfig()
        {
            var factions = new List<object> { Mod.NoFaction };
            factions.AddRange(Mod.AllFactionTags().Cast<object>());

            var techLevels = new List<object>();
            for (int i = 1; i <= 10; i++) techLevels.Add(i);

            List<object> ranks = SmasherRanks.Options();

            _configNote = new ConfigValue("__ae_config_note", "ae.cfg.modconfig.note", Mod.ConfigHeader);
            _restartNote = new ConfigValue("__ae_restart_note", RestartNote(), Mod.ConfigHeader);

            _generalFields.Clear();
            _generalFields.Add(new ConfigValue(Mod.KeyVAClipping, Mod._vaClipping, Mod.GeneralHeader, false,
                L("ae.cfg.vaclipping.tip"), "ae.cfg.vaclipping.label"));
            _generalFields.Add(new ConfigValue(
                Mod.KeyHandFootRecipe, Mod._handFootRecipe, Mod.GeneralHeader, true,
                L("ae.cfg.handfootrecipe.tip"), "ae.cfg.handfootrecipe.label"));
            _generalFields.Add(new ConfigValue(
                Mod.KeyRankRestriction, Mod._rankRestriction, Mod.GeneralHeader, false,
                L("ae.cfg.rankrestriction.tip"), "ae.cfg.rankrestriction.label"));
            _generalFields.Add(new ConfigValue(
                Mod.KeyMultiSlotLoss, Mod._multiSlotLoss, Mod.GeneralHeader, false,
                MultiSlotTooltip(), "ae.cfg.multislotloss.label"));
            _generalFields.Add(new DropdownConfig(
                SmasherGunArms.KeyMode, SmasherGunArms.Mode, Mod.GeneralHeader, SmasherGunArms.ModeVanilla,
                L(SmasherGunArms.ModeTipKey), SmasherGunArms.ModeLabelKey,
                SmasherGunArms.Modes.Cast<object>().ToList()));

            _setToggle = new ConfigValue(
                Mod.KeyEnabled, Mod._enabled, Mod.Header, true,
                SetTooltip("ae.cfg.setname.smasher"),
                "ae.cfg.smasher.toggle.label");

            _nachtToggle = new ConfigValue(
                Mod.KeyNacht, Mod._nacht, Mod.NachtHeader, true,
                SetTooltip("ae.cfg.setname.nacht"),
                "ae.cfg.nacht.toggle.label");

            _scoutToggle = new ConfigValue(
                Mod.KeyScout, Mod._scout, Mod.Scout22Header, true,
                SetTooltip("ae.cfg.setname.scout22"),
                "ae.cfg.scout22.toggle.label");

            _andrasToggle = new ConfigValue(
                Mod.KeyAndras, Mod._andras, Mod.AndrasHeader, true,
                SetTooltip("ae.cfg.setname.andras", "ae.cfg.set.toggle.nochip.tip"),
                "ae.cfg.andras.toggle.label");

            _nemesisToggle = new ConfigValue(
                Mod.KeyNemesis, Mod._nemesis, Mod.NemesisHeader, true,
                SetTooltip("ae.cfg.setname.nemesis"),
                "ae.cfg.nemesis.toggle.label");

            _opiToggle = new ConfigValue(
                Mod.KeyOpi, Mod._opi, Mod.OpiHeader, true,
                SetTooltip("ae.cfg.setname.opi", "ae.cfg.set.toggle.nocrate.tip"),
                "ae.cfg.opi.toggle.label");

            _mercToggle = new ConfigValue(
                Mod.KeyMerc, Mod._merc, Mod.SetlessHeader, true,
                L("ae.cfg.merc.toggle.tip"),
                "ae.cfg.merc.toggle.label");

            _gunArmToggles.Clear();
            foreach (SmasherGunArms.GunArm arm in SmasherGunArms.All)
                _gunArmToggles.Add(new ConfigValue(
                    arm.ConfigKey, arm.Enabled, Mod.SetlessHeader, true,
                    L(arm.TipKey), arm.LabelKey));

            _uhmgToggle = new ConfigValue(
                SmasherUhmg.ConfigKey, SmasherUhmg.Enabled, Mod.SetlessHeader, true,
                L(SmasherUhmg.TipKey), SmasherUhmg.LabelKey);

            string TipPieceAndras  = PieceFactionTooltip(SmasherAugmentations.SetAndras);
            string TipPieceNemesis = PieceFactionTooltip(SmasherAugmentations.SetNemesis);
            string TipPieceSmasher = PieceFactionTooltip(SmasherAugmentations.SetSmasher);
            string TipPieceNacht   = PieceFactionTooltip(SmasherAugmentations.SetNachtreiher);
            string TipPieceScout   = PieceFactionTooltip(SmasherAugmentations.SetScout22);
            string TipPieceOpi     = PieceFactionTooltip(SmasherAugmentations.SetOpi);
            string TipAug          = L("ae.cfg.augtech.tip");
            string TipAugSingle    = L("ae.cfg.augtech.single.tip");
            string TipAugNoChip    = L("ae.cfg.augtech.nochip.tip");
            string TipCrateFaction = L("ae.cfg.cratefaction.tip");
            string TipCrateTech    = L("ae.cfg.cratetech.tip");
            string TipChipFaction  = L("ae.cfg.chipfaction.tip");
            string TipChipTech     = L("ae.cfg.chiptech.tip");

            _chipAndBoxFields.Clear();
            _chipAndBoxFields.Add(RankDropdown(
                Mod.KeyMinRank, Mod._minRank, Mod.Header, Mod.DefaultMinRank, "ae.cfg.setname.smasher", ranks));
            _chipAndBoxFields.Add(new DropdownConfig(
                Mod.KeyPieceFaction, Mod._pieceFaction, Mod.Header, Mod.DefaultFaction,
                TipPieceSmasher, "ae.cfg.piecefaction.label", new List<object>(factions)));
            _chipAndBoxFields.Add(new DropdownConfig(
                Mod.KeyAugTech, Mod._augTech, Mod.Header, Mod.DefaultAugTech,
                TipAug, "ae.cfg.augtech.label", new List<object>(techLevels)));
            _chipAndBoxFields.Add(PmcToggle(
                Mod.KeyPmc, Mod._pmc, Mod.Header, Mod.DefaultPmc, "ae.cfg.setname.smasher"));
            _chipAndBoxFields.Add(new DropdownConfig(
                Mod.KeyBoxFaction, Mod._boxFaction, Mod.Header, Mod.DefaultFaction,
                TipCrateFaction, "ae.cfg.cratefaction.label", new List<object>(factions)));
            _chipAndBoxFields.Add(new DropdownConfig(
                Mod.KeyBoxTech, Mod._boxTech, Mod.Header, Mod.DefaultBoxTech,
                TipCrateTech, "ae.cfg.cratetech.label", new List<object>(techLevels)));
            _chipAndBoxFields.Add(new ConfigValue(
                Mod.KeyCraftChip, Mod._craftChip, Mod.Header, true,
                CraftChipTooltip("ae.cfg.setname.smasher"),
                "ae.cfg.craftchip.label"));
            _chipAndBoxFields.Add(new DropdownConfig(
                Mod.KeyFaction, Mod._faction, Mod.Header, Mod.DefaultFaction,
                TipChipFaction, "ae.cfg.chipfaction.label", factions));
            _chipAndBoxFields.Add(new DropdownConfig(
                Mod.KeyTech, Mod._techLevel, Mod.Header, Mod.DefaultTech,
                TipChipTech, "ae.cfg.chiptech.label", techLevels));

            _nachtFields.Clear();
            _nachtFields.Add(RankDropdown(
                Mod.KeyNachtMinRank, Mod._nMinRank, Mod.NachtHeader, Mod.NachtDefaultMinRank, "ae.cfg.setname.nacht", ranks));
            _nachtFields.Add(new DropdownConfig(
                Mod.KeyNachtPieceFaction, Mod._nPieceFaction, Mod.NachtHeader, Mod.NachtDefaultFaction,
                TipPieceNacht, "ae.cfg.piecefaction.label", new List<object>(factions)));
            _nachtFields.Add(new DropdownConfig(
                Mod.KeyNachtAugTech, Mod._nAugTech, Mod.NachtHeader, Mod.NachtDefaultAugTech,
                TipAug, "ae.cfg.augtech.label", new List<object>(techLevels)));
            _nachtFields.Add(PmcToggle(
                Mod.KeyNachtPmc, Mod._nPmc, Mod.NachtHeader, Mod.NachtDefaultPmc, "ae.cfg.setname.nacht"));
            _nachtFields.Add(new DropdownConfig(
                Mod.KeyNachtBoxFaction, Mod._nBoxFaction, Mod.NachtHeader, Mod.NachtDefaultFaction,
                TipCrateFaction, "ae.cfg.cratefaction.label", new List<object>(factions)));
            _nachtFields.Add(new DropdownConfig(
                Mod.KeyNachtBoxTech, Mod._nBoxTech, Mod.NachtHeader, Mod.NachtDefaultBoxTech,
                TipCrateTech, "ae.cfg.cratetech.label", new List<object>(techLevels)));
            _nachtFields.Add(new ConfigValue(
                Mod.KeyNachtCraftChip, Mod._nCraftChip, Mod.NachtHeader, true,
                CraftChipTooltip("ae.cfg.setname.nacht"),
                "ae.cfg.craftchip.label"));
            _nachtFields.Add(new DropdownConfig(
                Mod.KeyNachtFaction, Mod._nFaction, Mod.NachtHeader, Mod.NachtDefaultFaction,
                TipChipFaction, "ae.cfg.chipfaction.label", new List<object>(factions)));
            _nachtFields.Add(new DropdownConfig(
                Mod.KeyNachtTech, Mod._nTech, Mod.NachtHeader, Mod.NachtDefaultTech,
                TipChipTech, "ae.cfg.chiptech.label", new List<object>(techLevels)));

            _scoutFields.Clear();
            _scoutFields.Add(RankDropdown(
                Mod.KeyScoutMinRank, Mod._scMinRank, Mod.Scout22Header, Mod.Scout22DefaultMinRank, "ae.cfg.setname.scout22", ranks));
            _scoutFields.Add(new DropdownConfig(
                Mod.KeyScoutPieceFaction, Mod._scPieceFaction, Mod.Scout22Header, Mod.Scout22DefaultFaction,
                TipPieceScout, "ae.cfg.piecefaction.label", new List<object>(factions)));
            _scoutFields.Add(new DropdownConfig(
                Mod.KeyScoutAugTech, Mod._scAugTech, Mod.Scout22Header, Mod.Scout22DefaultAugTech,
                TipAug, "ae.cfg.augtech.label", new List<object>(techLevels)));
            _scoutFields.Add(PmcToggle(
                Mod.KeyScoutPmc, Mod._scPmc, Mod.Scout22Header, Mod.Scout22DefaultPmc, "ae.cfg.setname.scout22"));
            _scoutFields.Add(new DropdownConfig(
                Mod.KeyScoutBoxFaction, Mod._scBoxFaction, Mod.Scout22Header, Mod.Scout22DefaultFaction,
                TipCrateFaction, "ae.cfg.cratefaction.label", new List<object>(factions)));
            _scoutFields.Add(new DropdownConfig(
                Mod.KeyScoutBoxTech, Mod._scBoxTech, Mod.Scout22Header, Mod.Scout22DefaultBoxTech,
                TipCrateTech, "ae.cfg.cratetech.label", new List<object>(techLevels)));
            _scoutFields.Add(new ConfigValue(
                Mod.KeyScoutCraftChip, Mod._scCraftChip, Mod.Scout22Header, true,
                CraftChipTooltip("ae.cfg.setname.scout22"),
                "ae.cfg.craftchip.label"));
            _scoutFields.Add(new DropdownConfig(
                Mod.KeyScoutFaction, Mod._scFaction, Mod.Scout22Header, Mod.Scout22DefaultFaction,
                TipChipFaction, "ae.cfg.chipfaction.label", new List<object>(factions)));
            _scoutFields.Add(new DropdownConfig(
                Mod.KeyScoutTech, Mod._scTech, Mod.Scout22Header, Mod.Scout22DefaultTech,
                TipChipTech, "ae.cfg.chiptech.label", new List<object>(techLevels)));

            _andrasFields.Clear();
            _andrasFields.Add(RankDropdown(
                Mod.KeyAndrasMinRank, Mod._anMinRank, Mod.AndrasHeader, Mod.AndrasDefaultMinRank, "ae.cfg.setname.andras", ranks));
            _andrasFields.Add(new DropdownConfig(
                Mod.KeyAndrasPieceFaction, Mod._anPieceFaction, Mod.AndrasHeader, Mod.AndrasDefaultFaction,
                TipPieceAndras, "ae.cfg.piecefaction.label", new List<object>(factions)));
            _andrasFields.Add(new DropdownConfig(
                Mod.KeyAndrasAugTech, Mod._anAugTech, Mod.AndrasHeader, Mod.AndrasDefaultAugTech,
                TipAugNoChip, "ae.cfg.augtech.label", new List<object>(techLevels)));
            _andrasFields.Add(PmcToggle(
                Mod.KeyAndrasPmc, Mod._anPmc, Mod.AndrasHeader, Mod.AndrasDefaultPmc, "ae.cfg.setname.andras"));
            _andrasFields.Add(new DropdownConfig(
                Mod.KeyAndrasBoxFaction, Mod._anBoxFaction, Mod.AndrasHeader, Mod.AndrasDefaultFaction,
                TipCrateFaction, "ae.cfg.cratefaction.label", new List<object>(factions)));
            _andrasFields.Add(new DropdownConfig(
                Mod.KeyAndrasBoxTech, Mod._anBoxTech, Mod.AndrasHeader, Mod.AndrasDefaultBoxTech,
                TipCrateTech, "ae.cfg.cratetech.label", new List<object>(techLevels)));

            _nemesisFields.Clear();
            _nemesisFields.Add(RankDropdown(
                Mod.KeyNemesisMinRank, Mod._neMinRank, Mod.NemesisHeader, Mod.NemesisDefaultMinRank, "ae.cfg.setname.nemesis", ranks));
            _nemesisFields.Add(new DropdownConfig(
                Mod.KeyNemesisPieceFaction, Mod._nePieceFaction, Mod.NemesisHeader, Mod.NemesisDefaultFaction,
                TipPieceNemesis, "ae.cfg.piecefaction.label", new List<object>(factions)));
            _nemesisFields.Add(new DropdownConfig(
                Mod.KeyNemesisAugTech, Mod._neAugTech, Mod.NemesisHeader, Mod.NemesisDefaultAugTech,
                TipAug, "ae.cfg.augtech.label", new List<object>(techLevels)));
            _nemesisFields.Add(PmcToggle(
                Mod.KeyNemesisPmc, Mod._nePmc, Mod.NemesisHeader, Mod.NemesisDefaultPmc, "ae.cfg.setname.nemesis"));
            _nemesisFields.Add(new DropdownConfig(
                Mod.KeyNemesisBoxFaction, Mod._neBoxFaction, Mod.NemesisHeader, Mod.NemesisDefaultFaction,
                TipCrateFaction, "ae.cfg.cratefaction.label", new List<object>(factions)));
            _nemesisFields.Add(new DropdownConfig(
                Mod.KeyNemesisBoxTech, Mod._neBoxTech, Mod.NemesisHeader, Mod.NemesisDefaultBoxTech,
                TipCrateTech, "ae.cfg.cratetech.label", new List<object>(techLevels)));
            _nemesisFields.Add(new ConfigValue(
                Mod.KeyNemesisCraftChip, Mod._neCraftChip, Mod.NemesisHeader, true,
                CraftChipTooltip("ae.cfg.setname.nemesis"),
                "ae.cfg.craftchip.label"));
            _nemesisFields.Add(new DropdownConfig(
                Mod.KeyNemesisFaction, Mod._neFaction, Mod.NemesisHeader, Mod.NemesisDefaultFaction,
                TipChipFaction, "ae.cfg.chipfaction.label", new List<object>(factions)));
            _nemesisFields.Add(new DropdownConfig(
                Mod.KeyNemesisTech, Mod._neTech, Mod.NemesisHeader, Mod.NemesisDefaultTech,
                TipChipTech, "ae.cfg.chiptech.label", new List<object>(techLevels)));

            _opiFields.Clear();
            _opiFields.Add(RankDropdown(
                Mod.KeyOpiMinRank, Mod._opMinRank, Mod.OpiHeader, Mod.OpiDefaultMinRank, "ae.cfg.setname.opi", ranks));
            _opiFields.Add(new DropdownConfig(
                Mod.KeyOpiPieceFaction, Mod._opPieceFaction, Mod.OpiHeader, Mod.OpiDefaultFaction,
                TipPieceOpi, "ae.cfg.piecefaction.label", new List<object>(factions)));
            _opiFields.Add(new DropdownConfig(
                Mod.KeyOpiAugTech, Mod._opAugTech, Mod.OpiHeader, Mod.OpiDefaultAugTech,
                TipAugSingle, "ae.cfg.augtech.label", new List<object>(techLevels)));
            _opiFields.Add(PmcToggle(
                Mod.KeyOpiPmc, Mod._opPmc, Mod.OpiHeader, Mod.OpiDefaultPmc, "ae.cfg.setname.opi"));
            _opiFields.Add(new DropdownConfig(
                Mod.KeyOpiRewardFaction, Mod._opRewardFaction, Mod.OpiHeader, Mod.OpiDefaultFaction,
                L("ae.cfg.rewardfaction.tip"), "ae.cfg.rewardfaction.label", new List<object>(factions)));
            _opiFields.Add(new ConfigValue(
                Mod.KeyOpiCraftChip, Mod._opCraftChip, Mod.OpiHeader, Mod.OpiDefaultCraftChip,
                CraftChipTooltip("ae.cfg.setname.opi"),
                "ae.cfg.craftchip.label"));
            _opiFields.Add(new DropdownConfig(
                Mod.KeyOpiFaction, Mod._opFaction, Mod.OpiHeader, Mod.OpiDefaultFaction,
                TipChipFaction, "ae.cfg.chipfaction.label", new List<object>(factions)));
            _opiFields.Add(new DropdownConfig(
                Mod.KeyOpiTech, Mod._opTech, Mod.OpiHeader, Mod.OpiDefaultTech,
                TipChipTech, "ae.cfg.chiptech.label", new List<object>(techLevels)));

            _config = new List<IConfigValue>();
            Fill(true);
            return _config;
        }

        /// <summary>
        /// Rebuilds, in order, the list that MCM draws, leaving out the fields of the sets that are
        /// switched off. MCM cannot draw a disabled field and builds its interface once, so the
        /// change shows after a restart.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the set switches of the settings.
        /// Output: Nothing is returned. Changes the content of the panel list; does nothing when
        /// the panel was never built, as happens without MCM.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void RefreshFields()
        {
            if (_config == null || _setToggle == null) return;
            Fill(false);
        }

        /// <summary>
        /// Fills the list that MCM draws, in panel order: introduction and restart reminder,
        /// general switches, the six sets with their fields, then the setless augments. MCM
        /// groups the entries by header in the order the headers first appear, so this order is
        /// the order of the sections on screen.
        /// </summary>
        /// <param name="everything">True to add the fields of every set, as needed when the panel
        /// is registered; false to leave out the fields of the sets that are switched
        /// off.</param>
        /// <remarks>
        /// Input: The parameter and the set switches of the settings.
        /// Output: Nothing is returned. Replaces the content of the panel list.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void Fill(bool everything)
        {
            _config.Clear();
            _config.Add(_configNote);
            _config.Add(_restartNote);
            _config.AddRange(_generalFields);
            _config.Add(_setToggle);
            if (everything || Mod._enabled) _config.AddRange(_chipAndBoxFields);
            _config.Add(_nachtToggle);
            if (everything || Mod._nacht) _config.AddRange(_nachtFields);
            _config.Add(_scoutToggle);
            if (everything || Mod._scout) _config.AddRange(_scoutFields);
            _config.Add(_andrasToggle);
            if (everything || Mod._andras) _config.AddRange(_andrasFields);
            _config.Add(_nemesisToggle);
            if (everything || Mod._nemesis) _config.AddRange(_nemesisFields);
            _config.Add(_opiToggle);
            if (everything || Mod._opi) _config.AddRange(_opiFields);
            _config.Add(_mercToggle);
            _config.AddRange(_gunArmToggles);
            _config.Add(_uhmgToggle);
        }

        /// <summary>
        /// Builds every entry of the icons panel: the introduction with the red restart reminder
        /// and the adaptive inventory size switch, then for each set and each setless item one
        /// dropdown per kind of art it has (item icons, wound slot icons, chip icons, limb weapon
        /// icon). A kind the section does not have is left out: ANDRAS and the setless items have
        /// no chip, M.E.R.C. has no weapon.
        /// </summary>
        /// <returns>The list that MCM keeps and draws. Never null.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the icon packs, the choices and the adaptive size switch
        /// loaded from config.json and the sprites looked up at startup.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static List<IConfigValue> BuildIconsConfig()
        {
            var list = new List<IConfigValue>
            {
                new ConfigValue("__ae_icons_note", "ae.icons.info.note", IconsInfoHeader),
                new ConfigValue("__ae_icons_restart_note", RestartNote(), IconsInfoHeader),
                new ConfigValue(SmasherIcons.KeyAdaptiveSize, SmasherIcons.AdaptiveSize, IconsInfoHeader, false,
                                L("ae.icons.adaptive.tip"), "ae.icons.adaptive.label")
            };

            foreach (SmasherIcons.Section section in SmasherIcons.Sections())
                foreach (SmasherIcons.Kind kind in SmasherIcons.Kinds)
                {
                    if (!SmasherIcons.HasEntry(section, kind)) continue;
                    list.Add(new DropdownConfig(
                        SmasherIcons.Key(section, kind), SmasherIcons.ChosenName(section, kind),
                        section.HeaderKey, SmasherIcons.DefaultName,
                        IconTooltip(section, kind), IconLabelKey(kind),
                        SmasherIcons.Options(section, kind)));
                }
            return list;
        }

        /// <summary>
        /// Localization key of the label of an icons dropdown.
        /// </summary>
        /// <param name="kind">The kind of art.</param>
        /// <returns>The key, e.g. "ae.icons.woundslots.label".</returns>
        /// <remarks>
        /// Input: The parameter only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string IconLabelKey(SmasherIcons.Kind kind)
        {
            switch (kind)
            {
                case SmasherIcons.Kind.Items:      return "ae.icons.items.label";
                case SmasherIcons.Kind.WoundSlots: return "ae.icons.woundslots.label";
                case SmasherIcons.Kind.Chip:       return "ae.icons.chip.label";
                default:                           return "ae.icons.weapon.label";
            }
        }

        /// <summary>
        /// Tooltip of an icons dropdown: what art it changes for that set or item, followed by
        /// how icon packs work.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <param name="kind">The kind of art.</param>
        /// <returns>The localized tooltip, with "the {set} set" or the item name in place of
        /// {NAME}.</returns>
        /// <remarks>
        /// Input: The parameters and the game's text tables.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string IconTooltip(SmasherIcons.Section section, SmasherIcons.Kind kind)
        {
            string name = section.IsSet
                ? L("ae.icons.setphrase").Replace("{SET}", L(section.NameKey))
                : L(section.NameKey);
            string tipKey;
            switch (kind)
            {
                case SmasherIcons.Kind.Items:
                    tipKey = section.IsSet ? "ae.icons.items.set.tip" : "ae.icons.items.item.tip"; break;
                case SmasherIcons.Kind.WoundSlots:
                    tipKey = "ae.icons.woundslots.tip"; break;
                case SmasherIcons.Kind.Chip:
                    tipKey = "ae.icons.chip.tip"; break;
                default:
                    tipKey = section.IsSet ? "ae.icons.weapon.set.tip" : "ae.icons.weapon.item.tip"; break;
            }
            return L(tipKey).Replace("{NAME}", name) + "\n\n" + L("ae.icons.common.tip");
        }

        /// <summary>
        /// Minimum clone rank dropdown of a set, with every rank of the game as "{number} -
        /// {name}".
        /// </summary>
        /// <param name="key">Config key, e.g. "nacht_min_rank".</param>
        /// <param name="current">Rank number saved in the settings; clamped to the ranks that
        /// exist.</param>
        /// <param name="header">Header of the set, e.g. "ae.cfg.header.nacht".</param>
        /// <param name="defaultRank">Default rank number of the set.</param>
        /// <param name="setNameKey">Localization key of the set name, e.g.
        /// "ae.cfg.setname.nacht".</param>
        /// <param name="ranks">The dropdown entries from SmasherRanks.Options; copied.</param>
        /// <returns>The dropdown.</returns>
        /// <remarks>
        /// Input: The parameters, the rank list and the game's text tables.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static IConfigValue RankDropdown(string key, int current, string header, int defaultRank,
                                                 string setNameKey, List<object> ranks)
        {
            return new DropdownConfig(
                key, SmasherRanks.Label(current), header, SmasherRanks.Label(defaultRank),
                L("ae.cfg.minrank.tip").Replace("{SET}", L(setNameKey)), "ae.cfg.minrank.label",
                new List<object>(ranks));
        }

        /// <summary>
        /// PMC switch of a set.
        /// </summary>
        /// <param name="key">Config key, e.g. "nacht_pmc".</param>
        /// <param name="current">Value saved in the settings.</param>
        /// <param name="header">Header of the set, e.g. "ae.cfg.header.nacht".</param>
        /// <param name="defaultValue">Default value of the set.</param>
        /// <param name="setNameKey">Localization key of the set name, e.g.
        /// "ae.cfg.setname.nacht".</param>
        /// <returns>The switch.</returns>
        /// <remarks>
        /// Input: The parameters and the game's text tables.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static IConfigValue PmcToggle(string key, bool current, string header, bool defaultValue,
                                              string setNameKey)
        {
            return new ConfigValue(key, current, header, defaultValue,
                                   L("ae.cfg.pmc.tip").Replace("{SET}", L(setNameKey)), "ae.cfg.pmc.label");
        }

        /// <summary>
        /// The restart reminder shown at the top of both panels, in red, as a text value that MCM
        /// draws as a plain line (the same way the Hold To Skip Turn mod shows its note).
        /// </summary>
        /// <returns>The localized text inside a red color tag.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the game's text tables.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string RestartNote()
        {
            return "<color=" + RestartColor + ">" + L("ae.cfg.restart.note") + "</color>";
        }

        /// <summary>
        /// Tooltip of the preferred faction dropdown of a set, ending with the set's own tags as
        /// read from the records.
        /// </summary>
        /// <param name="set">Set name, i.e. the subfolder name under Assets: "Smasher",
        /// "Nachtreiher", "Scout22", "Andras", "Nemesis", "OPI", "Setless" or "WeaponLimbs". An
        /// unknown name changes nothing.</param>
        /// <returns>The localized tooltip followed by the tags, e.g. "... associated with are:
        /// Specops."</returns>
        /// <remarks>
        /// Input: The parameter, the localized text and the tags read from the json of the set.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string PieceFactionTooltip(string set)
        {
            var tags = SmasherAugmentations.BaseTags(set);
            string list = tags.Count == 0 ? L("ae.cfg.piecefaction.notags")
                                          : string.Join(", ", tags.ToArray());
            return L("ae.cfg.piecefaction.tip") + " " + list + ".";
        }

        /// <summary>
        /// Tooltip of the switch that decides whether Multi-WoundSlot augmentations are lost to an
        /// amputation (ae.cfg.multislotloss.label), ending with the
        /// names of the augmentations tagged as Multi-WoundSlot, one per line.
        /// </summary>
        /// <returns>The localized tooltip followed by the list, e.g. "... are affected:\n-
        /// (Full Body) OP-Intensify".</returns>
        /// <remarks>
        /// Input: The localized texts and the tags read from the augmentation json files.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string MultiSlotTooltip()
        {
            var names = new List<string>();
            foreach (string id in SmasherAugmentations.MultiSlotIds) names.Add("- " + L(SmasherGunArms.McmNameKeyOf(id)));
            return L("ae.cfg.multislotloss.tip") + "\n"
                 + (names.Count == 0 ? L("ae.cfg.multislotloss.none") : string.Join("\n", names.ToArray()));
        }

        /// <summary>
        /// Localized text of a key in the current language.
        /// </summary>
        /// <param name="key">Localization key, e.g. "ae.cfg.augtech.tip".</param>
        /// <returns>The text, or the key itself when the text is missing, so a missing translation
        /// shows on screen.</returns>
        /// <remarks>
        /// Input: The parameter and the game's text tables.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string L(string key)
        {
            return Localization.Get(key);
        }

        /// <summary>
        /// Tooltip of a set switch, with the localized set name in place of {SET}. A set without a
        /// chip, such as ANDRAS, uses the text that lists only augmentations and crate.
        /// </summary>
        /// <param name="setNameKey">Localization key of the set name, e.g.
        /// "ae.cfg.setname.smasher".</param>
        /// <param name="tipKey">Localization key of the tooltip text: "ae.cfg.set.toggle.tip" by
        /// default, "ae.cfg.set.toggle.nochip.tip" for a set without a chip.</param>
        /// <returns>The localized tooltip.</returns>
        /// <remarks>
        /// Input: The parameters and the game's text tables.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string SetTooltip(string setNameKey, string tipKey = "ae.cfg.set.toggle.tip")
        {
            return L(tipKey).Replace("{SET}", L(setNameKey));
        }

        /// <summary>
        /// Tooltip of the "can craft chip" switch, with the localized set name in place of {SET}.
        /// </summary>
        /// <param name="setNameKey">Localization key of the set name, e.g.
        /// "ae.cfg.setname.nacht".</param>
        /// <returns>The localized tooltip.</returns>
        /// <remarks>
        /// Input: The parameter and the game's text tables.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string CraftChipTooltip(string setNameKey)
        {
            return L("ae.cfg.craftchip.tip").Replace("{SET}", L(setNameKey));
        }
    }
}
