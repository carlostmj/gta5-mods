using System;
using GTA;
using GTA.Native;
using YnixPolice.Core;

namespace YnixPolice.Systems
{
    public class PoliceEscalationSystem
    {
        private int _lastEscalationCheck = 0;
        private int _lastPacifyCheck = 0;

        public void OnTick()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;

            int wanted = Game.Player.WantedLevel;

            // ABSOLUTE RULE: If player has 0 wanted stars, NO COP SHOULD EVER ATTACK OR SHOOT!
            if (wanted == 0)
            {
                PacifyNearbyCops(player);
                return;
            }

            if (!ConfigManager.NonLethalAtLowStars) return;
            if (wanted > 2) return;

            int now = Game.GameTime;
            if (now - _lastEscalationCheck < 800) return;
            _lastEscalationCheck = now;

            // If player aims firearm or shoots, escalate to lethal immediately
            bool playerHasFirearm = player.Weapons.Current != null && 
                                    player.Weapons.Current.Group != WeaponGroup.Unarmed && 
                                    player.Weapons.Current.Group != WeaponGroup.Melee;

            if (playerHasFirearm || player.IsShooting || Game.Player.IsAiming)
            {
                return;
            }

            // Player is unarmed: enforce StunGun on nearby officers
            Ped[] nearbyPeds = World.GetNearbyPeds(player.Position, 50.0f);
            foreach (var p in nearbyPeds)
            {
                if (PoliceUtils.IsCop(p) && !p.IsDead)
                {
                    if (!p.Weapons.HasWeapon(WeaponHash.StunGun))
                    {
                        p.Weapons.Give(WeaponHash.StunGun, 100, true, true);
                    }
                    if (p.Weapons.Current.Hash != WeaponHash.StunGun && p.Weapons.Current.Hash != WeaponHash.Nightstick)
                    {
                        p.Weapons.Select(WeaponHash.StunGun, true);
                    }
                }
            }
        }

        public void PacifyNearbyCops(Ped player)
        {
            int now = Game.GameTime;
            if (now - _lastPacifyCheck < 400) return;
            _lastPacifyCheck = now;

            Ped[] nearby = World.GetNearbyPeds(player.Position, 80.0f);
            foreach (var p in nearby)
            {
                if (PoliceUtils.IsCop(p) && !p.IsDead)
                {
                    if (p.IsInCombatAgainst(player) || p.IsShooting )
                    {
                        p.BlockPermanentEvents = false;
                        p.Task.ClearAll();
                        Function.Call(Hash.CLEAR_PED_TASKS, p.Handle);
                        
                        p.Weapons.Select(WeaponHash.Unarmed, true);
                        p.Task.WanderAround();
                    }
                }
            }
        }
    }
}
