using System;
using GTA;
using GTA.Native;
using YnixPolice.Core;

namespace YnixPolice.Systems
{
    public class PoliceEscalationSystem
    {
        private int _lastEscalationCheck = 0;

        public void OnTick()
        {
            if (!ConfigManager.NonLethalAtLowStars) return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;

            // Only applies at 1 or 2 stars
            int wanted = Game.Player.WantedLevel;
            if (wanted <= 0 || wanted > 2) return;

            int now = Game.GameTime;
            if (now - _lastEscalationCheck < 1000) return;
            _lastEscalationCheck = now;

            // If player is aiming a firearm or shooting, escalate immediately to lethal
            bool playerHasFirearm = player.Weapons.Current != null && 
                                    player.Weapons.Current.Group != WeaponGroup.Unarmed && 
                                    player.Weapons.Current.Group != WeaponGroup.Melee;

            if (playerHasFirearm || player.IsShooting || Game.Player.IsAiming)
            {
                // Allow lethal response
                return;
            }

            // Player is unarmed or running on foot: enforce non-lethal equipment on nearby officers
            Ped[] nearbyPeds = World.GetNearbyPeds(player.Position, 40.0f);
            foreach (var p in nearbyPeds)
            {
                if (p != null && p.Exists() && !p.IsDead && (p.RelationshipGroup == 0x432D1DE1 || Function.Call<int>(Hash.GET_PED_TYPE, p.Handle) == 6))
                {
                    // Give taser or nightstick
                    if (!p.Weapons.HasWeapon(WeaponHash.StunGun))
                    {
                        p.Weapons.Give(WeaponHash.StunGun, 100, true, true);
                    }
                    else if (p.Weapons.Current.Hash != WeaponHash.StunGun && p.Weapons.Current.Hash != WeaponHash.Nightstick)
                    {
                        p.Weapons.Select(WeaponHash.StunGun, true);
                    }
                }
            }
        }
    }
}
