using System;
using GTA;
using GTA.Native;
using NativeUI;
using YnixTrainer.Core;

namespace YnixTrainer.Modules
{
    public class WeaponModule : IYnixModule
    {
        public string Name { get { return "Weapon"; } }

        private bool _infiniteAmmo = false;
        private bool _explosiveAmmo = false;
        private bool _fireAmmo = false;
        private bool _superDamage = false;

        public void Initialize(UIMenu mainMenu, MenuPool menuPool)
        {
            var weaponMenu = menuPool.AddSubMenu(mainMenu, Localization.Get("General", "SubmenuWeapons", "Armas e Munição"));

            // Give All Weapons
            var giveAllItem = new UIMenuItem(Localization.Get("Weapons", "GiveAllWeapons", "Obter Todas as Armas"), Localization.Get("Weapons", "GiveAllWeaponsDesc", "Entrega todas as armas"));
            giveAllItem.Activated += (sender, selected) =>
            {
                Ped player = Game.Player.Character;
                if (player.Exists())
                {
                    foreach (WeaponHash hash in Enum.GetValues(typeof(WeaponHash)))
                    {
                        try { player.Weapons.Give(hash, 9999, false, true); } catch { }
                    }
                    UI.Notify("~g~Todas as armas concedidas com munição máxima!");
                }
            };
            weaponMenu.AddItem(giveAllItem);

            // Remove All Weapons
            var removeAllItem = new UIMenuItem(Localization.Get("Weapons", "RemoveAllWeapons", "Remover Todas as Armas"), Localization.Get("Weapons", "RemoveAllWeaponsDesc", "Remove todas as armas"));
            removeAllItem.Activated += (sender, selected) =>
            {
                Ped player = Game.Player.Character;
                if (player.Exists())
                {
                    player.Weapons.RemoveAll();
                    UI.Notify("~y~Todas as armas foram removidas!");
                }
            };
            weaponMenu.AddItem(removeAllItem);

            // Infinite Ammo
            var infiniteAmmoItem = new UIMenuCheckboxItem(Localization.Get("Weapons", "InfiniteAmmo", "Munição Infinita"), _infiniteAmmo, Localization.Get("Weapons", "InfiniteAmmoDesc", "Nunca recarrega"));
            infiniteAmmoItem.CheckboxEvent += (sender, state) =>
            {
                _infiniteAmmo = state;
                Ped player = Game.Player.Character;
                if (player.Exists())
                {
                    Function.Call(Hash.SET_PED_INFINITE_AMMO_CLIP, player, _infiniteAmmo);
                }
            };
            weaponMenu.AddItem(infiniteAmmoItem);

            // Explosive Ammo
            var explosiveAmmoItem = new UIMenuCheckboxItem(Localization.Get("Weapons", "ExplosiveAmmo", "Balas Explosivas"), _explosiveAmmo, Localization.Get("Weapons", "ExplosiveAmmoDesc", "Tiros explodem"));
            explosiveAmmoItem.CheckboxEvent += (sender, state) => { _explosiveAmmo = state; };
            weaponMenu.AddItem(explosiveAmmoItem);

            // Fire Ammo
            var fireAmmoItem = new UIMenuCheckboxItem(Localization.Get("Weapons", "FireAmmo", "Balas Incendiárias"), _fireAmmo, Localization.Get("Weapons", "FireAmmoDesc", "Tiros pegam fogo"));
            fireAmmoItem.CheckboxEvent += (sender, state) => { _fireAmmo = state; };
            weaponMenu.AddItem(fireAmmoItem);

            // Super Damage
            var dmgItem = new UIMenuCheckboxItem(Localization.Get("Weapons", "SuperDamage", "Super Dano (Morte em 1 Tiro)"), _superDamage, Localization.Get("Weapons", "SuperDamageDesc", "Dano multiplicado"));
            dmgItem.CheckboxEvent += (sender, state) => { _superDamage = state; };
            weaponMenu.AddItem(dmgItem);
        }

        public void OnTick()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            if (_infiniteAmmo)
            {
                Function.Call(Hash.SET_PED_INFINITE_AMMO_CLIP, player, true);
            }
            if (_explosiveAmmo)
            {
                Function.Call(Hash.SET_EXPLOSIVE_AMMO_THIS_FRAME, Game.Player);
            }
            if (_fireAmmo)
            {
                Function.Call(Hash.SET_FIRE_AMMO_THIS_FRAME, Game.Player);
            }
            if (_superDamage)
            {
                Function.Call(Hash.SET_PLAYER_WEAPON_DAMAGE_MODIFIER, Game.Player, 100.0f);
            }
        }
    }
}
