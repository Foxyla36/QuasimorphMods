using System;
using System.Collections.Generic;
using System.Text;
using MGSC;
using UnityEngine;

namespace AugmentationsExpanded
{
    internal static class SmasherGunArms
    {
        internal const int DefaultTechLevel = 4;

        internal const string KeyMode       = "gunarm_mode";
        internal const string ModeVanilla   = "Vanilla";
        internal const string ModeExtraSlot = "Extra slot";
        internal const string ModeBareHands = "Bare hands only";
        private const string LegacyModeSuffix = " (experimental)";
        internal const string ModeLabelKey  = "ae.cfg.gunarm.mode.label";
        internal const string ModeTipKey    = "ae.cfg.gunarm.mode.tip";

        internal static readonly string[] Modes = { ModeVanilla, ModeExtraSlot, ModeBareHands };

        internal static string Mode = ModeVanilla;
        internal static bool BlocksPrimary  => Mode != ModeBareHands;
        internal static bool OpensExtraSlot => Mode == ModeExtraSlot;

        internal sealed class GunArm
        {
            internal readonly string Name;
            internal readonly string AugId;
            internal readonly string WeaponId;
            internal readonly string ArmSlotId;
            internal readonly string ShoulderSlotId;
            internal readonly string ConfigKey;
            internal readonly string LabelKey;
            internal readonly string TipKey;
            internal readonly string McmNameKey;
            internal bool Enabled = true;

            /// <summary>
            /// Describes one gun arm and derives every id from its short name: augmentation
            /// ae_{name}_arm, weapon ae_nature_{name}_arm, hand wound slot AE_{slot}Arm, shoulder
            /// wound slot AE_{slot}Shoulder, config key and localization keys.
            /// </summary>
            /// <param name="name">Lowercase short name: "gl", "mantis", "disc", "shotgun" or
            /// "taser".</param>
            /// <param name="slot">Wound slot part of the name: "GL", "Mantis", "Disc", "Shotgun" or
            /// "Taser".</param>
            /// <remarks>
            /// Input: The parameters only.
            /// Output: A new object with every id filled in and the switch on.
            /// AI-generated documentation, written to help external users understand the mod.
            /// </remarks>
            internal GunArm(string name, string slot)
            {
                Name      = name;
                AugId     = "ae_" + name + "_arm";
                WeaponId  = "ae_nature_" + name + "_arm";
                ArmSlotId = "AE_" + slot + "Arm";
                ShoulderSlotId = "AE_" + slot + "Shoulder";
                ConfigKey = "gunarm_" + name + "_enabled";
                LabelKey  = "ae.cfg.gunarm." + name + ".toggle.label";
                TipKey    = "ae.cfg.gunarm." + name + ".toggle.tip";
                McmNameKey = "ae.cfg.itemname." + AugId;
            }
        }

        internal static readonly GunArm[] All =
        {
            new GunArm("gl",      "GL"),
            new GunArm("mantis",  "Mantis"),
            new GunArm("disc",    "Disc"),
            new GunArm("shotgun", "Shotgun"),
            new GunArm("taser",   "Taser"),
        };

        /// <summary>
        /// Localization key of the name an augmentation shows in the Mod Config Menu. The gun
        /// arms keep there the long name with the weapon type, e.g. "(Arm Grenade Launcher)
        /// RGL-40 Reprisal", easier to recognise than the in-game "(Full Arm) RGL-40 Reprisal".
        /// The setless weapon arms that used to be gun arms (pile bunker, gatling, energy cannon)
        /// keep the same long name, which is why the setless list is searched as well.
        /// </summary>
        /// <param name="augId">Augmentation id, e.g. "ae_gl_arm" or "smasher_hand".</param>
        /// <returns>"ae.cfg.itemname.{id}" for a gun arm or a setless weapon arm that has such a
        /// name, otherwise the in-game name key "item.{id}.name".</returns>
        /// <remarks>
        /// Input: The parameter, the gun arm list and the setless list.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string McmNameKeyOf(string augId)
        {
            foreach (GunArm arm in All)
                if (arm.AugId == augId) return arm.McmNameKey;
            foreach (SmasherSetless.Item it in SmasherSetless.All)
                if (it.AugId == augId) return it.McmNameKey;
            return "item." + augId + ".name";
        }

        /// <summary>
        /// Applies the Mod Config Menu switch of each gun arm to its spawn on enemies.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the switch of each gun arm.
        /// Output: Nothing is returned. Changes the Categories of the five gun arm augmentations.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ApplySpawn()
        {
            foreach (GunArm arm in All)
                SmasherAugmentations.SetItemSpawnEnabled(arm.AugId, arm.Enabled);
        }

        /// <summary>
        /// Gives each gun arm a reward row in the equipment table of every faction listed in its
        /// json Categories, at the tech level written in its json, so the item and the faction
        /// technology window always agree. Removes the rows when its switch is off.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the switch of each gun arm and the factions and tech level
        /// of its augmentation record.
        /// Output: Nothing is returned. Changes the faction reward tables.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ApplyRewards()
        {
            foreach (GunArm arm in All)
                SmasherChipDrop.ApplyAugmentation(arm.AugId, SmasherAugmentations.FactionTags(arm.AugId),
                                                  TechLevelOf(arm.AugId), !arm.Enabled);
        }

