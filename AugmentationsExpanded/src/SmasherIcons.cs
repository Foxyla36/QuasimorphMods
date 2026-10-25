using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MGSC;
using UnityEngine;

namespace AugmentationsExpanded
{
    internal static class SmasherIcons
    {
        internal enum Kind { Items, WoundSlots, Chip, Weapon }

        internal static readonly Kind[] Kinds = { Kind.Items, Kind.WoundSlots, Kind.Chip, Kind.Weapon };

        internal const string DefaultId     = "default";
        internal const string DefaultName   = "Default - Foxyla36";
        internal const string LegacyPrefix  = "Custom - ";
        internal const string FirstAuthor   = "Foxyla36";
        internal const string PackFolder    = "IconPacks";
        internal const string KeyPrefix     = "icons_";
        internal const int    OneCellWidth  = 24;
        internal const string KeyAdaptiveSize = "adaptive_inventory_size";

        internal static bool AdaptiveSize = false;

        internal sealed class Section
        {
            internal readonly string Key;
            internal readonly string HeaderKey;
            internal readonly string NameKey;
            internal readonly string SetFolder;
            internal readonly string[] ItemIds;
            internal readonly string[] SlotIds;
            internal readonly string[] WeaponIds;

            /// <summary>
            /// One section of the icons panel: a set, whose files are found by the folder they sit
            /// in, or a single setless item, whose files are listed explicitly.
            /// </summary>
            /// <param name="key">Short name used in the config keys, e.g. "smasher" or
            /// "gl".</param>
            /// <param name="headerKey">Localization key of the section header, e.g.
            /// "ae.cfg.header.smasher", "item.ae_merc.name" or, for a gun arm, the MCM-only name
            /// "ae.cfg.itemname.ae_gl_arm".</param>
            /// <param name="nameKey">Localization key of the name used in the tooltips, e.g.
            /// "ae.cfg.setname.smasher".</param>
            /// <param name="setFolder">Set folder name under Assets, e.g. "Smasher", or null for a
            /// setless item.</param>
            /// <param name="itemIds">Augmentation ids of a setless item, e.g. ["ae_gl_arm"]; null
            /// for a set.</param>
            /// <param name="slotIds">Wound slot ids of a setless item, e.g. ["AE_GLArm",
            /// "AE_GLShoulder"]; null for a set.</param>
            /// <param name="weaponIds">Limb weapon ids of a setless item, e.g.
            /// ["ae_nature_gl_arm"]; null for a set or an item without weapon.</param>
            /// <remarks>
            /// Input: The parameters only.
            /// Output: A new object.
            /// AI-generated documentation, written to help external users understand the mod.
            /// </remarks>
            internal Section(string key, string headerKey, string nameKey, string setFolder,
                             string[] itemIds = null, string[] slotIds = null, string[] weaponIds = null)
            {
                Key       = key;
                HeaderKey = headerKey;
                NameKey   = nameKey;
                SetFolder = setFolder;
                ItemIds   = itemIds   ?? new string[0];
                SlotIds   = slotIds   ?? new string[0];
                WeaponIds = weaponIds ?? new string[0];
            }

            internal bool IsSet => SetFolder != null;
        }

        private sealed class Family
        {
            internal string Id;
            internal string Name;
            internal string Root;
        }

        private static readonly List<Family> _families = new List<Family>();
        private static readonly Dictionary<string, string> _choice =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, HashSet<string>> _wanted =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private static readonly HashSet<string> _missingWarned = new HashSet<string>();
        private static List<Section> _sections;

