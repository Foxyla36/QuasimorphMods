using System;
using System.Collections.Generic;
using System.IO;
using MGSC;
using SimpleJSON;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class SmasherRecords
    {
        private static readonly HashSet<string> _slots = new HashSet<string>();

        private static readonly Dictionary<string, string> _sets = new Dictionary<string, string>();

        /// <summary>
        /// The wound slots of this mod registered by RegisterWoundSlots. Used to find the set of
        /// a fist from the wound slot that uses it as bare hands weapon.
        /// </summary>
        /// <remarks>
        /// Input: The ids remembered while registering.
        /// Output: The ids, e.g. "NachtArm". Empty before the wound slots are registered.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static IEnumerable<string> Ids { get { return _slots; } }

        /// <summary>
        /// Set of a wound slot: the folder under Assets/WoundSlots its json was loaded from.
        /// </summary>
        /// <param name="slotId">Wound slot id, e.g. "NachtArm".</param>
        /// <returns>The set name, e.g. "Nachtreiher", or null for a slot this mod did not
        /// load.</returns>
        /// <remarks>
        /// Input: The parameter and the folders recorded while loading.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static string SetOf(string slotId)
        {
            string set;
            return slotId != null && _sets.TryGetValue(slotId, out set) ? set : null;
        }

        /// <summary>
        /// Loads every json under Assets/WoundSlots as a wound slot record. The game only builds
        /// wound slots from its own tables, so the mod creates them itself. Must run before
        /// anything else, because creating an augmentation item reads its wound slots. Ids that
        /// already exist are kept.
        /// </summary>
        /// <param name="modContentPath">Folder of the mod, the one that contains Assets. Null or
        /// empty loads nothing.</param>
        /// <returns>The number of wound slots added; 0 when the folder is missing.</returns>
        /// <remarks>
        /// Input: The parameter and the json files under Assets/WoundSlots.
        /// Output: The return value. Adds the records to the game's wound slots and remembers their
        /// ids and the set folder of each.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static int RegisterWoundSlots(string modContentPath)
        {
            string dir = Sub(modContentPath, "WoundSlots");
            if (dir == null) return 0;

            int added = 0;
            foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    JSONNode d = JSON.Parse(File.ReadAllText(file))["Data"];
                    string id = d["Id"].Value;
                    if (string.IsNullOrEmpty(id) || _slots.Contains(id)) continue;
                    _sets[id] = SmasherAugmentations.SetOf(dir, file);
                    if (Data.WoundSlots.GetRecord(id) != null) { _slots.Add(id); continue; }

                    var r = new WoundSlotRecord
                    {
                        SlotType               = d["SlotType"].Value,
                        NatureType             = d["NatureType"].Value,
                        SlotWeight             = d["SlotWeight"].AsFloat,
                        CoreEffects            = Floats(d["CoreEffects"]),
                        AmputationCoreEffects  = Floats(d["AmputationCoreEffects"]),
                        ImplicitBonusEffects   = Floats(d["ImplicitBonusEffects"]),
                        ImplicitPenaltyEffects = Floats(d["ImplicitPenaltyEffects"]),
                        IgnoreEffects          = SmasherJson.Strings(d["IgnoreEffects"]),
                        IgnoreStatusEffects    = SmasherJson.Strings(d["IgnoreStatusEffects"]),
                        AmputatedDrop          = Drops(d["AmputatedDrop"]),
                        AdditionalAmputation   = SmasherJson.Strings(d["AdditionalAmputation"]),
                        IsGroundAffected       = d["IsGroundAffected"].AsBool,
                        BareHandWeapon         = d["BareHandWeapon"].Value,
                        ImplantSocketsDefault  = d["ImplantSocketsDefault"].AsInt,
                        ImplantSocketsMin      = d["ImplantSocketsMin"].AsInt,
                        ImplantSocketsMax      = d["ImplantSocketsMax"].AsInt
                    };
                    SmasherSprites.SetProperty(r, "Id", id);

                    Data.WoundSlots.AddRecord(id, r);
                    _slots.Add(id);
                    added++;
                }
                catch (Exception e)
                {
                    Debug.LogError("[AugmentationsExpanded] wound slot " + Path.GetFileName(file) + ": " + e);
                }
            }
            Debug.Log("[AugmentationsExpanded] wound slots registered: " + added + "/" + _slots.Count);
            return added;
        }

        /// <summary>
        /// Path of a subfolder of Assets.
        /// </summary>
        /// <param name="modContentPath">Folder of the mod. Null or empty gives null.</param>
        /// <param name="name">Name of the subfolder, e.g. "WoundSlots".</param>
        /// <returns>The full path, or null when it does not exist.</returns>
        /// <remarks>
        /// Input: The parameters and the folders on disk.
        /// Output: The return value, plus a warning in the log when missing.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string Sub(string modContentPath, string name)
        {
            if (string.IsNullOrEmpty(modContentPath)) return null;
            string dir = Path.Combine(Path.Combine(modContentPath, "Assets"), name);
            if (Directory.Exists(dir)) return dir;
            Debug.LogWarning("[AugmentationsExpanded] folder missing: " + dir);
            return null;
        }

        /// <summary>
        /// Reads a JSON object of effect id and value pairs.
        /// </summary>
        /// <param name="n">A JSON object node, e.g. {"max_health": 20, "dodge_reduce": -0.1}. Null
        /// or not an object is accepted.</param>
        /// <returns>The effects. Never null; empty when the node is missing or not an
        /// object.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static Dictionary<string, float> Floats(JSONNode n)
        {
            var d = new Dictionary<string, float>();
            if (n == null || !n.IsObject) return d;
            foreach (string k in n.Keys) d[k] = n[k].AsFloat;
            return d;
        }

        /// <summary>
        /// Reads the AmputatedDrop list of a wound slot. When the limb is amputated the game picks
        /// one entry by weight.
        /// </summary>
        /// <param name="n">A JSON array node of {"Item1": weight, "Item2": item id}, e.g.
        /// [{"Item1": 2, "Item2": "smasher_hand"}]. Null or not an array is accepted; entries
        /// without Item2 are skipped.</param>
        /// <returns>The entries as (weight, item id). Never null; empty when the node is
        /// missing.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static List<Tuple<float, string>> Drops(JSONNode n)
        {
            var l = new List<Tuple<float, string>>();
            if (n == null || !n.IsArray) return l;
            for (int i = 0; i < n.Count; i++)
            {
                string id = n[i]["Item2"].Value;
                if (string.IsNullOrEmpty(id)) continue;
                l.Add(new Tuple<float, string>(n[i]["Item1"].AsFloat, id));
            }
            return l;
        }
    }
}
