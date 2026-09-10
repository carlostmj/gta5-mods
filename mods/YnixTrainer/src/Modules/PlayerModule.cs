using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using NativeUI;
using YnixTrainer.Core;

namespace YnixTrainer.Modules
{
    public class PlayerModule : IYnixModule
    {
        public string Name { get { return "Player"; } }

        private bool _godMode = false;
        private bool _neverWanted = false;
        private bool _copsIgnore = false;
        private bool _superJump = false;
        private bool _fastRun = false;
        private bool _fastSwim = false;
        private bool _noRagdoll = false;
        private bool _unlimitedStamina = false;
        private bool _unlimitedOxygen = false;
        private bool _isInvisible = false;

        public void Initialize(UIMenu mainMenu, MenuPool menuPool)
        {
            var playerMenu = menuPool.AddSubMenu(mainMenu, Localization.Get("General", "SubmenuPlayer", "Opções do Jogador"));

            // God Mode
            var godItem = new UIMenuCheckboxItem(Localization.Get("Player", "GodMode", "Modo Deus (Invencibilidade)"), _godMode, Localization.Get("Player", "GodModeDesc", "Invulnerável a qualquer dano"));
            godItem.CheckboxEvent += (sender, state) =>
            {
                _godMode = state;
                if (Game.Player.Character.Exists())
                {
                    Game.Player.Character.IsInvincible = _godMode;
                }
            };
            playerMenu.AddItem(godItem);

            // Never Wanted
            var neverWantedItem = new UIMenuCheckboxItem(Localization.Get("Player", "NeverWanted", "Nunca Procurado"), _neverWanted, Localization.Get("Player", "NeverWantedDesc", "A polícia não persegue"));
            neverWantedItem.CheckboxEvent += (sender, state) =>
            {
                _neverWanted = state;
                if (_neverWanted) Game.Player.WantedLevel = 0;
            };
            playerMenu.AddItem(neverWantedItem);

            // Cops Ignore Player
            var copsIgnoreItem = new UIMenuCheckboxItem(Localization.Get("Player", "CopsIgnore", "Polícia Ignora o Jogador"), _copsIgnore, Localization.Get("Player", "CopsIgnoreDesc", "Policiais ignoram crimes"));
            copsIgnoreItem.CheckboxEvent += (sender, state) =>
            {
                _copsIgnore = state;
                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, _copsIgnore);
            };
            playerMenu.AddItem(copsIgnoreItem);

            // Clear Wanted Level
            var clearWantedItem = new UIMenuItem(Localization.Get("Player", "ClearWanted", "Limpar Nível de Procurado"), Localization.Get("Player", "ClearWantedDesc", "Remove estrelas imediatamente"));
            clearWantedItem.Activated += (sender, selected) =>
            {
                Game.Player.WantedLevel = 0;
                UI.Notify("~g~Nível de procurado limpo!");
            };
            playerMenu.AddItem(clearWantedItem);

            // Wanted Level List
            var wantedList = new List<dynamic> { 0, 1, 2, 3, 4, 5 };
            var wantedItem = new UIMenuListItem(Localization.Get("Player", "WantedLevel", "Nível de Procurado"), wantedList, Game.Player.WantedLevel, Localization.Get("Player", "WantedLevelDesc", "Definir estrelas"));
            wantedItem.OnListChanged += (sender, index) =>
            {
                Game.Player.WantedLevel = (int)wantedList[index];
            };
            playerMenu.AddItem(wantedItem);

            // Heal Player
            var healItem = new UIMenuItem(Localization.Get("Player", "HealPlayer", "Restaurar Vida e Colete"), Localization.Get("Player", "HealPlayerDesc", "Restaura vida e armadura"));
            healItem.Activated += (sender, selected) =>
            {
                Ped p = Game.Player.Character;
                if (p.Exists())
                {
                    p.Health = p.MaxHealth;
                    p.Armor = 100;
                    UI.Notify("~g~Vida e Colete restaurados!");
                }
            };
            playerMenu.AddItem(healItem);

            // Invisibility
            var invisItem = new UIMenuCheckboxItem(Localization.Get("Player", "Invisibility", "Invisibilidade"), _isInvisible, Localization.Get("Player", "InvisibilityDesc", "Fica invisível"));
            invisItem.CheckboxEvent += (sender, state) =>
            {
                _isInvisible = state;
                if (Game.Player.Character.Exists())
                {
                    Game.Player.Character.IsVisible = !_isInvisible;
                }
            };
            playerMenu.AddItem(invisItem);

            // Super Jump
            var jumpItem = new UIMenuCheckboxItem(Localization.Get("Player", "SuperJump", "Super Pulo"), _superJump, Localization.Get("Player", "SuperJumpDesc", "Pulos gigantes"));
            jumpItem.CheckboxEvent += (sender, state) => { _superJump = state; };
            playerMenu.AddItem(jumpItem);

