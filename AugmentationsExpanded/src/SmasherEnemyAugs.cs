using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MGSC;
using UnityEngine;

namespace AugmentationsExpanded
{
    /// <summary>
    /// The two enemy augmentation sliders, "Enemy aug chance" and "Enemy extra aug rolls". Every
    /// enemy class (MobClassRecord) rolls AugCount times on its AugmentationClasses table; a roll
    /// that lands on "None" gives no augmentation, any other class picks an augmentation weighted
    /// by that class. The chance slider lowers only the weight of "None", so more rolls give an
    /// augmentation while the weights that choose which augmentation it is stay the same: no
    /// augmentation becomes more likely than another. The extra rolls slider adds the same number
    /// of rolls to both ends of AugCount, so enemies can carry more augmentations at once; the
    /// game still skips a piece whose body part is taken or that would leave the enemy with too
    /// little max health. Enemies that cannot have augmentations (an AugCount that never goes
    /// above 0, or no class but "None") are left alone by both. Works on the records only,
    /// without patches, and can be changed at any time.
    /// </summary>
    internal static class SmasherEnemyAugs
    {
        internal const string ConfigKey      = "enemy_aug_chance_bonus";
        internal const int    DefaultBonus   = 0;
        internal const int    MaxBonus       = 100;
        internal const string RollsConfigKey = "enemy_aug_extra_rolls";
        internal const int    DefaultRolls   = 0;
        internal const int    MaxRolls       = 9;

        internal static int Bonus = DefaultBonus;
        internal static int ExtraRolls = DefaultRolls;

        private static readonly Dictionary<Dictionary<AugmentationClass, float>, float> _baseNone =
            new Dictionary<Dictionary<AugmentationClass, float>, float>();
        private static readonly Dictionary<Dictionary<AugmentationClass, float>, float> _written =
            new Dictionary<Dictionary<AugmentationClass, float>, float>();
        private static readonly Dictionary<MobClassRecord, IntRange> _baseCount =
            new Dictionary<MobClassRecord, IntRange>(ByReference.Instance);
        private static readonly Dictionary<MobClassRecord, IntRange> _writtenCount =
            new Dictionary<MobClassRecord, IntRange>(ByReference.Instance);

        private sealed class ByReference : IEqualityComparer<MobClassRecord>
        {
            internal static readonly ByReference Instance = new ByReference();
            public bool Equals(MobClassRecord x, MobClassRecord y) { return ReferenceEquals(x, y); }
            public int GetHashCode(MobClassRecord obj) { return RuntimeHelpers.GetHashCode(obj); }
        }

        /// <summary>
        /// Applies both sliders to every enemy class in the game data, other mods' classes
        /// included. The "None" weight of each table and the AugCount of each class, as they were
        /// before the mod first touched them, are kept, so the sliders can be moved any number of
        /// times and 0 puts back exactly the old values. When another mod changes one of them
        /// after this method wrote it, the new value becomes the base. "None" weights are tracked
        /// per table instance, so a table shared by several classes is changed once. Safe to call
        /// repeatedly.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads Bonus, ExtraRolls and the enemy classes.
        /// Output: Nothing is returned. Changes the "None" weight of the AugmentationClasses and
        /// the AugCount of the enemy classes that can roll an augmentation, and writes a line in
        /// the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void Apply()
        {
            if (Data.MobClasses == null) return;
            float bonus = Mathf.Clamp(Bonus, 0, MaxBonus) / 100f;
            int rolls = Mathf.Clamp(ExtraRolls, 0, MaxRolls);
            int changed = 0;
            foreach (MobClassRecord mob in Data.MobClasses.Records)
            {
                Dictionary<AugmentationClass, float> classes = mob == null ? null : mob.AugmentationClasses;
                if (classes == null) continue;

                IntRange count = mob.AugCount, writtenCount;
                if (!_writtenCount.TryGetValue(mob, out writtenCount)
                    || writtenCount.Min != count.Min || writtenCount.Max != count.Max)
                    _baseCount[mob] = count;
                IntRange baseCount = _baseCount[mob];

                float others = 0f;
                foreach (KeyValuePair<AugmentationClass, float> kv in classes)
                    if (kv.Key != AugmentationClass.None && kv.Value > 0f) others += kv.Value;
                if (baseCount.Max <= 0 || others <= 0f) continue;

                IntRange newCount = new IntRange(baseCount.Min + rolls, baseCount.Max + rolls);
                mob.AugCount = newCount;
                _writtenCount[mob] = newCount;
                bool touched = rolls > 0;

                float none;
                if (classes.TryGetValue(AugmentationClass.None, out none))
                {
                    float written;
                    if (!_written.TryGetValue(classes, out written) || !Mathf.Approximately(written, none))
                        _baseNone[classes] = none;
                    float baseNone = _baseNone[classes];
                    if (baseNone > 0f)
                    {
                        float value = NoneWeight(baseNone, others, bonus);
                        classes[AugmentationClass.None] = value;
                        _written[classes] = value;
                        touched |= !Mathf.Approximately(value, baseNone);
                    }
                }
                if (touched) changed++;
            }
            Debug.Log("[AugmentationsExpanded] enemy augmentation chance bonus: " + Mathf.Clamp(Bonus, 0, MaxBonus)
                    + "%, extra rolls: " + rolls + " (" + changed + " enemy classes changed).");
        }

