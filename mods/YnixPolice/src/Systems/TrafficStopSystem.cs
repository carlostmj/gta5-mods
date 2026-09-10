using System;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using YnixPolice.Core;

namespace YnixPolice.Systems
{
    public enum VehicleStopState
    {
        Idle,
        OrderingPullOver,
        CopParkingBehind,
        CopWalkingToWindow,
        DialogAtWindow,
        CopReturningToCar
    }

    public class TrafficStopSystem
    {
        public VehicleStopState CurrentState = VehicleStopState.Idle;

        private Vehicle _copVehicle = null;
        private Ped _copPed = null;
        private int _stopStartTime = 0;
        private int _lastCheckTime = 0;
        private Blip _copBlip = null;
        private int _fledCheckCounter = 0;

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
                case VehicleStopState.Idle:
                    CheckForTrafficViolations(player);
                    break;

                case VehicleStopState.OrderingPullOver:
                    HandleOrderingPullOver(player);
                    break;

                case VehicleStopState.CopParkingBehind:
                    HandleCopParkingBehind(player);
                    break;

                case VehicleStopState.CopWalkingToWindow:
                    HandleCopWalkingToWindow(player);
                    break;

                case VehicleStopState.DialogAtWindow:
                    HandleDialogAtWindow(player);
                    break;

