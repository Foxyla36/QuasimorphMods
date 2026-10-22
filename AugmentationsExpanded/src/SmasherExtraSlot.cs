using System;
using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class SmasherExtraSlot
    {
        private const string GunArmSlotId    = "AE_DummyVanillaGunArm";
        private const string GunArmWeaponId  = "ae_nature_dummy_vanilla_gun_arm";
        private const string MiniArmSlotId   = "AE_DummySpecialGunArm";
        private const string MiniArmWeaponId = "ae_nature_dummy_special_gun_arm";

        private const string MiniArmAugId = "ae_dummy_special_gun_arm";

        private static bool _complained;

        private static ItemStorage _cargo;
        private static ItemsOnFloor _floor;
        private static MapGrid _grid;

        /// <summary>
        /// Runs every frame on the ship and checks every mercenary. The weapon limbs are checked
        /// continuously instead of patching the game because a limb can appear or disappear in many
        /// places (installation, amputation, quasimorphosis, cloning). No Harmony patch is used, so
        /// nothing can conflict with other mods. On the ship an item that has to leave a weapon
        /// slot and fits neither in the backpack nor in the vest goes to the ship cargo, where the
        /// game also puts removed augmentations and implants.
        /// </summary>
        /// <param name="context">The context given by the game. Its State must hold the
        /// mercenaries; null does nothing.</param>
        /// <remarks>
        /// Input: The context, the ship cargo and the gun arm settings.
        /// Output: Nothing is returned. Updates the mercenaries through Reconcile; the first error
        /// is logged.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        [Hook(ModHookType.SpaceUpdateBeforeGameLoop)]
        public static void SpaceTick(IModContext context)
        {
            if (context == null || context.State == null) return;
            try
            {
                Mercenaries mercs = context.State.Get<Mercenaries>();
                if (mercs == null || mercs.Values == null) return;
                _floor = null;
                _grid = null;
                _cargo = ShipCargo(context);
                for (int i = 0; i < mercs.Values.Count; i++)
                {
                    Mercenary m = mercs.Values[i];
                    if (m != null) Reconcile(m.CreatureData);
                }
            }
            catch (Exception e) { Complain(e); }
        }

        /// <summary>
        /// Runs every frame during a mission and checks the player's creature, like SpaceTick.
        /// In a mission an item that has to leave a weapon slot and fits neither in the backpack
        /// nor in the vest is dropped at the character's feet, as the game does with the primary
        /// weapon when an arm is lost.
        /// </summary>
        /// <param name="context">The context given by the game. Its State must hold the creatures;
        /// null does nothing.</param>
        /// <remarks>
        /// Input: The context, the map and the items on the floor, and the gun arm settings.
        /// Output: Nothing is returned. Updates the player's creature through Reconcile; the first
        /// error is logged.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        [Hook(ModHookType.DungeonUpdateBeforeGameLoop)]
        public static void DungeonTick(IModContext context)
        {
            if (context == null || context.State == null) return;
            try
            {
                Creatures creatures = context.State.Get<Creatures>();
                if (creatures == null || creatures.Player == null) return;
                _cargo = null;
                _floor = context.State.Get<ItemsOnFloor>();
                _grid = context.State.Get<MapGrid>();
                Reconcile(creatures.Player.CreatureData);
            }
            catch (Exception e) { Complain(e); }
        }

        /// <summary>
        /// The first storage of the ship cargo, the one the game uses for removed augmentations.
        /// </summary>
        /// <param name="context">The context given by the game, on the ship.</param>
        /// <returns>The cargo storage, or null when the state has no cargo.</returns>
        /// <remarks>
        /// Input: The parameter only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static ItemStorage ShipCargo(IModContext context)
        {
            try
            {
                MagnumCargo cargo = context.State.Get<MagnumCargo>();
                if (cargo == null || cargo.ShipCargo == null || cargo.ShipCargo.Count == 0) return null;
                return cargo.ShipCargo[0];
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Brings one creature in line with its weapon limbs: removes the weapon of amputated
        /// limbs, applies the gun arm mode to the installed gun arms and opens or closes the third
        /// weapon slot. Safe to repeat and cheap when nothing changed.
        /// </summary>
        /// <param name="cd">The creature's data. Null, or a creature without inventory, is
        /// ignored.</param>
        /// <remarks>
        /// Input: The parameter and the gun arm settings.
        /// Output: Nothing is returned. May change the bare hands slot, the augmentation effects,
        /// the primary slot and the third weapon slot.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void Reconcile(CreatureData cd)
        {
            if (cd == null) return;
            Inventory inv = cd.Inventory;
            if (inv == null) return;

            ReconcileBareHandWeapon(cd, inv, GunArmSlotId,  GunArmWeaponId);
            ReconcileBareHandWeapon(cd, inv, MiniArmSlotId, MiniArmWeaponId);
            foreach (SmasherGunArms.GunArm arm in SmasherGunArms.All)
                ReconcileBareHandWeapon(cd, inv, arm.ArmSlotId, arm.WeaponId);
            ReconcileBareHandWeapon(cd, inv, SmasherUhmg.ArmSlotId, SmasherUhmg.WeaponId);
            foreach (SmasherSetless.Item it in SmasherSetless.All)
                if (it.WeaponId != null) ReconcileBareHandWeapon(cd, inv, it.SlotIds[0], it.WeaponId);
            ReconcileGunArmBlock(cd, inv);
            ReconcileExtraSlot(cd, inv);
        }

        /// <summary>
        /// Keeps the primary slot block of the installed gun arms in line with the gun arm mode.
        /// The effects of an augmentation are saved and only rebuilt on installation, so after a
        /// mode change they are rebuilt here. Before blocking, the weapon in the primary slot is
        /// moved to the backpack or vest, or else to the ship cargo, because the game would drop
        /// it on the floor and the ship has none. An amputated gun arm is skipped: its slot is
        /// already blocked by the amputation, in every mode.
        /// </summary>
        /// <param name="cd">The creature's data, not null.</param>
        /// <param name="inv">The creature's inventory, not null.</param>
        /// <remarks>
        /// Input: The parameters and the gun arm mode.
        /// Output: Nothing is returned. May move the primary weapon, rebuild the augmentation
        /// effects and the bare hands slot, and write a line in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void ReconcileGunArmBlock(CreatureData cd, Inventory inv)
        {
            bool want = SmasherGunArms.BlocksPrimary;
            foreach (SmasherGunArms.GunArm arm in SmasherGunArms.All)
            {
                if (!HasLimb(cd, arm.ArmSlotId, arm.AugId)) continue;
                if (HasArmBlock(cd, arm.ArmSlotId) == want) continue;
                if (want && !inv.PrimarySlot.Empty && !Stow(cd, inv, inv.PrimarySlot)) return;

                AugmentationSystem.ConfigureImplicitEffects(cd, preserveCurrentHp: true);
                CreatureSystem.SetBareHandSlot(cd);
                Debug.Log("[AugmentationsExpanded] gun arm effects rebuilt for mode " + SmasherGunArms.Mode);
                return;
            }
        }

        /// <summary>
        /// Whether the creature has the primary slot block that an installed augmentation puts on a
        /// wound slot. The block caused by an arm amputation is not counted.
        /// </summary>
        /// <param name="cd">The creature's data, not null.</param>
        /// <param name="woundSlotId">Id of the hand wound slot, e.g. "AE_GLArm".</param>
        /// <returns>True when the block is present; false otherwise or when the creature has no
        /// effects.</returns>
        /// <remarks>
        /// Input: The parameters and the creature's active effects.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool HasArmBlock(CreatureData cd, string woundSlotId)
        {
            EffectsController effects = cd.EffectsController;
            if (effects == null) return false;
            foreach (BaseEffect effect in effects)
            {
                WoundEffectArmUnavailable block = effect as WoundEffectArmUnavailable;
                if (block != null && block.FromAugment && block.SlotType == woundSlotId) return true;
            }
            return false;
        }

        /// <summary>
        /// Empties a weapon slot: its item goes to the backpack, or to the vest, or else where the
        /// current tick allows, the ship cargo on the ship or the floor in a mission.
        /// </summary>
        /// <param name="cd">The creature's data, not null; its position is used for the
        /// floor.</param>
        /// <param name="inv">The creature's inventory, not null.</param>
        /// <param name="slot">A weapon slot of that inventory, e.g. the primary or the third
        /// slot.</param>
        /// <returns>True when the slot is empty afterwards (also when it already was); false, with
        /// the item left in place, only when no destination is available.</returns>
        /// <remarks>
        /// Input: The parameters and the cargo or floor set by the current tick.
        /// Output: The return value. Moves the item.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool Stow(CreatureData cd, Inventory inv, ItemStorage slot)
        {
            BasePickupItem inside = slot.First;
            if (inside == null) return true;
            slot.Remove(inside);
            if (inv.BackpackStore.TryPutItem(inside, CellPosition.Zero)
             || inv.VestStore.TryPutItem(inside, CellPosition.Zero))
                return true;
            if (_floor != null && _grid != null)
            {
                ItemOnFloorSystem.SpawnItem(_floor, _grid, inside, cd.Position);
                return true;
            }
            if (_cargo != null)
            {
                _cargo.AddItemAndReshuffleOptional(inside);
                return true;
            }
            slot.AddItemAndReshuffle(inside);
            return false;
        }

        /// <summary>
        /// Removes the weapon of a limb from the bare hands slot once the limb is amputated. In the
        /// game every amputation of a living creature goes through AmputationSystem.Amputate, which
        /// recalculates the bare hands slot at the end, chain amputations included; this is a
        /// safety net for anything that severs the limb another way, such as another mod. It only
        /// removes: giving the weapon and restoring it after healing stay with the game.
        /// </summary>
        /// <param name="cd">The creature's data, not null.</param>
        /// <param name="inv">The creature's inventory, not null.</param>
        /// <param name="woundSlotId">Id of the limb's hand wound slot, e.g. "AE_TaserArm".</param>
        /// <param name="weaponId">Id of the limb's weapon, e.g. "ae_nature_taser_arm".</param>
        /// <remarks>
        /// Input: The parameters, the bare hands slot and the creature's wounds.
        /// Output: Nothing is returned. Empties the bare hands slot when it holds that weapon and
        /// the limb is amputated.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void ReconcileBareHandWeapon(CreatureData cd, Inventory inv,
                                                    string woundSlotId, string weaponId)
        {
            BasePickupItem held = inv.BareHandsSlot.First;
            if (held == null || held.Id != weaponId) return;
            if (!IsAmputated(cd, woundSlotId)) return;
            inv.RemoveBareHandWeapon();
        }

        /// <summary>
        /// Opens the third weapon slot while the MA-8 limb, or a gun arm in Extra slot mode, is
        /// installed and not amputated, and closes it otherwise: after a partial amputation (hand
        /// only), a chain amputation (shoulder and hand), the removal of the augmentation or a
        /// mode change. It is never closed when a perk grants it. Before closing, the item inside
        /// is moved out with Stow, because resizing the slot would destroy it.
        /// </summary>
        /// <param name="cd">The creature's data, not null.</param>
        /// <param name="inv">The creature's inventory, not null.</param>
        /// <remarks>
        /// Input: The parameters, the installed augmentations, the creature's perks and the gun arm
        /// mode.
        /// Output: Nothing is returned. May open or close the third weapon slot, move its item and
        /// switch the current weapon away from it.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void ReconcileExtraSlot(CreatureData cd, Inventory inv)
        {
            bool wanted = HasLimb(cd, MiniArmSlotId, MiniArmAugId);
            if (SmasherGunArms.OpensExtraSlot)
                foreach (SmasherGunArms.GunArm arm in SmasherGunArms.All)
                    wanted |= HasLimb(cd, arm.ArmSlotId, arm.AugId);

            if (wanted)
            {
                if (!inv.HasAdditionalSlot) inv.EnableAdditionalSlot();
                return;
            }

            if (!inv.HasAdditionalSlot) return;

            if (PerkSystem.GetPerkParameterBool(cd, "BThirdWeaponSlot")) return;

            ItemStorage slot = inv.AdditionalSlot;
            if (!Stow(cd, inv, slot)) return;

            inv.ResizeStorage(slot, 0, 0, null);
            if (inv.CurrentSlotType == WeaponSlotType.Additional)
                inv.SwapCurrentWeapon(true);
        }

        /// <summary>
        /// Whether an augmentation is installed on a wound slot and that limb is not amputated.
        /// Amputation does not remove the augmentation from the creature's list.
        /// </summary>
        /// <param name="cd">The creature's data, not null.</param>
        /// <param name="woundSlotId">Id of the wound slot, e.g. "AE_MantisArm".</param>
        /// <param name="augId">Id of the augmentation expected on it, e.g. "ae_mantis_arm".</param>
        /// <returns>True when that augmentation holds the slot and the limb is not amputated;
        /// otherwise false.</returns>
        /// <remarks>
        /// Input: The parameters, the creature's augmentations and its wounds.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool HasLimb(CreatureData cd, string woundSlotId, string augId)
        {
            Dictionary<string, string> augs = cd.AugmentationMap;
            string installedId;
            bool installed = augs != null
                          && augs.TryGetValue(woundSlotId, out installedId)
                          && installedId == augId;

            return installed && !IsAmputated(cd, woundSlotId);
        }

        /// <summary>
        /// Whether the creature has an active amputation of a wound slot, with the same test the
        /// game uses for the bare hands slot. Healed wounds do not count.
        /// </summary>
        /// <param name="cd">The creature's data, not null.</param>
        /// <param name="woundSlotId">Id of the wound slot, e.g. "AE_GLArm".</param>
        /// <returns>True when an active amputation or lost limb wound is on that slot; otherwise
        /// false.</returns>
        /// <remarks>
        /// Input: The parameters and the creature's active effects.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool IsAmputated(CreatureData cd, string woundSlotId)
        {
            EffectsController effects = cd.EffectsController;
            if (effects == null) return false;
            foreach (BaseEffect effect in effects)
            {
                BodyPartWound wound = effect as BodyPartWound;
                if (wound == null) continue;
                if (!wound.IsAmputation && !wound.IsCompletelyGone) continue;
                if (wound.Duration <= 0 && !wound.Endless) continue;
                if (wound.WoundSlotId == woundSlotId) return true;
            }
            return false;
        }

        /// <summary>
        /// Logs the first error of the per frame checks and ignores the following ones, so the log
        /// is not flooded every frame.
        /// </summary>
        /// <param name="e">The caught exception.</param>
        /// <remarks>
        /// Input: The parameter and whether an error was already logged.
        /// Output: Nothing is returned. The first call writes the error in the log; later calls do
        /// nothing.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void Complain(Exception e)
        {
            if (_complained) return;
            _complained = true;
            Debug.LogError("[AugmentationsExpanded] extra slot: " + e);
        }
    }
}
