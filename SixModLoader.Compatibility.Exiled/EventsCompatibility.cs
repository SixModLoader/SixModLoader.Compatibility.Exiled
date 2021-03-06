﻿using System;
using System.Linq;
using System.Reflection;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using HarmonyLib;
using SixModLoader.Api.Events.Player.Class;
using SixModLoader.Api.Events.Player.Inventory;
using SixModLoader.Api.Events.Player.Weapon;
using SixModLoader.Events;

namespace SixModLoader.Compatibility.Exiled
{
    using Handlers = global::Exiled.Events.Handlers;

    public class EventsCompatibility
    {
        public EventsCompatibility()
        {
            var events = global::Exiled.Events.Events.Instance;
            if (events == null)
            {
                Logger.Error("Exiled.Events instance is not created!");
                return;
            }

            events.Config.IsNameTrackingEnabled = false;

            Logger.Info("Unpatching bad Exiled.Events");
            global::Exiled.Events.Events.DisabledPatchesHashSet.UnionWith(new[]
            {
                AccessTools.Method(typeof(WeaponManager), nameof(WeaponManager.CallCmdShoot)),
                AccessTools.Method(typeof(Inventory), nameof(Inventory.CallCmdDropItem)),
                AccessTools.Method(typeof(CharacterClassManager), nameof(CharacterClassManager.SetPlayersClass))
            });
            events.ReloadDisabledPatches();

            var npcsType = Type.GetType("NPCS.Plugin, 0NPCS") ?? Type.GetType("NPCS.Plugin, NPCS");

            if (npcsType != null)
            {
                Logger.Info("Unpatching bad NPCS");

                var instanceProperty = npcsType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
                var instance = instanceProperty!.GetValue(null);

                var harmonyProperty = npcsType.GetProperty("Harmony", BindingFlags.Instance | BindingFlags.Public);
                var harmony = (Harmony) harmonyProperty!.GetValue(instance);

                harmony.Unpatch(AccessTools.Method(typeof(WeaponManager), nameof(WeaponManager.CallCmdShoot)), HarmonyPatchType.All, harmony.Id);
            }
        }

        [EventHandler]
        public void OnPlayerShoot(PlayerShootEvent e)
        {
            var @event = new ShootingEventArgs(Player.Get(e.Player.gameObject), e.Target, e.TargetPos);
            Handlers.Player.OnShooting(@event);
            if (!@event.IsAllowed)
            {
                e.Cancelled = true;
            }
        }

        [EventHandler]
        public void OnPlayerShotByPlayer(PlayerShotByPlayerEvent e)
        {
            var @event = new ShotEventArgs(Player.Get(e.Shooter.gameObject), e.Player.gameObject, e.HitBoxType, e.Distance, e.HitInfo.Amount);
            Handlers.Player.OnShot(@event);
            if (!@event.CanHurt)
            {
                e.Cancelled = true;
            }
        }

        [EventHandler]
        public void OnPlayerDropItem(PlayerDropItemEvent e)
        {
            var @event = new DroppingItemEventArgs(Player.Get(e.Player.gameObject), e.Item);
            Handlers.Player.OnDroppingItem(@event);
            if (!@event.IsAllowed)
            {
                e.Cancelled = true;
            }
        }

        [EventHandler]
        public void OnPlayerDroppedItem(PlayerDroppedItemEvent e)
        {
            var @event = new ItemDroppedEventArgs(Player.Get(e.Player.gameObject), e.Pickup);
            Handlers.Player.OnItemDropped(@event);
        }

        [EventHandler]
        public void OnPlayerClassChange(PlayerRoleChangeEvent e)
        {
            var @event = new ChangingRoleEventArgs(Player.Get(e.Player.gameObject), e.RoleType, e.Items, e.Lite, e.Escape);
            Handlers.Player.OnChangingRole(@event);
        }
    }
}