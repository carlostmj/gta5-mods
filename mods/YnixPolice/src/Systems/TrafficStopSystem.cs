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
        PlayerExitingToSurrender,
        ArrestingPlayer,
        FadingToStation,
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

                case VehicleStopState.PlayerExitingToSurrender:
                    HandlePlayerExitingToSurrender(player);
                    break;

                case VehicleStopState.ArrestingPlayer:
                    HandleArrestingPlayer(player);
                    break;

                case VehicleStopState.FadingToStation:
                    HandleFadingToStation(player);
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

            // REALISM CHECK: Only initiate stop if an actual police cruiser is already within 70m!
            if (Game.Player.WantedLevel == 1)
            {
                Vehicle nearbyCop = PoliceUtils.FindClosestPoliceVehicle(player.Position, 70.0f);
                if (nearbyCop != null && nearbyCop.Exists())
                {
                    InitiateStop(player, "Infração de trânsito detectada", nearbyCop);
                }
            }
            // Or when driving at high speed near police with 0 stars
            else if (Game.Player.WantedLevel == 0)
            {
                float kmh = playerVeh.Speed * 3.6f;
                if (kmh >= ConfigManager.SpeedingThresholdKMH)
                {
                    Vehicle nearbyCop = PoliceUtils.FindClosestPoliceVehicle(player.Position, 55.0f);
                    if (nearbyCop != null && nearbyCop.Exists())
                    {
                        Game.Player.WantedLevel = 1;
                        InitiateStop(player, "Excesso de velocidade (" + (int)kmh + " km/h)", nearbyCop);
                    }
                }
            }
        }

        private void InitiateStop(Ped player, string reason, Vehicle designatedCruiser)
        {
            Vehicle playerVeh = player.CurrentVehicle;
            if (playerVeh == null || !playerVeh.Exists()) return;

            // KEEP WantedLevel = 1 visible on HUD so the player sees the police star!
            Game.Player.WantedLevel = 1;

            // Disable dispatch of backup police units so GTA V DOES NOT flood the scene with other cruisers!
            SetDispatchServicesEnabled(false);

            // Suppress native lethal aggression from random cops
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);

            // Assign the actual nearby cruiser that spotted the player
            _copVehicle = designatedCruiser;
            if (_copVehicle == null || !_copVehicle.Exists())
            {
                Reset();
                return;
            }

            // Dismiss other random police units in the area so ONLY this cruiser conducts the stop
            DismissOtherPoliceUnits(player.Position, _copVehicle);

            _copPed = _copVehicle.GetPedOnSeat(VehicleSeat.Driver);
            _passengerPed = _copVehicle.GetPedOnSeat(VehicleSeat.Passenger);

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

        private void TriggerFleeing(string msg = "Fuga em flagrante! Perseguição armada iniciada!")
        {
            RemoveCopBlip();
            UI.Notify(msg);
            Game.Player.WantedLevel = 2;
            Reset();
        }

        private void HandleOrderingPullOver(Ped player)
        {
            if (!player.IsInVehicle() || _copVehicle == null || !_copVehicle.Exists())
            {
                Reset();
                return;
            }

            Vehicle playerVeh = player.CurrentVehicle;
            int elapsedMs = Game.GameTime - _stopStartTime;
            float distToCop = World.GetDistance(player.Position, _copVehicle.Position);
            float kmh = playerVeh.Speed * 3.6f;

            // REALISTIC FLEEING LOGIC:
            // 1. High speed evasion: Driving faster than 90 km/h or pulling far ahead (> 75m)
            if (kmh > 90.0f || (kmh > 65.0f && distToCop > 75.0f))
            {
                _fledCheckCounter++;
                if (_fledCheckCounter > 25)
                {
                    TriggerFleeing("Você abriu fuga em alta velocidade! Perseguição armada iniciada.");
                    return;
                }
            }
            // 2. Timeout: Player completely ignored the order to pull over for 18 seconds without stopping
            else if (elapsedMs > 18000 && playerVeh.Speed > 2.5f)
            {
                TriggerFleeing("Tempo esgotado para encostar o veículo! Perseguição iniciada.");
                return;
            }
            else
            {
                _fledCheckCounter = 0;
            }

            // SUCCESS: When player slows down and stops at the side of the road
            if (playerVeh.Speed < 1.2f)
            {
                CurrentState = VehicleStopState.CopDrivingToParkBehind;
                _stopStartTime = Game.GameTime;
                _lastDriveTime = 0;

                UI.Notify("Veículo parado. A viatura policial está estacionando logo atrás.");
            }
            else
            {
                int remainingSec = Math.Max(1, (18000 - elapsedMs) / 1000);
                string hint = string.Format("Encoste no acostamento à direita e pare o carro ({0}s para encostar)", remainingSec);
                DrawStandardText(hint, UI.HEIGHT - 45);

                // Keep cop pursuing closely behind at matching speed
                if (_copPed != null && _copPed.Exists() && (Game.GameTime - _lastDriveTime > 1800))
                {
                    _lastDriveTime = Game.GameTime;
                    Vector3 followPos = playerVeh.GetOffsetInWorldCoords(new Vector3(0f, -8.0f, 0f));
                    float chaseSpeed = Math.Max(playerVeh.Speed * 1.15f, 10.0f);
                    _copPed.Task.DriveTo(_copVehicle, followPos, 3.0f, chaseSpeed, 786603);
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
            float distToPlayer = World.GetDistance(_copVehicle.Position, playerVeh.Position);

            // FLEEING CHECK: If player stepped on the gas or drove away, trigger escape immediately!
            if (playerVeh.Speed > 6.5f || distToPlayer > 35.0f)
            {
                TriggerFleeing();
                return;
            }

            Vector3 parkPos = playerVeh.GetOffsetInWorldCoords(new Vector3(0f, -5.5f, 0f));
            float distToPark = World.GetDistance(_copVehicle.Position, parkPos);

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

                // Cop smoothly opens door and exits the vehicle
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

            Vehicle playerVeh = player.CurrentVehicle;
            float distToPlayer = World.GetDistance(_copVehicle.Position, playerVeh.Position);

            // FLEEING CHECK: If player steps on the gas while cop is exiting, flee!
            if (playerVeh.Speed > 6.0f || distToPlayer > 30.0f)
            {
                TriggerFleeing();
                return;
            }

            // Wait until cop has physically stepped out on his feet
            if (!_copPed.IsInVehicle())
            {
                CurrentState = VehicleStopState.CopWalkingToWindow;
                _stopStartTime = Game.GameTime;
                _lastCopWalkTime = 0;

                // Walk straight to driver window
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

            Vehicle playerVeh = player.CurrentVehicle;
            float distToPlayer = World.GetDistance(_copPed.Position, playerVeh.Position);

            // FLEEING CHECK: If player steps on gas while cop is walking, flee!
            if (playerVeh.Speed > 6.0f || distToPlayer > 25.0f)
            {
                TriggerFleeing();
                return;
            }

            Vector3 driverWindow = playerVeh.GetOffsetInWorldCoords(new Vector3(-1.35f, 0.35f, 0f));
            float dist = World.GetDistance(_copPed.Position, driverWindow);

            // Re-issue walking only every 3.5s so cop doesn't stutter
            if (Game.GameTime - _lastCopWalkTime > 3500)
            {
                _lastCopWalkTime = Game.GameTime;
                Function.Call(Hash.TASK_GO_TO_COORD_ANY_MEANS, _copPed.Handle, driverWindow.X, driverWindow.Y, driverWindow.Z, 1.25f, 0, 0, 786603, 0xbf800000);
            }

            // STRICT CHECK: Cop MUST physically reach within 1.7m of driver window!
            if (dist <= 1.7f)
            {
                CurrentState = VehicleStopState.DialogAtWindow;
                _copPed.Task.ClearAll();
                Function.Call(Hash.TASK_TURN_PED_TO_FACE_ENTITY, _copPed.Handle, player.Handle, 800);
                Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, _copPed.Handle, "WORLD_HUMAN_COP_IDLES", 0, true);
            }
            else
            {
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

            // If player stepped out on their own without pressing surrender:
            if (!player.IsInVehicle() || player.IsDead || player.IsRagdoll)
            {
                Reset();
                return;
            }

            // Check if player fled by accelerating
            if (player.CurrentVehicle.Speed > 6.0f)
            {
                TriggerFleeing();
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
            // Option 3: Step out and surrender naturally!
            else if (Game.IsKeyPressed(ConfigManager.SurrenderKey))
            {
                CurrentState = VehicleStopState.PlayerExitingToSurrender;
                _stopStartTime = Game.GameTime;

                // Player opens door and steps out naturally (NO ClearAllImmediately!)
                player.Task.LeaveVehicle(player.CurrentVehicle, false);

                // Cop covers with taser/unarmed
                if (_copPed != null && _copPed.Exists())
                {
                    _copPed.Task.ClearAll();
                    _copPed.Weapons.Select(WeaponHash.Unarmed, true);
                    Function.Call(Hash.TASK_AIM_GUN_AT_ENTITY, _copPed.Handle, player.Handle, -1, false);
                }

                UI.Notify("Desembarque com calma e com as mãos visíveis.");
            }
        }

        private void HandlePlayerExitingToSurrender(Ped player)
        {
            // Guarantee total cease-fire: Cops DO NOT shoot, punch, or beat the complying player!
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);
            player.IsInvincible = true;

            // Wait until player has physically completed the exit animation and is standing on the ground
            if (!player.IsInVehicle())
            {
                // Player is standing outside the car! Hands up peacefully
                player.Task.ClearAll();
                Function.Call(Hash.TASK_HANDS_UP, player.Handle, -1, 0, -1, true);

                if (_copPed != null && _copPed.Exists())
                {
                    Function.Call(Hash.TASK_TURN_PED_TO_FACE_ENTITY, _copPed.Handle, player.Handle, 600);
                }

                DrawStandardText("Aguarde o oficial algemá-lo...", UI.HEIGHT - 45);

                // Give 1.8 seconds for the player to watch the confrontation, then initiate physical cuffing
                if (Game.GameTime - _stopStartTime > 2000)
                {
                    CurrentState = VehicleStopState.ArrestingPlayer;
                    _stopStartTime = Game.GameTime;
                    RemoveCopBlip();

                    if (_copPed != null && _copPed.Exists())
                    {
                        _copPed.Task.ClearAll();
                        _copPed.Weapons.Select(WeaponHash.Unarmed, true);
                        Function.Call(Hash.TASK_ARREST_PED, _copPed.Handle, player.Handle);
                    }

                    PoliceUtils.PlayHandcuffSound();
                    UI.Notify("Você foi algemado pelo oficial.");
                }
            }
            else
            {
                DrawStandardText("Desembarcando do veículo...", UI.HEIGHT - 45);
            }
        }

        private void HandleArrestingPlayer(Ped player)
        {
            // Block controls during cuffing
            Game.DisableControlThisFrame(0, GTA.Control.MoveLeftRight);
            Game.DisableControlThisFrame(0, GTA.Control.MoveUpDown);
            Game.DisableControlThisFrame(0, GTA.Control.Attack);
            Game.DisableControlThisFrame(0, GTA.Control.Aim);
            Game.DisableControlThisFrame(0, GTA.Control.Jump);
            Game.DisableControlThisFrame(0, GTA.Control.Enter);

            // Display physical arrest for 3.5 seconds
            if (Game.GameTime - _stopStartTime > 3500)
            {
                CurrentState = VehicleStopState.FadingToStation;
                _stopStartTime = Game.GameTime;
                RemoveCopBlip();
                Function.Call(Hash.DO_SCREEN_FADE_OUT, 1200);
            }
        }

        private void HandleFadingToStation(Ped player)
        {
            // Keep controls disabled during fade
            Game.DisableControlThisFrame(0, GTA.Control.MoveLeftRight);
            Game.DisableControlThisFrame(0, GTA.Control.MoveUpDown);
            Game.DisableControlThisFrame(0, GTA.Control.Attack);

            if (Game.GameTime - _stopStartTime > 1600)
            {
                // Deduct legal bail
                int fee = Math.Min(Game.Player.Money, ConfigManager.BailAmount);
                Game.Player.Money -= fee;
                Game.Player.WantedLevel = 0;

                // Move to police station
                Vector3 stationPos = PoliceUtils.GetClosestPoliceStation(player.Position);
                player.Position = stationPos;
                player.Task.ClearAllImmediately();
                player.IsInvincible = false;

                if (_copPed != null && _copPed.Exists())
                {
                    _copPed.BlockPermanentEvents = false;
                }

                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
                SetDispatchServicesEnabled(true);
                Function.Call(Hash.DO_SCREEN_FADE_IN, 1200);

                UI.Notify("PRESO\nLiberado sob fiança de $" + fee + " na delegacia.");
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

            RemoveCopBlip();

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

        private void RemoveCopBlip()
        {
            try
            {
                if (_copBlip != null)
                {
                    if (_copBlip.Exists())
                    {
                        _copBlip.Remove();
                    }
                    _copBlip = null;
                }

                if (_copVehicle != null && _copVehicle.Exists())
                {
                    Blip b = _copVehicle.CurrentBlip;
                    if (b != null && b.Exists())
                    {
                        b.Remove();
                    }
                }
            }
            catch { }
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
