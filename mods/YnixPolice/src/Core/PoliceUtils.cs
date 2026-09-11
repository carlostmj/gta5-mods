using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace YnixPolice.Core
{
    public static class PoliceUtils
    {
        private static readonly int CopRelGroup = Game.GenerateHash("COP");

        public static bool IsCop(Ped p)
        {
            if (p == null || !p.Exists() || p.IsDead) return false;

            // Check relationship group
            if (p.RelationshipGroup == CopRelGroup) return true;

            // Check ped type (6 = COP, 27 = SWAT, 29 = ARMY)
            int pedType = Function.Call<int>(Hash.GET_PED_TYPE, p.Handle);
            if (pedType == 6 || pedType == 27) return true;

            // Check common police model hashes
            int model = p.Model.Hash;
            if (model == Game.GenerateHash("s_m_y_cop_01") ||
                model == Game.GenerateHash("s_m_y_cop_02") ||
                model == Game.GenerateHash("s_m_y_sheriff_01") ||
                model == Game.GenerateHash("s_m_y_hwaycop_01") ||
                model == Game.GenerateHash("s_m_m_snowcop_01") ||
                model == Game.GenerateHash("s_f_y_cop_01") ||
                model == Game.GenerateHash("s_f_y_sheriff_01") ||
                model == Game.GenerateHash("s_m_y_swat_01") ||
                model == Game.GenerateHash("s_m_m_security_01"))
            {
                return true;
            }

            // Check if inside any emergency vehicle
            if (p.IsInVehicle())
            {
                Vehicle v = p.CurrentVehicle;
                if (v != null && v.Exists() && v.ClassType == VehicleClass.Emergency)
                {
                    return true;
                }
            }

            return false;
        }

        public static Ped FindClosestPoliceOfficer(Vector3 pos, float radius)
        {
            Ped[] peds = World.GetNearbyPeds(pos, radius);
            Ped closest = null;
            float minD = float.MaxValue;
            foreach (var p in peds)
            {
                if (IsCop(p) && !p.IsDead)
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

        public static Ped FindClosestOnFootPoliceOfficer(Vector3 pos, float radius)
        {
            Ped[] peds = World.GetNearbyPeds(pos, radius);
            Ped closest = null;
            float minD = float.MaxValue;
            foreach (var p in peds)
            {
                if (IsCop(p) && !p.IsDead && !p.IsInVehicle())
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

        public static Vehicle FindClosestPoliceVehicle(Vector3 pos, float radius)
        {
            Vehicle[] vehs = World.GetNearbyVehicles(pos, radius);
            Vehicle closest = null;
            float minD = float.MaxValue;
            foreach (var v in vehs)
            {
                if (v != null && v.Exists() && v.ClassType == VehicleClass.Emergency)
                {
                    float d = World.GetDistance(pos, v.Position);
                    if (d < minD)
                    {
                        minD = d;
                        closest = v;
                    }
                }
            }
            return closest;
        }

        public static Vector3 GetClosestPoliceStation(Vector3 currentPos)
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

        public static void PlayHandcuffSound()
        {
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "WEAPON_ATTACHMENT_EQUIP", "HUD_AMMO_SHOP_SOUNDSET", true);
        }

        public static void PlayPaperSound()
        {
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET", true);
        }
    }
}
