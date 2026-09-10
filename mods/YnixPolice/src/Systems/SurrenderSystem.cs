using System;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using YnixPolice.Core;

namespace YnixPolice.Systems
{
    public class SurrenderSystem
    {
        private static bool _isSurrendering = false;
        private static int _surrenderTime = 0;
        private static Ped _arrestingOfficer = null;
        private static int _fadeStage = 0;

        public void OnTick()
        {
            if (!ConfigManager.EnableSurrender) return;

            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead)
            {
                _isSurrendering = false;
                return;
            }

            if (_isSurrendering)
            {
                HandleSurrenderProcess(player);
                return;
            }

            // Available during 1, 2 or 3 stars (even on foot or inside vehicle)
            if (Game.Player.WantedLevel > 0 && Game.Player.WantedLevel <= 3)
            {
                // Show hint on screen
                string hint = string.Format("Segure [{0}] para Erguer as Mãos e Render-se", ConfigManager.SurrenderKey.ToString());
                new UIText(hint, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 65), 0.4f, System.Drawing.Color.FromArgb(220, 255, 255, 255), GTA.Font.ChaletComprimeCologne, true, false, true).Draw();

                if (Game.IsKeyPressed(ConfigManager.SurrenderKey))
                {
                    if (player.IsInVehicle())
                    {
                        player.Task.LeaveVehicle(player.CurrentVehicle, false);
                    }

                    Ped officer = FindClosestPoliceOfficer(player.Position, 50.0f);
                    TriggerSurrender(player, officer);
                }
            }
        }

        public static void TriggerSurrender(Ped player, Ped officer)
        {
            _isSurrendering = true;
            _surrenderTime = Game.GameTime;
            _arrestingOfficer = officer;
            _fadeStage = 0;

            // Protect player during arrest sequence
            player.IsInvincible = true;

            // Make police stop shooting and ignore aggression
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);

            // Put hands up
            player.Task.ClearAllImmediately();
            Function.Call(Hash.TASK_HANDS_UP, player.Handle, -1, 0, -1, true);

            // Command all nearby cops to cease fire and aim
            Ped[] nearbyPeds = World.GetNearbyPeds(player.Position, 60.0f);
            foreach (var p in nearbyPeds)
            {
                if (PoliceUtils.IsCop(p))
                {
                    p.Task.ClearAllImmediately();
                    Function.Call(Hash.TASK_AIM_GUN_AT_ENTITY, p.Handle, player.Handle, 8000, false);
                }
            }

            if (_arrestingOfficer != null && _arrestingOfficer.Exists())
            {
                _arrestingOfficer.Task.GoTo(player.Position + player.ForwardVector * 1.0f);
            }

            UI.Notify("~b~Você se rendeu! ~w~Aguarde a polícia efetuar a prisão pacífica.");
        }

        private void HandleSurrenderProcess(Ped player)
        {
            // Disable movement controls
            Game.DisableControlThisFrame(0, GTA.Control.MoveLeftRight);
            Game.DisableControlThisFrame(0, GTA.Control.MoveUpDown);
            Game.DisableControlThisFrame(0, GTA.Control.Attack);
            Game.DisableControlThisFrame(0, GTA.Control.Aim);
            Game.DisableControlThisFrame(0, GTA.Control.Jump);

            // Keep ignoring player so no rogue bullets
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);

            int elapsed = Game.GameTime - _surrenderTime;

            if (_fadeStage == 0 && elapsed > 2000)
            {
                _fadeStage = 1;
                Function.Call(Hash.DO_SCREEN_FADE_OUT, 1200);
            }
            else if (_fadeStage == 1 && elapsed > 3500)
            {
                _fadeStage = 2;

                int fee = Math.Min(Game.Player.Money, ConfigManager.BailAmount);
                Game.Player.Money -= fee;
                Game.Player.WantedLevel = 0;

                // Teleport to nearest police station
                Vector3 stationPos = GetClosestPoliceStation(player.Position);
                player.Position = stationPos;
                player.Task.ClearAllImmediately();
                player.IsInvincible = false;

                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
                Function.Call(Hash.DO_SCREEN_FADE_IN, 1200);

                UI.Notify("~y~PRESO (BUSTED)\n~w~Você foi fichado e liberado sob fiança de ~r~$" + fee + "~w~ na delegacia.");
                _isSurrendering = false;
            }
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

        private static Ped FindClosestPoliceOfficer(Vector3 pos, float radius)
        {
            Ped[] peds = World.GetNearbyPeds(pos, radius);
            Ped closest = null;
            float minDist = float.MaxValue;

            foreach (var p in peds)
            {
                if (PoliceUtils.IsCop(p))
                {
                    float d = World.GetDistance(pos, p.Position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closest = p;
                    }
                }
            }
            return closest;
        }
    }
}