        /// <summary>
        /// The "None" weight that turns the given share of the empty rolls into augmentation
        /// rolls: with q the chance of an empty roll, the new chance is q * (1 - bonus).
        /// </summary>
        /// <param name="baseNone">The "None" weight of the class without the slider, above
        /// 0.</param>
        /// <param name="others">The sum of the other weights of the class, above 0.</param>
        /// <param name="bonus">The slider as a fraction, 0 to 1.</param>
        /// <returns>The weight; baseNone itself for 0, 0 for 1.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static float NoneWeight(float baseNone, float others, float bonus)
        {
            if (bonus <= 0f) return baseNone;
            if (bonus >= 1f) return 0f;
            float empty = baseNone / (baseNone + others) * (1f - bonus);
            return empty * others / (1f - empty);
        }

        /// <summary>
        /// Reads the slider values saved by the Mod Config Menu.
        /// </summary>
        /// <param name="config">The panel values keyed by config key. Null or a missing key keeps
        /// the current value.</param>
        /// <remarks>
        /// Input: The parameter.
        /// Output: Nothing is returned. Changes Bonus (clamped to 0-100) and ExtraRolls (clamped
        /// to 0-9).
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ReadConfig(Dictionary<string, object> config)
        {
            object v;
            if (config == null) return;
            if (config.TryGetValue(ConfigKey, out v) && v != null)
                Bonus = Mathf.Clamp(Convert.ToInt32(v), 0, MaxBonus);
            if (config.TryGetValue(RollsConfigKey, out v) && v != null)
                ExtraRolls = Mathf.Clamp(Convert.ToInt32(v), 0, MaxRolls);
        }

        /// <summary>
        /// Reads a slider value from a config.json line, when the key is one of this class's.
        /// </summary>
        /// <param name="key">The key of the line.</param>
        /// <param name="value">The value of the line, without quotes.</param>
        /// <returns>True when the key belongs to this class, also when the value is not a
        /// number (the value is then kept).</returns>
        /// <remarks>
        /// Input: The parameters.
        /// Output: The return value. Changes Bonus (clamped to 0-100) or ExtraRolls (clamped to
        /// 0-9).
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static bool TryLoad(string key, string value)
        {
            int n;
            if (key == ConfigKey)
            {
                if (int.TryParse(value, out n)) Bonus = Mathf.Clamp(n, 0, MaxBonus);
                return true;
            }
            if (key == RollsConfigKey)
            {
                if (int.TryParse(value, out n)) ExtraRolls = Mathf.Clamp(n, 0, MaxRolls);
                return true;
            }
            return false;
        }

        /// <summary>
        /// The config.json entries of the two sliders, to append to the other entries.
        /// </summary>
        /// <returns>For each slider a comma, a new line and the "key": value pair.</returns>
        /// <remarks>
        /// Input: No parameters. Reads Bonus and ExtraRolls.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string ConfigJsonTail()
        {
            return ",\n  \"" + ConfigKey + "\": " + Bonus
                 + ",\n  \"" + RollsConfigKey + "\": " + ExtraRolls;
        }
    }
}
