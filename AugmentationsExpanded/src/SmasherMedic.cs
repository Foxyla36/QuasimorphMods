using System;
using System.Collections.Generic;
using Mod = AugmentationsExpanded.AugmentationsExpandedMod;

namespace AugmentationsExpanded
{
    /// <summary>
    /// Settings of the CMEDIC-32 set (folder Medic): the same fields as the other sets with a
    /// chip and a crate (spawn switch, piece tech level and preferred faction, PMC tag, minimum
    /// clone rank, crate faction and tech level, chip crafting, chip faction and tech level). They
    /// live in this class instead of AugmentationsExpandedMod, which calls it at the same points
    /// where it handles the other sets.
    /// </summary>
    internal static class SmasherMedic
    {
        internal const string ChipId         = "medic_chip";
        internal const string BoxId          = "medic_box";
        internal const string Header         = "ae.cfg.header.medic";
        internal const string SetNameKey     = "ae.cfg.setname.medic";
        internal const string ToggleLabelKey = "ae.cfg.medic.toggle.label";

        internal const string KeyEnabled      = "medic_enabled";
        internal const string KeyAugTech      = "medic_aug_techlevel";
        internal const string KeyPieceFaction = "medic_piece_faction";
        internal const string KeyFaction      = "medic_faction";
        internal const string KeyTech         = "medic_techlevel";
        internal const string KeyBoxFaction   = "medic_box_faction";
        internal const string KeyBoxTech      = "medic_box_techlevel";
        internal const string KeyCraftChip    = "medic_craft_chip";
        internal const string KeyMinRank      = "medic_min_rank";
        internal const string KeyPmc          = "medic_pmc";

        internal const string DefaultFaction = "Tianming";
        internal const int    DefaultTech    = 4;
        internal const int    DefaultAugTech = 4;
        internal const int    DefaultBoxTech = 5;
        internal const int    DefaultMinRank = 2;
        internal const bool   DefaultPmc     = false;

        internal static bool   Enabled      = true;
        internal static int    AugTech      = DefaultAugTech;
        internal static string PieceFaction = DefaultFaction;
        internal static string Faction      = DefaultFaction;
        internal static int    Tech         = DefaultTech;
        internal static string BoxFaction   = DefaultFaction;
        internal static int    BoxTech      = DefaultBoxTech;
        internal static bool   CraftChip    = true;
        internal static int    MinRank      = DefaultMinRank;
        internal static bool   Pmc          = DefaultPmc;

        /// <summary>
        /// Applies the set switch: spawn of the pieces on enemies, loot tags of the crate, the chip
        /// conversion recipe (set on and chip crafting on) and the chip in the map containers (set
        /// on and a chip faction chosen).
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the settings.
        /// Output: Nothing is returned. Changes the Categories of pieces and crate, the unlocked
        /// recipes and the map container loot tables.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ApplySpawn()
        {
            SmasherAugmentations.SetSpawnEnabled(SmasherAugmentations.SetMedic, Enabled);
            SmasherTrash.SetSpawnEnabled(SmasherAugmentations.SetMedic, Enabled);
            ApplyCrafting();
            Mod.ApplyMapDrop(ChipId, Enabled && Faction != Mod.NoFaction);
        }

        /// <summary>
        /// Unlocks or locks the chip conversion recipe of the set.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the set and chip crafting switches.
        /// Output: Nothing is returned. Changes the unlocked recipes.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ApplyCrafting()
        {
            SmasherCrafting.SetEnabled(ChipId, Enabled && CraftChip);
        }

        /// <summary>
        /// Applies chip, pieces and crate settings to the records and the reward tables, like the
        /// other sets in AugmentationsExpandedMod.ApplyToChip.
        /// </summary>
        /// <returns>False when the chip record is not loaded; otherwise true.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the settings.
        /// Output: The return value. Changes the chip, the pieces, the crate and the faction
        /// reward tables.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static bool ApplyToChip()
        {
            bool ok = Mod.ApplyChip(ChipId, Faction, Tech, Enabled);
            SmasherAugmentations.SetTechLevel(SmasherAugmentations.SetMedic, AugTech);
            SmasherAugmentations.SetFaction(SmasherAugmentations.SetMedic, PieceFaction);
            SmasherAugmentations.SetPmc(SmasherAugmentations.SetMedic, Pmc);
            Mod.ApplyBox(BoxId, BoxFaction, BoxTech, Enabled);
            return ok;
        }

