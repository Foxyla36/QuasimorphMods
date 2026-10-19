using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace AugmentationsExpanded.VisibleAugments;

// VA stores clothing visibility on both groups (including skin masks) and individual pieces.
// Keep its original values so enabling clipping restores VA's own exceptions as well.
internal sealed class VisibleAugmentClipping
{
    [Flags]
    internal enum Clothing { None = 0, Armor = 1, Leggings = 2, Boots = 4 }

    private readonly Dictionary<object, Clothing> _original = new Dictionary<object, Clothing>();
    private static readonly string[] Fields = { "HideIfArmorEquipped", "HideIfLeggingsEquipped", "HideIfBootsEquipped" };

    internal static bool IsHead(string bone) => bone == "Head" || bone == "Neck";

    internal static Clothing Coverage(string bone)
    {
        switch (bone)
        {
            case "Body": case "LShoulder": case "RShoulder": case "LArm": case "RArm":
                return Clothing.Armor;
            case "Hip": case "LThigh": case "RThigh": case "LLeg": case "RLeg":
                return Clothing.Leggings;
            case "LFeet": case "RFeet":
                return Clothing.Boots;
            default:
                return Clothing.None;
        }
    }

    internal void Apply(IEnumerable groups, Func<string, string> pieceBone, bool allowClipping)
    {
        var current = new HashSet<object>();
        foreach (object group in groups)
        {
            Clothing coverage = Clothing.None;
            bool hasHead = false;
            foreach (object prefab in (IEnumerable)Field(group, "Prefabs").GetValue(group))
            {
                string bone = pieceBone((string)Field(prefab, "Name").GetValue(prefab));
                hasHead |= IsHead(bone);
                Clothing partCoverage = Coverage(bone);
                coverage |= partCoverage;
                Set(prefab, partCoverage, allowClipping, current);
            }

            // Boots cover only the feet of a full leg. Keep its bare upper pieces visible.
            // A dedicated feet group, however, must restore the human skin when boots hide it.
            if ((coverage & Clothing.Leggings) != 0) coverage &= ~Clothing.Boots;
            Set(group, hasHead ? Clothing.None : coverage, allowClipping, current);
        }
        var stale = new List<object>();
        foreach (object item in _original.Keys) if (!current.Contains(item)) stale.Add(item);
        foreach (object item in stale) _original.Remove(item);
    }

    private void Set(object target, Clothing coverage, bool allowClipping, HashSet<object> current)
    {
        current.Add(target);
        if (!_original.TryGetValue(target, out Clothing original))
        {
            original = Clothing.None;
            for (int i = 0; i < Fields.Length; i++)
                if ((bool)Field(target, Fields[i]).GetValue(target)) original |= (Clothing)(1 << i);
            _original.Add(target, original);
        }
        Write(target, allowClipping ? original : original | coverage);
    }

    internal void Restore()
    {
        foreach (var item in _original) Write(item.Key, item.Value);
        _original.Clear();
    }

    private static void Write(object target, Clothing value)
    {
        for (int i = 0; i < Fields.Length; i++) Field(target, Fields[i]).SetValue(target, (value & (Clothing)(1 << i)) != 0);
    }

    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name)
        ?? throw new MissingFieldException(target.GetType().FullName, name);
}
