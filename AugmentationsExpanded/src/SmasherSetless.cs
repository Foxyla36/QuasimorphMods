using System;
using System.Collections.Generic;
using System.Linq;

namespace AugmentationsExpanded
{
    internal static class SmasherSetless
    {
        /// <summary>
        /// One setless augmentation with its own Mod Config Menu switch, handled like the UHMG:
        /// spawn on enemies from the Categories of its json, reward rows in the equipment table of
        /// fixed factions, and its recipe in the vanilla augmentation chip.
        /// </summary>
        internal sealed class Item
        {
            internal readonly string Name;
            internal readonly string AugId;
            internal readonly string[] SlotIds;
            internal readonly string WeaponId;
            internal readonly KeyValuePair<string, int>[] RewardFactions;
            internal readonly string ConfigKey;
            internal readonly string LabelKey;
            internal readonly string TipKey;
            internal bool Enabled = true;

            /// <summary>
            /// Describes one setless augmentation.
            /// </summary>
            /// <param name="name">Short name used in the config keys, e.g. "cargotrack".</param>
            /// <param name="augId">Augmentation id, e.g. "ae_cargo_track".</param>
            /// <param name="slotIds">Its wound slot ids, the hand slot first when it has a
            /// weapon.</param>
            /// <param name="weaponId">Id of the limb weapon it puts in the hand slot, or null.</param>
            /// <param name="rewards">Faction and tech level of each reward row, e.g. "CoreWard", 4.</param>
            /// <remarks>
            /// Input: The parameters only.
            /// Output: The new object.
            /// AI-generated documentation, written to help external users understand the mod.
            /// </remarks>
            internal Item(string name, string augId, string[] slotIds, string weaponId,
                          params KeyValuePair<string, int>[] rewards)
                : this(name, augId, slotIds, weaponId, name, "ae.cfg." + name, rewards)
            {
            }

            /// <summary>
            /// Describes one setless augmentation whose config key and texts do not follow its
            /// name, e.g. the weapon arms that kept the keys they had as gun arms.
            /// </summary>
            /// <param name="name">Short name used in the icons panel, e.g. "pile".</param>
            /// <param name="augId">Augmentation id, e.g. "ae_pile_arm".</param>
            /// <param name="slotIds">Its wound slot ids, the hand slot first.</param>
            /// <param name="weaponId">Id of the limb weapon it puts in the hand slot, or null.</param>
            /// <param name="keyBase">Config key without "_enabled", e.g. "gunarm_pile".</param>
            /// <param name="locBase">Text key without ".toggle.label", e.g. "ae.cfg.gunarm.pile".</param>
            /// <param name="rewards">Faction and tech level of each reward row.</param>
            /// <remarks>
            /// Input: The parameters only.
            /// Output: The new object.
            /// AI-generated documentation, written to help external users understand the mod.
            /// </remarks>
            internal Item(string name, string augId, string[] slotIds, string weaponId,
                          string keyBase, string locBase, params KeyValuePair<string, int>[] rewards)
            {
                Name = name;
                AugId = augId;
                SlotIds = slotIds;
                WeaponId = weaponId;
                RewardFactions = rewards;
                ConfigKey = keyBase + "_enabled";
                LabelKey  = locBase + ".toggle.label";
                TipKey    = locBase + ".toggle.tip";
            }
        }

        /// <summary>
        /// One reward row of the list below: a faction and the tech level of its row.
        /// </summary>
        /// <param name="faction">Faction id, e.g. "CoreWard".</param>
        /// <param name="techLevel">Tech level of the row, e.g. 4.</param>
        /// <returns>The pair.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static KeyValuePair<string, int> R(string faction, int techLevel)
        {
            return new KeyValuePair<string, int>(faction, techLevel);
        }