        /// <summary>
        /// Reads the settings from the values saved by the Mod Config Menu.
        /// </summary>
        /// <param name="config">The panel values keyed by config key. Missing keys keep their
        /// value.</param>
        /// <remarks>
        /// Input: The parameter.
        /// Output: Nothing is returned. Changes the settings.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ReadConfig(Dictionary<string, object> config)
        {
            object v;
            if (config.TryGetValue(KeyEnabled, out v) && v != null)      Enabled      = Convert.ToBoolean(v);
            if (config.TryGetValue(KeyAugTech, out v) && v != null)      AugTech      = Convert.ToInt32(v);
            if (config.TryGetValue(KeyPieceFaction, out v) && v != null) PieceFaction = v.ToString();
            if (config.TryGetValue(KeyFaction, out v) && v != null)      Faction      = v.ToString();
            if (config.TryGetValue(KeyTech, out v) && v != null)         Tech         = Convert.ToInt32(v);
            if (config.TryGetValue(KeyBoxFaction, out v) && v != null)   BoxFaction   = v.ToString();
            if (config.TryGetValue(KeyBoxTech, out v) && v != null)      BoxTech      = Convert.ToInt32(v);
            if (config.TryGetValue(KeyCraftChip, out v) && v != null)    CraftChip    = Convert.ToBoolean(v);
            if (config.TryGetValue(KeyMinRank, out v))                   MinRank      = SmasherRanks.Parse(v, MinRank);
            if (config.TryGetValue(KeyPmc, out v) && v != null)          Pmc          = Convert.ToBoolean(v);
        }

        /// <summary>
        /// Reads one line of config.json if it is a setting of this set.
        /// </summary>
        /// <param name="k">The key of the line.</param>
        /// <param name="v">The value as text.</param>
        /// <returns>True when the key belonged to this set.</returns>
        /// <remarks>
        /// Input: The parameters.
        /// Output: The return value. Changes the matching setting.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static bool TryLoad(string k, string v)
        {
            int n;
            if (k == KeyEnabled)                                Enabled      = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
            else if (k == KeyAugTech && int.TryParse(v, out n)) AugTech      = n;
            else if (k == KeyPieceFaction && v.Length > 0)      PieceFaction = v;
            else if (k == KeyFaction && v.Length > 0)           Faction      = v;
            else if (k == KeyTech && int.TryParse(v, out n))    Tech         = n;
            else if (k == KeyBoxFaction && v.Length > 0)        BoxFaction   = v;
            else if (k == KeyBoxTech && int.TryParse(v, out n)) BoxTech      = n;
            else if (k == KeyCraftChip)                         CraftChip    = !v.Equals("false", StringComparison.OrdinalIgnoreCase);
            else if (k == KeyMinRank)                           MinRank      = SmasherRanks.Parse(v, MinRank);
            else if (k == KeyPmc)                               Pmc          = v.Equals("true", StringComparison.OrdinalIgnoreCase);
            else return false;
            return true;
        }

        /// <summary>
        /// The lines of config.json for this set.
        /// </summary>
        /// <returns>Text starting with a comma, one "key": value per line, without the closing
        /// newline.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the settings.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string ConfigJsonTail()
        {
            return ",\n  \"" + KeyEnabled      + "\": "   + (Enabled ? "true" : "false")
                 + ",\n  \"" + KeyAugTech      + "\": "   + AugTech
                 + ",\n  \"" + KeyPieceFaction + "\": \"" + PieceFaction + "\""
                 + ",\n  \"" + KeyFaction      + "\": \"" + Faction + "\""
                 + ",\n  \"" + KeyTech         + "\": "   + Tech
                 + ",\n  \"" + KeyBoxFaction   + "\": \"" + BoxFaction + "\""
                 + ",\n  \"" + KeyBoxTech      + "\": "   + BoxTech
                 + ",\n  \"" + KeyCraftChip    + "\": "   + (CraftChip ? "true" : "false")
                 + ",\n  \"" + KeyMinRank      + "\": "   + MinRank
                 + ",\n  \"" + KeyPmc          + "\": "   + (Pmc ? "true" : "false");
        }

        /// <summary>
        /// Short text for the log and the save message.
        /// </summary>
        /// <returns>E.g. "Tianming TL4 / crate Tianming TL5", or "off".</returns>
        /// <remarks>
        /// Input: No parameters. Reads the settings.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string Summary()
        {
            return Enabled ? Faction + " TL" + Tech + " / crate " + BoxFaction + " TL" + BoxTech : "off";
        }
    }
}