        /// <summary>
        /// Tech level of a gun arm, read from its augmentation record, which holds the value of
        /// its json.
        /// </summary>
        /// <param name="augId">Id of the gun arm augmentation, e.g. "ae_shotgun_arm".</param>
        /// <returns>The record's tech level, or DefaultTechLevel when the record is not
        /// registered.</returns>
        /// <remarks>
        /// Input: The parameter and the items in the game data.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static int TechLevelOf(string augId)
        {
            if (Data.Items.GetRecord(augId) == null) return DefaultTechLevel;
            AugmentationRecord rec = Data.Items.GetSimpleRecord<AugmentationRecord>(augId);
            return rec != null ? rec.TechLevel : DefaultTechLevel;
        }

        /// <summary>
        /// Adds the recipes of the five gun arms to the unlock list of a chip.
        /// </summary>
        /// <param name="chipId">Id of an existing chip, the vanilla "augment_chip".</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the chip's unlock list.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void AddToChip(string chipId)
        {
            foreach (GunArm arm in All)
                SmasherAugmentations.AddToChip(chipId, arm.AugId);
        }

        /// <summary>
        /// Writes the gun arm mode into the records of the hand wound slots: arm_slot_unavailable,
        /// which blocks the primary weapon slot, is present in the Vanilla and Extra slot modes and
        /// removed in Bare hands only. The game reads these records when an arm is installed; arms
        /// that are already installed are updated by SmasherExtraSlot.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the gun arm mode.
        /// Output: Nothing is returned. Changes the penalty effects of the five hand wound slot
        /// records and writes a line in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ApplyMode()
        {
            foreach (GunArm arm in All)
            {
                WoundSlotRecord rec = Data.WoundSlots.GetRecord(arm.ArmSlotId);
                if (rec == null || rec.ImplicitPenaltyEffects == null) continue;
                if (BlocksPrimary) rec.ImplicitPenaltyEffects["arm_slot_unavailable"] = 1f;
                else rec.ImplicitPenaltyEffects.Remove("arm_slot_unavailable");
            }
            Debug.Log("[AugmentationsExpanded] gun arm mode: " + Mode);
        }

        /// <summary>
        /// Checks a mode read from the settings.
        /// </summary>
        /// <param name="value">The saved text, expected "Vanilla", "Extra slot" or "Bare hands
        /// only". The names saved by older versions, "Extra slot (experimental)" and "Bare hands
        /// only (experimental)", are accepted too. Anything else, null included, is
        /// accepted.</param>
        /// <returns>The value when it is one of the three modes (the old names give the current
        /// ones), otherwise "Vanilla".</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string ValidMode(string value)
        {
            if (value != null && value.EndsWith(LegacyModeSuffix))
                value = value.Substring(0, value.Length - LegacyModeSuffix.Length);
            foreach (string m in Modes)
                if (m == value) return m;
            return ModeVanilla;
        }

        /// <summary>
        /// Reads the gun arm switches and the mode from the values saved by the Mod Config Menu.
        /// </summary>
        /// <param name="config">The panel values keyed by config key, e.g. "gunarm_gl_enabled":
        /// true, "gunarm_mode": "Vanilla". Missing keys keep their current value.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the switch of each gun arm and the mode.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ReadConfig(Dictionary<string, object> config)
        {
            object v;
            foreach (GunArm arm in All)
                if (config.TryGetValue(arm.ConfigKey, out v) && v != null)
                    arm.Enabled = Convert.ToBoolean(v);
            if (config.TryGetValue(KeyMode, out v) && v != null)
                Mode = ValidMode(v.ToString());
        }

        /// <summary>
        /// Reads one config.json entry when it belongs to the gun arms.
        /// </summary>
        /// <param name="key">Config key, e.g. "gunarm_gl_enabled" or "gunarm_mode".</param>
        /// <param name="value">Value as text without quotes: "true" or "false" for a switch
        /// (anything but "false" means on), a mode name for the mode.</param>
        /// <returns>True when the key belongs to the gun arms and was read; false for any other
        /// key.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. Changes the matching switch or the mode.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static bool TryLoad(string key, string value)
        {
            if (key == KeyMode)
            {
                Mode = ValidMode(value);
                return true;
            }
            foreach (GunArm arm in All)
            {
                if (key != arm.ConfigKey) continue;
                arm.Enabled = !value.Equals("false", StringComparison.OrdinalIgnoreCase);
                return true;
            }
            return false;
        }

        /// <summary>
        /// The gun arm entries of config.json, added by SaveSettings before the closing brace.
        /// </summary>
        /// <returns>Text that continues the JSON object: for each gun arm a comma, a new line and
        /// an entry such as "gunarm_gl_enabled": true, then the gunarm_mode entry.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the switch of each gun arm and the mode.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string ConfigJsonTail()
        {
            var sb = new StringBuilder();
            foreach (GunArm arm in All)
                sb.Append(",\n  \"").Append(arm.ConfigKey).Append("\": ").Append(arm.Enabled ? "true" : "false");
            sb.Append(",\n  \"").Append(KeyMode).Append("\": \"").Append(Mode).Append("\"");
            return sb.ToString();
        }

        /// <summary>
        /// Short description of the gun arm settings for the log and the save message.
        /// </summary>
        /// <returns>Text such as "ae_gl_arm on, ae_mantis_arm off, ... (Vanilla)".</returns>
        /// <remarks>
        /// Input: No parameters. Reads the switch of each gun arm and the mode.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string Summary()
        {
            var parts = new List<string>();
            foreach (GunArm arm in All)
                parts.Add(arm.AugId + (arm.Enabled ? " on" : " off"));
            return string.Join(", ", parts.ToArray()) + " (" + Mode + ")";
        }
    }
}