        /// <summary>
        /// The sections of the icons panel, in panel order: the seven sets (OP-I and CMEDIC-32
        /// included), then M.E.R.C., the five gun arms, the UHMG and the other setless
        /// augmentations.
        /// </summary>
        /// <returns>The sections. Never null.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the gun arm list.
        /// Output: The return value, built on the first call.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static List<Section> Sections()
        {
            if (_sections != null) return _sections;
            var list = new List<Section>
            {
                new Section("smasher", "ae.cfg.header.smasher", "ae.cfg.setname.smasher", SmasherAugmentations.SetSmasher),
                new Section("nacht",   "ae.cfg.header.nacht",   "ae.cfg.setname.nacht",   SmasherAugmentations.SetNachtreiher),
                new Section("scout22", "ae.cfg.header.scout22", "ae.cfg.setname.scout22", SmasherAugmentations.SetScout22),
                new Section("andras",  "ae.cfg.header.andras",  "ae.cfg.setname.andras",  SmasherAugmentations.SetAndras),
                new Section("nemesis", "ae.cfg.header.nemesis", "ae.cfg.setname.nemesis", SmasherAugmentations.SetNemesis),
                new Section("opi",     "ae.cfg.header.opi",     "ae.cfg.setname.opi",     SmasherAugmentations.SetOpi),
                new Section("medic",   SmasherMedic.Header,     SmasherMedic.SetNameKey,  SmasherAugmentations.SetMedic),
                new Section("merc", "item." + AugmentationsExpandedMod.MercId + ".name",
                            "item." + AugmentationsExpandedMod.MercId + ".name", null,
                            new[] { AugmentationsExpandedMod.MercId }, new[] { "AE_Merc" })
            };
            foreach (SmasherGunArms.GunArm arm in SmasherGunArms.All)
                list.Add(new Section(arm.Name, arm.McmNameKey, arm.McmNameKey, null,
                                     new[] { arm.AugId }, new[] { arm.ArmSlotId, arm.ShoulderSlotId },
                                     new[] { arm.WeaponId }));
            list.Add(new Section("uhmg", "item." + SmasherUhmg.AugId + ".name", "item." + SmasherUhmg.AugId + ".name", null,
                                 new[] { SmasherUhmg.AugId },
                                 new[] { SmasherUhmg.ArmSlotId, SmasherUhmg.ShoulderSlotId, SmasherUhmg.ChestSlotId },
                                 new[] { SmasherUhmg.WeaponId }));
            foreach (SmasherSetless.Item it in SmasherSetless.All)
                list.Add(new Section(it.Name, "item." + it.AugId + ".name", "item." + it.AugId + ".name", null,
                                     new[] { it.AugId }, it.SlotIds,
                                     it.WeaponId == null ? new string[0] : new[] { it.WeaponId }));
            _sections = list;
            return _sections;
        }

        /// <summary>
        /// Config key of one dropdown of the icons panel.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <param name="kind">The kind of art.</param>
        /// <returns>The key, e.g. "icons_smasher_items", "icons_gl_weapon".</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string Key(Section section, Kind kind)
        {
            string suffix = kind == Kind.Items ? "items"
                          : kind == Kind.WoundSlots ? "woundslots"
                          : kind == Kind.Chip ? "chip"
                          : "weapon";
            return KeyPrefix + section.Key + "_" + suffix;
        }