                case VehicleStopState.CopReturningToCar:
                    HandleCopReturning(player);
                    break;
            }
        }

        private void CheckForTrafficViolations(Ped player)
        {
            if (!player.IsInVehicle()) return;

            int now = Game.GameTime;
            if (now - _lastCheckTime < 800) return;
            _lastCheckTime = now;

            // Trigger when player has 1 star OR speeds near police
            if (Game.Player.WantedLevel == 1)
            {
                // Find or spawn police cruiser behind player
                InitiateStop(player, "Infração de Trânsito Detectada");
            }
            else if (Game.Player.WantedLevel == 0)
            {
                Vehicle playerVeh = player.CurrentVehicle;
                if (playerVeh != null && playerVeh.Exists())
                {
                    float kmh = playerVeh.Speed * 3.6f;
                    if (kmh >= ConfigManager.SpeedingThresholdKMH)
                    {
                        // Check if a police vehicle is within 50m
                        Vehicle copVeh = PoliceUtils.FindClosestPoliceVehicle(player.Position, 50.0f);
                        if (copVeh != null)
                        {
                            Game.Player.WantedLevel = 1;
                            InitiateStop(player, "Excesso de Velocidade (" + (int)kmh + " KM/H)");
                        }
                    }
                }
            }
        }

        private void InitiateStop(Ped player, string reason)
        {
            Vehicle playerVeh = player.CurrentVehicle;
            if (playerVeh == null || !playerVeh.Exists()) return;

            // Find closest police cruiser
            _copVehicle = PoliceUtils.FindClosestPoliceVehicle(player.Position, 60.0f);

            // If no police cruiser nearby, dynamically spawn one 45m behind player!
            if (_copVehicle == null)
            {
                Model copModel = new Model("police3");
                copModel.Request(1500);
                if (copModel.IsInCdImage && copModel.IsValid)
                {
                    Vector3 spawnPos = playerVeh.Position - playerVeh.ForwardVector * 45.0f;
                    _copVehicle = World.CreateVehicle(copModel, spawnPos, playerVeh.Heading);
                    if (_copVehicle != null && _copVehicle.Exists())
                    {
                        Model copPedModel = new Model("s_m_y_cop_01");
                        copPedModel.Request(1500);
                        if (copPedModel.IsInCdImage && copPedModel.IsValid)
                        {
                            _copPed = _copVehicle.CreatePedOnSeat(VehicleSeat.Driver, copPedModel);
                        }
                        copPedModel.MarkAsNoLongerNeeded();
                    }
                }
                copModel.MarkAsNoLongerNeeded();
            }
            else
            {
                _copPed = _copVehicle.GetPedOnSeat(VehicleSeat.Driver);
            }

            if (_copVehicle == null || !_copVehicle.Exists()) return;

            // Turn on lights and sirens
            Function.Call(Hash.SET_VEHICLE_SIREN, _copVehicle.Handle, true);

            // Add blip
            if (_copBlip != null && _copBlip.Exists()) _copBlip.Remove();
            _copBlip = _copVehicle.AddBlip();
            _copBlip.Color = BlipColor.Blue;
            _copBlip.IsFlashing = true;

            CurrentState = VehicleStopState.OrderingPullOver;
            _stopStartTime = Game.GameTime;

            UI.Notify("~b~POLÍCIA DE LOS SANTOS:~w~\n" + reason + "!\nEncoste o veículo no acostamento à direita e desligue o motor.");
        }

        private void HandleOrderingPullOver(Ped player)
        {
            if (!player.IsInVehicle() || _copVehicle == null || !_copVehicle.Exists())
            {
                Reset();
                return;
            }

            Vehicle playerVeh = player.CurrentVehicle;

            // Check if player speeds away (fleeing)
            if (playerVeh.Speed > 22.0f && World.GetDistance(player.Position, _copVehicle.Position) > 65.0f)
            {
                _fledCheckCounter++;
                if (_fledCheckCounter > 20)
                {
                    UI.Notify("~r~Você desobedeceu a ordem de parada! Perseguição iniciada!");
                    Game.Player.WantedLevel = 2;
                    Reset();
                    return;
                }
            }
            else
            {
                _fledCheckCounter = 0;
            }

            // If player slowed down and stopped
            if (playerVeh.Speed < 1.0f)
            {
                CurrentState = VehicleStopState.CopParkingBehind;
                _stopStartTime = Game.GameTime;

                // Command cop car to pull up and park directly behind player (6 meters behind)
                Vector3 parkPos = playerVeh.Position - playerVeh.ForwardVector * 6.5f;
                if (_copPed != null && _copPed.Exists())
                {
                    _copPed.Task.ParkVehicle(_copVehicle, parkPos, playerVeh.Heading);
                }

                UI.Notify("~b~Veículo encostado.~w~ A viatura policial está estacionando logo atrás de você.");
            }
            else
            {
                string hint = "~y~ORDEM DE PARADA POLICIAL ~w~| Encoste no acostamento à direita (Segure [S] ou [Espaço])";
                new UIText(hint, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 40), 0.45f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();

                // Keep cop pursuing closely behind
                if (_copPed != null && _copPed.Exists())
                {
                    _copPed.Task.DriveTo(_copVehicle, playerVeh.Position - playerVeh.ForwardVector * 10.0f, 6.0f, 25.0f, 786468);
                }
            }
        }

        private void HandleCopParkingBehind(Ped player)
        {
            if (_copPed == null || !_copPed.Exists() || _copVehicle == null || !_copVehicle.Exists())
            {
                Reset();
                return;
            }

            float distToCop = World.GetDistance(_copVehicle.Position, player.Position);

            // Wait until cop vehicle is close and stopped behind player, or 3 seconds elapsed
            if (distToCop < 12.0f && (_copVehicle.Speed < 1.0f || (Game.GameTime - _stopStartTime > 3000)))
            {
                CurrentState = VehicleStopState.CopWalkingToWindow;

                // Cop leaves car and closes door
                _copPed.Task.LeaveVehicle(_copVehicle, false);

                // Walk to driver side window
                Vehicle playerVeh = player.CurrentVehicle;
                Vector3 driverWindow = playerVeh.Position + playerVeh.RightVector * -1.35f;
                _copPed.Task.GoTo(driverWindow);

                UI.Notify("~b~O oficial desceu da viatura e está caminhando até a sua janela...~w~\nAguarde no banco do motorista.");
            }
            else
            {
                string wText = "~b~Viatura policial manobrando atrás do seu carro...~w~";
                new UIText(wText, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 40), 0.45f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();
            }
        }

        private void HandleCopWalkingToWindow(Ped player)
        {
            if (_copPed == null || !_copPed.Exists())
            {
                Reset();
                return;
            }

            // If player steps on gas while cop is walking: FLED!
            if (player.IsInVehicle() && player.CurrentVehicle.Speed > 6.0f)
            {
                UI.Notify("~r~Fuga em flagrante! Perseguição armada iniciada!");
                Game.Player.WantedLevel = 2;
                Reset();
                return;
            }

            Vehicle playerVeh = player.CurrentVehicle;
            Vector3 driverWindow = playerVeh != null ? (playerVeh.Position + playerVeh.RightVector * -1.35f) : player.Position;
            float dist = World.GetDistance(_copPed.Position, driverWindow);

            if (dist < 2.0f)
            {
                CurrentState = VehicleStopState.DialogAtWindow;
                _copPed.Task.ClearAllImmediately();
                _copPed.Task.TurnTo(player, 800);
                Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, _copPed.Handle, "WORLD_HUMAN_COP_IDLES", 0, true);
            }
            else
            {
                _copPed.Task.GoTo(driverWindow);
                string waitNotice = "~b~Oficial se aproximando da janela do motorista...~w~";
                new UIText(waitNotice, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 40), 0.45f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();
            }
        }

        private void HandleDialogAtWindow(Ped player)
        {
            if (_copPed == null || !_copPed.Exists())
            {
                Reset();
                return;
            }

            // Check if player fled by accelerating
            if (player.IsInVehicle() && player.CurrentVehicle.Speed > 7.0f)
            {
                UI.Notify("~r~Fuga de fiscalização policial! Reforços acionados!");
                Game.Player.WantedLevel = 2;
                Reset();
                return;
            }

            // Interactive dialogue box on screen
            string dialogLine1 = "~b~Oficial:~w~ \\\"Boa tarde senhor. Documentos do veículo e habilitação, por favor.\\\"";
            string dialogLine2 = string.Format("Pressione ~g~[{0}]~w~ Entregar Docs & Pagar Multa (${1}) | ~y~[E]~w~ Pedir Advertência | ~r~[{2}]~w~ Descer e Render-se",
                ConfigManager.AcceptFineKey.ToString(), ConfigManager.TicketFineAmount, ConfigManager.SurrenderKey.ToString());

            new UIText(dialogLine1, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 70), 0.45f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();
            new UIText(dialogLine2, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 40), 0.42f, System.Drawing.Color.FromArgb(240, 240, 240), GTA.Font.ChaletComprimeCologne, true, false, true).Draw();

            // Option 1: Hand over documents & Pay fine
            if (Game.IsKeyPressed(ConfigManager.AcceptFineKey))
            {
                if (Game.Player.Money >= ConfigManager.TicketFineAmount)
                {
                    Game.Player.Money -= ConfigManager.TicketFineAmount;
                    Game.Player.WantedLevel = 0;

                    PoliceUtils.PlayPaperSound();
                    UI.Notify("~g~Documentos entregues! Multa de $" + ConfigManager.TicketFineAmount + " quitada.\\n~w~Oficial: \\\"Tudo certo. Dirija com atenção e tenha um bom dia!\\\"");

                    CurrentState = VehicleStopState.CopReturningToCar;
                    ReturnCopToCar();
                }
                else
                {
                    UI.Notify("~r~Você não tem dinheiro suficiente para pagar a multa!");
                }
            }
            // Option 2: Request verbal warning (50% chance)
            else if (Game.IsKeyPressed(Keys.E))
            {
                Random rnd = new Random();
                if (rnd.Next(100) < 50)
                {
                    Game.Player.WantedLevel = 0;
                    PoliceUtils.PlayPaperSound();
                    UI.Notify("~g~Oficial: \\\"Vou deixar passar apenas como uma advertência desta vez. Não repita isso!\\\"");
                    CurrentState = VehicleStopState.CopReturningToCar;
                    ReturnCopToCar();
                }
                else
                {
                    UI.Notify("~r~Oficial: \\\"Sem desculpas hoje, senhor. A infração é grave e a multa é obrigatória.\\\"");
                }
            }
            // Option 3: Step out and surrender
            else if (Game.IsKeyPressed(ConfigManager.SurrenderKey))
            {
                if (player.IsInVehicle())
                {
                    player.Task.LeaveVehicle(player.CurrentVehicle, false);
                }
                SurrenderSystem.StartSurrender(player, _copPed);
                Reset();
            }
        }

        private void ReturnCopToCar()
        {
            if (_copPed != null && _copPed.Exists())
            {
                _copPed.Task.ClearAll();
                if (_copVehicle != null && _copVehicle.Exists())
                {
                    _copPed.Task.EnterVehicle(_copVehicle, VehicleSeat.Driver);
                }
            }
            _stopStartTime = Game.GameTime;
        }

        private void HandleCopReturning(Ped player)
        {
            // Wait for cop to get in car, then turn off sirens and finish
            if (Game.GameTime - _stopStartTime > 4000)
            {
                if (_copVehicle != null && _copVehicle.Exists())
                {
                    Function.Call(Hash.SET_VEHICLE_SIREN, _copVehicle.Handle, false);
                }
                Reset();
            }
        }

        public void Reset()
        {
            CurrentState = VehicleStopState.Idle;
            _fledCheckCounter = 0;
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
