using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using MGSC;
using SimpleJSON;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class SmasherWeapons
    {
        private static readonly HashSet<string> _loaded = new HashSet<string>();

        private static readonly Dictionary<string, float> _critBonus = new Dictionary<string, float>();

        private const string HarmonyId = "Augmentations_Expanded.WeaponCrit";

        public const string CritBonusKey = "ae_crit_chance_bonus";

        public const string SoundFromKey = "SoundFrom";

        private static readonly string[] SoundFields =
            { "_attackSoundBanks", "_reloadSoundBanks", "_dryShotSoundBanks", "_failedAttackSoundBanks" };

        private static bool _patched;

        /// <summary>
        /// Loads every json under Assets/Weapons: the weapons that the limbs put in the bare hands
        /// slot. Must run before the augmentations.
        /// </summary>
        /// <param name="modContentPath">Folder of the mod, the one that contains Assets. Null or
        /// empty loads nothing.</param>
        /// <returns>The number of weapons added; 0 when the path or the folder is
        /// missing.</returns>
        /// <remarks>
        /// Input: The parameter, the json files under Assets/Weapons, their icons under
        /// Assets/Images/Weapons and the vanilla weapons they copy.
        /// Output: The return value. Adds the weapons to the game items and writes lines in the
        /// log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static int Register(string modContentPath)
        {
            if (string.IsNullOrEmpty(modContentPath)) return 0;

            string assets = Path.Combine(modContentPath, "Assets");
            string dir = Path.Combine(assets, "Weapons");
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning("[AugmentationsExpanded] no Weapons folder: " + dir);
                return 0;
            }

            int added = 0;
            foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    if (LoadOne(file, assets)) added++;
                }
                catch (Exception e)
                {
                    Debug.LogError("[AugmentationsExpanded] weapon " + Path.GetFileName(file) + ": " + e);
                }
            }
            Debug.Log("[AugmentationsExpanded] weapons registered: " + added
                    + " (known in total: " + _loaded.Count + ")");
            return added;
        }

        /// <summary>
        /// Creates one weapon as a copy of the vanilla weapon named in CloneFrom, then overrides
        /// the values present in the json. A mod cannot build the visual and audio part of a
        /// weapon, so the copy keeps the vanilla one; "SoundFrom" takes the sounds of another
        /// vanilla weapon instead. Traits, Categories and Disassembly would stay
        /// shared with the vanilla weapon, so they are replaced with new lists. Weapons are
        /// implicit by default: they cannot be dropped, swapped or looted. The model is hidden
        /// when the json says "HideModel": true, and the icon is replaced when the weapon has its
        /// own image. For a grenade launcher, AllowedGrenadeIds replaces the list of grenades it
        /// can load. The game takes a weapon's crit chance only from its ammo, so a crit chance of
        /// the weapon itself goes in "ae_crit_chance_bonus" and is added by PatchCritBonus.
        /// </summary>
        /// <param name="file">Full path of a json with a Data object holding at least Id and
        /// CloneFrom, the id of a vanilla weapon, e.g. "tia_blade_1".</param>
        /// <param name="assetsRoot">Full path of the Assets folder.</param>
        /// <returns>True when a new weapon was added; false without Id or CloneFrom, for an id
        /// already there, or when CloneFrom is missing or not a weapon.</returns>
        /// <remarks>
        /// Input: The parameters, the json file, the optional icon and the vanilla weapon it
        /// copies.
        /// Output: The return value. Adds the weapon to the game items and writes a line in the
        /// log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool LoadOne(string file, string assetsRoot)
        {
            JSONNode d = JSON.Parse(File.ReadAllText(file))["Data"];
            string id = d["Id"].Value;
            if (string.IsNullOrEmpty(id)) return false;
            if (_loaded.Contains(id)) return false;

            if (Data.Items.GetRecord(id) != null)
            {
                _loaded.Add(id);
                Debug.Log("[AugmentationsExpanded] weapon " + id + " already present, left as it is.");
                return false;
            }

            string source = d["CloneFrom"].Value;
            if (string.IsNullOrEmpty(source))
            {
                Debug.LogError("[AugmentationsExpanded] " + id + " has no CloneFrom, skipped.");
                return false;
            }
            if (Data.Items.GetRecord(source) == null)
            {
                Debug.LogError("[AugmentationsExpanded] " + id + ": source weapon '" + source + "' not found.");
                return false;
            }
            WeaponRecord src = Data.Items.GetSimpleRecord<WeaponRecord>(source);
            if (src == null)
            {
                Debug.LogError("[AugmentationsExpanded] " + id + ": '" + source + "' is not a weapon.");
                return false;
            }

            WeaponRecord rec = src.Clone(id);

            rec.Categories  = SmasherJson.Strings(d["Categories"]);
            rec.Traits      = SmasherJson.Strings(d["Traits"]);
            rec.Disassembly = new List<ItemQuantity>();

            rec.IsImplicit = d["IsImplicit"] == null || d["IsImplicit"].IsNull
                             ? true
                             : d["IsImplicit"].AsBool;

            if (!d["WeaponClass"].IsNull && !string.IsNullOrEmpty(d["WeaponClass"].Value))
            {
                WeaponClass wc;
                if (SmasherJson.TryEnum(d["WeaponClass"].Value, out wc)) rec.WeaponClass = wc;
                else Debug.LogWarning("[AugmentationsExpanded] " + id + ": unknown WeaponClass '"
                                    + d["WeaponClass"].Value + "', kept " + rec.WeaponClass);
            }

            if (!string.IsNullOrEmpty(d["RequiredAmmo"].Value)) rec.RequiredAmmo  = d["RequiredAmmo"].Value;
            if (!string.IsNullOrEmpty(d["DefaultAmmoId"].Value)) rec.DefaultAmmoId = d["DefaultAmmoId"].Value;

            if (d["OverrideAmmo"].IsArray) rec.OverrideAmmo = SmasherJson.Strings(d["OverrideAmmo"]);

            if (d["AllowedGrenadeIds"].IsArray)
            {
                rec.AllowedGrenadeIds = SmasherJson.Strings(d["AllowedGrenadeIds"]);
                foreach (string g in rec.AllowedGrenadeIds)
                    if (Data.Items.GetRecord(g) == null)
                        Debug.LogWarning("[AugmentationsExpanded] " + id + ": grenade '" + g + "' does not exist.");
            }

            if (d["Firemodes"].IsArray)
            {
                List<string> modes = SmasherJson.Strings(d["Firemodes"]);
                foreach (string m in modes)
                    if (Data.Firemodes.GetRecord(m) == null)
                        Debug.LogWarning("[AugmentationsExpanded] " + id + ": firemode '" + m + "' does not exist.");
                if (modes.Count > 0) rec.Firemodes = modes;
            }

            SetInt  (d, "MagazineCapacity",   v => rec.MagazineCapacity   = v);
            SetInt  (d, "MinRandomAmmoCount", v => rec.MinRandomAmmoCount = v);
            SetInt  (d, "ReloadDuration",     v => rec.ReloadDuration     = v);
            SetInt  (d, "Range",              v => rec.Range              = v);
            SetInt  (d, "TechLevel",          v => rec.TechLevel          = v);
            SetInt  (d, "InventoryWidthSize", v => rec.InventoryWidthSize = v);
            SetFloat(d, "Falloff",            v => rec.Falloff            = v);
            SetFloat(d, "BonusAccuracy",      v => rec.BonusAccuracy      = v);
            SetFloat(d, "BonusScatterAngle",  v => rec.BonusScatterAngle  = v);
            SetFloat(d, "Price",              v => rec.Price              = v);
            SetFloat(d, "Weight",             v => rec.Weight             = v);

            SetInt  (d, "MaxDurability",      v => rec.MaxDurability      = v);
            SetBool (d, "Unbreakable",        v => rec.Unbreakable        = v);

            JSONNode dmg = d["Damage"];
            if (dmg != null && dmg.IsObject)
            {
                DmgInfo info = rec.Damage;
                if (!string.IsNullOrEmpty(dmg["damage"].Value)) info.damage = dmg["damage"].Value;
                if (!dmg["minDmg"].IsNull)     info.minDmg     = dmg["minDmg"].AsInt;
                if (!dmg["maxDmg"].IsNull)     info.maxDmg     = dmg["maxDmg"].AsInt;
                if (!dmg["critChance"].IsNull) info.critChance = dmg["critChance"].AsFloat;
                if (!dmg["critDmg"].IsNull)    info.critDmg    = dmg["critDmg"].AsFloat;
                rec.Damage = info;
            }

            if (d["HideModel"].AsBool) HideModel(rec, src);
            SetIcon(rec, src, assetsRoot);
            if (!string.IsNullOrEmpty(d[SoundFromKey].Value)) SetSounds(rec, src, d[SoundFromKey].Value);

            if (d[CritBonusKey] != null && !d[CritBonusKey].IsNull && d[CritBonusKey].AsFloat != 0f)
                _critBonus[id] = d[CritBonusKey].AsFloat;

            Data.Items.AddRecord(id, rec);
            _loaded.Add(id);
            Debug.Log("[AugmentationsExpanded] + weapon " + id + " (from " + source + ")"
                    + "  " + rec.WeaponClass + "  ammo=" + rec.RequiredAmmo
                    + "  mag=" + rec.MagazineCapacity
                    + "  modes=[" + string.Join(" ", (rec.Firemodes ?? new List<string>()).ToArray()) + "]"
                    + "  implicit=" + rec.IsImplicit);
            return true;
        }

        /// <summary>
        /// Patches the game so the weapons with "ae_crit_chance_bonus" in their json get that
        /// value added to their crit chance. WeaponComponent.Damage replaces the weapon's crit
        /// chance with the one of the ammo it fires; it is the value used both when shooting and
        /// in the tooltip, so the bonus is added there. Does nothing when no weapon has a bonus.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the bonuses collected by Register.
        /// Output: Nothing is returned. Patches WeaponComponent.Damage once and writes a line in
        /// the log; a failure is logged and the weapons keep the crit chance of their ammo.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void PatchCritBonus()
        {
            if (_patched || _critBonus.Count == 0) return;
            _patched = true;

            MethodBase getter = AccessTools.PropertyGetter(typeof(WeaponComponent), nameof(WeaponComponent.Damage));
            if (getter == null)
            {
                Debug.LogError("[AugmentationsExpanded] weapon crit bonus: WeaponComponent.Damage not found, "
                             + "the weapons keep the crit chance of their ammo.");
                return;
            }
            try
            {
                new Harmony(HarmonyId).Patch(getter, postfix: new HarmonyMethod(
                    AccessTools.Method(typeof(SmasherWeapons), nameof(DamagePostfix))));
                Debug.Log("[AugmentationsExpanded] weapon crit bonus: patch applied for "
                        + string.Join(", ", new List<string>(_critBonus.Keys).ToArray()) + ".");
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] weapon crit bonus: patching failed, "
                             + "the weapons keep the crit chance of their ammo: " + e);
            }
        }

        /// <summary>
        /// Adds the weapon's crit bonus to the damage WeaponComponent.Damage returns.
        /// </summary>
        /// <param name="__instance">The weapon being read.</param>
        /// <param name="__result">The damage computed by the game, with the ammo's crit
        /// chance.</param>
        /// <remarks>
        /// Input: The parameters and the bonuses collected by Register.
        /// Output: Nothing is returned. Raises the crit chance of __result for weapons with a
        /// bonus.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void DamagePostfix(WeaponComponent __instance, ref DmgInfo __result)
        {
            WeaponRecord rec = __instance == null ? null : __instance.WeaponRecord;
            float bonus;
            if (rec != null && _critBonus.TryGetValue(rec.Id, out bonus))
                __result.critChance += bonus;
        }

        /// <summary>
        /// Removes the 3D model and texture from the weapon's visual data and sets the bare hands
        /// grip, so the character is not drawn holding the copied weapon, as with the vanilla
        /// weapon arms. Icon, sounds and muzzle flash stay. The vanilla weapon keeps its model.
        /// </summary>
        /// <param name="rec">The weapon record just copied.</param>
        /// <param name="src">The vanilla weapon record it was copied from.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the weapon's own copy of the visual data; logs a
        /// warning and changes nothing when the record has no weapon visual data.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void HideModel(WeaponRecord rec, WeaponRecord src)
        {
            WeaponDescriptor own = OwnDescriptor(rec, src);
            if (own == null)
            {
                Debug.LogWarning("[AugmentationsExpanded] " + rec.Id + ": no weapon descriptor, model left as it is.");
                return;
            }
            SmasherSprites.SetField(own, "_prefab", null);
            SmasherSprites.SetField(own, "_texture", null);
            SmasherSprites.SetField(own, "_grip", HandsGrip.BareHands);
        }

        /// <summary>
        /// Replaces the weapon's icon with Images/Weapons/{id}_inv.png when that file exists, taken
        /// from the icon pack chosen for the weapon of its set or item when the pack has it. The
        /// game shows this icon both in the bare hands slot of the inventory and in the current
        /// weapon panel at the top left of the screen. Without the file the icon of the vanilla
        /// weapon stays. Must run after the wound slots are registered, which tell the set of a
        /// fist.
        /// </summary>
        /// <param name="rec">The weapon record just copied.</param>
        /// <param name="src">The vanilla weapon record it was copied from.</param>
        /// <param name="assetsRoot">Full path of the Assets folder.</param>
        /// <remarks>
        /// Input: The parameters, the chosen icon pack and the image file, loaded with 100 pixels
        /// per unit and pivot (0.5, 0) like the vanilla weapon icons.
        /// Output: Nothing is returned. Sets the icon of the weapon's own copy of the visual data;
        /// logs a warning when the image cannot be used.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void SetIcon(WeaponRecord rec, WeaponRecord src, string assetsRoot)
        {
            string root, path;
            string wanted = "Images/Weapons/" + rec.Id + "_inv.png";
            SmasherIcons.Pick(assetsRoot, SmasherIcons.ForWeapon(rec.Id), SmasherIcons.Kind.Weapon,
                              wanted, wanted, out root, out path);
            if (!File.Exists(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)))) return;

            Sprite icon = SmasherSprites.Load(root, path, 100f, 0.5f, 0f);
            WeaponDescriptor own = OwnDescriptor(rec, src);
            if (icon == null || own == null)
            {
                Debug.LogWarning("[AugmentationsExpanded] " + rec.Id + ": icon " + path + " not applied.");
                return;
            }
            SmasherSprites.SetField(own, "_icon", icon);
        }

        /// <summary>
        /// Gives the weapon the sounds of another vanilla weapon (attack, reload, miss and failed
        /// attack), for a weapon copied from one whose sounds do not fit, e.g. the pile bunker
        /// copied from a blade, which would play the knife sound and nothing on a miss.
        /// </summary>
        /// <param name="rec">The weapon record just copied.</param>
        /// <param name="src">The vanilla weapon record it was copied from.</param>
        /// <param name="soundFrom">Id of the vanilla weapon whose sounds are used, the json's
        /// "SoundFrom", e.g. "civ_powerfist_1".</param>
        /// <remarks>
        /// Input: The parameters and the visual data of the sound source.
        /// Output: Nothing is returned. Sets the sound lists of the weapon's own copy of the visual
        /// data; logs a warning and changes nothing when the source is missing or has no weapon
        /// visual data.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void SetSounds(WeaponRecord rec, WeaponRecord src, string soundFrom)
        {
            WeaponRecord from = Data.Items.GetRecord(soundFrom) == null
                                ? null : Data.Items.GetSimpleRecord<WeaponRecord>(soundFrom);
            var fromDesc = from == null ? null : from.ContentDescriptor as WeaponDescriptor;
            WeaponDescriptor own = fromDesc == null ? null : OwnDescriptor(rec, src);
            if (own == null)
            {
                Debug.LogWarning("[AugmentationsExpanded] " + rec.Id + ": sounds of '" + soundFrom + "' not applied.");
                return;
            }
            foreach (string field in SoundFields)
            {
                FieldInfo f = typeof(WeaponDescriptor).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
                if (f != null) f.SetValue(own, f.GetValue(fromDesc));
                else Debug.LogError("[AugmentationsExpanded] field " + field + " not found on WeaponDescriptor");
            }
        }

        /// <summary>
        /// The weapon's own copy of its visual data. The copy made by WeaponRecord.Clone still
        /// shares the vanilla weapon's visual data, so it is duplicated the first time it has to
        /// change, and every later change goes to the same duplicate.
        /// </summary>
        /// <param name="rec">The weapon record just copied.</param>
        /// <param name="src">The vanilla weapon record it was copied from.</param>
        /// <returns>The weapon's own visual data, or null when the record has no weapon visual
        /// data.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. The first call gives the record a duplicate of the visual
        /// data, kept between scenes.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static WeaponDescriptor OwnDescriptor(WeaponRecord rec, WeaponRecord src)
        {
            var current = rec.ContentDescriptor as WeaponDescriptor;
            if (current == null || current != src.ContentDescriptor) return current;

            WeaponDescriptor copy = UnityEngine.Object.Instantiate(current);
            copy.name = rec.Id + "_descriptor";
            UnityEngine.Object.DontDestroyOnLoad(copy);
            rec.ContentDescriptor = copy;
            return copy;
        }

        /// <summary>
        /// Passes the integer value of a key to apply, only when the key is present and not null,
        /// so the copied value is kept otherwise.
        /// </summary>
        /// <param name="d">The Data object of the weapon json.</param>
        /// <param name="key">The key, e.g. "MagazineCapacity".</param>
        /// <param name="apply">Receives the value, usually by setting a field of the
        /// record.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Calls apply once, or not at all.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void SetInt(JSONNode d, string key, Action<int> apply)
        {
            if (d[key] != null && !d[key].IsNull) apply(d[key].AsInt);
        }

        /// <summary>
        /// Passes the decimal value of a key to apply, only when the key is present and not null,
        /// so the copied value is kept otherwise.
        /// </summary>
        /// <param name="d">The Data object of the weapon json.</param>
        /// <param name="key">The key, e.g. "BonusAccuracy".</param>
        /// <param name="apply">Receives the value, usually by setting a field of the
        /// record.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Calls apply once, or not at all.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void SetFloat(JSONNode d, string key, Action<float> apply)
        {
            if (d[key] != null && !d[key].IsNull) apply(d[key].AsFloat);
        }

        /// <summary>
        /// Passes the true/false value of a key to apply, only when the key is present and not
        /// null, so the copied value is kept otherwise.
        /// </summary>
        /// <param name="d">The Data object of the weapon json.</param>
        /// <param name="key">The key, e.g. "Unbreakable".</param>
        /// <param name="apply">Receives the value, usually by setting a field of the
        /// record.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Calls apply once, or not at all.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void SetBool(JSONNode d, string key, Action<bool> apply)
        {
            if (d[key] != null && !d[key].IsNull) apply(d[key].AsBool);
        }
    }
}