        /// <summary>
        /// Finds the installed icon packs, the alternative art for the items, wound slots, chips
        /// and limb weapons of the mod that the Augmentations Expanded Icons panel lets the player
        /// pick per set and per setless item. The art in Assets/Images is "Default - Foxyla36".
        /// Every pack is a subfolder of Assets/IconPacks in the mod (packs shipped with it) or of
        /// Quasimorph_ModConfigs/Augmentations_Expanded/IconPacks (packs added by the player, kept
        /// across mod updates), listed with its folder name as it is, e.g. "Vanilla - Foxyla36"
        /// or "EmoUsedHM01". The packs whose name contains "Foxyla36" come first, then all the
        /// others; each group keeps the order the folders were read in (the mod's packs, then
        /// the player's, each in alphabetical order). A pack mirrors Assets/Images
        /// (Augmentations, Trash, Datadisks, WoundSlots, Weapons) and may hold only some files:
        /// every file it lacks keeps the default art. A name already found is skipped. The second
        /// folder is created when missing, so players can find where to put packs.
        /// </summary>
        /// <param name="modContentPath">Folder of the mod, the one that contains Assets. Null or
        /// empty skips the packs of the mod.</param>
        /// <param name="userDir">Full path of the mod's folder in Quasimorph_ModConfigs. Null or
        /// empty skips the packs of the player.</param>
        /// <remarks>
        /// Input: The parameters and the folders on disk.
        /// Output: Nothing is returned. Fills the family list, may create the player's IconPacks
        /// folder and writes a line in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void Init(string modContentPath, string userDir)
        {
            _families.Clear();
            if (!string.IsNullOrEmpty(modContentPath))
                AddPacks(Path.Combine(Path.Combine(modContentPath, "Assets"), PackFolder));
            if (!string.IsNullOrEmpty(userDir))
            {
                string dir = Path.Combine(userDir, PackFolder);
                try { Directory.CreateDirectory(dir); }
                catch (Exception e) { Debug.Log("[AugmentationsExpanded] cannot create " + dir + ": " + e.Message); }
                AddPacks(dir);
            }
            var first = new List<Family>();
            var rest = new List<Family>();
            foreach (Family f in _families)
                (f.Name.IndexOf(FirstAuthor, StringComparison.OrdinalIgnoreCase) >= 0 ? first : rest).Add(f);
            _families.Clear();
            _families.AddRange(first);
            _families.AddRange(rest);

            var names = new List<string>();
            foreach (Family f in _families) names.Add(f.Name);
            Debug.Log("[AugmentationsExpanded] icon packs: " + (names.Count == 0
                      ? "none, only " + DefaultName
                      : string.Join(", ", names.ToArray())));
        }

        /// <summary>
        /// Adds every subfolder of a folder as an icon family, in alphabetical order, with the
        /// folder name as dropdown entry.
        /// </summary>
        /// <param name="dir">Full path of an IconPacks folder. A missing folder adds
        /// nothing.</param>
        /// <remarks>
        /// Input: The parameters and the folders on disk.
        /// Output: Nothing is returned. Adds to the family list; a duplicate name is logged and
        /// skipped.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void AddPacks(string dir)
        {
            if (!Directory.Exists(dir)) return;
            string[] subs;
            try { subs = Directory.GetDirectories(dir); }
            catch (Exception e)
            {
                Debug.Log("[AugmentationsExpanded] reading " + dir + " failed: " + e.Message);
                return;
            }
            Array.Sort(subs, StringComparer.OrdinalIgnoreCase);
            foreach (string sub in subs)
            {
                string name = Path.GetFileName(sub.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrEmpty(name) || name.StartsWith(".")) continue;
                if (name.Equals(DefaultId, StringComparison.OrdinalIgnoreCase)
                    || name.Equals(DefaultName, StringComparison.OrdinalIgnoreCase) || Find(name) != null)
                {
                    Debug.LogWarning("[AugmentationsExpanded] icon pack " + sub + " skipped: the name "
                                   + name + " is already used.");
                    continue;
                }
                _families.Add(new Family { Id = name, Name = name, Root = sub });
            }
        }

        /// <summary>
        /// The family with the given id, ignoring case.
        /// </summary>
        /// <param name="id">A family id, i.e. the author folder name.</param>
        /// <returns>The family, or null for "default" or an unknown id.</returns>
        /// <remarks>
        /// Input: The parameter and the family list.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static Family Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (Family f in _families)
                if (f.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) return f;
            return null;
        }

