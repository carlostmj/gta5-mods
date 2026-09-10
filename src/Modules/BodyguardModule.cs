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
        private bool _godModeSquad = false;

        public void Initialize(UIMenu mainMenu, MenuPool menuPool)
        {
            var bgMenu = menuPool.AddSubMenu(mainMenu, Localization.Get("General", "SubmenuBodyguards", "Guarda-Costas e Gangue"));

            // 1. Single Bodyguards
            var singleMenu = menuPool.AddSubMenu(bgMenu, "Invocar Guarda Individual");
            AddSingleBgItem(singleMenu, "Policial SWAT", "s_m_y_swat_01", WeaponHash.CarbineRifle);
            AddSingleBgItem(singleMenu, "Soldado Militar", "s_m_y_marine_01", WeaponHash.CombatMG);
            AddSingleBgItem(singleMenu, "Agente Secreto", "s_m_m_security_01", WeaponHash.SMG);
            AddSingleBgItem(singleMenu, "Membro dos Ballas", "g_m_y_ballaorig_01", WeaponHash.MicroSMG);
            AddSingleBgItem(singleMenu, "Membro das Famílias (Groove)", "g_m_y_famdnf_01", WeaponHash.PumpShotgun);
            AddSingleBgItem(singleMenu, "Membro dos Vagos", "g_m_y_salvagoon_01", WeaponHash.CombatPistol);

            // 2. Squads
            var squadMenu = menuPool.AddSubMenu(bgMenu, "Invocar Esquadrão Completo (Gangue)");
            AddSquadItem(squadMenu, "Esquadrão Famílias (Groove St - 3 Membros)", "g_m_y_famdnf_01", WeaponHash.CarbineRifle, 3);
            AddSquadItem(squadMenu, "Esquadrão Ballas (3 Membros)", "g_m_y_ballaorig_01", WeaponHash.MicroSMG, 3);
            AddSquadItem(squadMenu, "Esquadrão Los Vagos (3 Membros)", "g_m_y_salvagoon_01", WeaponHash.PumpShotgun, 3);
            AddSquadItem(squadMenu, "Esquadrão Tático SWAT (4 Membros)", "s_m_y_swat_01", WeaponHash.CarbineRifle, 4);
            AddSquadItem(squadMenu, "Esquadrão Militar de Elite (4 Membros)", "s_m_y_marine_01", WeaponHash.CombatMG, 4);

            // 3. Gang Armory
            var armoryMenu = menuPool.AddSubMenu(bgMenu, "Arsenal & Equipamento da Gangue");
            AddWeaponEquipItem(armoryMenu, "Equipar Fuzis de Assalto (Carbine Rifle)", WeaponHash.CarbineRifle);
            AddWeaponEquipItem(armoryMenu, "Equipar Metralhadoras Pesadas (Combat MG)", WeaponHash.CombatMG);
            AddWeaponEquipItem(armoryMenu, "Equipar Escopetas de Combate", WeaponHash.PumpShotgun);
            AddWeaponEquipItem(armoryMenu, "Equipar Submetralhadoras (SMG)", WeaponHash.SMG);
            AddWeaponEquipItem(armoryMenu, "Equipar Lança-Foguetes (RPG)", WeaponHash.RPG);
            AddWeaponEquipItem(armoryMenu, "Equipar Minigun", WeaponHash.Minigun);

            var armorAllItem = new UIMenuItem("Blindagem Total para a Gangue (100% Colete)", "Recarrega vida máxima e colete de todos os membros");
            armorAllItem.Activated += (sender, selected) =>
            {
                int count = 0;
                foreach (var bg in _bodyguards)
                {
                    if (bg != null && bg.Exists())
                    {
                        bg.Health = bg.MaxHealth;
                        bg.Armor = 100;
                        count++;
                    }
                }
                UI.Notify("~g~" + count + " membros receberam blindagem total!");
            };
            armoryMenu.AddItem(armorAllItem);

            var godSquadItem = new UIMenuCheckboxItem("Gangue Invencível (God Mode)", _godModeSquad, "Torna todos os membros imunes a balas e explosões");
            godSquadItem.CheckboxEvent += (sender, state) =>
            {
                _godModeSquad = state;
                foreach (var bg in _bodyguards)
                {
                    if (bg != null && bg.Exists())
                    {
                        bg.IsInvincible = _godModeSquad;
                    }
                }
                UI.Notify(_godModeSquad ? "~g~Gangue agora é invencível!" : "~y~Invencibilidade da gangue desativada.");
            };
            armoryMenu.AddItem(godSquadItem);

            // 4. Tactical Commands
            var cmdMenu = menuPool.AddSubMenu(bgMenu, "Ordens e Táticas de Grupo");

            var attackTargetItem = new UIMenuItem("Todos Atacar Alvo Mirado", "Ordena que a gangue ataque o inimigo em que você mirar");
            attackTargetItem.Activated += (sender, selected) =>
            {
                Ped player = Game.Player.Character;
                Entity target = null;
                RaycastResult ray = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, 150.0f, IntersectOptions.Peds1);
                if (ray.DitHitEntity && ray.HitEntity is Ped) target = ray.HitEntity;

                if (target != null && target is Ped)
                {
                    foreach (var bg in _bodyguards)
                    {
                        if (bg != null && bg.Exists())
                        {
                            bg.Task.FightAgainst((Ped)target);
                        }
                    }
                    UI.Notify("~r~Gangue atacando alvo marcado!");
                }
                else
                {
                    UI.Notify("~y~Mire em um NPC primeiro!");
                }
            };
            cmdMenu.AddItem(attackTargetItem);

            var holdItem = new UIMenuItem("Segurar Posição (Ficar Parado)", "Os membros defenderão a área atual");
            holdItem.Activated += (sender, selected) =>
            {
                foreach (var bg in _bodyguards)
                {
                    if (bg != null && bg.Exists())
                    {
                        bg.Task.StandStill(-1);
                    }
                }
                UI.Notify("~y~Membros segurando posição.");
            };
            cmdMenu.AddItem(holdItem);

            var followItem = new UIMenuItem("Reagrupar e Seguir Jogador", "Cancela ordens anteriores e volta a seguir");
            followItem.Activated += (sender, selected) =>
            {
                Ped player = Game.Player.Character;
                foreach (var bg in _bodyguards)
                {
                    if (bg != null && bg.Exists())
                    {
                        bg.Task.ClearAll();
                        Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, bg.Handle, player.CurrentPedGroup.Handle);
                    }
                }
                UI.Notify("~g~Gangue reagrupada com o jogador.");
            };
            cmdMenu.AddItem(followItem);

            // 5. Gang War Simulator
            var warItem = new UIMenuItem("Iniciar Guerra de Gangues (Simulador de Confronto)", "Spawna Ballas vs Famílias em um tiroteio no local");
            warItem.Activated += (sender, selected) =>
            {
                StartGangWar();
            };
            bgMenu.AddItem(warItem);

            // Dismiss All
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

        private void AddSingleBgItem(UIMenu menu, string label, string modelName, WeaponHash weapon)
        {
            var item = new UIMenuItem(label, "Convoca " + label);
            item.Activated += (sender, selected) =>
            {
                SpawnBodyguard(modelName, weapon, label);
            };
            menu.AddItem(item);
        }

        private void AddSquadItem(UIMenu menu, string label, string modelName, WeaponHash weapon, int count)
        {
            var item = new UIMenuItem(label, "Spawna " + count + " membros de uma vez");
            item.Activated += (sender, selected) =>
            {
                for (int i = 0; i < count; i++)
                {
                    SpawnBodyguard(modelName, weapon, label);
                }
                UI.Notify("~g~" + label + " convocado com sucesso!");
            };
            menu.AddItem(item);
        }

        private void AddWeaponEquipItem(UIMenu menu, string label, WeaponHash weapon)
        {
            var item = new UIMenuItem(label, "Entrega a arma a todos os membros ativos");
            item.Activated += (sender, selected) =>
            {
                int count = 0;
                foreach (var bg in _bodyguards)
                {
                    if (bg != null && bg.Exists())
                    {
                        bg.Weapons.Give(weapon, 9999, true, true);
                        count++;
                    }
                }
                UI.Notify("~g~" + count + " membros equipados com " + label + "!");
            };
            menu.AddItem(item);
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
                    float angle = _bodyguards.Count * 0.8f;
                    Vector3 offset = new Vector3((float)Math.Cos(angle) * 3.0f, (float)Math.Sin(angle) * 3.0f, 0f);
                    Vector3 spawnPos = player.Position + offset;
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

                        if (_godModeSquad) bg.IsInvincible = true;

                        _bodyguards.Add(bg);
                    }
                }
                model.MarkAsNoLongerNeeded();
            }
            catch { }
        }

        private void StartGangWar()
        {
            try
            {
                Ped player = Game.Player.Character;
                Model ballaModel = new Model("g_m_y_ballaorig_01");
                Model famModel = new Model("g_m_y_famdnf_01");
                ballaModel.Request(2000);
                famModel.Request(2000);

                int ballaRel = World.AddRelationshipGroup("BALLAS_GANG");
                int famRel = World.AddRelationshipGroup("FAMILIES_GANG");

                World.SetRelationshipBetweenGroups(Relationship.Hate, ballaRel, famRel);
                World.SetRelationshipBetweenGroups(Relationship.Hate, famRel, ballaRel);

                for (int i = 0; i < 4; i++)
                {
                    Vector3 bPos = player.Position + player.ForwardVector * 15.0f + player.RightVector * (i * 3.0f - 4.5f);
                    Ped b = World.CreatePed(ballaModel, bPos);
                    if (b != null)
                    {
                        b.RelationshipGroup = ballaRel;
                        b.Weapons.Give(WeaponHash.MicroSMG, 9999, true, true);
                        b.Armor = 50;
                    }

                    Vector3 fPos = player.Position + player.ForwardVector * 35.0f + player.RightVector * (i * 3.0f - 4.5f);
                    Ped f = World.CreatePed(famModel, fPos);
                    if (f != null)
                    {
                        f.RelationshipGroup = famRel;
                        f.Weapons.Give(WeaponHash.PumpShotgun, 9999, true, true);
                        f.Armor = 50;
                    }
                }

                UI.Notify("~r~Guerra de Gangues iniciada! Ballas vs Famílias!");
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Erro na guerra de gangues: " + ex.Message);
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
                UI.Notify("~y~" + count + " membros dispensados.");
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Erro: " + ex.Message);
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
