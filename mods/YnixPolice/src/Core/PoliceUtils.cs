using System;
using GTA;
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

            // Check police vehicle
            if (p.IsInVehicle())
            {
                Vehicle v = p.CurrentVehicle;
                if (v != null && v.Exists() && v.ClassType == VehicleClass.Emergency)
                {
                    return true;
                }
            }

            // Check common police model hashes
            int model = p.Model.Hash;
            if (model == Game.GenerateHash("s_m_y_cop_01") ||
                model == Game.GenerateHash("s_m_y_sheriff_01") ||
                model == Game.GenerateHash("s_m_y_hwaycop_01") ||
                model == Game.GenerateHash("s_m_m_snowcop_01") ||
                model == Game.GenerateHash("s_f_y_cop_01") ||
                model == Game.GenerateHash("s_f_y_sheriff_01") ||
                model == Game.GenerateHash("s_m_y_swat_01"))
            {
                return true;
            }

            return false;
        }
    }
}