        /// <summary>
        /// The family chosen for one dropdown, as read from config.json or the panel.
        /// </summary>
        /// <param name="key">Config key, e.g. "icons_nacht_woundslots".</param>
        /// <returns>The family, or null for the default art. A chosen pack that is no longer
        /// installed also gives null, with one warning in the log.</returns>
        /// <remarks>
        /// Input: The parameter, the choices and the family list.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static Family Chosen(string key)
        {
            string id;
            if (!_choice.TryGetValue(key, out id) || string.IsNullOrEmpty(id)
                || id.Equals(DefaultId, StringComparison.OrdinalIgnoreCase))
                return null;
            Family f = Find(id);
            if (f == null && _missingWarned.Add(key))
                Debug.LogWarning("[AugmentationsExpanded] icon pack '" + id + "' chosen for " + key
                               + " is not installed: using " + DefaultName + ".");
            return f;
        }

        /// <summary>
        /// The section that owns an item: the setless item listing it, otherwise the set of the
        /// folder it was loaded from.
        /// </summary>
        /// <param name="itemId">Item id, e.g. "smasher_head", "nacht_box", "nacht_chip" or
        /// "ae_merc".</param>
        /// <param name="setFolder">Set folder the item's json sits in, e.g. "Smasher".</param>
        /// <returns>The section, or null for items outside the panel (e.g. the hidden test
        /// arms).</returns>
        /// <remarks>
        /// Input: The parameters and the section list.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static Section ForItem(string itemId, string setFolder)
        {
            foreach (Section s in Sections())
                if (Array.IndexOf(s.ItemIds, itemId) >= 0) return s;
            return ForSet(setFolder);
        }

        /// <summary>
        /// The section of a set.
        /// </summary>
        /// <param name="setFolder">Set folder name, e.g. "Nachtreiher". Null is
        /// accepted.</param>
        /// <returns>The section, or null when the name is not one of the seven sets.</returns>
        /// <remarks>
        /// Input: The parameter and the section list.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static Section ForSet(string setFolder)
        {
            if (string.IsNullOrEmpty(setFolder)) return null;
            foreach (Section s in Sections())
                if (s.SetFolder == setFolder) return s;
            return null;
        }

        /// <summary>
        /// The section that owns a wound slot: the setless item listing it, otherwise the set of
        /// the folder its json was loaded from.
        /// </summary>
        /// <param name="slotId">Wound slot id, e.g. "NachtArm" or "AE_GLShoulder".</param>
        /// <returns>The section, or null for slots outside the panel.</returns>
        /// <remarks>
        /// Input: The parameter, the section list and the folders recorded by SmasherRecords.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static Section ForSlot(string slotId)
        {
            foreach (Section s in Sections())
                if (Array.IndexOf(s.SlotIds, slotId) >= 0) return s;
            return ForSet(SmasherRecords.SetOf(slotId));
        }

        /// <summary>
        /// The section that owns a limb weapon: the setless item listing it, otherwise the
        /// section of the wound slot that uses it as bare hands weapon (the fists of the sets).
        /// Needs the wound slots to be registered.
        /// </summary>
        /// <param name="weaponId">Weapon id, e.g. "ae_nature_nacht_fist".</param>
        /// <returns>The section, or null for weapons outside the panel.</returns>
        /// <remarks>
        /// Input: The parameter, the section list and the wound slot records of the mod.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static Section ForWeapon(string weaponId)
        {
            foreach (Section s in Sections())
                if (Array.IndexOf(s.WeaponIds, weaponId) >= 0) return s;
            foreach (string slotId in SmasherRecords.Ids)
            {
                WoundSlotRecord rec = Data.WoundSlots.GetRecord(slotId);
                if (rec != null && rec.BareHandWeapon == weaponId) return ForSlot(slotId);
            }
            return null;
        }

