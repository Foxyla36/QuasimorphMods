using System.Collections.Generic;
using System.Reflection;
using MGSC;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class SmasherChipDrop
    {
        private const float ChipWeight = 45f;
        private const float ChipPoints = 520f;

        private const float BoxWeight = 25f;
        private const float BoxPoints = 900f;

        private const float AugWeight = 60f;
        private const float AugPoints = 400f;

        private static readonly Dictionary<string, List<ContentDropRecord>> _rows =
            new Dictionary<string, List<ContentDropRecord>>();

        /// <summary>
        /// Reward rows added by this mod for one item, created on first use. Kept per item, so
        /// updating one item never removes the rows of another.
        /// </summary>
        /// <param name="itemId">Id of the item the rows belong to.</param>
        /// <returns>The live list of rows of that item. Never null.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. The first call for an id creates its empty list.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static List<ContentDropRecord> RowsOf(string itemId)
        {
            List<ContentDropRecord> list;
            if (!_rows.TryGetValue(itemId, out list))
            {
                list = new List<ContentDropRecord>();
                _rows[itemId] = list;
            }
            return list;
        }

        /// <summary>
        /// Puts a set chip in the chip reward table of a faction
        /// (factiondrop_{Faction}_rewardChips), replacing the row of the previous call. These
        /// tables feed both the mission rewards and the faction technology window.
        /// </summary>
        /// <param name="chipId">Id of a registered chip, e.g. "smasher_chip".</param>
        /// <param name="factionTag">A faction id from the game data, e.g. "RealWare", or "none" for
        /// no faction. None, null or empty only removes the previous row.</param>
        /// <param name="techLevel">Tech level of the row, clamped to the game's range.</param>
        /// <param name="disabled">True to only remove the previous row.</param>
        /// <returns>True when a row was added; false when disabled, without faction or when the
        /// chip is not registered.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. Changes the faction reward tables and writes lines in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static bool Apply(string chipId, string factionTag, int techLevel, bool disabled)
        {
            return Rebuild(RowsOf(chipId), chipId, factionTag, "rewardChips",
                           techLevel, ChipWeight, ChipPoints, disabled);
        }

        /// <summary>
        /// Puts a crate in the equipment reward table of a faction ({Faction}_rewardEquipment). It
        /// is the only way a crate enters the game. Also used for the OP-I augmentation, which is
        /// rewarded in the place of a crate because its set has none.
        /// </summary>
        /// <param name="boxId">Id of a registered crate, e.g. "andras_box", or "ae_opi".</param>
        /// <param name="factionTag">A faction id from the game data, e.g. "RealWare", or "none" for
        /// no faction. None, null or empty only removes the previous row.</param>
        /// <param name="techLevel">Tech level of the row, clamped to the game's range.</param>
        /// <param name="disabled">True to only remove the previous row.</param>
        /// <returns>True when a row was added; false when disabled, without faction or when the
        /// crate is not registered.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. Changes the faction reward tables and writes lines in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static bool ApplyBox(string boxId, string factionTag, int techLevel, bool disabled)
        {
            return Rebuild(RowsOf(boxId), boxId, factionTag, "rewardEquipment",
                           techLevel, BoxWeight, BoxPoints, disabled);
        }

        /// <summary>
        /// Puts a single augmentation (M.E.R.C.) in the equipment reward table of one faction,
        /// replacing the row of the previous call.
        /// </summary>
        /// <param name="augId">Id of a registered augmentation, e.g. "ae_merc".</param>
        /// <param name="factionTag">A faction id from the game data, e.g. "RealWare", or "none" for
        /// no faction. None, null or empty only removes the previous row.</param>
        /// <param name="techLevel">Tech level of the row, clamped to the game's range.</param>
        /// <param name="disabled">True to only remove the previous row.</param>
        /// <returns>True when a row was added; false when disabled, without faction or when the
        /// augmentation is not registered.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. Changes the faction reward tables and writes lines in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static bool ApplyAugmentation(string augId, string factionTag, int techLevel, bool disabled)
        {
            return Rebuild(RowsOf(augId), augId, factionTag, "rewardEquipment",
                           techLevel, AugWeight, AugPoints, disabled);
        }

        /// <summary>
        /// Puts a single augmentation in the equipment reward table of several factions, one row
        /// each. The rows are tracked together and all replaced by the next call.
        /// </summary>
        /// <param name="augId">Id of a registered augmentation, e.g. "ae_gl_arm".</param>
        /// <param name="factionTags">Faction ids, e.g. ["RealWare", "FrancheComte"]. Null, empty
        /// and "none" entries are skipped.</param>
        /// <param name="techLevel">Tech level of the rows, clamped to the game's range.</param>
        /// <param name="disabled">True to only remove the previous rows.</param>
        /// <returns>True when at least one row was added; otherwise false.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. Changes the faction reward tables and writes lines in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static bool ApplyAugmentation(string augId, IEnumerable<string> factionTags, int techLevel, bool disabled)
        {
            List<ContentDropRecord> mine = RowsOf(augId);
            RemovePrevious(mine);

            if (disabled)
            {
                Debug.Log("[AugmentationsExpanded] " + augId + " kept out of the reward tables.");
                return false;
            }
            bool added = false;
            foreach (string faction in factionTags)
            {
                if (string.IsNullOrEmpty(faction) || faction == "none") continue;
                added |= AddRow(mine, augId, faction, "rewardEquipment",
                                Mathf.Clamp(techLevel, 0, Data.Global.MaxTechLevel), AugWeight, AugPoints);
            }
            return added;
        }

        /// <summary>
        /// Puts a single augmentation in the equipment reward table of several factions, one row
        /// each, every faction at its own tech level. The rows are tracked together and all
        /// replaced by the next call.
        /// </summary>
        /// <param name="augId">Id of a registered augmentation, e.g. "ae_uhmg".</param>
        /// <param name="techByFaction">Faction id to tech level of its row, e.g. {"Tianming": 6,
        /// "RealWare": 7}. Null, empty and "none" ids are skipped; tech levels are clamped to the
        /// game's range.</param>
        /// <param name="disabled">True to only remove the previous rows.</param>
        /// <returns>True when at least one row was added; otherwise false.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. Changes the faction reward tables and writes lines in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static bool ApplyAugmentation(string augId, IEnumerable<KeyValuePair<string, int>> techByFaction, bool disabled)
        {
            List<ContentDropRecord> mine = RowsOf(augId);
            RemovePrevious(mine);

            if (disabled)
            {
                Debug.Log("[AugmentationsExpanded] " + augId + " kept out of the reward tables.");
                return false;
            }
            bool added = false;
            foreach (KeyValuePair<string, int> kv in techByFaction)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Key == "none") continue;
                added |= AddRow(mine, augId, kv.Key, "rewardEquipment",
                                Mathf.Clamp(kv.Value, 0, Data.Global.MaxTechLevel), AugWeight, AugPoints);
            }
            return added;
        }

        /// <summary>
        /// Removes the previous rows of an item and adds the new one, unless the item is disabled
        /// or has no faction. The tech level is clamped to the game's range.
        /// </summary>
        /// <param name="mine">The item's row list, from RowsOf.</param>
        /// <param name="itemId">Id of the item.</param>
        /// <param name="factionTag">A faction id from the game data, e.g. "RealWare", or "none" for
        /// no faction. None, null or empty only removes the previous rows.</param>
        /// <param name="suffix">Table suffix: "rewardChips" or "rewardEquipment".</param>
        /// <param name="techLevel">Tech level of the row, before clamping.</param>
        /// <param name="weight">Weight of the row in the reward draw, e.g. 45.</param>
        /// <param name="points">Reward points of the row, e.g. 520.</param>
        /// <param name="disabled">True to only remove the previous rows.</param>
        /// <returns>True when a row was added; otherwise false.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. Changes the faction reward tables and the list mine.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool Rebuild(List<ContentDropRecord> mine, string itemId, string factionTag,
                                    string suffix, int techLevel, float weight, float points,
                                    bool disabled)
        {
            RemovePrevious(mine);

            if (disabled || string.IsNullOrEmpty(factionTag) || factionTag == "none")
            {
                Debug.Log("[AugmentationsExpanded] " + itemId + " kept out of the reward tables.");
                return false;
            }
            return AddRow(mine, itemId, factionTag, suffix,
                          Mathf.Clamp(techLevel, 0, Data.Global.MaxTechLevel), weight, points);
        }

        /// <summary>
        /// Adds one reward row for an item to the table {faction}_{suffix}. The game copies the row
        /// into every tech level from the given one up to the maximum.
        /// </summary>
        /// <param name="mine">The item's row list; the new row is added to it.</param>
        /// <param name="itemId">Id of the item.</param>
        /// <param name="factionTag">A faction id, e.g. "RealWare".</param>
        /// <param name="suffix">Table suffix: "rewardChips" or "rewardEquipment".</param>
        /// <param name="techLevel">Tech level of the row, already within the game's range.</param>
        /// <param name="weight">Weight of the row in the reward draw.</param>
        /// <param name="points">Reward points of the row.</param>
        /// <returns>True when the row was added; false when itemId is empty or not
        /// registered.</returns>
        /// <remarks>
        /// Input: The parameters and the items in the game data.
        /// Output: The return value. Adds the row to the faction reward table and to mine, and
        /// writes a line in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool AddRow(List<ContentDropRecord> mine, string itemId, string factionTag,
                                   string suffix, int techLevel, float weight, float points)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            if (Data.Items.GetRecord(itemId) == null)
            {
                Debug.LogWarning("[AugmentationsExpanded] " + itemId + " is not registered: no row in " + suffix + ".");
                return false;
            }

            var record = new ContentDropRecord
            {
                TechLevel  = techLevel,
                ContentIds = new List<string> { itemId },
                Weight     = weight,
                Points     = points
            };

            string table = factionTag + "_" + suffix;
            Data.FactionDrop.AddRecord("factiondrop_" + table, record);
            mine.Add(record);

            Debug.Log("[AugmentationsExpanded] " + itemId + " added to " + table
                    + " (TechLevel " + techLevel + ", weight " + weight + ", points " + points + ").");
            return true;
        }

        /// <summary>
        /// Removes from the reward tables the rows added by the previous call. The game offers no
        /// way to remove rows, so its private _recordsByFactions dictionary is edited through
        /// reflection. Rows are matched by object identity, so vanilla rows are never touched.
        /// </summary>
        /// <param name="mine">The item's row list, from RowsOf.</param>
        /// <remarks>
        /// Input: The parameter and the game's faction reward tables.
        /// Output: Nothing is returned. Removes the rows from every table and empties mine. When
        /// the tables cannot be read the rows are kept in mine instead of being forgotten, so the
        /// next call can still take them out; forgetting them would leave them in the reward
        /// tables for good and add a copy at every save.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void RemovePrevious(List<ContentDropRecord> mine)
        {
            if (mine.Count == 0) return;

            try
            {
                FieldInfo f = typeof(FactionDropCollection).GetField(
                    "_recordsByFactions", BindingFlags.Instance | BindingFlags.NonPublic);
                if (f == null)
                {
                    Debug.LogWarning("[AugmentationsExpanded] _recordsByFactions not found: "
                                   + "the previous rows stay where they are.");
                    return;
                }

                var map = f.GetValue(Data.FactionDrop)
                          as Dictionary<string, Dictionary<int, List<ContentDropRecord>>>;
                if (map == null)
                {
                    Debug.LogWarning("[AugmentationsExpanded] the reward tables could not be read: "
                                   + "the previous rows stay where they are.");
                    return;
                }

                int removed = 0;
                foreach (var byTech in map.Values)
                    foreach (var list in byTech.Values)
                        foreach (var rec in mine)
                            while (list.Remove(rec)) removed++;

                mine.Clear();
                Debug.Log("[AugmentationsExpanded] removed " + removed + " reward rows from the previous pass.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] removing drop rows: " + e);
            }
        }
    }
}
