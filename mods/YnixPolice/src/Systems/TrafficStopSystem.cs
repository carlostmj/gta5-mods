using System;
using System.Drawing;
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
        CopDrivingToParkBehind,
        CopSteppingOut,
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
        private int _lastDriveTime = 0;
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

                case VehicleStopState.CopDrivingToParkBehind:
                    HandleCopDrivingToParkBehind(player);
                    break;

                case VehicleStopState.CopSteppingOut:
                    HandleCopSteppingOut(player);
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
                InitiateStop(player, "Infração de trânsito detectada");
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
                        Game.Player.WantedLevel = 1;
                        InitiateStop(player, "Excesso de velocidade (" + (int)kmh + " km/h)");
                    }
                }
            }
        }

        private void InitiateStop(Ped player, string reason)
        {
            Vehicle playerVeh = player.CurrentVehicle;
            if (playerVeh == null || !playerVeh.Exists()) return;

            // KEEP WantedLevel = 1 visible on HUD so the player sees the police star!
            Game.Player.WantedLevel = 1;

            // Disable dispatch of backup police units so GTA V DOES NOT flood the scene with other cruisers!
            SetDispatchServicesEnabled(false);

            // Suppress native lethal aggression from random cops
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);

            // Dismiss other random police units in the area so ONLY our cruiser conducts the stop
            DismissOtherPoliceUnits(player.Position, null);

            // Look for an existing cruiser that is ACTUALLY BEHIND player and driving in the SAME DIRECTION
            _copVehicle = FindValidCruiserBehind(playerVeh);

            // If no valid cruiser is behind in the same lane, spawn one cleanly 22m directly behind in the same lane!
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
                    Vector3 spawnPos = playerVeh.GetOffsetInWorldCoords(new Vector3(0f, -22.0f, 0f));
                    _copVehicle = World.CreateVehicle(copModel, spawnPos, playerVeh.Heading);
                    if (_copVehicle != null && _copVehicle.Exists())
                    {
                        _copVehicle.Speed = Math.Max(playerVeh.Speed * 0.9f, 5.0f);
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
                Reset();
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
                _copPed.Task.ClearAll();
            }

            if (_passengerPed != null && _passengerPed.Exists())
            {
                _passengerPed.BlockPermanentEvents = true;
                _passengerPed.Task.ClearAll();
                _passengerPed.Weapons.Select(WeaponHash.Unarmed, true);
            }

            // Turn on lights and sirens
            Function.Call(Hash.SET_VEHICLE_SIREN, _copVehicle.Handle, true);

            // Add map blip
            if (_copBlip != null && _copBlip.Exists()) _copBlip.Remove();
            _copBlip = _copVehicle.AddBlip();
            _copBlip.Color = BlipColor.Blue;
            _copBlip.IsFlashing = true;

            CurrentState = VehicleStopState.OrderingPullOver;
            _stopStartTime = Game.GameTime;
            _lastDriveTime = 0;

            // Clean GTA V notification
            UI.Notify("Polícia de Los Santos:\n" + reason + ".\nEncoste o veículo à direita e pare o carro.");
        }

        private Vehicle FindValidCruiserBehind(Vehicle playerVeh)
        {
            Vehicle[] vehs = World.GetNearbyVehicles(playerVeh.Position, 50.0f);
            Vehicle best = null;
            float bestDist = float.MaxValue;

            foreach (var v in vehs)
            {
                if (v != null && v.Exists() && v.ClassType == VehicleClass.Emergency)
                {
                    Vector3 toCop = v.Position - playerVeh.Position;
                    float dist = toCop.Length();
                    float dotHeading = Vector3.Dot(playerVeh.ForwardVector, v.ForwardVector);
                    float dotBehind = Vector3.Dot(playerVeh.ForwardVector, toCop);

                    // Crucial: Must be BEHIND (dotBehind < 0), in SAME DIRECTION (dotHeading > 0.4), and not on a different vertical level
                    if (dotBehind < -3.0f && dotHeading > 0.4f && Math.Abs(toCop.Z) < 3.0f && dist < bestDist)
                    {
                        bestDist = dist;
                        best = v;
                    }
                }
            }
            return best;
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

        private static void SetDispatchServicesEnabled(bool enable)
        {
            try
            {
                Function.Call(Hash.SET_DISPATCH_COPS_FOR_PLAYER, Game.Player, enable);
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

            // Check if player fled by driving away fast
            if (playerVeh.Speed > 20.0f && World.GetDistance(player.Position, _copVehicle.Position) > 60.0f)
            {
                _fledCheckCounter++;
                if (_fledCheckCounter > 20)
                {
                    UI.Notify("Você desobedeceu a ordem de parada! Perseguição armada iniciada.");
                    Game.Player.WantedLevel = 2;
                    Reset();
                    return;
                }
            }
            else
            {
                _fledCheckCounter = 0;
            }

            // When player has slowed down to a full stop
            if (playerVeh.Speed < 1.0f)
            {
                CurrentState = VehicleStopState.CopDrivingToParkBehind;
                _stopStartTime = Game.GameTime;
                _lastDriveTime = 0;

                UI.Notify("Veículo parado. A viatura policial está estacionando logo atrás.");
            }
            else
            {
                string hint = "Encoste no acostamento à direita e pare o veículo (Segure [S] ou [Espaço])";
                DrawStandardText(hint, UI.HEIGHT - 45);

                // Keep cop pursuing closely behind - THROTTLED to once every 2 seconds
                if (_copPed != null && _copPed.Exists() && (Game.GameTime - _lastDriveTime > 2000))
                {
                    _lastDriveTime = Game.GameTime;
                    Vector3 followPos = playerVeh.GetOffsetInWorldCoords(new Vector3(0f, -8.0f, 0f));
                    _copPed.Task.DriveTo(_copVehicle, followPos, 3.0f, 22.0f, 786603);
                }
            }
        }

        private void HandleCopDrivingToParkBehind(Ped player)
        {
            if (!player.IsInVehicle() || _copPed == null || !_copPed.Exists() || _copVehicle == null || !_copVehicle.Exists())
            {
                Reset();
                return;
            }

            Vehicle playerVeh = player.CurrentVehicle;
            Vector3 parkPos = playerVeh.GetOffsetInWorldCoords(new Vector3(0f, -5.5f, 0f));
            float distToPark = World.GetDistance(_copVehicle.Position, parkPos);
            float distToPlayer = World.GetDistance(_copVehicle.Position, playerVeh.Position);

            // Re-assert drive command ONLY every 2.5s (style 786603 = straight standard driving)
            if (Game.GameTime - _lastDriveTime > 2500)
            {
                _lastDriveTime = Game.GameTime;
                if (distToPark > 2.5f)
                {
                    _copPed.Task.DriveTo(_copVehicle, parkPos, 2.0f, 8.0f, 786603);
                }
            }

            // ARRIVAL CONDITIONS:
            // 1. Reached close to target park spot (< 3.5m)
            // 2. OR within 7.5m of player vehicle and moving slowly (< 2.0 m/s)
            // 3. OR 4.5 seconds elapsed and within 10m of player
            bool arrived = (distToPark < 3.5f) || 
                           (distToPlayer < 7.5f && _copVehicle.Speed < 2.0f) ||
                           ((Game.GameTime - _stopStartTime > 4500) && distToPlayer < 10.0f);

            if (arrived)
            {
                // Cruiser has arrived directly behind player!
                _copVehicle.Speed = 0f;
                _copVehicle.HandbrakeOn = true;
                Function.Call(Hash.SET_VEHICLE_HANDBRAKE, _copVehicle.Handle, true);
                _copPed.Task.ClearAll();

                // Mute siren audio but keep flashing lights
                try { _copVehicle.IsSirenSilent = true; } catch { }

                // Cop smoothly opens door and exits the vehicle (NO warping, NO clearAllImmediately)
                _copPed.Task.LeaveVehicle(_copVehicle, false);

                // If passenger exists, passenger also steps out to cover
                if (_passengerPed != null && _passengerPed.Exists())
                {
                    _passengerPed.Task.LeaveVehicle(_copVehicle, false);
                }

                CurrentState = VehicleStopState.CopSteppingOut;
                _stopStartTime = Game.GameTime;

                UI.Notify("O oficial está desembarcando da viatura.");
            }
            else
            {
                // If taking more than 8 seconds (e.g. slight obstruction), force arrival
                if (Game.GameTime - _stopStartTime > 8000 && distToPlayer < 15.0f)
                {
                    _copVehicle.Speed = 0f;
                    _copVehicle.HandbrakeOn = true;
                    _copPed.Task.ClearAll();
                    _copPed.Task.LeaveVehicle(_copVehicle, false);
                    CurrentState = VehicleStopState.CopSteppingOut;
                    _stopStartTime = Game.GameTime;
                    return;
                }

                DrawStandardText("Aguarde a viatura policial estacionar atrás do seu veículo...", UI.HEIGHT - 45);
            }
        }

        private void HandleCopSteppingOut(Ped player)
        {
            if (!player.IsInVehicle() || _copPed == null || !_copPed.Exists())
            {
                Reset();
                return;
            }

            // Wait until cop has physically stepped out on his feet
            if (!_copPed.IsInVehicle())
            {
                CurrentState = VehicleStopState.CopWalkingToWindow;
                _stopStartTime = Game.GameTime;
                _lastCopWalkTime = 0;

                // Walk straight to driver window
                Vehicle playerVeh = player.CurrentVehicle;
                Vector3 driverWindow = playerVeh.GetOffsetInWorldCoords(new Vector3(-1.35f, 0.35f, 0f));
                Function.Call(Hash.TASK_GO_TO_COORD_ANY_MEANS, _copPed.Handle, driverWindow.X, driverWindow.Y, driverWindow.Z, 1.25f, 0, 0, 786603, 0xbf800000);
            }
            else
            {
                DrawStandardText("Aguarde o oficial se aproximar...", UI.HEIGHT - 45);
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
                UI.Notify("Fuga em flagrante! Perseguição armada iniciada!");
                Game.Player.WantedLevel = 2;
                Reset();
                return;
            }

            Vehicle playerVeh = player.CurrentVehicle;
            Vector3 driverWindow = playerVeh.GetOffsetInWorldCoords(new Vector3(-1.35f, 0.35f, 0f));
            float dist = World.GetDistance(_copPed.Position, driverWindow);

            // Re-issue walking only every 3.5s so cop doesn't stutter
            if (Game.GameTime - _lastCopWalkTime > 3500)
            {
                _lastCopWalkTime = Game.GameTime;
                Function.Call(Hash.TASK_GO_TO_COORD_ANY_MEANS, _copPed.Handle, driverWindow.X, driverWindow.Y, driverWindow.Z, 1.25f, 0, 0, 786603, 0xbf800000);
            }

            // STRICT CHECK: Cop MUST physically reach within 1.7m of driver window!
            // NO TIMEOUT THAT FIRES FROM 30 METERS AWAY!
            if (dist <= 1.7f)
            {
                CurrentState = VehicleStopState.DialogAtWindow;
                _copPed.Task.ClearAll();
                Function.Call(Hash.TASK_TURN_PED_TO_FACE_ENTITY, _copPed.Handle, player.Handle, 800);
                Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, _copPed.Handle, "WORLD_HUMAN_COP_IDLES", 0, true);
            }
            else
            {
                // If after 25s cop NEVER reached (stuck), cancel cleanly instead of faking dialogue
                if (Game.GameTime - _stopStartTime > 25000)
                {
                    UI.Notify("O oficial não conseguiu alcançar a sua janela.");
                    Reset();
                    return;
                }

                DrawStandardText("Oficial se aproximando da janela do motorista...", UI.HEIGHT - 45);
            }
        }

        private void HandleDialogAtWindow(Ped player)
        {
            if (_copPed == null || !_copPed.Exists())
            {
                Reset();
                return;
            }

            // STRICT CHECK: If player stepped out or was knocked down, cancel window dialog immediately!
            if (!player.IsInVehicle() || player.IsDead || player.IsRagdoll)
            {
                Reset();
                return;
            }

            // Check if player fled by accelerating
            if (player.CurrentVehicle.Speed > 7.0f)
            {
                UI.Notify("Fuga de fiscalização policial! Reforços acionados.");
                Game.Player.WantedLevel = 2;
                Reset();
                return;
            }

            // CLEAN GTA V STANDARD SUBTITLES
            string dialogLine1 = "Oficial: \"Boa tarde senhor. Documentos do veículo e habilitação, por favor.\"";
            string dialogLine2 = string.Format("Pressione [{0}] Pagar Multa (${1}) | [E] Pedir Advertência | [{2}] Descer e Render-se",
                ConfigManager.AcceptFineKey.ToString(), ConfigManager.TicketFineAmount, ConfigManager.SurrenderKey.ToString());

            DrawStandardSubtitle(dialogLine1, UI.HEIGHT - 75);
            DrawStandardHint(dialogLine2, UI.HEIGHT - 45);

            // Option 1: Hand over documents & Pay fine
            if (Game.IsKeyPressed(ConfigManager.AcceptFineKey))
            {
                if (Game.Player.Money >= ConfigManager.TicketFineAmount)
                {
                    Game.Player.Money -= ConfigManager.TicketFineAmount;

                    // Clear wanted level upon legitimate payment!
                    Game.Player.WantedLevel = 0;

                    PoliceUtils.PlayPaperSound();
                    UI.Notify("Documentos entregues. Multa de $" + ConfigManager.TicketFineAmount + " quitada.\nOficial: \"Tudo em ordem. Dirija com atenção e tenha um bom dia!\"");

                    CurrentState = VehicleStopState.CopReturningToCar;
                    ReturnCopToCar();
                }
                else
                {
                    UI.Notify("Você não possui dinheiro suficiente para pagar a multa.");
                }
            }
            // Option 2: Request verbal warning (50% chance)
            else if (Game.IsKeyPressed(Keys.E))
            {
                Random rnd = new Random();
                if (rnd.Next(100) < 50)
                {
                    // Warning granted: clear wanted level!
                    Game.Player.WantedLevel = 0;

                    PoliceUtils.PlayPaperSound();
                    UI.Notify("Oficial: \"Vou liberar apenas com uma advertência verbal desta vez. Dirija devagar!\"");
                    CurrentState = VehicleStopState.CopReturningToCar;
                    ReturnCopToCar();
                }
                else
                {
                    UI.Notify("Oficial: \"Sem justificativa hoje, senhor. A infração é clara e a multa é devida.\"");
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
            if (_passengerPed != null && _passengerPed.Exists())
            {
                _passengerPed.Task.ClearAll();
                if (_copVehicle != null && _copVehicle.Exists())
                {
                    _passengerPed.Task.EnterVehicle(_copVehicle, VehicleSeat.Passenger);
                }
            }
            _stopStartTime = Game.GameTime;
        }

        private void HandleCopReturning(Ped player)
        {
            // Wait for cop to enter vehicle, then turn off sirens and finish
            if (Game.GameTime - _stopStartTime > 4500)
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

            // Re-enable backup dispatches
            SetDispatchServicesEnabled(true);

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

        // Standard GTA V clean subtitles and text rendering
        private static void DrawStandardSubtitle(string text, int yPos)
        {
            new UIText(text, new Point(UI.WIDTH / 2, yPos), 0.44f, Color.White, GTA.Font.ChaletComprimeCologne, true, true, true).Draw();
        }

        private static void DrawStandardHint(string text, int yPos)
        {
            new UIText(text, new Point(UI.WIDTH / 2, yPos), 0.40f, Color.FromArgb(230, 230, 230), GTA.Font.ChaletComprimeCologne, true, true, true).Draw();
        }

        private static void DrawStandardText(string text, int yPos)
        {
            new UIText(text, new Point(UI.WIDTH / 2, yPos), 0.42f, Color.White, GTA.Font.ChaletComprimeCologne, true, true, true).Draw();
        }
    }
}
