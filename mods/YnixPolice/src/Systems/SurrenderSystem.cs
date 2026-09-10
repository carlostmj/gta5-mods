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

        private void HandleIdleState(Ped player)
        {
            // Only show surrender prompt if player has wanted level AND an alive cop is within 30m!
            if (Game.Player.WantedLevel > 0 && Game.Player.WantedLevel <= 3 && !player.IsInVehicle())
            {
                Ped nearbyCop = PoliceUtils.FindClosestPoliceOfficer(player.Position, 30.0f);
                if (nearbyCop != null)
                {
                    // Show prompt on screen
                    string hint = string.Format("~y~POLÍCIA PRÓXIMA ~w~| Pressione ~g~[{0}]~w~ para Mãos ao Alto e Render-se", ConfigManager.SurrenderKey.ToString());
                    new UIText(hint, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 65), 0.42f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();

                    if (Game.IsKeyPressed(ConfigManager.SurrenderKey))
                    {
                        StartSurrender(player, nearbyCop);
                    }
                }
            }
        }

        public static void StartSurrender(Ped player, Ped officer)
        {
            if (officer == null || !officer.Exists())
            {
                UI.Notify("~r~Nenhum policial por perto para efetuar a rendição!");
                return;
            }

            State = SurrenderState.HandsUpWaitingCop;
            _arrestingOfficer = officer;
            _stateStartTime = Game.GameTime;
            _animLoopTime = Game.GameTime;

            // Player puts hands up
            player.Task.ClearAllImmediately();
            Function.Call(Hash.TASK_HANDS_UP, player.Handle, -1, 0, -1, true);

            // Temporarily protect player from wild stray bullets while complying
            player.IsInvincible = true;

            // Command all nearby cops to cease lethal fire
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);
            Ped[] nearby = World.GetNearbyPeds(player.Position, 50.0f);
            foreach (var p in nearby)
            {
                if (PoliceUtils.IsCop(p) && p != _arrestingOfficer)
                {
                    p.Task.ClearAllImmediately();
                    Function.Call(Hash.TASK_AIM_GUN_AT_ENTITY, p.Handle, player.Handle, 12000, false);
                }
            }

            // Command arresting officer to holster weapon and walk directly to player
            if (_arrestingOfficer.IsInVehicle())
            {
                _arrestingOfficer.Task.LeaveVehicle(_arrestingOfficer.CurrentVehicle, false);
            }
            _arrestingOfficer.Task.ClearAllImmediately();
            _arrestingOfficer.Weapons.Select(WeaponHash.Unarmed, true);
            Function.Call(Hash.TASK_GO_TO_ENTITY, _arrestingOfficer.Handle, player.Handle, -1, 1.2f, 1.4f, 0, 0);
            _lastGoToTime = Game.GameTime;

            UI.Notify("~b~Mãos ao alto! ~w~Não se mova enquanto o policial se aproxima para algemá-lo.");
        }

        private void HandleHandsUpWaiting(Ped player)
        {
            // Block player movement controls
            DisablePlayerControls();

            // Re-apply hands up if dropped
            if (Game.GameTime - _animLoopTime > 1500)
            {
                _animLoopTime = Game.GameTime;
                Function.Call(Hash.TASK_HANDS_UP, player.Handle, -1, 0, -1, true);
            }

            if (_arrestingOfficer == null || !_arrestingOfficer.Exists() || _arrestingOfficer.IsDead)
            {
                // Find another cop if this one died
                _arrestingOfficer = PoliceUtils.FindClosestPoliceOfficer(player.Position, 30.0f);
                if (_arrestingOfficer == null)
                {
                    UI.Notify("~y~Nenhum policial alcançou você. Rendição cancelada.");
                    Reset();
                    player.IsInvincible = false;
                    Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
                    return;
                }
            }

            float dist = World.GetDistance(_arrestingOfficer.Position, player.Position);

            // Re-assert walking only every 3 seconds so the cop doesn't freeze in place!
            if (Game.GameTime - _lastGoToTime > 3000)
            {
                _lastGoToTime = Game.GameTime;
                Function.Call(Hash.TASK_GO_TO_ENTITY, _arrestingOfficer.Handle, player.Handle, -1, 1.2f, 1.4f, 0, 0);
            }

            if (dist < 2.2f || (Game.GameTime - _stateStartTime > 12000))
            {
                // Officer has physically arrived!
                State = SurrenderState.CopCuffingPlayer;
                _stateStartTime = Game.GameTime;

                _arrestingOfficer.Task.ClearAllImmediately();
                Function.Call(Hash.TASK_TURN_PED_TO_FACE_ENTITY, _arrestingOfficer.Handle, player.Handle, 1000);
                Function.Call(Hash.TASK_ARREST_PED, _arrestingOfficer.Handle, player.Handle);

                // Play metallic handcuff sound
                PoliceUtils.PlayHandcuffSound();

                UI.Notify("~y~Você foi algemado pelo oficial.");
            }
            else
            {
                string waitText = "~b~Oficial se aproximando... ~w~(" + (int)dist + "m)";
                new UIText(waitText, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 40), 0.42f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();
            }
        }

        private void HandleCuffing(Ped player)
        {
            DisablePlayerControls();

            // After 2.5 seconds of physical cuffing animation, start screen fade
            if (Game.GameTime - _stateStartTime > 2500)
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

                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
                Function.Call(Hash.DO_SCREEN_FADE_IN, 1200);

                UI.Notify("~y~PRESO (BUSTED)\n~w~Você foi fichado e liberado sob fiança de ~r~$" + fee + "~w~ na delegacia.");
                Reset();
            }
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
            _arrestingOfficer = null;
        }
    }
}