        /// <summary>
        /// The setless augmentations of this list, in the order of their switches in the Mod
        /// Config Menu. The UHMG and M.E.R.C. keep their own code. The three weapon arms are not
        /// Special Weapon Arms: like the UHMG they always block the primary weapon slot, whatever
        /// the special weapon arm mode, and they keep the config keys they had as gun arms.
        /// </summary>
        internal static readonly Item[] All =
        {
            new Item("pile", "ae_pile_arm", new[] { "AE_PileArm", "AE_PileShoulder" }, "ae_nature_pile_arm",
                     "gunarm_pile", "ae.cfg.gunarm.pile", R("PlanetBridge", 6), R("CoreWard", 6)),
            new Item("gatling", "ae_gatling_arm", new[] { "AE_GatlingArm", "AE_GatlingShoulder" }, "ae_nature_gatling_arm",
                     "gunarm_gatling", "ae.cfg.gunarm.gatling", R("CoreWard", 5), R("Grasshopper", 5)),
            new Item("ecannon", "ae_ecannon_arm", new[] { "AE_ECannonArm", "AE_ECannonShoulder" }, "ae_nature_ecannon_arm",
                     "gunarm_ecannon", "ae.cfg.gunarm.ecannon", R("SBN", 7), R("Dilthey", 7)),
            new Item("cargotrack", "ae_cargo_track",
                     new[] { "AE_CargoTrackThigh", "AE_CargoTrackKnee", "AE_CargoTrackFeet" }, null,
                     R("PlanetBridge", 4), R("CoreWard", 4), R("Grasshopper", 5)),
            new Item("cargochicken", "ae_cargo_chicken",
                     new[] { "AE_CargoChickenThigh", "AE_CargoChickenKnee", "AE_CargoChickenFeet" }, null,
                     R("Grasshopper", 4), R("Dilthey", 5)),
            new Item("sniperhead", "ae_sniper_head", new[] { "AE_SniperHead" }, null,
                     R("RealWare", 5), R("FrancheComte", 5)),
            new Item("radarhead", "ae_radar_head", new[] { "AE_RadarHead" }, null,
                     R("SBN", 4), R("Dilthey", 4)),
            new Item("flammen", "ae_flammen",
                     new[] { "AE_FlammenArm", "AE_FlammenShoulder", "AE_FlammenChest" }, "ae_nature_flammen_arm",
                     R("CoreWard", 4), R("CResistance", 4)),
            new Item("chainshotgun", "ae_chainshotgun",
                     new[] { "AE_ChainShotgunArm", "AE_ChainShotgunShoulder", "AE_ChainShotgunChest" },
                     "ae_nature_chainshotgun",
                     R("FrancheComte", 7), R("ChurchRevelation", 7)),
        };

        /// <summary>
        /// Applies the switch of every augmentation of the list to its spawn on enemies. Must run
        /// after the M.E.R.C. switch, which rewrites the Categories of every Setless item.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the switches.
        /// Output: Nothing is returned. Changes the Categories of the augmentations.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ApplySpawn()
        {
            foreach (Item it in All)
                SmasherAugmentations.SetItemSpawnEnabled(it.AugId, it.Enabled);
        }

        /// <summary>
        /// Gives every augmentation of the list its reward rows, one per faction of its
        /// RewardFactions at that tech level, or removes them when its switch is off.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the switches.
        /// Output: Nothing is returned. Changes the faction reward tables.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ApplyRewards()
        {
            foreach (Item it in All)
                SmasherChipDrop.ApplyAugmentation(it.AugId, it.RewardFactions, !it.Enabled);
        }

        /// <summary>
        /// Adds every augmentation of the list to the pool of a chip, so the chip can unlock its
        /// recipe.
        /// </summary>
        /// <param name="chipId">Id of the chip, e.g. "augment_chip".</param>
        /// <remarks>
        /// Input: The parameter.
        /// Output: Nothing is returned. Changes the UnlockIds of the chip.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void AddToChip(string chipId)
        {
            foreach (Item it in All)
                SmasherAugmentations.AddToChip(chipId, it.AugId);
        }

        /// <summary>
        /// Reads the switches from the values saved by the Mod Config Menu.
        /// </summary>
        /// <param name="config">The panel values keyed by config key. Missing keys keep their
        /// value.</param>
        /// <remarks>
        /// Input: The parameter.
        /// Output: Nothing is returned. Changes the switches.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ReadConfig(Dictionary<string, object> config)
        {
            foreach (Item it in All)
            {
                object v;
                if (config.TryGetValue(it.ConfigKey, out v) && v != null)
                    it.Enabled = Convert.ToBoolean(v);
            }
        }

        /// <summary>
        /// Reads one line of config.json if it is the switch of an augmentation of the list.
        /// </summary>
        /// <param name="key">The key of the line.</param>
        /// <param name="value">The value as text.</param>
        /// <returns>True when the key belonged to this list.</returns>
        /// <remarks>
        /// Input: The parameters.
        /// Output: The return value. Changes the matching switch.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static bool TryLoad(string key, string value)
        {
            foreach (Item it in All)
            {
                if (key != it.ConfigKey) continue;
                it.Enabled = !value.Equals("false", StringComparison.OrdinalIgnoreCase);
                return true;
            }
            return false;
        }

        /// <summary>
        /// The lines of config.json for the switches of the list.
        /// </summary>
        /// <returns>Text starting with a comma, one "key": value per line, without the closing
        /// newline.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the switches.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string ConfigJsonTail()
        {
            return string.Concat(All.Select(it =>
                ",\n  \"" + it.ConfigKey + "\": " + (it.Enabled ? "true" : "false")));
        }

        /// <summary>
        /// The state of every augmentation of the list for the log and the save message, e.g.
        /// "pile on, gatling off".
        /// </summary>
        /// <returns>The text.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the switches.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string Summary()
        {
            return string.Join(", ", All.Select(it => it.Name + (it.Enabled ? " on" : " off")).ToArray());
        }
    }
}
