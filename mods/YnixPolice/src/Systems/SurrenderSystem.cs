using System;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using YnixPolice.Core;

namespace YnixPolice.Systems
{
    public enum SurrenderState
    {
        Idle,
        HandsUpWaitingCop,
        CopCuffingPlayer,
        FadingToStation
    }

    public class SurrenderSystem
    {
        public static SurrenderState State = SurrenderState.Idle;
        private static Ped _arrestingOfficer = null;
        private static int _stateStartTime = 0;
        private static int _animLoopTime = 0;
        private static int _lastGoToTime = 0;

        public void OnTick()
        {
            if (!ConfigManager.EnableSurrender) return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead)
            {
                Reset();
                return;
            }

            // Yield completely to vanilla GTA V native arrest (e.g. at 1-star when standing still)
            if (IsNativeArrestInProgress(player))
            {
                Reset();
                return;
            }

            switch (State)
            {
                case SurrenderState.Idle:
                    HandleIdleState(player);
                    break;

                case SurrenderState.HandsUpWaitingCop:
                    HandleHandsUpWaiting(player);
                    break;

                case SurrenderState.CopCuffingPlayer:
                    HandleCuffing(player);
                    break;

                case SurrenderState.FadingToStation:
                    HandleFading(player);
                    break;
            }
        }

        private bool IsNativeArrestInProgress(Ped player)
        {
            try
            {
                if (Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player.Handle, true))
                    return true;

                if (Function.Call<bool>(Hash.IS_PED_BEING_ARRESTED, player.Handle))
                    return true;
            }
            catch { }

