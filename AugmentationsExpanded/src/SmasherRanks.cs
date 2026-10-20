using System;
using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace AugmentationsExpanded
{
    internal static class SmasherRanks
    {
        private static readonly string[] VanillaRankIds =
            { "rank_0", "rank_1", "rank_2", "rank_3", "rank_4", "rank_5" };

        private static List<string> _ids;

        /// <summary>
        /// Ids of the clone ranks in promotion order, read once from the game's perks: every perk
        /// of type Rank, vanilla or added by another mod, ordered by following NextPerkId from the
        /// first rank (the one no other rank points to). Ranks outside the chain are appended,
        /// sorted by the number in "rank_N". The position in this list is the rank number saved in
        /// config.json, so rank_0 is 0; the Mod Config Menu and the tooltips show it plus one, as
        /// the game's character panel does (rank_0 is 1 - Rookie).
        /// </summary>
        /// <returns>The perk ids, e.g. ["rank_0", ..., "rank_5"]. Never null or empty: when the
        /// perks cannot be read the six vanilla ids are returned, and the read is tried again on
        /// the next call.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the perks in the game data.
        /// Output: The return value. A successful read is cached and logged.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static List<string> Ids()
        {
            if (_ids != null) return _ids;

            var byId = new Dictionary<string, PerkRecord>();
            try
            {
                foreach (PerkRecord p in Data.Perks.Records)
                    if (p != null && p.PerkType == PerkType.Rank && !string.IsNullOrEmpty(p.Id))
                        byId[p.Id] = p;
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] reading the clone ranks failed: " + e.Message);
            }
            if (byId.Count == 0) return new List<string>(VanillaRankIds);

            var pointedTo = new HashSet<string>();
            foreach (PerkRecord p in byId.Values)
                if (!string.IsNullOrEmpty(p.NextPerkId)) pointedTo.Add(p.NextPerkId);

            var roots = new List<string>();
            foreach (string id in byId.Keys)
                if (!pointedTo.Contains(id)) roots.Add(id);
            roots.Sort(CompareIds);

            var ordered = new List<string>();
            var seen = new HashSet<string>();
            foreach (string root in roots)
            {
                string id = root;
                while (!string.IsNullOrEmpty(id) && byId.ContainsKey(id) && seen.Add(id))
                {
                    ordered.Add(id);
                    id = byId[id].NextPerkId;
                }
            }
            var rest = new List<string>();
            foreach (string id in byId.Keys)
                if (!seen.Contains(id)) rest.Add(id);
            rest.Sort(CompareIds);
            ordered.AddRange(rest);

            _ids = ordered;
            Debug.Log("[AugmentationsExpanded] clone ranks: " + string.Join(", ", _ids.ToArray()));
            return _ids;
        }

        /// <summary>
        /// Orders two rank ids by the number in "rank_N", ids without a number last, then by
        /// name.
        /// </summary>
        /// <param name="a">A perk id, e.g. "rank_2".</param>
        /// <param name="b">A perk id, e.g. "rank_10".</param>
        /// <returns>Negative when a comes first, positive when b does, 0 when equal.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static int CompareIds(string a, string b)
        {
            int na = ParseRankId(a), nb = ParseRankId(b);
            if (na != nb) return (na < 0 ? int.MaxValue : na).CompareTo(nb < 0 ? int.MaxValue : nb);
            return string.CompareOrdinal(a, b);
        }

        /// <summary>
        /// The number written in a rank id of the form "rank_N".
        /// </summary>
        /// <param name="id">A perk id, e.g. "rank_3". Null is accepted.</param>
        /// <returns>The number, e.g. 3, or -1 when the id has another form.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static int ParseRankId(string id)
        {
            int n;
            if (id != null && id.StartsWith("rank_", StringComparison.Ordinal)
                && int.TryParse(id.Substring(5), out n) && n >= 0)
                return n;
            return -1;
        }

        /// <summary>
        /// Keeps a rank number inside the ranks that exist, so a default or a saved value above
        /// the last rank (e.g. after removing a mod that added ranks) points to the last one.
        /// </summary>
        /// <param name="number">A rank number, 0 for the first rank.</param>
        /// <returns>The number clamped to 0 .. last rank.</returns>
        /// <remarks>
        /// Input: The parameter and the rank list.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static int Clamp(int number)
        {
            return Mathf.Clamp(number, 0, Ids().Count - 1);
        }

        /// <summary>
        /// Text of a rank in the dropdown, "{number} - {name}", with the name in the current
        /// language and its first letter in upper case. The number counts from 1, like the rank
        /// level in the game's character panel.
        /// </summary>
        /// <param name="number">A rank number, 0 for the first rank; clamped to the ranks that
        /// exist.</param>
        /// <returns>The text, e.g. "1 - Rookie" or "6 - Commander". The perk id stands in for a
        /// missing name.</returns>
        /// <remarks>
        /// Input: The parameter, the rank list and the game's text tables.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string Label(int number)
        {
            int n = Clamp(number);
            string id = Ids()[n];
            string key = "perk." + id + ".name";
            string name = Localization.Get(key);
            if (string.IsNullOrEmpty(name) || name == key) name = id;
            else name = char.ToUpperInvariant(name[0]) + name.Substring(1);
            return (n + 1) + " - " + name;
        }

        /// <summary>
        /// Every rank as dropdown text, in order, for the Mod Config Menu.
        /// </summary>
        /// <returns>The texts, e.g. ["1 - Rookie", "2 - Private", ...]. Never empty.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the rank list and the game's text tables.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static List<object> Options()
        {
            var list = new List<object>();
            for (int i = 0; i < Ids().Count; i++) list.Add(Label(i));
            return list;
        }

        /// <summary>
        /// Rank number of a value saved by the Mod Config Menu or read from config.json, from the
        /// leading digits of the text. A dropdown entry counts from 1 (see Label) and a bare
        /// number from config.json counts from 0, so both "5 - Colonel" and "4" give 4.
        /// </summary>
        /// <param name="value">The saved value. Null or text without leading digits is
        /// accepted.</param>
        /// <param name="fallback">Number returned when no digits are found.</param>
        /// <returns>The rank number, 0 for the first rank, or fallback.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static int Parse(object value, int fallback)
        {
            if (value == null) return fallback;
            string s = value.ToString().Trim();
            int end = 0;
            while (end < s.Length && char.IsDigit(s[end])) end++;
            int n;
            if (end == 0 || !int.TryParse(s.Substring(0, end), out n)) return fallback;
            return end < s.Length ? n - 1 : n;
        }

        /// <summary>
        /// Rank number of a clone of the player, from its rank perk.
        /// </summary>
        /// <param name="merc">A mercenary of the player. Null is accepted.</param>
        /// <returns>The position of its rank in the rank list, the number in "rank_N" for a rank
        /// that is not in the list, or -1 when it cannot be read.</returns>
        /// <remarks>
        /// Input: The parameter and the rank list.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static int Of(Mercenary merc)
        {
            try
            {
                if (merc == null || merc.CreatureData == null || merc.CreatureData.Perks == null
                    || merc.CreatureData.Perks.Count < 2)
                    return -1;
                Perk rank = merc.CreatureData.RankPerk;
                if (rank == null || string.IsNullOrEmpty(rank.PerkId)) return -1;
                int i = Ids().IndexOf(rank.PerkId);
                return i >= 0 ? i : ParseRankId(rank.PerkId);
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}