        /// <summary>
        /// Picks the file to load for one sprite, following the family chosen for the section.
        /// The pack is searched first for a file of this item only (ownPath), then for the file
        /// the default art uses (sharedPath); when the pack has neither, the default file is
        /// used. Both paths are remembered, so the panel only lists packs that have art for the
        /// section.
        /// </summary>
        /// <param name="assetsRoot">Full path of the mod's Assets folder.</param>
        /// <param name="section">Section of the item, from ForItem, ForSlot or ForWeapon. Null
        /// always picks the default file.</param>
        /// <param name="kind">Kind of art.</param>
        /// <param name="ownPath">Path below Assets of a file for this item only, e.g.
        /// "Images/Augmentations/smasher_head_inv.png". Null or empty is accepted.</param>
        /// <param name="sharedPath">Path below Assets of the default file, e.g.
        /// "Images/Augmentations/smasher_component_inv.png".</param>
        /// <param name="root">Set by the method: the base folder to load from, the pack folder
        /// or assetsRoot.</param>
        /// <param name="path">Set by the method: the path below root.</param>
        /// <remarks>
        /// Input: The parameters, the chosen families and the files on disk.
        /// Output: root and path. Remembers the paths for the panel.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void Pick(string assetsRoot, Section section, Kind kind, string ownPath,
                                  string sharedPath, out string root, out string path)
        {
            root = assetsRoot;
            path = sharedPath;
            if (section == null) return;

            string key = Key(section, kind);
            Want(key, ownPath);
            Want(key, sharedPath);

            Family f = Chosen(key);
            if (f == null) return;
            if (InPack(f, ownPath))         { root = f.Root; path = PackPath(ownPath); }
            else if (InPack(f, sharedPath)) { root = f.Root; path = PackPath(sharedPath); }
        }

        /// <summary>
        /// Loads one item sprite (inventory, floor or shadow) with the parameters of the vanilla
        /// items, 100 pixels per unit and pivot (0.5, 0): from the icon pack chosen for the section
        /// when the pack has the file, otherwise from the default path (see Pick).
        /// </summary>
        /// <param name="assetsRoot">Full path of the mod's Assets folder.</param>
        /// <param name="section">Section of the item; null loads the default path.</param>
        /// <param name="kind">Kind of art: Items for augmentations and crates, Chip for
        /// datadisks.</param>
        /// <param name="ownPath">Path below Assets of a file for this item only, e.g.
        /// "Images/Augmentations/smasher_head_inv.png".</param>
        /// <param name="sharedPath">Path below Assets of the default file, e.g.
        /// "Images/Augmentations/smasher_component_inv.png".</param>
        /// <param name="fromPack">Set by the method: true when the file comes from the icon
        /// pack.</param>
        /// <returns>The sprite, or null when the file is missing (logged).</returns>
        /// <remarks>
        /// Input: The parameters, the chosen icon pack and the image file.
        /// Output: The return value and fromPack.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static Sprite LoadItemSprite(string assetsRoot, Section section, Kind kind, string ownPath,
                                              string sharedPath, out bool fromPack)
        {
            string root, path;
            Pick(assetsRoot, section, kind, ownPath, sharedPath, out root, out path);
            fromPack = root != assetsRoot;
            return SmasherSprites.Load(root, path, 100f, 0.5f, 0f);
        }

        /// <summary>
        /// Inventory width of an item, in cells. With the "Adaptive inventory size" switch of the
        /// icons panel on, it follows the inventory icon when that icon comes from an icon pack: up
        /// to 24 pixels wide it takes 1 cell, wider it takes 2, so a pack can draw a piece as a
        /// 1x1 or a 2x1 item. With the switch off (the default), or with the default art, the json
        /// width is kept: 2x1 for the set pieces, gun arms and crates, 1x1 for M.E.R.C. and the
        /// chips. The default art keeps it even with the switch on, since some default icons are
        /// narrower than their item (the set crates are 2 cells with a 22-24 pixel icon). The game
        /// copies the width into every item when it is created and saves it with the item, so only
        /// items created from now on take the new size; items already in a save keep theirs,
        /// without overlapping, until they are recreated (e.g. an augment installed on a clone and
        /// removed again).
        /// </summary>
        /// <param name="itemId">Item id, for the log, e.g. "smasher_head".</param>
        /// <param name="packIcon">The inventory icon when it was loaded from an icon pack; null
        /// when it is the default one or could not be loaded.</param>
        /// <param name="jsonWidth">Width written in the item's json, 1 or 2.</param>
        /// <returns>1 or 2 for a pack icon with the switch on, otherwise jsonWidth.</returns>
        /// <remarks>
        /// Input: The parameters and the adaptive size switch.
        /// Output: The return value, plus a line in the log when the width differs from the
        /// json.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static int InventoryWidth(string itemId, Sprite packIcon, int jsonWidth)
        {
            if (!AdaptiveSize || packIcon == null) return jsonWidth;
            int pixels = (int)packIcon.rect.width;
            int cells = pixels > OneCellWidth ? 2 : 1;
            if (cells != jsonWidth)
                Debug.Log("[AugmentationsExpanded] " + itemId + ": icon pack art is " + pixels
                        + " px wide, inventory size " + cells + "x1 instead of " + jsonWidth + "x1.");
            return cells;
        }

        /// <summary>
        /// Remembers a path looked up for a dropdown.
        /// </summary>
        /// <param name="key">Config key of the dropdown.</param>
        /// <param name="path">Path below Assets. Null or empty is ignored.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Adds the path to the dropdown's list.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void Want(string key, string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            HashSet<string> set;
            if (!_wanted.TryGetValue(key, out set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _wanted[key] = set;
            }
            set.Add(path);
        }

        /// <summary>
        /// Path inside a pack of a path below Assets: packs mirror Assets/Images, so the leading
        /// "Images/" is dropped.
        /// </summary>
        /// <param name="path">Path below Assets, e.g. "Images/Trash/nacht_box_inv.png".</param>
        /// <returns>The path inside the pack, e.g. "Trash/nacht_box_inv.png".</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string PackPath(string path)
        {
            string p = path.Replace('\\', '/');
            return p.StartsWith("Images/", StringComparison.OrdinalIgnoreCase) ? p.Substring(7) : p;
        }

        /// <summary>
        /// Whether a pack has the file for a path below Assets.
        /// </summary>
        /// <param name="f">The family.</param>
        /// <param name="path">Path below Assets. Null or empty gives false.</param>
        /// <returns>True when the file exists in the pack.</returns>
        /// <remarks>
        /// Input: The parameters and the files on disk.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool InPack(Family f, string path)
        {
            if (f == null || string.IsNullOrEmpty(path)) return false;
            return File.Exists(Path.Combine(f.Root, PackPath(path).Replace('/', Path.DirectorySeparatorChar)));
        }

        /// <summary>
        /// Whether a dropdown has anything to change: true when at least one sprite of that kind
        /// was looked up for the section while loading. A set without chip (ANDRAS) has no chip
        /// dropdown, M.E.R.C. has no weapon dropdown.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <param name="kind">The kind of art.</param>
        /// <returns>True when the dropdown should be shown.</returns>
        /// <remarks>
        /// Input: The parameters and the paths remembered by Pick.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static bool HasEntry(Section section, Kind kind)
        {
            HashSet<string> set;
            return _wanted.TryGetValue(Key(section, kind), out set) && set.Count > 0;
        }

        /// <summary>
        /// The dropdown entries of one section and kind: "Default - Foxyla36" followed by every
        /// pack that has at least one of its files.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <param name="kind">The kind of art.</param>
        /// <returns>The entries, e.g. ["Default - Foxyla36", "Someone"]. Never
        /// empty.</returns>
        /// <remarks>
        /// Input: The parameters, the family list, the remembered paths and the files on disk.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static List<object> Options(Section section, Kind kind)
        {
            var list = new List<object> { DefaultName };
            HashSet<string> paths;
            if (!_wanted.TryGetValue(Key(section, kind), out paths)) return list;
            foreach (Family f in _families)
                foreach (string p in paths)
                    if (InPack(f, p)) { list.Add(f.Name); break; }
            return list;
        }

        /// <summary>
        /// The entry shown as selected in a dropdown: the chosen pack when it is one of the
        /// dropdown's entries, otherwise "Default - Foxyla36", which is also the art in use when
        /// the chosen pack has no file for this dropdown.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <param name="kind">The kind of art.</param>
        /// <returns>The chosen pack's entry, or "Default - Foxyla36".</returns>
        /// <remarks>
        /// Input: The parameters, the choices, the family list and the files on disk.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string ChosenName(Section section, Kind kind)
        {
            Family f = Chosen(Key(section, kind));
            return f != null && Options(section, kind).Contains(f.Name) ? f.Name : DefaultName;
        }

        /// <summary>
        /// Reads one config.json entry when it belongs to the icons panel.
        /// </summary>
        /// <param name="key">Config key, e.g. "icons_nacht_items" or
        /// "adaptive_inventory_size".</param>
        /// <param name="value">Value without quotes: for a dropdown the family id ("default" or
        /// an author folder name), for the adaptive size switch "true" or "false".</param>
        /// <returns>True when the key belongs to the icons panel and was read.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. Changes the choice of that dropdown or the adaptive size
        /// switch.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static bool TryLoad(string key, string value)
        {
            if (key == KeyAdaptiveSize)
            {
                AdaptiveSize = "true".Equals(value, StringComparison.OrdinalIgnoreCase);
                return true;
            }
            if (key == null || !key.StartsWith(KeyPrefix, StringComparison.Ordinal)) return false;
            _choice[key] = string.IsNullOrEmpty(value) ? DefaultId : value;
            return true;
        }

        /// <summary>
        /// Reads the choices saved in the icons panel. The panel gives the entry text of each
        /// dropdown, which is the family id itself (the pack's folder name). An entry from older
        /// versions, "Custom - {folder name}", is still accepted.
        /// </summary>
        /// <param name="config">The panel values keyed by config key, e.g.
        /// "icons_smasher_items": "Someone", "adaptive_inventory_size": true. Keys of
        /// other panels are ignored.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the choices and the adaptive size switch.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ReadConfig(Dictionary<string, object> config)
        {
            if (config == null) return;
            object adaptive;
            if (config.TryGetValue(KeyAdaptiveSize, out adaptive) && adaptive != null)
                AdaptiveSize = Convert.ToBoolean(adaptive);
            foreach (KeyValuePair<string, object> kv in config)
            {
                if (kv.Key == null || !kv.Key.StartsWith(KeyPrefix, StringComparison.Ordinal) || kv.Value == null)
                    continue;
                string v = kv.Value.ToString();
                if (v == DefaultName) v = DefaultId;
                else if (Find(v) == null && v.StartsWith(LegacyPrefix, StringComparison.Ordinal))
                    v = v.Substring(LegacyPrefix.Length);
                _choice[kv.Key] = v;
            }
        }

        /// <summary>
        /// The icons panel entries of config.json, added by SaveSettings before the closing
        /// brace: the adaptive size switch, then the dropdown choices different from the
        /// default.
        /// </summary>
        /// <returns>Text that continues the JSON object: a comma, a new line and
        /// "adaptive_inventory_size": true or false, then the same for each choice, e.g.
        /// "icons_smasher_items": "Someone".</returns>
        /// <remarks>
        /// Input: No parameters. Reads the switch and the choices.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string ConfigJsonTail()
        {
            var keys = new List<string>(_choice.Keys);
            keys.Sort(StringComparer.Ordinal);
            var sb = new StringBuilder();
            sb.Append(",\n  \"").Append(KeyAdaptiveSize).Append("\": ").Append(AdaptiveSize ? "true" : "false");
            foreach (string k in keys)
            {
                string v = _choice[k];
                if (string.IsNullOrEmpty(v) || v.Equals(DefaultId, StringComparison.OrdinalIgnoreCase)) continue;
                sb.Append(",\n  \"").Append(k).Append("\": \"")
                  .Append(v.Replace("\"", string.Empty).Replace("\\", string.Empty)).Append("\"");
            }
            return sb.ToString();
        }
    }
}
