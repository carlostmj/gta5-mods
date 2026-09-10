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
        private Ped _passengerPed = null;
        private int _stopStartTime = 0;
        private int _lastCheckTime = 0;
        private int _lastCopWalkTime = 0;
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

            Vehicle playerVeh = player.CurrentVehicle;
            if (playerVeh == null || !playerVeh.Exists()) return;

            // Trigger traffic stop when player is in vehicle and has 1 wanted star
            if (Game.Player.WantedLevel == 1)
            {
                InitiateStop(player, "Infração de Trânsito Detectada");
            }
            // Or when driving at high speed near police with 0 stars
            else if (Game.Player.WantedLevel == 0)
            {
                float kmh = playerVeh.Speed * 3.6f;
                if (kmh >= ConfigManager.SpeedingThresholdKMH)
                {
                    Vehicle copVeh = PoliceUtils.FindClosestPoliceVehicle(player.Position, 55.0f);
                    if (copVeh != null)
                    {
                        InitiateStop(player, "Excesso de Velocidade (" + (int)kmh + " KM/H)");
                    }
                }
            }
        }

        private void InitiateStop(Ped player, string reason)
        {
            Vehicle playerVeh = player.CurrentVehicle;
            if (playerVeh == null || !playerVeh.Exists()) return;

            // Zero out native wanted level during the traffic stop so GTA V's native dispatch
            // DOES NOT swarm the scene with multiple other police cars!
            Game.Player.WantedLevel = 0;
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);

            // Find closest police cruiser
            _copVehicle = PoliceUtils.FindClosestPoliceVehicle(player.Position, 75.0f);

            // If no police cruiser nearby, dynamically spawn one safely behind on the road!
            if (_copVehicle == null || !_copVehicle.Exists())
            {
                Model copModel = new Model(VehicleHash.Police3);
                if (!copModel.IsLoaded) copModel.Request(1200);

                if (!copModel.IsLoaded)
                {
                    copModel = new Model(VehicleHash.Police);
                    if (!copModel.IsLoaded) copModel.Request(1200);
                }

                if (copModel.IsLoaded)
                {
                    Vector3 targetBehind = playerVeh.Position - playerVeh.ForwardVector * 40.0f;
                    Vector3 spawnPos = World.GetNextPositionOnStreet(targetBehind);
                    if (spawnPos == Vector3.Zero) spawnPos = targetBehind;

                    _copVehicle = World.CreateVehicle(copModel, spawnPos, playerVeh.Heading);
                    if (_copVehicle != null && _copVehicle.Exists())
                    {
                        Model pedModel = new Model(PedHash.Cop01SMY);
                        if (!pedModel.IsLoaded) pedModel.Request(1200);
                        if (pedModel.IsLoaded)
                        {
                            _copPed = _copVehicle.CreatePedOnSeat(VehicleSeat.Driver, pedModel);
                        }
                        pedModel.MarkAsNoLongerNeeded();
                    }
                }
                copModel.MarkAsNoLongerNeeded();
            }
            else
            {
                _copPed = _copVehicle.GetPedOnSeat(VehicleSeat.Driver);
                _passengerPed = _copVehicle.GetPedOnSeat(VehicleSeat.Passenger);
            }

            if (_copVehicle == null || !_copVehicle.Exists())
            {
                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
                return;
            }

            // Ensure cop driver exists and is in controlled mode
            if (_copPed == null || !_copPed.Exists())
            {
                Model pedModel = new Model(PedHash.Cop01SMY);
                if (!pedModel.IsLoaded) pedModel.Request(1200);
                if (pedModel.IsLoaded)
                {
                    _copPed = _copVehicle.CreatePedOnSeat(VehicleSeat.Driver, pedModel);
                }
                pedModel.MarkAsNoLongerNeeded();
            }

            if (_copPed != null && _copPed.Exists())
            {
                _copPed.BlockPermanentEvents = true;
                _copPed.Task.ClearAllImmediately();
            }

            // If there is a passenger cop, calm him down so he doesn't shoot
            if (_passengerPed != null && _passengerPed.Exists())
            {
                _passengerPed.BlockPermanentEvents = true;
                _passengerPed.Task.ClearAllImmediately();
                _passengerPed.Weapons.Select(WeaponHash.Unarmed, true);
            }

            // Dismiss any OTHER nearby police vehicles so only this car conducts the stop!
            DismissOtherPoliceUnits(player.Position, _copVehicle);

            // Turn on lights and sirens
            Function.Call(Hash.SET_VEHICLE_SIREN, _copVehicle.Handle, true);

            // Add map blip
            if (_copBlip != null && _copBlip.Exists()) _copBlip.Remove();
            _copBlip = _copVehicle.AddBlip();
            _copBlip.Color = BlipColor.Blue;
            _copBlip.IsFlashing = true;

            CurrentState = VehicleStopState.OrderingPullOver;
            _stopStartTime = Game.GameTime;

            UI.Notify("~b~POLÍCIA DE LOS SANTOS:~w~\n" + reason + "!\nEncoste o veículo no acostamento à direita e pare o carro.");
        }

        private void DismissOtherPoliceUnits(Vector3 playerPos, Vehicle stopVeh)
        {
            try
            {
                Vehicle[] vehs = World.GetNearbyVehicles(playerPos, 120.0f);
                foreach (var v in vehs)
                {
                    if (v != null && v.Exists() && v != stopVeh && v.ClassType == VehicleClass.Emergency)
                    {
                        Ped driver = v.GetPedOnSeat(VehicleSeat.Driver);
                        if (driver != null && driver.Exists() && PoliceUtils.IsCop(driver))
                        {
                            driver.BlockPermanentEvents = true;
                            driver.Task.ClearAll();
                            driver.Task.CruiseWithVehicle(v, 14.0f, 786468);
                            Function.Call(Hash.SET_VEHICLE_SIREN, v.Handle, false);
                        }
                    }
                }
            }
            catch { }
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
            if (playerVeh.Speed > 20.0f && World.GetDistance(player.Position, _copVehicle.Position) > 60.0f)
            {
                _fledCheckCounter++;
                if (_fledCheckCounter > 20)
                {
                    UI.Notify("~r~Você desobedeceu a ordem de parada! Perseguição armada iniciada!");
                    Game.Player.WantedLevel = 2;
                    Reset();
                    return;
                }
            }
            else
            {
                _fledCheckCounter = 0;
            }

            // If player slowed down to a stop
            if (playerVeh.Speed < 1.2f)
            {
                CurrentState = VehicleStopState.CopParkingBehind;
                _stopStartTime = Game.GameTime;

                // Tell cop vehicle to pull up behind player
                Vector3 parkPos = playerVeh.GetOffsetInWorldCoords(new Vector3(0f, -7.0f, 0f));
                if (_copPed != null && _copPed.Exists())
                {
                    _copPed.Task.ClearAllImmediately();
                    _copPed.Task.DriveTo(_copVehicle, parkPos, 2.5f, 15.0f, 786468);
                }

                UI.Notify("~b~Veículo parado.~w~ A viatura policial está estacionando logo atrás de você.");
            }
            else
            {
                string hint = "~y~ORDEM DE PARADA POLICIAL ~w~| Encoste no acostamento à direita e pare (Segure [S] / [Espaço])";
                new UIText(hint, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 40), 0.45f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();

                // Keep cop pursuing closely behind
                if (_copPed != null && _copPed.Exists())
                {
                    Vector3 followPos = playerVeh.GetOffsetInWorldCoords(new Vector3(0f, -10.0f, 0f));
                    _copPed.Task.DriveTo(_copVehicle, followPos, 5.0f, 28.0f, 786468);
                }
            }
        }

        private void HandleCopParkingBehind(Ped player)
        {
            if (!player.IsInVehicle() || _copPed == null || !_copPed.Exists() || _copVehicle == null || !_copVehicle.Exists())
            {
                Reset();
                return;
            }

            float distToCop = World.GetDistance(_copVehicle.Position, player.Position);

            // Wait until cop vehicle is close (< 13m) and stopped, or 3.5s elapsed
            if (distToCop < 14.0f && (_copVehicle.Speed < 1.0f || (Game.GameTime - _stopStartTime > 3500)))
            {
                CurrentState = VehicleStopState.CopWalkingToWindow;
                _stopStartTime = Game.GameTime;
                _lastCopWalkTime = 0;

                // Cop steps out of cruiser
                _copPed.Task.ClearAllImmediately();
                _copPed.Task.LeaveVehicle(_copVehicle, false);

                Vehicle playerVeh = player.CurrentVehicle;
                Vector3 driverWindow = playerVeh.GetOffsetInWorldCoords(new Vector3(-1.35f, 0.4f, 0f));
                Function.Call(Hash.TASK_GO_TO_COORD_ANY_MEANS, _copPed.Handle, driverWindow.X, driverWindow.Y, driverWindow.Z, 1.2f, 0, 0, 786603, 0xbf800000);

                UI.Notify("~b~O policial desceu da viatura e está caminhando até a sua janela...~w~\nAguarde no banco do motorista.");
            }
            else
            {
                string wText = "~b~Viatura policial estacionando logo atrás do seu carro...~w~";
                new UIText(wText, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 40), 0.45f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();
            }
        }

        private void HandleCopWalkingToWindow(Ped player)
        {
            if (!player.IsInVehicle() || player.IsDead || player.IsRagdoll || _copPed == null || !_copPed.Exists())
            {
                Reset();
                return;
            }

            // If player steps on gas while cop is walking: FLED!
            if (player.CurrentVehicle.Speed > 6.0f)
            {
                UI.Notify("~r~Fuga em flagrante! Perseguição armada iniciada!");
                Game.Player.WantedLevel = 2;
                Reset();
                return;
            }

            Vehicle playerVeh = player.CurrentVehicle;
            Vector3 driverWindow = playerVeh.GetOffsetInWorldCoords(new Vector3(-1.35f, 0.4f, 0f));
            float dist = World.GetDistance(_copPed.Position, driverWindow);

            // Re-issue walking only every 3.5s so cop doesn't freeze
            if (Game.GameTime - _lastCopWalkTime > 3500)
            {
                _lastCopWalkTime = Game.GameTime;
                Function.Call(Hash.TASK_GO_TO_COORD_ANY_MEANS, _copPed.Handle, driverWindow.X, driverWindow.Y, driverWindow.Z, 1.2f, 0, 0, 786603, 0xbf800000);
            }

            if (dist < 2.2f || (Game.GameTime - _stopStartTime > 14000))
            {
                CurrentState = VehicleStopState.DialogAtWindow;
                _copPed.Task.ClearAllImmediately();
                Function.Call(Hash.TASK_TURN_PED_TO_FACE_ENTITY, _copPed.Handle, player.Handle, 800);
                Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, _copPed.Handle, "WORLD_HUMAN_COP_IDLES", 0, true);
            }
            else
            {
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

            // STRICT CHECK: If player stepped out of vehicle or is knocked down, stop the window dialog immediately!
            if (!player.IsInVehicle() || player.IsDead || player.IsRagdoll)
            {
                Reset();
                return;
            }

            // Check if player fled by accelerating
            if (player.CurrentVehicle.Speed > 7.0f)
            {
                UI.Notify("~r~Fuga de fiscalização policial! Reforços acionados!");
                Game.Player.WantedLevel = 2;
                Reset();
                return;
            }

            // Interactive dialogue box on screen
            string dialogLine1 = "~b~Oficial:~w~ \"Boa tarde senhor. Documentos do veículo e habilitação, por favor.\"";
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
                    UI.Notify("~g~Documentos entregues! Multa de $" + ConfigManager.TicketFineAmount + " quitada.\n~w~Oficial: \"Tudo certo. Dirija com atenção e tenha um bom dia!\"");

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
                    UI.Notify("~g~Oficial: \"Vou deixar passar apenas como uma advertência desta vez. Não repita isso!\"");
                    CurrentState = VehicleStopState.CopReturningToCar;
                    ReturnCopToCar();
                }
                else
                {
                    UI.Notify("~r~Oficial: \"Sem desculpas hoje, senhor. A infração é grave e a multa é obrigatória.\"");
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
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);

            if (_copBlip != null && _copBlip.Exists())
            {
                _copBlip.Remove();
                _copBlip = null;
            }
            if (_copPed != null && _copPed.Exists())
            {
                _copPed.BlockPermanentEvents = false;
            }
            if (_passengerPed != null && _passengerPed.Exists())
            {
                _passengerPed.BlockPermanentEvents = false;
            }
            _copVehicle = null;
            _copPed = null;
            _passengerPed = null;
        }
    }
}
