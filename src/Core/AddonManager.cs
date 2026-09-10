using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace YnixTrainer.Core
{
    public class AddonVehicle
    {
        private string _name;
        private string _model;
        private string _category;

        public string name { get { return _name; } set { _name = value; } }
        public string model { get { return _model; } set { _model = value; } }
        public string category { get { return _category; } set { _category = value; } }
    }

    public static class AddonManager
    {
        private static readonly string AddonsPath = @"scripts\YnixTrainer\addons.json";
        private static List<AddonVehicle> _addons = new List<AddonVehicle>();

        public static List<AddonVehicle> Addons
        {
            get { return _addons; }
        }

        public static void Load()
        {
            try
            {
                _addons.Clear();
                if (File.Exists(AddonsPath))
                {
                    string json = File.ReadAllText(AddonsPath);
                    var serializer = new JavaScriptSerializer();
                    var list = serializer.Deserialize<List<AddonVehicle>>(json);
                    if (list != null)
                    {
                        _addons = list;
                    }
                }
            }
            catch { }
        }
    }
}
