using System;
using System.Collections.Generic;
using System.IO;
using MGSC;
using SimpleJSON;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class SmasherAugmentations
    {
        private static readonly HashSet<string> _loaded = new HashSet<string>();

        private static readonly Dictionary<string, List<string>> _categories =
            new Dictionary<string, List<string>>();

        private static readonly Dictionary<string, string> _sets =
            new Dictionary<string, string>();

        private static readonly Dictionary<string, List<string>> _jsonCategories =
            new Dictionary<string, List<string>>();

        private static readonly HashSet<string> _longStats = new HashSet<string>();

        private static readonly List<string> _multiSlot = new List<string>();

        private static readonly List<string> _specialArms = new List<string>();

        public const string PmcTag = "PMC";

        public const string LongStatsKey = "ae_kill_long_stats";
        public const string MultiSlotKey = "ae_lose_augmentation_on_amputate";
        public const string SpecialArmKey = "ae_special_weapon_arm";

        public const string SetSmasher     = "Smasher";
        public const string SetNachtreiher = "Nachtreiher";
        public const string SetScout22     = "Scout22";
        public const string SetAndras      = "Andras";
        public const string SetNemesis     = "Nemesis";
        public const string SetOpi         = "OPI";
        public const string SetMedic       = "Medic";
        public const string SetSetless     = "Setless";

        /// <summary>
        /// Switches on or off the spawn on enemies of every piece of a set. Enemies only get items
        /// whose Categories share a tag with their whitelist, so empty Categories stop the spawn;
        /// switching back on restores the tags read from the json.
        /// </summary>
        /// <param name="set">Set name, i.e. the subfolder name under Assets: "Smasher",
        /// "Nachtreiher", "Scout22", "Andras", "Nemesis", "OPI", "Setless" or "WeaponLimbs". An
        /// unknown name changes nothing.</param>
        /// <param name="enabled">True to allow the spawn, false to stop it.</param>
        /// <remarks>
        /// Input: The parameters and the tags kept from the json of each piece.
        /// Output: Nothing is returned. Changes the Categories of the pieces of the set and writes
        /// a line in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void SetSpawnEnabled(string set, bool enabled)
        {
            int touched = 0;
            foreach (var kv in _categories)
            {
                string owner;
                if (!_sets.TryGetValue(kv.Key, out owner) || owner != set) continue;

                var rec = Data.Items.GetSimpleRecord<AugmentationRecord>(kv.Key);
                if (rec == null) continue;
                rec.Categories = enabled ? new List<string>(kv.Value) : new List<string>();
                touched++;
            }
            Debug.Log("[AugmentationsExpanded] " + set + ": spawning on enemies "
                    + (enabled ? "on" : "off") + " for " + touched + " pieces.");
        }

        /// <summary>
        /// Same as SetSpawnEnabled for a single augmentation, for those with a switch of their own
        /// (M.E.R.C. and the gun arms).
        /// </summary>
        /// <param name="id">Id of an augmentation loaded by the mod, e.g. "ae_merc". Unknown ids
        /// are ignored.</param>
        /// <param name="enabled">True to allow the spawn, false to stop it.</param>
        /// <remarks>
        /// Input: The parameters and the tags kept from the json of the augmentation.
        /// Output: Nothing is returned. Changes the Categories of the augmentation and writes a
        /// line in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void SetItemSpawnEnabled(string id, bool enabled)
        {
            List<string> cats;
            if (!_categories.TryGetValue(id, out cats)) return;
            var rec = Data.Items.GetSimpleRecord<AugmentationRecord>(id);
            if (rec == null) return;
            rec.Categories = enabled ? new List<string>(cats) : new List<string>();
            Debug.Log("[AugmentationsExpanded] " + id + ": spawning on enemies "
                    + (enabled ? "on" : "off") + ".");
        }

        /// <summary>
        /// Faction ids among the Categories of an augmentation, in json order. Lets the json stay
        /// the only place where the factions of a gun arm are written.
        /// </summary>
        /// <param name="id">Id of an augmentation loaded by the mod, e.g. "ae_gl_arm".</param>
        /// <returns>The faction ids, e.g. ["RealWare"]. Never null; empty for an unknown id or when
        /// the factions cannot be read.</returns>
        /// <remarks>
        /// Input: The parameter, the tags kept from the json and the factions in the game data.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static List<string> FactionTags(string id)
        {
            var outTags = new List<string>();
            var factions = FactionIds();
            List<string> cats;
            if (factions == null || !_categories.TryGetValue(id, out cats)) return outTags;
            foreach (string c in cats)
                if (!string.IsNullOrEmpty(c) && factions.Contains(c) && !outTags.Contains(c))
                    outTags.Add(c);
            return outTags;
        }

        /// <summary>
        /// Adds an augmentation of the mod to the unlock list of an existing chip, such as the
        /// vanilla augmentation chip. The list is rebuilt from the game configs at every launch, so
        /// nothing is stored in the save; the game skips unlocked recipes whose item no longer
        /// exists.
        /// </summary>
        /// <param name="chipId">Id of an existing chip, e.g. "augment_chip".</param>
        /// <param name="augId">Id of the augmentation to unlock, which should have a crafting
        /// recipe.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Adds augId to the chip's unlock list when not already
        /// there; logs a warning when the chip does not exist.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void AddToChip(string chipId, string augId)
        {
            var chip = Data.Items.GetSimpleRecord<DatadiskRecord>(chipId);
            if (chip == null || chip.UnlockIds == null)
            {
                Debug.LogWarning("[AugmentationsExpanded] chip " + chipId + " not found: "
                               + augId + " stays out of its pool.");
                return;
            }
            if (chip.UnlockIds.Contains(augId)) return;
            chip.UnlockIds.Add(augId);
            Debug.Log("[AugmentationsExpanded] " + augId + " added to the " + chipId + " pool.");
        }

        private static HashSet<string> _factionIds;

        /// <summary>
        /// Every faction id in the game data, disabled ones included, cached after the first
        /// successful read. Used to tell faction tags apart from enemy type tags.
        /// </summary>
        /// <returns>The faction ids, compared ignoring case, or null when the factions cannot be
        /// read.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the factions in the game data.
        /// Output: The return value. The first successful read is cached.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static HashSet<string> FactionIds()
        {
            if (_factionIds != null) return _factionIds;
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var f in Data.Factions.Records)
                    if (f != null && !string.IsNullOrEmpty(f.Id)) ids.Add(f.Id);
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] reading factions failed, "
                             + "the faction tag of the pieces is left as it is: " + e.Message);
                return null;
            }
            _factionIds = ids;
            return _factionIds;
        }

        /// <summary>
        /// Tags of a set that are not faction ids (the enemy type tags such as Specops), in json
        /// order, for the preferred faction tooltip.
        /// </summary>
        /// <param name="set">Set name, i.e. the subfolder name under Assets: "Smasher",
        /// "Nachtreiher", "Scout22", "Andras", "Nemesis", "OPI", "Setless" or "WeaponLimbs". An
        /// unknown name changes nothing.</param>
        /// <returns>The tags, e.g. ["Specops"]. Never null; empty for an unknown set.</returns>
        /// <remarks>
        /// Input: The parameter, the tags kept from the json and the factions in the game data.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static List<string> BaseTags(string set)
        {
            var factions = FactionIds();
            var outTags = new List<string>();
            foreach (var kv in _sets)
            {
                if (kv.Value != set) continue;
                List<string> cats;
                if (!_categories.TryGetValue(kv.Key, out cats)) continue;
                foreach (string c in cats)
                    if (!string.IsNullOrEmpty(c)
                        && (factions == null || !factions.Contains(c))
                        && !outTags.Contains(c))
                        outTags.Add(c);
            }
            return outTags;
        }

        /// <summary>
        /// Replaces the faction tag of every piece of a set, keeping its other tags: every tag that
        /// is a faction id is removed and the chosen one is added, so calling it twice gives the
        /// same result. "Faction" is refused because it is a key of the enemy whitelists, not a
        /// faction id. The record is only changed while the set spawns, because empty Categories is
        /// how a switched off set is marked.
        /// </summary>
        /// <param name="set">Set name, i.e. the subfolder name under Assets: "Smasher",
        /// "Nachtreiher", "Scout22", "Andras", "Nemesis", "OPI", "Setless" or "WeaponLimbs". An
        /// unknown name changes nothing.</param>
        /// <param name="factionTag">A faction id from the game data, e.g. "RealWare", or "none" for
        /// no faction. Null or empty also means none.</param>
        /// <remarks>
        /// Input: The parameters, the tags kept for each piece and the factions in the game data.
        /// Output: Nothing is returned. Changes the kept tags of the pieces and, while the set
        /// spawns, their Categories; writes a line in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void SetFaction(string set, string factionTag)
        {
            var factions = FactionIds();
            if (factions == null) return;

            bool none = string.IsNullOrEmpty(factionTag)
                     || factionTag.Equals("none", StringComparison.OrdinalIgnoreCase);
            if (!none && factionTag.Equals("Faction", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError("[AugmentationsExpanded] \"Faction\" is a whitelist key, "
                             + "not a faction id: " + set + " left as it is.");
                return;
            }

            int touched = 0;
            foreach (var kv in _sets)
            {
                if (kv.Value != set) continue;
                List<string> current;
                if (!_categories.TryGetValue(kv.Key, out current)) continue;

                var next = new List<string>();
                foreach (string c in current)
                    if (!string.IsNullOrEmpty(c) && !factions.Contains(c)) next.Add(c);
                if (!none) next.Add(factionTag);
                _categories[kv.Key] = next;

                var rec = Data.Items.GetSimpleRecord<AugmentationRecord>(kv.Key);
                if (rec != null && rec.Categories != null && rec.Categories.Count > 0)
                    rec.Categories = new List<string>(next);
                touched++;
            }
            Debug.Log("[AugmentationsExpanded] " + set + ": faction tag "
                    + (none ? "removed" : "set to " + factionTag) + " on " + touched + " pieces.");
        }

        /// <summary>
        /// Adds or removes the PMC tag on every piece of a set, so enemies whose loot whitelist
        /// accepts PMC (PMC soldiers, heavy PMC, assassins, bounty hunters) can carry it too. The
        /// tag is kept with the other tags, so SetFaction leaves it alone. A PMC tag written in a
        /// piece's json is never removed. The record is only changed while the set spawns,
        /// because empty Categories is how a switched off set is marked.
        /// </summary>
        /// <param name="set">Set name, i.e. the subfolder name under Assets: "Smasher",
        /// "Nachtreiher", "Scout22", "Andras" or "Nemesis". An unknown name changes
        /// nothing.</param>
        /// <param name="enabled">True to add the tag, false to remove it.</param>
        /// <remarks>
        /// Input: The parameters and the tags kept for each piece.
        /// Output: Nothing is returned. Changes the kept tags of the pieces and, while the set
        /// spawns, their Categories; writes a line in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void SetPmc(string set, bool enabled)
        {
            int touched = 0;
            foreach (var kv in _sets)
            {
                if (kv.Value != set) continue;
                List<string> current;
                if (!_categories.TryGetValue(kv.Key, out current)) continue;

                List<string> json;
                bool inJson = _jsonCategories.TryGetValue(kv.Key, out json)
                           && json.Exists(c => string.Equals(c, PmcTag, StringComparison.OrdinalIgnoreCase));
                bool has = current.Exists(c => string.Equals(c, PmcTag, StringComparison.OrdinalIgnoreCase));

                var next = new List<string>(current);
                if (enabled && !has) next.Add(PmcTag);
                else if (!enabled && has && !inJson)
                    next.RemoveAll(c => string.Equals(c, PmcTag, StringComparison.OrdinalIgnoreCase));
                else continue;
                _categories[kv.Key] = next;

                var rec = Data.Items.GetSimpleRecord<AugmentationRecord>(kv.Key);
                if (rec != null && rec.Categories != null && rec.Categories.Count > 0)
                    rec.Categories = new List<string>(next);
                touched++;
            }
            Debug.Log("[AugmentationsExpanded] " + set + ": PMC tag " + (enabled ? "added to " : "removed from ")
                    + touched + " pieces.");
        }

        /// <summary>
        /// Set of an augmentation loaded by the mod.
        /// </summary>
        /// <param name="id">Item id, e.g. "nacht_head". Null is accepted.</param>
        /// <returns>The set name, e.g. "Nachtreiher", "Setless" or "WeaponLimbs", or null for an
        /// item this mod did not load.</returns>
        /// <remarks>
        /// Input: The parameter and the sets remembered while loading.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static string SetOfItem(string id)
        {
            string set;
            return id != null && _sets.TryGetValue(id, out set) ? set : null;
        }

        /// <summary>
        /// Whether the item tooltip of an augmentation hides its effect list, because its json has
        /// "ae_kill_long_stats": "On" (or true) in Data. See SmasherLongStats.
        /// </summary>
        /// <param name="id">Item id, e.g. "ae_opi". Null is accepted.</param>
        /// <returns>True for an augmentation loaded by the mod with the switch on; false otherwise,
        /// also for every vanilla item.</returns>
        /// <remarks>
        /// Input: The parameter and the switches remembered while loading.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static bool HidesEffectList(string id)
        {
            return id != null && _longStats.Contains(id);
        }

        /// <summary>
        /// Whether an augmentation is tagged as Multi-WoundSlot: its json has
        /// "ae_lose_augmentation_on_amputate": "On" (or true) in Data and it has two or more wound
        /// slots. Whether it is lost after an amputation then follows the general switch of the
        /// mod (see SmasherMultiSlot).
        /// </summary>
        /// <param name="id">Item id, e.g. "ae_opi". Null is accepted.</param>
        /// <returns>True for a tagged augmentation loaded by the mod; false otherwise, also for
        /// every vanilla item.</returns>
        /// <remarks>
        /// Input: The parameter and the tags remembered while loading.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static bool IsMultiSlot(string id)
        {
            return id != null && _multiSlot.Contains(id);
        }

        /// <summary>
        /// The augmentations tagged as Multi-WoundSlot, in the order they were loaded. Used for
        /// the list in the tooltip of the general switch.
        /// </summary>
        /// <remarks>
        /// Input: The tags remembered while loading.
        /// Output: The ids, e.g. "ae_opi". Empty before the augmentations are loaded.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static IEnumerable<string> MultiSlotIds { get { return _multiSlot; } }

        /// <summary>
        /// Whether an augmentation is tagged as Special Weapon Arm (Data.ae_special_weapon_arm
        /// set to "On" in its json): a weapon arm of the mod whose primary slot block follows the
        /// special weapon arm mode of the Mod Config Menu (see SmasherGunArms).
        /// </summary>
        /// <param name="id">Item id, e.g. "ae_gl_arm". Null is accepted.</param>
        /// <returns>True for a tagged augmentation loaded by the mod; false otherwise, also for
        /// every vanilla item.</returns>
        /// <remarks>
        /// Input: The parameter and the tags remembered while loading.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static bool IsSpecialArm(string id)
        {
            return id != null && _specialArms.Contains(id);
        }

        /// <summary>
        /// The augmentations tagged as Special Weapon Arm, in the order they were loaded. Used for
        /// the list in the tooltip of the special weapon arm mode.
        /// </summary>
        /// <remarks>
        /// Input: The tags remembered while loading.
        /// Output: The ids, e.g. "ae_gl_arm". Empty before the augmentations are loaded.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static IEnumerable<string> SpecialArmIds { get { return _specialArms; } }

        /// <summary>
        /// Sets the tech level of every piece of a set. It decides from which tech level the pieces
        /// can appear in missions and is shown on their tooltip. Crafting ignores it.
        /// </summary>
        /// <param name="set">Set name, i.e. the subfolder name under Assets: "Smasher",
        /// "Nachtreiher", "Scout22", "Andras", "Nemesis", "OPI", "Setless" or "WeaponLimbs". An
        /// unknown name changes nothing.</param>
        /// <param name="techLevel">Tech level, expected 1 to 10. Not clamped.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the TechLevel of the pieces and writes a line in
        /// the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void SetTechLevel(string set, int techLevel)
        {
            int touched = 0;
            foreach (var kv in _sets)
            {
                if (kv.Value != set) continue;
                var rec = Data.Items.GetSimpleRecord<AugmentationRecord>(kv.Key);
                if (rec == null) continue;
                rec.TechLevel = techLevel;
                touched++;
            }
            Debug.Log("[AugmentationsExpanded] " + set + ": " + touched
                    + " pieces set to TL" + techLevel + ".");
        }

        /// <summary>
        /// Loads every json under Assets/Augmentations (one subfolder per set) as an augmentation
        /// record with its sprites. Ids that already exist in the game are left untouched.
        /// </summary>
        /// <param name="modContentPath">Folder of the mod, the one that contains Assets. Null or
        /// empty loads nothing.</param>
        /// <returns>The number of augmentations added; 0 when the path or the folder is
        /// missing.</returns>
        /// <remarks>
        /// Input: The parameter, the json files under Assets/Augmentations and their images.
        /// Output: The return value. Adds the records to the game items and remembers tags and set
        /// of each.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static int Register(string modContentPath)
        {
            if (string.IsNullOrEmpty(modContentPath)) return 0;

            string assets   = Path.Combine(modContentPath, "Assets");
            string recDir   = Path.Combine(assets, "Augmentations");
            string descDir  = Path.Combine(Path.Combine(assets, "Descriptors"), "Augmentations");
            if (!Directory.Exists(recDir))
            {
                Debug.LogError("[AugmentationsExpanded] Augmentations folder not found: " + recDir);
                return 0;
            }

            int added = 0;
            foreach (string file in Directory.GetFiles(recDir, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    if (LoadOne(file, descDir, assets, SetOf(recDir, file))) added++;
                }
                catch (Exception e)
                {
                    Debug.LogError("[AugmentationsExpanded] " + Path.GetFileName(file) + ": " + e);
                }
            }
            Debug.Log("[AugmentationsExpanded] augmentations registered: " + added
                    + " (known in total: " + _loaded.Count + ")");
            return added;
        }

        /// <summary>
        /// Set of a file: the name of the first folder below the root that contains it, so
        /// Assets/Augmentations/Smasher/x.json belongs to "Smasher". Also used for the crate, wound
        /// slot and datadisk folders.
        /// </summary>
        /// <param name="root">Full path of the folder that holds one subfolder per set, e.g.
        /// Assets/Augmentations.</param>
        /// <param name="file">Full path of a json inside root.</param>
        /// <returns>The set name, e.g. "Smasher", or an empty string for a file directly in
        /// root.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string SetOf(string root, string file)
        {
            string dir = Path.GetDirectoryName(file);
            if (string.IsNullOrEmpty(dir) || dir.Length <= root.Length) return string.Empty;

            string rel = dir.Substring(root.Length)
                            .Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (rel.Length == 0) return string.Empty;

            int cut = rel.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            return cut < 0 ? rel : rel.Substring(0, cut);
        }

        /// <summary>
        /// Builds and registers one augmentation from its json, and remembers its Categories and
        /// set for the spawn and faction switches, whether its tooltip hides the effect list
        /// (Data.ae_kill_long_stats) and whether it is tagged as Multi-WoundSlot
        /// (Data.ae_lose_augmentation_on_amputate, ignored with a warning on a single wound slot).
        /// </summary>
        /// <param name="file">Full path of a json with a Data object holding at least Id.</param>
        /// <param name="descDir">Full path of the Descriptors/Augmentations folder.</param>
        /// <param name="assetsRoot">Full path of the Assets folder.</param>
        /// <param name="set">Set of the augmentation, as returned by SetOf.</param>
        /// <returns>True when a new record was added; false without an Id, for an id already loaded
        /// or already in the game.</returns>
        /// <remarks>
        /// Input: The parameters, the json file and its images. The inventory width is the json
        /// one, or follows the inventory icon when it comes from an icon pack (see
        /// SmasherIcons.InventoryWidth).
        /// Output: The return value. Adds the record to the game items; throws on unreadable json,
        /// which the caller catches.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool LoadOne(string file, string descDir, string assetsRoot, string set)
        {
            JSONNode root = JSON.Parse(File.ReadAllText(file));
            JSONNode data = root["Data"];
            string id = data["Id"].Value;
            if (string.IsNullOrEmpty(id)) return false;
            if (_loaded.Contains(id)) return false;

            if (Data.Items.GetRecord(id) != null)
            {
                _loaded.Add(id);
                Debug.Log("[AugmentationsExpanded] " + id + " already present, left as it is.");
                return false;
            }

            var rec = new AugmentationRecord
            {
                Categories         = SmasherJson.Strings(data["Categories"]),
                TechLevel          = data["TechLevel"].AsInt,
                Price              = data["Price"].AsFloat,
                Weight             = data["Weight"].AsFloat,
                InventoryWidthSize = data["InventoryWidthSize"].AsInt,
                CanPutInVest       = data["CanPutInVest"].AsBool,
                WoundSlotIds       = SmasherJson.Strings(data["WoundSlotIds"]),
                TooltipIconTag     = data["TooltipIconTag"].Value,
                Disassembly        = SmasherJson.Quantities(data["Disassembly"])
            };
            if (rec.InventoryWidthSize <= 0) rec.InventoryWidthSize = 1;

            SmasherSprites.SetProperty(rec, "Id", id);

            ItemClass itemClass;
            if (SmasherJson.TryEnum(data["ItemClass"].Value, out itemClass)) rec.ItemClass = itemClass;
            else Debug.LogWarning("[AugmentationsExpanded] unknown ItemClass for " + id
                                + ": '" + data["ItemClass"].Value + "'");

            AugmentationClass augClass;
            if (SmasherJson.TryEnum(data["AugmentationClass"].Value, out augClass)) rec.AugmentationClass = augClass;
            else Debug.LogWarning("[AugmentationsExpanded] unknown AugmentationClass for " + id
                                + ": '" + data["AugmentationClass"].Value + "'");

            Sprite packIcon;
            rec.ContentDescriptor = BuildDescriptor(id, descDir, assetsRoot, set, out packIcon);
            rec.InventoryWidthSize = SmasherIcons.InventoryWidth(id, packIcon, rec.InventoryWidthSize);

            Data.Items.AddRecord(id, rec);
            _loaded.Add(id);
            _categories[id] = new List<string>(rec.Categories ?? new List<string>());
            _jsonCategories[id] = new List<string>(rec.Categories ?? new List<string>());
            _sets[id] = set;
            if (SmasherJson.IsOn(data[LongStatsKey])) _longStats.Add(id);
            if (SmasherJson.IsOn(data[MultiSlotKey]))
            {
                if (rec.WoundSlotIds.Count >= 2) _multiSlot.Add(id);
                else Debug.LogWarning("[AugmentationsExpanded] " + id + ": " + MultiSlotKey
                                    + " ignored, the augmentation has a single wound slot.");
            }
            if (SmasherJson.IsOn(data[SpecialArmKey])) _specialArms.Add(id);
            Debug.Log("[AugmentationsExpanded] + " + id + "  set=" + (set.Length == 0 ? "-" : set)
                    + "  TL" + rec.TechLevel
                    + "  " + rec.InventoryWidthSize + "x1  slot=["
                    + string.Join(" ", (rec.WoundSlotIds ?? new List<string>()).ToArray()) + "]");
            return true;
        }

        /// <summary>
        /// Creates the item descriptor with the inventory, floor and shadow sprites. The images are
        /// Images/Augmentations/{id}_inv.png, _floor.png and _shadow.png, unless an
        /// {id}_descriptor.json anywhere under Descriptors/Augmentations names other paths. When an
        /// icon pack is chosen for the item's section, its own {id} images or the pack's copy of
        /// the descriptor images are used instead (see SmasherIcons.Pick). Sprites use 100 pixels
        /// per unit and pivot (0.5, 0), like the vanilla items.
        /// </summary>
        /// <param name="id">Id of the augmentation, e.g. "smasher_head".</param>
        /// <param name="descDir">Full path of the Descriptors/Augmentations folder.</param>
        /// <param name="assetsRoot">Full path of the Assets folder.</param>
        /// <param name="set">Set of the augmentation, as returned by SetOf.</param>
        /// <param name="packIcon">Set by the method: the inventory icon when it was loaded from an
        /// icon pack, otherwise null.</param>
        /// <returns>The descriptor. Never null; a missing image leaves that sprite null and is
        /// logged.</returns>
        /// <remarks>
        /// Input: The parameters, the optional descriptor json, the chosen icon pack and the
        /// images.
        /// Output: The return value and packIcon.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static AugmentationDescriptor BuildDescriptor(string id, string descDir, string assetsRoot, string set,
                                                              out Sprite packIcon)
        {
            string iconPath   = "Images/Augmentations/" + id + "_inv.png";
            string smallPath  = "Images/Augmentations/" + id + "_floor.png";
            string shadowPath = "Images/Augmentations/" + id + "_shadow.png";

            string descFile = FindFile(descDir, id + "_descriptor.json");
            if (descFile != null)
            {
                try
                {
                    JSONNode img = JSON.Parse(File.ReadAllText(descFile))["Data"]["ImageProperties"];
                    if (!string.IsNullOrEmpty(img["IconSpriteIdOrPath"].Value))
                        iconPath = img["IconSpriteIdOrPath"].Value;
                    if (!string.IsNullOrEmpty(img["SmallIconSpriteIdOrPath"].Value))
                        smallPath = img["SmallIconSpriteIdOrPath"].Value;
                    if (!string.IsNullOrEmpty(img["ShadowOnFloorSpriteIdOrPath"].Value))
                        shadowPath = img["ShadowOnFloorSpriteIdOrPath"].Value;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[AugmentationsExpanded] descriptor for " + id + " is unreadable: " + e.Message);
                }
            }

            var desc = ScriptableObject.CreateInstance<AugmentationDescriptor>();
            desc.name = id + "_descriptor";
            UnityEngine.Object.DontDestroyOnLoad(desc);

            SmasherIcons.Section section = SmasherIcons.ForItem(id, set);
            bool fromPack;
            Sprite icon = SmasherIcons.LoadItemSprite(assetsRoot, section, SmasherIcons.Kind.Items,
                "Images/Augmentations/" + id + "_inv.png", iconPath, out fromPack);
            packIcon = fromPack ? icon : null;
            SmasherSprites.SetField(desc, "_icon",      icon);
            SmasherSprites.SetField(desc, "_smallIcon", SmasherIcons.LoadItemSprite(assetsRoot, section, SmasherIcons.Kind.Items,
                "Images/Augmentations/" + id + "_floor.png", smallPath, out _));
            SmasherSprites.SetField(desc, "_shadow",    SmasherIcons.LoadItemSprite(assetsRoot, section, SmasherIcons.Kind.Items,
                "Images/Augmentations/" + id + "_shadow.png", shadowPath, out _));
            return desc;
        }

        /// <summary>
        /// First file with the given name anywhere under a folder. A missing folder is not an
        /// error.
        /// </summary>
        /// <param name="root">Full path of the folder to search. Null, empty or missing gives
        /// null.</param>
        /// <param name="fileName">File name without folders, e.g. "x_descriptor.json".</param>
        /// <returns>The full path of the first match, or null.</returns>
        /// <remarks>
        /// Input: The parameters and the files on disk.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string FindFile(string root, string fileName)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return null;
            string[] hits = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);
            return hits.Length > 0 ? hits[0] : null;
        }
    }
}
