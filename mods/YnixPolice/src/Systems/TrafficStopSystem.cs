using System;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using YnixPolice.Core;

namespace YnixPolice.Systems
{
    public enum TrafficStopState
    {
        Idle,
        PulledOverWaitingCop,
        CopAtWindow
    }

    public class TrafficStopSystem
    {
        public TrafficStopState CurrentState = TrafficStopState.Idle;

        private Vehicle _copVehicle = null;
        private Ped _copPed = null;
        private int _stopStartTime = 0;
        private int _lastSpeedCheck = 0;

        public void OnTick()
        {
            if (!ConfigManager.EnableTrafficStops) return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
            {
                Reset();
                return;
            }

            // 1. Speeding trigger: If player speeds > threshold in city and no stars, give 1 star for traffic violation!
            if (player.IsInVehicle() && Game.Player.WantedLevel == 0)
            {
                int now = Game.GameTime;
                if (now - _lastSpeedCheck > 1000)
                {
                    _lastSpeedCheck = now;
                    Vehicle playerVeh = player.CurrentVehicle;
                    if (playerVeh != null && playerVeh.Exists())
                    {
                        float kmh = playerVeh.Speed * 3.6f;
                        if (kmh >= ConfigManager.SpeedingThresholdKMH)
                        {
                            // Check if near any cop car or cop ped within 50m
                            Vehicle[] nearby = World.GetNearbyVehicles(player.Position, 50.0f);
                            foreach (var v in nearby)
                            {
                                if (v != null && v.Exists() && v.ClassType == VehicleClass.Emergency)
                                {
                                    Game.Player.WantedLevel = 1;
                                    UI.Notify("~b~INFRAÇÃO DE TRÂNSITO!~w~\nExcesso de velocidade a " + (int)kmh + " KM/H.");
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // 2. Active Traffic Stop logic when WantedLevel == 1
            if (Game.Player.WantedLevel == 1)
            {
                HandleOneStarTrafficStop(player);
            }
            else
            {
                if (CurrentState != TrafficStopState.Idle)
                {
                    Reset();
                }
            }
        }

        private void HandleOneStarTrafficStop(Ped player)
        {
            // At 1 star: Police MUST NEVER SHOOT the player!
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);

            Ped[] nearby = World.GetNearbyPeds(player.Position, 50.0f);
            foreach (var p in nearby)
            {
                if (PoliceUtils.IsCop(p))
                {
                    // Block shooting
                    Function.Call(Hash.SET_PED_CAN_ARM_IK, p.Handle, false);
                    if (p.Weapons.Current != null && p.Weapons.Current.Group != WeaponGroup.Unarmed && p.Weapons.Current.Group != WeaponGroup.Melee)
                    {
                        if (!p.Weapons.HasWeapon(WeaponHash.StunGun))
                        {
                            p.Weapons.Give(WeaponHash.StunGun, 100, true, true);
                        }
                        p.Weapons.Select(WeaponHash.StunGun, true);
                    }
                }
            }

            if (player.IsInVehicle())
            {
                Vehicle playerVeh = player.CurrentVehicle;

                // If player is moving fast, show prompt to pull over
                if (playerVeh.Speed > 2.0f)
                {
                    CurrentState = TrafficStopState.Idle;
                    string hint = "~y~ORDEM DE PARADA POLICIAL ~w~| Encoste o carro no acostamento (Segure [S] ou [Espaço])";
                    new UIText(hint, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 40), 0.45f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();
                }
                else
                {
                    // Player has stopped the car!
                    if (CurrentState == TrafficStopState.Idle)
                    {
                        CurrentState = TrafficStopState.PulledOverWaitingCop;
                        _stopStartTime = Game.GameTime;

                        // Find closest cop vehicle
                        Vehicle[] emergencyVehs = World.GetNearbyVehicles(player.Position, 45.0f);
                        foreach (var v in emergencyVehs)
                        {
                            if (v != null && v.Exists() && v.ClassType == VehicleClass.Emergency)
                            {
                                _copVehicle = v;
                                _copPed = v.GetPedOnSeat(VehicleSeat.Driver);
                                break;
                            }
                        }

                        if (_copPed != null && _copPed.Exists())
                        {
                            _copPed.Task.LeaveVehicle(_copVehicle, false);
                            Vector3 targetPos = playerVeh.Position + playerVeh.RightVector * -1.3f;
                            _copPed.Task.GoTo(targetPos);
                            UI.Notify("~b~O policial está se aproximando da janela do motorista...\nAguarde no veículo.");
                        }
                    }
                    else if (CurrentState == TrafficStopState.PulledOverWaitingCop)
                    {
                        if (_copPed != null && _copPed.Exists())
                        {
                            float d = World.GetDistance(_copPed.Position, player.Position);
                            if (d < 3.0f)
                            {
                                CurrentState = TrafficStopState.CopAtWindow;
                                Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, _copPed.Handle, "WORLD_HUMAN_COP_IDLES", 0, true);
                            }
                            else
                            {
                                string wMsg = "~b~Aguarde: ~w~O policial está caminhando até o seu veículo...";
                                new UIText(wMsg, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 40), 0.45f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();
                            }
                        }
                        else
                        {
                            // No cop ped nearby, find any cop on foot
                            _copPed = FindClosestCopOnFoot(player.Position, 40.0f);
                            if (_copPed == null)
                            {
                                // Show direct fine resolution prompt
                                CurrentState = TrafficStopState.CopAtWindow;
                            }
                        }
                    }
                    else if (CurrentState == TrafficStopState.CopAtWindow)
                    {
                        // Show interaction dialog
                        string dialog = "~b~Polícia de Los Santos: ~w~Infração de Trânsito registrada.\nPressione ~g~[" + ConfigManager.AcceptFineKey.ToString() + "]~w~ Pagar Multa ($" + ConfigManager.TicketFineAmount + ") | ~r~[" + ConfigManager.SurrenderKey.ToString() + "]~w~ Render-se";
                        new UIText(dialog, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 45), 0.45f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();

                        // Option 1: Pay Fine
                        if (Game.IsKeyPressed(ConfigManager.AcceptFineKey))
                        {
                            if (Game.Player.Money >= ConfigManager.TicketFineAmount)
                            {
                                Game.Player.Money -= ConfigManager.TicketFineAmount;
                                Game.Player.WantedLevel = 0;
                                UI.Notify("~g~Multa de trânsito de $" + ConfigManager.TicketFineAmount + " paga com sucesso!\nVocê foi liberado com advertência.");
                                if (_copPed != null && _copPed.Exists())
                                {
                                    _copPed.Task.ClearAll();
                                    if (_copVehicle != null && _copVehicle.Exists())
                                    {
                                        _copPed.Task.EnterVehicle(_copVehicle, VehicleSeat.Driver);
                                    }
                                }
                                Reset();
                            }
                            else
                            {
                                UI.Notify("~r~Você não tem dinheiro suficiente para pagar a multa!");
                            }
                        }
                        // Option 2: Surrender
                        else if (Game.IsKeyPressed(ConfigManager.SurrenderKey))
                        {
                            player.Task.LeaveVehicle(playerVeh, false);
                            SurrenderSystem.TriggerSurrender(player, _copPed);
                            Reset();
                        }
                    }
                }
            }
        }

        private Ped FindClosestCopOnFoot(Vector3 pos, float radius)
        {
            Ped[] peds = World.GetNearbyPeds(pos, radius);
            Ped closest = null;
            float minD = float.MaxValue;
            foreach (var p in peds)
            {
                if (PoliceUtils.IsCop(p) && !p.IsInVehicle())
                {
                    float d = World.GetDistance(pos, p.Position);
                    if (d < minD)
                    {
                        minD = d;
                        closest = p;
                    }
                }
            }
            return closest;
        }

        public void Reset()
        {
            CurrentState = TrafficStopState.Idle;
            _copVehicle = null;
            _copPed = null;
        }
    }
}