            // Fast Run
            var runItem = new UIMenuCheckboxItem(Localization.Get("Player", "FastRun", "Super Velocidade"), _fastRun, Localization.Get("Player", "FastRunDesc", "Corrida rápida"));
            runItem.CheckboxEvent += (sender, state) => { _fastRun = state; };
            playerMenu.AddItem(runItem);

            // Fast Swim
            var swimItem = new UIMenuCheckboxItem(Localization.Get("Player", "FastSwim", "Nadar Rápido"), _fastSwim, Localization.Get("Player", "FastSwimDesc", "Natação veloz"));
            swimItem.CheckboxEvent += (sender, state) => { _fastSwim = state; };
            playerMenu.AddItem(swimItem);

            // No Ragdoll
            var ragItem = new UIMenuCheckboxItem(Localization.Get("Player", "NoRagdoll", "Sem Queda (Anti-Ragdoll)"), _noRagdoll, Localization.Get("Player", "NoRagdollDesc", "Nunca cai no chão"));
            ragItem.CheckboxEvent += (sender, state) =>
            {
                _noRagdoll = state;
                if (Game.Player.Character.Exists())
                {
                    Game.Player.Character.CanRagdoll = !_noRagdoll;
                }
            };
            playerMenu.AddItem(ragItem);

            // Unlimited Stamina
            var stamItem = new UIMenuCheckboxItem(Localization.Get("Player", "UnlimitedStamina", "Estamina Infinita"), _unlimitedStamina, Localization.Get("Player", "UnlimitedStaminaDesc", "Nunca cansa"));
            stamItem.CheckboxEvent += (sender, state) => { _unlimitedStamina = state; };
            playerMenu.AddItem(stamItem);

            // Unlimited Oxygen
            var oxyItem = new UIMenuCheckboxItem(Localization.Get("Player", "UnlimitedOxygen", "Oxigênio Infinito"), _unlimitedOxygen, Localization.Get("Player", "UnlimitedOxygenDesc", "Respiração infinita"));
            oxyItem.CheckboxEvent += (sender, state) => { _unlimitedOxygen = state; };
            playerMenu.AddItem(oxyItem);

            // Add Cash 1M
            var cash1M = new UIMenuItem(Localization.Get("Player", "AddCash1M", "Adicionar $1.000.000"), Localization.Get("Player", "AddCash1MDesc", "Adiciona 1 milhão"));
            cash1M.Activated += (sender, selected) =>
            {
                Game.Player.Money += 1000000;
                UI.Notify("~g~+$1,000,000 adicionados!");
            };
            playerMenu.AddItem(cash1M);

            // Add Cash 10M
            var cash10M = new UIMenuItem(Localization.Get("Player", "AddCash10M", "Adicionar $10.000.000"), Localization.Get("Player", "AddCash10MDesc", "Adiciona 10 milhões"));
            cash10M.Activated += (sender, selected) =>
            {
                Game.Player.Money += 10000000;
                UI.Notify("~g~+$10,000,000 adicionados!");
            };
            playerMenu.AddItem(cash10M);

            // Clean Clothes
            var cleanItem = new UIMenuItem(Localization.Get("Player", "CleanClothes", "Limpar Roupas e Sangue"), Localization.Get("Player", "CleanClothesDesc", "Limpa o personagem"));
            cleanItem.Activated += (sender, selected) =>
            {
                if (Game.Player.Character.Exists())
                {
                    Game.Player.Character.ClearBloodDamage();
                    UI.Notify("~b~Roupas limpas!");
                }
            };
            playerMenu.AddItem(cleanItem);

            // Suicide
            var suicideItem = new UIMenuItem(Localization.Get("Player", "Suicide", "Suicídio"), Localization.Get("Player", "SuicideDesc", "Elimina o personagem"));
            suicideItem.Activated += (sender, selected) =>
            {
                if (Game.Player.Character.Exists())
                {
                    Game.Player.Character.Kill();
                }
            };
            playerMenu.AddItem(suicideItem);
        }

        public void OnTick()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            if (_godMode)
            {
                player.IsInvincible = true;
            }
            if (_neverWanted)
            {
                if (Game.Player.WantedLevel > 0)
                {
                    Game.Player.WantedLevel = 0;
                }
            }
            if (_copsIgnore)
            {
                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);
            }
            if (_superJump)
            {
                Game.Player.SetSuperJumpThisFrame();
            }
            if (_fastRun)
            {
                Game.Player.SetRunSpeedMultThisFrame(1.49f);
            }
            if (_fastSwim)
            {
                Function.Call(Hash.SET_SWIM_MULTIPLIER_FOR_PLAYER, Game.Player, 1.49f);
            }
            if (_noRagdoll)
            {
                player.CanRagdoll = false;
            }
            if (_unlimitedStamina)
            {
                Function.Call(Hash.RESET_PLAYER_STAMINA, Game.Player);
            }
            if (_unlimitedOxygen)
            {
                Function.Call(Hash.SET_PED_MAX_TIME_UNDERWATER, player, 9999.0f);
            }
        }
    }
}
