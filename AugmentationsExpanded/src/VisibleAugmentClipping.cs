using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace AugmentationsExpanded.VisibleAugments;

/// <summary>
/// The clothing rule of the Visible Augments pieces, the mod's and Visible Augments' own: a piece
/// keeps the rules Visible Augments gave it (Original), or is hidden when the armor, leggings or
/// boots that cover its bone are worn, so the 3D augment does not poke through the clothes
/// (HiddenByClothing), or is never hidden by clothing (AlwaysShown, used when the pieces hide the
/// worn armor instead). Visible Augments keeps these rules (HideIfArmorEquipped,
/// HideIfLeggingsEquipped, HideIfBootsEquipped) both on its groups, where they also drive the skin
/// masks, and on the single pieces, so both are changed. The values found the first time are kept,
/// so going back to Original restores Visible Augments' own exceptions as well.
/// </summary>
/// <remarks>
/// AI-generated documentation, written to help external users understand the mod.
/// </remarks>
internal sealed class VisibleAugmentClipping
{
    [Flags]
    internal enum Clothing { None = 0, Armor = 1, Leggings = 2, Boots = 4 }

    internal enum Policy { Original, HiddenByClothing, AlwaysShown }

    private readonly Dictionary<object, Clothing> _original = new Dictionary<object, Clothing>();
    private static readonly string[] Fields = { "HideIfArmorEquipped", "HideIfLeggingsEquipped", "HideIfBootsEquipped" };

    /// <summary>
    /// Whether a bone belongs to the head, which no clothing covers.
    /// </summary>
    /// <param name="bone">Bone id, e.g. "Head" or "LArm".</param>
    /// <returns>True for "Head" and "Neck".</returns>
    /// <remarks>
    /// Input: The parameter only.
    /// Output: Only the return value.
    /// AI-generated documentation, written to help external users understand the mod.
    /// </remarks>
    internal static bool IsHead(string bone) => bone == "Head" || bone == "Neck";

    /// <summary>
    /// The clothing that covers a bone: the armor covers body, shoulders and arms, the leggings
    /// hip, thighs and legs, the boots the feet.
    /// </summary>
    /// <param name="bone">Bone id, e.g. "LThigh".</param>
    /// <returns>The covering clothing, or None for a bone no clothing covers (head, hands).</returns>
    /// <remarks>
    /// Input: The parameter only.
    /// Output: Only the return value.
    /// AI-generated documentation, written to help external users understand the mod.
    /// </remarks>
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