            return false;
        }

        private void HandleIdleState(Ped player)
        {
            // Only allow surrender when:
            // 1. Player has 2 or 3 stars (where GTA V vanilla refuses to arrest and only shoots to kill)
            // 2. Player is on foot (not in a vehicle)
            // 3. There is an officer ON FOOT (already outside of any vehicle) within 22 meters!
            if (Game.Player.WantedLevel >= 2 && Game.Player.WantedLevel <= 3 && !player.IsInVehicle())
            {
                Ped onFootCop = PoliceUtils.FindClosestOnFootPoliceOfficer(player.Position, 22.0f);
                if (onFootCop != null)
                {
                    string hint = string.Format("Pressione [{0}] para erguer as mãos e se render", ConfigManager.SurrenderKey.ToString());
                    new UIText(hint, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 65), 0.42f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();

                    if (Game.IsKeyPressed(ConfigManager.SurrenderKey))
                    {
                        StartSurrender(player, onFootCop);
                    }
                }
            }
        }

        public static void StartSurrender(Ped player, Ped officer)
        {
            if (officer == null || !officer.Exists() || officer.IsDead || officer.IsInVehicle())
            {
                UI.Notify("Nenhum policial a pé por perto para efetuar a rendição.");
                return;
            }

            State = SurrenderState.HandsUpWaitingCop;
            _arrestingOfficer = officer;
            _stateStartTime = Game.GameTime;
            _animLoopTime = Game.GameTime;

            // Player raises hands
            player.Task.ClearAllImmediately();
            Function.Call(Hash.TASK_HANDS_UP, player.Handle, -1, 0, -1, true);

            // Temporarily protect player from stray bullets while complying
            player.IsInvincible = true;

            // Command all nearby cops to cease lethal fire and aim
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);
            Ped[] nearby = World.GetNearbyPeds(player.Position, 50.0f);
            foreach (var p in nearby)
            {
                if (PoliceUtils.IsCop(p) && p != _arrestingOfficer && !p.IsInVehicle())
                {
                    p.Task.ClearAllImmediately();
                    Function.Call(Hash.TASK_AIM_GUN_AT_ENTITY, p.Handle, player.Handle, 15000, false);
                }
            }

            // Command arresting officer to holster lethal weapon and JOG directly to player!
            _arrestingOfficer.BlockPermanentEvents = true;
            _arrestingOfficer.Task.ClearAllImmediately();
            _arrestingOfficer.Weapons.Select(WeaponHash.Unarmed, true);
            
            // Speed 2.8f = brisk run/jog directly to the player
            Function.Call(Hash.TASK_GO_TO_ENTITY, _arrestingOfficer.Handle, player.Handle, -1, 1.0f, 2.8f, 0, 0);
            _lastGoToTime = Game.GameTime;

            UI.Notify("Mãos na cabeça. Aguarde o policial se aproximar para algemá-lo.");
        }

        private void HandleHandsUpWaiting(Ped player)
        {
            DisablePlayerControls();

            // Re-apply hands up if dropped
            if (Game.GameTime - _animLoopTime > 1500)
            {
                _animLoopTime = Game.GameTime;
                Function.Call(Hash.TASK_HANDS_UP, player.Handle, -1, 0, -1, true);
            }

            // Verify officer is alive and on foot
            if (_arrestingOfficer == null || !_arrestingOfficer.Exists() || _arrestingOfficer.IsDead)
            {
                // Find another on-foot cop
                _arrestingOfficer = PoliceUtils.FindClosestOnFootPoliceOfficer(player.Position, 25.0f);
                if (_arrestingOfficer == null)
                {
                    UI.Notify("Rendição cancelada.");
                    CancelSurrender(player);
                    return;
                }
            }

            float dist = World.GetDistance(_arrestingOfficer.Position, player.Position);

            // Re-assert jog navigation every 3.0s
            if (Game.GameTime - _lastGoToTime > 3000)
            {
                _lastGoToTime = Game.GameTime;
                Function.Call(Hash.TASK_GO_TO_ENTITY, _arrestingOfficer.Handle, player.Handle, -1, 1.0f, 2.8f, 0, 0);
            }

            // STRICT PHYSICAL PROXIMITY CHECK: Officer MUST be within 1.35m (no fake distance arrest!)
            if (dist <= 1.35f)
            {
                // Officer has physically touched / reached the player!
                State = SurrenderState.CopCuffingPlayer;
                _stateStartTime = Game.GameTime;

                // Turn officer and perform native physical arrest
                _arrestingOfficer.Task.ClearAllImmediately();
                Function.Call(Hash.TASK_TURN_PED_TO_FACE_ENTITY, _arrestingOfficer.Handle, player.Handle, 600);
                Function.Call(Hash.TASK_ARREST_PED, _arrestingOfficer.Handle, player.Handle);

                // Play metallic handcuff ratchet sound
                PoliceUtils.PlayHandcuffSound();

                UI.Notify("O policial alcançou você e colocou as algemas.");
            }
            else
            {
                // If 20 seconds passed and officer NEVER reached (e.g. stuck behind fence/mesh), cancel cleanly
                if (Game.GameTime - _stateStartTime > 20000)
                {
                    UI.Notify("Rendição cancelada.");
                    CancelSurrender(player);
                    return;
                }

                string waitText = string.Format("Policial se aproximando... ({0:0.0}m)", dist);
                new UIText(waitText, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 40), 0.42f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();
            }
        }

        private void HandleCuffing(Ped player)
        {
            DisablePlayerControls();

            // Give 3.0 seconds so the player visibly watches the physical handcuffing interaction!
            if (Game.GameTime - _stateStartTime > 3000)
            {
                State = SurrenderState.FadingToStation;
                _stateStartTime = Game.GameTime;
                Function.Call(Hash.DO_SCREEN_FADE_OUT, 1200);
            }
        }

        private void HandleFading(Ped player)
        {
            DisablePlayerControls();

            if (Game.GameTime - _stateStartTime > 1600)
            {
                // Deduct legal bail
                int fee = Math.Min(Game.Player.Money, ConfigManager.BailAmount);
                Game.Player.Money -= fee;
                Game.Player.WantedLevel = 0;

                // Move to police station
                Vector3 stationPos = GetClosestPoliceStation(player.Position);
                player.Position = stationPos;
                player.Task.ClearAllImmediately();
                player.IsInvincible = false;

                if (_arrestingOfficer != null && _arrestingOfficer.Exists())
                {
                    _arrestingOfficer.BlockPermanentEvents = false;
                }

                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
                Function.Call(Hash.DO_SCREEN_FADE_IN, 1200);

                UI.Notify("~y~PRESO (BUSTED)\n~w~Você foi fichado e liberado sob fiança de ~r~$" + fee + "~w~ na delegacia.");
                Reset();
            }
        }

        private static void CancelSurrender(Ped player)
        {
            player.IsInvincible = false;
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
            Reset();
        }

        private void DisablePlayerControls()
        {
            Game.DisableControlThisFrame(0, GTA.Control.MoveLeftRight);
            Game.DisableControlThisFrame(0, GTA.Control.MoveUpDown);
            Game.DisableControlThisFrame(0, GTA.Control.Attack);
            Game.DisableControlThisFrame(0, GTA.Control.Aim);
            Game.DisableControlThisFrame(0, GTA.Control.Jump);
            Game.DisableControlThisFrame(0, GTA.Control.Sprint);
            Game.DisableControlThisFrame(0, GTA.Control.Enter);
        }

        private static Vector3 GetClosestPoliceStation(Vector3 currentPos)
        {
            Vector3[] stations = new[]
            {
                new Vector3(425.1f, -979.5f, 30.7f),    // Mission Row
                new Vector3(-448.2f, 6012.3f, 31.7f),   // Paleto Bay
                new Vector3(1853.2f, 3687.5f, 34.2f),   // Sandy Shores
                new Vector3(-1108.2f, -845.6f, 19.3f),  // Vespucci
                new Vector3(638.5f, 1.4f, 82.7f),       // Vinewood
                new Vector3(-561.6f, -131.6f, 38.2f)    // Rockford Hills
            };

            Vector3 closest = stations[0];
            float minDist = float.MaxValue;
            foreach (var pos in stations)
            {
                float d = World.GetDistance(currentPos, pos);
                if (d < minDist)
                {
                    minDist = d;
                    closest = pos;
                }
            }
            return closest;
        }

        public static void Reset()
        {
            State = SurrenderState.Idle;
            if (_arrestingOfficer != null && _arrestingOfficer.Exists())
            {
                _arrestingOfficer.BlockPermanentEvents = false;
                _arrestingOfficer.Task.ClearAll();
                _arrestingOfficer.Weapons.Select(WeaponHash.Unarmed, true);
                _arrestingOfficer.Task.WanderAround();
            }
            _arrestingOfficer = null;
        }
    }
}
