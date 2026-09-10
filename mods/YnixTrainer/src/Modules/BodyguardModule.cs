using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using YnixTrainer.Core;

namespace YnixTrainer.Modules
{
    public class BodyguardModule : IYnixModule
    {
        public string Name { get { return "Bodyguards"; } }

        private List<Ped> _bodyguards = new List<Ped>();

        public void Initialize(UIMenu mainMenu, MenuPool menuPool)
        {
            var bgMenu = menuPool.AddSubMenu(mainMenu, Localization.Get("General", "SubmenuBodyguards", "Guarda-Costas e Proteção"));

            var swatItem = new UIMenuItem(
                Localization.Get("Bodyguards", "SpawnSwat", "Invocar Guarda-Costas SWAT"),
                Localization.Get("Bodyguards", "SpawnSwatDesc", "Soldado tático fortemente armado")
            );
            swatItem.Activated += (sender, selected) =>
            {
                SpawnBodyguard("s_m_y_swat_01", WeaponHash.CarbineRifle, "SWAT");
            };
            bgMenu.AddItem(swatItem);

            var milItem = new UIMenuItem(
                Localization.Get("Bodyguards", "SpawnMilitary", "Invocar Soldado Militar"),
                Localization.Get("Bodyguards", "SpawnMilitaryDesc", "Militar com metralhadora pesada")
            );
            milItem.Activated += (sender, selected) =>
            {
                SpawnBodyguard("s_m_y_marine_01", WeaponHash.CombatMG, "Militar");
            };
            bgMenu.AddItem(milItem);

            var agentItem = new UIMenuItem(
                Localization.Get("Bodyguards", "SpawnAgent", "Invocar Agente Secreto"),
                Localization.Get("Bodyguards", "SpawnAgentDesc", "Agente em terno com submetralhadora")
            );
            agentItem.Activated += (sender, selected) =>
            {
                SpawnBodyguard("s_m_m_security_01", WeaponHash.SMG, "Agente");
            };
            bgMenu.AddItem(agentItem);

            var dismissItem = new UIMenuItem(
                Localization.Get("Bodyguards", "DismissAll", "Dispensar Todos os Guarda-Costas"),
                Localization.Get("Bodyguards", "DismissAllDesc", "Remove todos os seguranças do grupo")
            );
            dismissItem.Activated += (sender, selected) =>
            {
                DismissAllBodyguards();
            };
            bgMenu.AddItem(dismissItem);
        }

        private void SpawnBodyguard(string modelName, WeaponHash weapon, string displayName)
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists()) return;

                Model model = new Model(modelName);
                model.Request(2500);
                if (model.IsInCdImage && model.IsValid)
                {
                    Vector3 spawnPos = player.Position + player.RightVector * (2.0f + _bodyguards.Count);
                    Ped bg = World.CreatePed(model, spawnPos, player.Heading);
                    if (bg != null && bg.Exists())
                    {
                        bg.Armor = 100;
                        bg.Health = 200;
                        bg.MaxHealth = 200;
                        bg.Weapons.Give(weapon, 9999, true, true);
                        bg.NeverLeavesGroup = true;
                        bg.Accuracy = 85;
                        bg.RelationshipGroup = player.RelationshipGroup;

                        player.CurrentPedGroup.Add(bg, false);
                        Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, bg.Handle, player.CurrentPedGroup.Handle);
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, bg.Handle, 5, true);
                        Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, bg.Handle, 46, true);
                        Function.Call(Hash.SET_PED_CAN_BE_TARGETTED, bg.Handle, false);

                        _bodyguards.Add(bg);
                        UI.Notify("~g~Guarda-costas (" + displayName + ") convocado!");
                    }
                }
                else
                {
                    UI.Notify("~r~Modelo não encontrado: " + modelName);
                }
                model.MarkAsNoLongerNeeded();
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Erro ao criar guarda-costas: " + ex.Message);
            }
        }

        private void DismissAllBodyguards()
        {
            try
            {
                int count = 0;
                for (int i = _bodyguards.Count - 1; i >= 0; i--)
                {
                    Ped bg = _bodyguards[i];
                    if (bg != null && bg.Exists())
                    {
                        bg.LeaveGroup();
                        bg.Delete();
                        count++;
                    }
                }
                _bodyguards.Clear();
                UI.Notify("~y~" + count + " guarda-costas dispensados.");
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Erro ao dispensar: " + ex.Message);
            }
        }

        public void OnTick()
        {
            for (int i = _bodyguards.Count - 1; i >= 0; i--)
            {
                if (_bodyguards[i] == null || !_bodyguards[i].Exists() || _bodyguards[i].IsDead)
                {
                    _bodyguards.RemoveAt(i);
                }
            }
        }
    }
}