    /// <summary>
    /// Writes the clothing rules of every group and piece. Each piece is hidden by the clothing
    /// of its own bone, each group by the clothing of all its pieces, except that a group with a
    /// head piece is never hidden and a group that also has leggings pieces is not hidden by the
    /// boots: boots cover only the feet of a full leg, so its bare upper pieces stay visible,
    /// while a group of feet alone must still give back the human skin when boots hide it.
    /// Groups and pieces no longer given are forgotten.
    /// </summary>
    /// <param name="groups">The Visible Augments groups of the mod's augments; each has a
    /// "Prefabs" list of pieces with a "Name".</param>
    /// <param name="pieceBone">Gives the bone id of a piece from its name.</param>
    /// <param name="policy">Original to keep Visible Augments' own rules, HiddenByClothing to
    /// hide the pieces under the clothing as well, AlwaysShown to never hide them.</param>
    /// <remarks>
    /// Input: The parameters and the rules found the first time.
    /// Output: Nothing is returned. Sets the Hide fields of the groups and pieces and remembers
    /// their original values.
    /// AI-generated documentation, written to help external users understand the mod.
    /// </remarks>
    internal void Apply(IEnumerable groups, Func<string, string> pieceBone, Policy policy)
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
                Set(prefab, partCoverage, policy, current);
            }

            if ((coverage & Clothing.Leggings) != 0) coverage &= ~Clothing.Boots;
            Set(group, hasHead ? Clothing.None : coverage, policy, current);
        }
        var stale = new List<object>();
        foreach (object item in _original.Keys) if (!current.Contains(item)) stale.Add(item);
        foreach (object item in stale) _original.Remove(item);
    }

    /// <summary>
    /// Writes the rule of one group or piece: its original rule, plus the given clothing with
    /// HiddenByClothing, or no clothing at all with AlwaysShown. The original rule is read and
    /// kept the first time the object is seen.
    /// </summary>
    /// <param name="target">A Visible Augments group or piece.</param>
    /// <param name="coverage">The clothing that should hide it with HiddenByClothing.</param>
    /// <param name="policy">Which rule to write.</param>
    /// <param name="current">The objects written by this Apply; target is added to it.</param>
    /// <remarks>
    /// Input: The parameters and the kept original rules.
    /// Output: Nothing is returned. Sets the Hide fields of target and may remember its
    /// original rule.
    /// AI-generated documentation, written to help external users understand the mod.
    /// </remarks>
    private void Set(object target, Clothing coverage, Policy policy, HashSet<object> current)
    {
        current.Add(target);
        if (!_original.TryGetValue(target, out Clothing original))
        {
            original = Clothing.None;
            for (int i = 0; i < Fields.Length; i++)
                if ((bool)Field(target, Fields[i]).GetValue(target)) original |= (Clothing)(1 << i);
            _original.Add(target, original);
        }
        Write(target, policy == Policy.HiddenByClothing ? original | coverage
                    : policy == Policy.AlwaysShown ? Clothing.None
                    : original);
    }

    /// <summary>
    /// Gives every group and piece back the rule Visible Augments had given it and forgets them.
    /// </summary>
    /// <remarks>
    /// Input: No parameters. Reads the kept original rules.
    /// Output: Nothing is returned. Sets the Hide fields and empties the kept rules.
    /// AI-generated documentation, written to help external users understand the mod.
    /// </remarks>
    internal void Restore()
    {
        foreach (var item in _original) Write(item.Key, item.Value);
        _original.Clear();
    }

    /// <summary>
    /// Writes the three Hide fields of a group or piece.
    /// </summary>
    /// <param name="target">A Visible Augments group or piece.</param>
    /// <param name="value">The clothing that hides it.</param>
    /// <remarks>
    /// Input: The parameters only.
    /// Output: Nothing is returned. Sets HideIfArmorEquipped, HideIfLeggingsEquipped and
    /// HideIfBootsEquipped of target.
    /// AI-generated documentation, written to help external users understand the mod.
    /// </remarks>
    private static void Write(object target, Clothing value)
    {
        for (int i = 0; i < Fields.Length; i++) Field(target, Fields[i]).SetValue(target, (value & (Clothing)(1 << i)) != 0);
    }

    /// <summary>
    /// A public field of a Visible Augments object, read by reflection since that mod is not
    /// referenced at build time.
    /// </summary>
    /// <param name="target">The object.</param>
    /// <param name="name">Field name, e.g. "Prefabs".</param>
    /// <returns>The field.</returns>
    /// <remarks>
    /// Input: The parameters only.
    /// Output: The return value; throws MissingFieldException when the field does not exist.
    /// AI-generated documentation, written to help external users understand the mod.
    /// </remarks>
    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name)
        ?? throw new MissingFieldException(target.GetType().FullName, name);
}

/// <summary>
/// The choices of the "VA armor mode" dropdown of the Mod Config Menu, how the Visible Augments
/// pieces meet the armor, leggings and boots worn: Clip keeps Visible Augments' own rules, so both
/// are drawn and can clip; Hide armor always shows the pieces and hides the worn part on the same
/// bone; Hidden by armor hides the pieces under the clothing that covers them; Off removes every
/// piece of Augmentations Expanded from Visible Augments. The texts are both the saved values and
/// the dropdown entries.
/// </summary>
/// <remarks>
/// AI-generated documentation, written to help external users understand the mod.
/// </remarks>
internal static class VisibleAugmentMode
{
    internal const string Clip          = "Clip";
    internal const string HideArmor     = "Hide armor";
    internal const string HiddenByArmor = "Hidden by armor";
    internal const string Off           = "Off";

    internal static readonly string[] All = { Clip, HideArmor, HiddenByArmor, Off };

    /// <summary>
    /// Checks a mode read from the settings.
    /// </summary>
    /// <param name="value">The saved text, e.g. "Hide armor". Case is ignored; null or anything
    /// else is accepted.</param>
    /// <returns>The mode as written in All, or Clip, the default, for an unknown value.</returns>
    /// <remarks>
    /// Input: The parameter only.
    /// Output: Only the return value.
    /// AI-generated documentation, written to help external users understand the mod.
    /// </remarks>
    internal static string Valid(string value)
    {
        foreach (string mode in All)
            if (string.Equals(mode, value == null ? null : value.Trim(), StringComparison.OrdinalIgnoreCase)) return mode;
        return Clip;
    }
}
