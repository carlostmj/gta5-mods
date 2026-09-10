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
        PursuingToStop,
        CopApproaching,
        AtDriverWindow,
        ResolvingFine
    }

    public class TrafficStopSystem
    {
        public TrafficStopState CurrentState = TrafficStopState.Idle;

        private Vehicle _copVehicle = null;
        private Ped _copPed = null;
        private int _stopStartTime = 0;
        private int _lastSpeedCheckTime = 0;
        private Blip _copBlip = null;

        public void OnTick()
        {
            if (!ConfigManager.EnableTrafficStops) return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists())
            {
                Reset();
                return;
            }

            switch (CurrentState)
            {
                case TrafficStopState.Idle:
                    CheckForTrafficInfractions(player);
                    break;

                case TrafficStopState.PursuingToStop:
                    HandlePursuingToStop(player);
                    break;

                case TrafficStopState.CopApproaching:
                    HandleCopApproaching(player);
                    break;

                case TrafficStopState.AtDriverWindow:
                    HandleAtDriverWindow(player);
                    break;
            }
        }

        private void CheckForTrafficInfractions(Ped player)
        {
            if (!player.IsInVehicle()) return;
            Vehicle playerVeh = player.CurrentVehicle;
            if (playerVeh == null || !playerVeh.Exists()) return;
            if (Game.Player.WantedLevel > 0) return;

            // Only check every 500ms for high performance
            int now = Game.GameTime;
            if (now - _lastSpeedCheckTime < 500) return;
            _lastSpeedCheckTime = now;

            float kmh = playerVeh.Speed * 3.6f;
            bool isSpeeding = kmh >= ConfigManager.SpeedingThresholdKMH;

            // Look for nearby police vehicles within 40m
            Vehicle[] nearbyVehs = World.GetNearbyVehicles(player.Position, 40.0f);
            foreach (var veh in nearbyVehs)
            {
                if (veh != null && veh.Exists() && veh.ClassType == VehicleClass.Emergency)
                {
                    Ped driver = veh.GetPedOnSeat(VehicleSeat.Driver);
                    if (driver != null && driver.Exists() && (driver.RelationshipGroup == 0x432D1DE1 || Function.Call<int>(Hash.GET_PED_TYPE, driver.Handle) == 6))
                    {
                        // Found a patrol car
                        if (isSpeeding)
                        {
                            InitiateTrafficStop(player, veh, driver, "Excesso de Velocidade (" + (int)kmh + " KM/H)");
                            return;
                        }
                    }
                }
            }
        }

        private void InitiateTrafficStop(Ped player, Vehicle copVeh, Ped copPed, string reason)
        {
            _copVehicle = copVeh;
            _copPed = copPed;
            _stopStartTime = Game.GameTime;
            CurrentState = TrafficStopState.PursuingToStop;

            // Siren on
            Function.Call(Hash.SET_VEHICLE_SIREN, _copVehicle.Handle, true);

            if (_copBlip != null && _copBlip.Exists()) _copBlip.Remove();
            _copBlip = _copVehicle.AddBlip();
            _copBlip.Color = BlipColor.Blue;
            _copBlip.IsFlashing = true;

            UI.Notify("~b~POLÍCIA DE LOS SANTOS:~w~\n" + reason + "!\nEncoste o veículo no acostamento e desligue o motor.");
        }

        private void HandlePursuingToStop(Ped player)
        {
            if (_copVehicle == null || !_copVehicle.Exists() || _copPed == null || !_copPed.Exists())
            {
                Reset();
                return;
            }

            // Check if player fled (drove away at speed)
            if (player.IsInVehicle() && player.CurrentVehicle.Speed > 18.0f && World.GetDistance(player.Position, _copVehicle.Position) > 60.0f)
            {
                UI.Notify("~r~Você desobedeceu a ordem de parada! Fuga em andamento!");
                Game.Player.WantedLevel = 2;
                Reset();
                return;
            }

            // Check timeout (player ignored for 30s)
            if (Game.GameTime - _stopStartTime > 30000)
            {
                Game.Player.WantedLevel = 2;
                Reset();
                return;
            }

            // Check if player vehicle has stopped
            if (player.IsInVehicle())
            {
                Vehicle playerVeh = player.CurrentVehicle;
                if (playerVeh.Speed < 1.0f)
                {
                    CurrentState = TrafficStopState.CopApproaching;

                    _copPed.Task.ParkVehicle(_copVehicle, playerVeh.Position - playerVeh.ForwardVector * 7.0f, playerVeh.Heading);
                    _copPed.Task.LeaveVehicle(_copVehicle, false);

                    Vector3 driverWindowPos = playerVeh.Position + playerVeh.RightVector * -1.3f;
                    _copPed.Task.GoTo(driverWindowPos);

                    UI.Notify("~b~O policial está se aproximando da janela do motorista...\nAguarde no veículo.");
                }
                else
                {
                    string prompt = "~y~ORDEM DE PARADA POLICIAL ~w~| Estacione o veículo no acostamento (Segure [S] ou [Espaço])";
                    new UIText(prompt, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 40), 0.45f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();
                }
            }
            else
            {
                CurrentState = TrafficStopState.CopApproaching;
                _copPed.Task.LeaveVehicle(_copVehicle, false);
                _copPed.Task.GoTo(player.Position + player.ForwardVector * 1.5f);
            }
        }

        private void HandleCopApproaching(Ped player)
        {
            if (_copPed == null || !_copPed.Exists())
            {
                Reset();
                return;
            }

            float dist = World.GetDistance(_copPed.Position, player.Position);
            if (dist < 2.5f)
            {
                CurrentState = TrafficStopState.AtDriverWindow;
                Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, _copPed.Handle, "WORLD_HUMAN_COP_IDLES", 0, true);
            }
            else
            {
                if (player.IsInVehicle() && player.CurrentVehicle.Speed > 8.0f)
                {
                    UI.Notify("~r~Fuga em flagrante! Perseguição iniciada!");
                    Game.Player.WantedLevel = 2;
                    Reset();
                }
            }
        }

        private void HandleAtDriverWindow(Ped player)
        {
            if (_copPed == null || !_copPed.Exists())
            {
                Reset();
                return;
            }

            string dialog = "~b~Oficial de Trânsito:~w~ \"Infração registrada.\"\nPressione ~g~[" + ConfigManager.AcceptFineKey.ToString() + "]~w~ Pagar Multa ($" + ConfigManager.TicketFineAmount + ") | ~r~[" + ConfigManager.SurrenderKey.ToString() + "]~w~ Descer e Render-se";
            new UIText(dialog, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 45), 0.45f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();

            if (Game.IsKeyPressed(ConfigManager.AcceptFineKey))
            {
                if (Game.Player.Money >= ConfigManager.TicketFineAmount)
                {
                    Game.Player.Money -= ConfigManager.TicketFineAmount;
                    UI.Notify("~g~Multa de trânsito de $" + ConfigManager.TicketFineAmount + " paga com sucesso.\nVocê foi liberado com uma advertência!");
                }
                else
                {
                    UI.Notify("~r~Você não tem dinheiro suficiente para a multa!");
                    Game.Player.WantedLevel = 1;
                }
                DismissCop();
                Reset();
            }
            else if (Game.IsKeyPressed(ConfigManager.SurrenderKey))
            {
                if (player.IsInVehicle())
                {
                    player.Task.LeaveVehicle(player.CurrentVehicle, false);
                }
                SurrenderSystem.TriggerSurrender(player, _copPed);
                Reset();
            }
            else if (player.IsInVehicle() && player.CurrentVehicle.Speed > 10.0f)
            {
                UI.Notify("~r~Fuga de abordagem policial!");
                Game.Player.WantedLevel = 2;
                Reset();
            }
        }

        private void DismissCop()
        {
            if (_copPed != null && _copPed.Exists())
            {
                _copPed.Task.ClearAll();
                if (_copVehicle != null && _copVehicle.Exists())
                {
                    _copPed.Task.EnterVehicle(_copVehicle, VehicleSeat.Driver);
                }
            }
            if (_copVehicle != null && _copVehicle.Exists())
            {
                Function.Call(Hash.SET_VEHICLE_SIREN, _copVehicle.Handle, false);
            }
        }

        public void Reset()
        {
            CurrentState = TrafficStopState.Idle;
            if (_copBlip != null && _copBlip.Exists())
            {
                _copBlip.Remove();
                _copBlip = null;
            }
            _copVehicle = null;
            _copPed = null;
        }
    }
}
