using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using YnixTrainer.Core;

namespace YnixTrainer.Modules
{
    public class VehicleModule : IYnixModule
    {
        public string Name { get { return "Vehicle"; } }

        private bool _vehicleGodMode = false;
        private bool _speedBoost = false;
        private bool _hornJump = false;
        private bool _driveOnWater = false;
        private bool _autopilot = false;
        private bool _speedometer = false;
        private UIMenuCheckboxItem _autopilotItem;

        public void Initialize(UIMenu mainMenu, MenuPool menuPool)
        {
            var vehMenu = menuPool.AddSubMenu(mainMenu, Localization.Get("General", "SubmenuVehicle", "Opções de Veículos"));

            // Fix & Wash
            var fixItem = new UIMenuItem(Localization.Get("Vehicle", "FixVehicle", "Reparar e Lavar Veículo"), Localization.Get("Vehicle", "FixVehicleDesc", "Conserta lataria e limpa"));
            fixItem.Activated += (sender, selected) =>
            {
                Ped player = Game.Player.Character;
                if (player.IsInVehicle())
                {
                    Vehicle v = player.CurrentVehicle;
                    v.Repair();
                    v.Wash();
                    UI.Notify("~g~Veículo reparado e limpo!");
                }
                else
                {
                    UI.Notify("~r~Você precisa estar dentro de um veículo!");
                }
            };
            vehMenu.AddItem(fixItem);

            // Vehicle God Mode
            var godItem = new UIMenuCheckboxItem(Localization.Get("Vehicle", "VehicleGodMode", "Veículo Invencível"), _vehicleGodMode, Localization.Get("Vehicle", "VehicleGodModeDesc", "Indestrutível"));
            godItem.CheckboxEvent += (sender, state) =>
            {
                _vehicleGodMode = state;
                Ped player = Game.Player.Character;
                if (player.IsInVehicle())
                {
                    player.CurrentVehicle.IsInvincible = _vehicleGodMode;
                    player.CurrentVehicle.CanBeVisiblyDamaged = !_vehicleGodMode;
                }
            };
            vehMenu.AddItem(godItem);

            // Max Tuning
            var tuneItem = new UIMenuItem(Localization.Get("Vehicle", "MaxTuning", "Tunar Veículo ao Máximo"), Localization.Get("Vehicle", "MaxTuningDesc", "Melhorias máximas de desempenho"));
            tuneItem.Activated += (sender, selected) =>
            {
                Ped player = Game.Player.Character;
                if (player.IsInVehicle())
                {
                    Vehicle v = player.CurrentVehicle;
                    v.InstallModKit();
                    foreach (VehicleMod mod in Enum.GetValues(typeof(VehicleMod)))
                    {
                        try
                        {
                            int count = v.GetModCount(mod);
                            if (count > 0) v.SetMod(mod, count - 1, false);
                        }
                        catch { }
                    }
                    foreach (VehicleToggleMod toggle in Enum.GetValues(typeof(VehicleToggleMod)))
                    {
                        try { v.ToggleMod(toggle, true); } catch { }
                    }
                    v.WindowTint = VehicleWindowTint.PureBlack;
                    UI.Notify("~g~Veículo tunado no máximo!");
                }
                else
                {
                    UI.Notify("~r~Entre em um veículo primeiro!");
                }
            };
            vehMenu.AddItem(tuneItem);

            // Speed Boost
            var boostItem = new UIMenuCheckboxItem(Localization.Get("Vehicle", "SpeedBoost", "Super Aceleração (Nitro)"), _speedBoost, Localization.Get("Vehicle", "SpeedBoostDesc", "Segure SHIFT para turbo"));
            boostItem.CheckboxEvent += (sender, state) => { _speedBoost = state; };
            vehMenu.AddItem(boostItem);

            // Horn Jump
            var hornItem = new UIMenuCheckboxItem(Localization.Get("Vehicle", "HornJump", "Pulo com Buzina"), _hornJump, Localization.Get("Vehicle", "HornJumpDesc", "Buzine para pular"));
            hornItem.CheckboxEvent += (sender, state) => { _hornJump = state; };
            vehMenu.AddItem(hornItem);

            // Drive on Water
            var waterItem = new UIMenuCheckboxItem(Localization.Get("Vehicle", "DriveOnWater", "Dirigir sobre a Água"), _driveOnWater, Localization.Get("Vehicle", "DriveOnWaterDesc", "O carro anda sobre a superfície da água"));
            waterItem.CheckboxEvent += (sender, state) => { _driveOnWater = state; };
            vehMenu.AddItem(waterItem);

            // Autopilot
            _autopilotItem = new UIMenuCheckboxItem(Localization.Get("Vehicle", "Autopilot", "Piloto Automático (Waypoint)"), _autopilot, Localization.Get("Vehicle", "AutopilotDesc", "Dirige sozinho até o destino marcado"));
            _autopilotItem.CheckboxEvent += (sender, state) =>
            {
                Ped player = Game.Player.Character;
                if (state)
                {
                    if (!player.IsInVehicle())
                    {
                        _autopilotItem.Checked = false;
                        UI.Notify("~r~Você precisa estar dentro de um veículo!");
                        return;
                    }
                    if (!Game.IsWaypointActive)
                    {
                        _autopilotItem.Checked = false;
                        UI.Notify("~r~Marque um destino no mapa (Waypoint) primeiro!");
                        return;
                    }
                    _autopilot = true;
                    Vector3 wp = World.GetWaypointPosition();
                    Vehicle v = player.CurrentVehicle;
                    player.Task.DriveTo(v, wp, 15.0f, 40.0f, 786603);
                    UI.Notify("~g~Piloto automático iniciado até o destino!");
                }
                else
                {
                    _autopilot = false;
                    player.Task.ClearAll();
                    UI.Notify("~y~Piloto automático cancelado.");
                }
            };
            vehMenu.AddItem(_autopilotItem);

            // Speedometer
            var speedoItem = new UIMenuCheckboxItem(Localization.Get("Vehicle", "Speedometer", "Velocímetro Digital (KM/H)"), _speedometer, Localization.Get("Vehicle", "SpeedometerDesc", "Exibe velocidade e marcha na tela"));
            speedoItem.CheckboxEvent += (sender, state) => { _speedometer = state; };
            vehMenu.AddItem(speedoItem);

            // Flip Vehicle
            var flipItem = new UIMenuItem(Localization.Get("Vehicle", "FlipVehicle", "Desvirar Veículo"), Localization.Get("Vehicle", "FlipVehicleDesc", "Desvira o carro"));
            flipItem.Activated += (sender, selected) =>
            {
                Ped player = Game.Player.Character;
                if (player.IsInVehicle())
                {
                    Vehicle v = player.CurrentVehicle;
                    v.Rotation = new Vector3(0.0f, 0.0f, v.Rotation.Z);
                    v.PlaceOnGround();
                    UI.Notify("~b~Veículo desvirado!");
                }
            };
            vehMenu.AddItem(flipItem);

            // Open/Close Doors
            var openDoors = new UIMenuItem(Localization.Get("Vehicle", "OpenAllDoors", "Abrir Todas as Portas"), "Abre portas, capô e porta-malas");
            openDoors.Activated += (sender, selected) =>
            {
                Ped player = Game.Player.Character;
                if (player.IsInVehicle())
                {
                    Vehicle v = player.CurrentVehicle;
                    for (int i = 0; i < 7; i++) v.OpenDoor((VehicleDoor)i, false, false);
                }
            };
            vehMenu.AddItem(openDoors);

            var closeDoors = new UIMenuItem(Localization.Get("Vehicle", "CloseAllDoors", "Fechar Todas as Portas"), "Fecha todas as portas");
            closeDoors.Activated += (sender, selected) =>
            {
                Ped player = Game.Player.Character;
                if (player.IsInVehicle())
                {
                    Vehicle v = player.CurrentVehicle;
                    for (int i = 0; i < 7; i++) v.CloseDoor((VehicleDoor)i, false);
                }
            };
            vehMenu.AddItem(closeDoors);

            // Delete Vehicle
            var delItem = new UIMenuItem(Localization.Get("Vehicle", "DeleteVehicle", "Excluir Veículo Atual"), Localization.Get("Vehicle", "DeleteVehicleDesc", "Exclui o veículo"));
            delItem.Activated += (sender, selected) =>
            {
                Ped player = Game.Player.Character;
                if (player.IsInVehicle())
                {
                    Vehicle v = player.CurrentVehicle;
                    v.Delete();
                    UI.Notify("~y~Veículo excluído.");
                }
            };
            vehMenu.AddItem(delItem);

            // -------------------------------------------------------------
            // SPAWNER MENU
            // -------------------------------------------------------------
            var spawnerMenu = menuPool.AddSubMenu(vehMenu, Localization.Get("Vehicle", "SpawnVehicle", "Spawmar Veículo"));

            // 1. Add-Ons from JSON
            AddonManager.Load();
            var addonMenu = menuPool.AddSubMenu(spawnerMenu, Localization.Get("Vehicle", "SpawnAddonMenu", "Meus Carros Add-On (JSON)"));
            if (AddonManager.Addons.Count > 0)
            {
                foreach (var addon in AddonManager.Addons)
                {
                    string m = addon.model;
                    string label = string.IsNullOrEmpty(addon.name) ? m.ToUpper() : addon.name;
                    var item = new UIMenuItem(label, "Modelo: " + m + " (" + addon.category + ")");
                    item.Activated += (sender, selected) => { SpawnVehicle(m); };
                    addonMenu.AddItem(item);
                }
            }
            else
            {
                addonMenu.AddItem(new UIMenuItem("Nenhum veículo em addons.json", "Edite scripts/YnixTrainer/addons.json"));
            }

            // 2. Input Box for Add-Ons
            var addonInputItem = new UIMenuItem(Localization.Get("Vehicle", "SpawnAddonInput", "Digitar Modelo Add-On"), Localization.Get("Vehicle", "SpawnAddonInputDesc", "Digite qualquer nome"));
            addonInputItem.Activated += (sender, selected) =>
            {
                string input = Game.GetUserInput(30);
                if (!string.IsNullOrEmpty(input))
                {
                    SpawnVehicle(input.Trim());
                }
            };
            spawnerMenu.AddItem(addonInputItem);

            // 3. Original GTA V Categories Submenu
            var origCatsMenu = menuPool.AddSubMenu(spawnerMenu, Localization.Get("Vehicle", "SpawnCategories", "Categorias Originais do GTA V"));

            // Super
            AddCategoryMenu(origCatsMenu, menuPool, "Super", new[]
            {
                "adder", "autarch", "banshee2", "bullet", "cheetah", "cyclone", "deveste", "entity2", "entityxf",
                "emerus", "fmj", "furia", "gp1", "infernus", "italigtb", "italigtb2", "krieger", "le7b", "nero",
                "nero2", "osiris", "penetrator", "pfister811", "proto", "reaper", "sc1", "sheava", "sultanrs",
                "t20", "taipan", "tempesta", "tezeract", "thrax", "tigon", "turismor", "tyrant", "tyrus",
                "vacca", "vagner", "visione", "voltic", "xa21", "zentorno", "zorrusso"
            });

            // Sports
            AddCategoryMenu(origCatsMenu, menuPool, "Esportivos (Sports)", new[]
            {
                "alpha", "banshee", "bestiagts", "blista2", "buffalo", "buffalo2", "buffalo3", "carbonizzare", "comet2",
                "comet3", "comet4", "comet5", "coquette", "coquette4", "drafter", "elegy", "elegy2", "feltzer2", "flashgt",
                "furoregt", "fusilade", "futo", "gb200", "hotring", "infernus2", "issi7", "italigto", "jester", "jester2",
                "jugular", "khamelion", "komoda", "kuruma", "locust", "lynx", "massacro", "massacro2", "neo", "neon",
                "ninef", "ninef2", "omnis", "paragon", "pariah", "penumbra", "raiden", "rapidgt", "raptor", "revolter",
                "ruston", "schafter2", "schafter3", "schafter4", "schlagen", "schwarzer", "sentinel3", "seven70", "specter",
                "streiter", "sugoi", "sultan", "surano", "tropos", "verlierer2"
            });

            // Sports Classics
            AddCategoryMenu(origCatsMenu, menuPool, "Clássicos Esportivos", new[]
            {
                "arwing", "casco", "cheetah2", "coquette2", "coquette3", "deluxo", "dynasty", "gt500", "infernusclassic",
                "jb700", "jb7002", "mamba", "manana", "manana2", "michelli", "monroe", "nebula", "peyote", "peyote2",
                "pigalle", "retinue", "retinue2", "savestra", "stinger", "stingergt", "stromberg", "swinger", "torero",
                "tornadob", "turismo2", "viseris", "ztype", "zion3"
            });

            // Muscle
            AddCategoryMenu(origCatsMenu, menuPool, "Muscle Cars", new[]
            {
                "blade", "buccaneer", "buccaneer2", "chino", "chino2", "clique", "coquette3", "deviant", "dominator",
                "dominator2", "dominator3", "dukes", "dukes2", "ellie", "faction", "faction2", "faction3", "gauntlet",
                "gauntlet2", "gauntlet3", "gauntlet4", "hermes", "hotknife", "hustler", "impaler", "imperator", "imperator2",
                "imperator3", "lurcher", "moonbeam", "moonbeam2", "nightshade", "phoenix", "picador", "ratloader", "ratloader2",
                "ruiner", "ruiner2", "sabregt", "sabregt2", "slamvan", "slamvan2", "slamvan3", "stalion", "tampa", "tampa3",
                "tulip", "vamos", "vigilante", "virgo", "virgo2", "virgo3", "voodoo", "voodoo2", "yosemite", "yosemite2"
            });

            // Sedans & Coupes & SUVs
            AddCategoryMenu(origCatsMenu, menuPool, "Sedans, Coupés e SUVs", new[]
            {
                "asea", "asterope", "cog55", "cognoscenti", "emperor", "fugitive", "glendale", "ingot", "intruder",
                "premier", "primo", "primo2", "regina", "romero", "schafter", "stanier", "stratum", "surge", "tailgater",
                "warrener", "washington", "exemplar", "f620", "felon", "felon2", "jackal", "oracle", "oracle2", "sentinel",
                "sentinel2", "windsor", "windsor2", "zion", "zion2", "baller", "baller2", "baller3", "baller4", "baller5",
                "baller6", "bjxl", "cavalcade", "cavalcade2", "contender", "dubsta", "dubsta2", "dubsta3", "fq2", "granger",
                "gresley", "habanero", "huntley", "landstalker", "landstalker2", "mesa", "mesa2", "mesa3", "novak", "patriot",
                "patriot2", "radi", "rebla", "rocoto", "seminole", "serrano", "toros", "xls"
            });

            // Motorcycles & Cycles
            AddCategoryMenu(origCatsMenu, menuPool, "Motos e Quadriciclos", new[]
            {
                "akuma", "avarus", "bagger", "bati", "bati2", "bf400", "carbonrs", "chimera", "cliffhanger", "daemon",
                "daemon2", "defiler", "deathbike", "deathbike2", "deathbike3", "diablous", "diablous2", "double", "enduro",
                "esskey", "faggio", "faggio2", "faggio3", "fcr", "fcr2", "gargoyle", "hakuchou", "hakuchou2", "hexer",
                "innovation", "lectro", "manchez", "nemesis", "nightblade", "oppressor", "oppressor2", "pcj", "ratbike",
                "rrocket", "ruffian", "sanchez", "sanchez2", "sanctus", "shotaro", "sovereign", "stryder", "thrust", "vader",
                "vindicator", "vortex", "wolfsbane", "zombiea", "zombieb", "bmx", "cruiser", "fixter", "scorcher", "tribike",
                "tribike2", "tribike3"
            });

            // Off-Road
            AddCategoryMenu(origCatsMenu, menuPool, "Off-Road", new[]
            {
                "bfinjection", "bifta", "blazer", "blazer2", "blazer3", "blazer4", "blazer5", "bodhi2", "brawler", "bruiser",
                "bruiser2", "bruiser3", "caracara", "caracara2", "dloader", "dune", "dune2", "dune3", "dune4", "dune5",
                "everon", "freecrawler", "hellion", "insurgent", "insurgent2", "insurgent3", "kamacho", "kalahari", "marshall",
                "menacer", "monster", "nightshark", "outlaw", "rancherxl", "rebel", "rebel2", "riata", "sandking", "sandking2",
                "technical", "technical2", "technical3", "trophytruck", "trophytruck2", "vagrant", "wastelander", "zhaba"
            });

            // Helicopters & Planes
            AddCategoryMenu(origCatsMenu, menuPool, "Helicópteros e Aviões", new[]
            {
                "akula", "annihilator", "buzzard", "buzzard2", "cargobob", "cargobob2", "cargobob3", "cargobob4", "frogger",
                "frogger2", "havok", "hunter", "maverick", "savage", "seasparrow", "skylift", "supervolito", "supervolito2",
                "swift", "swift2", "valkyrie", "valkyrie2", "volatus", "alphaz1", "avenger", "avenger2", "besra", "blimp",
                "blimp2", "blimp3", "bombushka", "cargoplane", "cuban800", "dodo", "duster", "howard", "hydra", "jet",
                "lazer", "luxor", "luxor2", "mammatus", "microlight", "miljet", "mogul", "molotok", "nimbus", "nokota",
                "pyro", "rogue", "seabreeze", "shamal", "starling", "strikeforce", "titan", "tula", "velum", "velum2",
                "vestra", "volatol"
            });

            // Boats & Military / Emergency
            AddCategoryMenu(origCatsMenu, menuPool, "Barcos, Militares e Emergência", new[]
            {
                "dinghy", "dinghy2", "dinghy3", "dinghy4", "jetmax", "marquis", "seashark", "seashark2", "seashark3",
                "speeder", "speeder2", "squalo", "submersible", "submersible2", "suntrap", "toro", "toro2", "tropic",
                "tropic2", "tug", "apc", "barracks", "barracks2", "barracks3", "chernobog", "crusader", "halftrack",
                "khanjali", "minitank", "rhino", "scarab", "scarab2", "scarab3", "thruster", "trailersmall2", "vetir",
                "ambulance", "fbi", "fbi2", "firetruk", "pbus", "police", "police2", "police3", "police4", "policeb",
                "policet", "pranger", "predator", "riot", "riot2", "sheriff", "sheriff2"
            });
        }

        private void AddCategoryMenu(UIMenu parent, MenuPool pool, string title, string[] models)
        {
            var catMenu = pool.AddSubMenu(parent, title);
            foreach (var modelName in models)
            {
                string m = modelName;
                var item = new UIMenuItem(m.ToUpper(), "Spawmar " + m);
                item.Activated += (sender, selected) =>
                {
                    SpawnVehicle(m);
                };
                catMenu.AddItem(item);
            }
        }

        private void SpawnVehicle(string modelName)
        {
            try
            {
                Ped player = Game.Player.Character;
                Model model = new Model(modelName);
                model.Request(2500);
                if (model.IsInCdImage && model.IsValid)
                {
                    Vector3 spawnPos = player.Position + player.ForwardVector * 4.0f;
                    Vehicle v = World.CreateVehicle(model, spawnPos, player.Heading);
                    if (v != null && v.Exists())
                    {
                        // Anti-Despawn: mission entity, persistent, owned by player, registered decorator
                        Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, v.Handle, true, true);
                        v.IsPersistent = true;
                        Function.Call(Hash.SET_VEHICLE_HAS_BEEN_OWNED_BY_PLAYER, v.Handle, true);
                        v.NeedsToBeHotwired = false;

                        if (!Function.Call<bool>(Hash.DECOR_IS_REGISTERED_AS_TYPE, "Player_Vehicle", 3))
                        {
                            Function.Call(Hash.DECOR_REGISTER, "Player_Vehicle", 3);
                        }
                        Function.Call(Hash.DECOR_SET_INT, v.Handle, "Player_Vehicle", Game.Player.Handle);

                        player.SetIntoVehicle(v, VehicleSeat.Driver);
                        v.PlaceOnGround();
                        if (_vehicleGodMode)
                        {
                            v.IsInvincible = true;
                            v.CanBeVisiblyDamaged = false;
                        }
                        UI.Notify("~g~Veículo " + modelName.ToUpper() + " criado!");
                    }
                }
                else
                {
                    UI.Notify("~r~Modelo inválido ou não carregado: " + modelName);
                }
                model.MarkAsNoLongerNeeded();
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Erro ao spawmar: " + ex.Message);
            }
        }

        public void OnTick()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            if (player.IsInVehicle())
            {
                Vehicle v = player.CurrentVehicle;
                if (v != null && v.Exists())
                {
                    if (_vehicleGodMode)
                    {
                        v.IsInvincible = true;
                        v.CanBeVisiblyDamaged = false;
                        v.Health = 1000;
                    }

                    if (_speedBoost && Game.IsKeyPressed(Keys.ShiftKey))
                    {
                        v.ApplyForce(v.ForwardVector * 1.5f);
                    }

                    if (_hornJump && Game.IsControlJustPressed(0, GTA.Control.VehicleHorn))
                    {
                        v.ApplyForce(new Vector3(0.0f, 0.0f, 10.0f));
                    }

                    // Drive on Water
                    if (_driveOnWater)
                    {
                        OutputArgument outHeight = new OutputArgument();
                        bool hasWater = Function.Call<bool>(Hash.GET_WATER_HEIGHT, v.Position.X, v.Position.Y, v.Position.Z, outHeight);
                        if (hasWater)
                        {
                            float waterHeight = outHeight.GetResult<float>();
                            if (v.Position.Z <= waterHeight + 1.2f && v.Position.Z >= waterHeight - 3.5f)
                            {
                                Vector3 vel = v.Velocity;
                                v.Velocity = new Vector3(vel.X, vel.Y, 0f);
                                v.Position = new Vector3(v.Position.X, v.Position.Y, waterHeight);
                                v.Rotation = new Vector3(0f, 0f, v.Rotation.Z);
                            }
                        }
                    }

                    // Digital Speedometer HUD
                    if (_speedometer)
                    {
                        float kmh = v.Speed * 3.6f;
                        int gear = v.CurrentGear;
                        string text = string.Format("{0:0} KM/H  |  M: {1}", kmh, gear);
                        new UIText(text, new System.Drawing.Point(UI.WIDTH - 180, UI.HEIGHT - 70), 0.6f, System.Drawing.Color.FromArgb(255, 0, 230, 255), GTA.Font.ChaletComprimeCologne, false, true, true).Draw();
                    }

                    // Autopilot Waypoint arrival check
                    if (_autopilot)
                    {
                        if (!Game.IsWaypointActive)
                        {
                            _autopilot = false;
                            if (_autopilotItem != null) _autopilotItem.Checked = false;
                            player.Task.ClearAll();
                            UI.Notify("~y~Piloto automático finalizado: sem destino.");
                        }
                        else
                        {
                            Vector3 wp = World.GetWaypointPosition();
                            float dist = World.GetDistance(v.Position, wp);
                            if (dist < 25.0f)
                            {
                                _autopilot = false;
                                if (_autopilotItem != null) _autopilotItem.Checked = false;
                                player.Task.ClearAll();
                                v.HandbrakeOn = true;
                                UI.Notify("~g~Você chegou ao seu destino!");
                            }
                        }
                    }
                }
            }
            else
            {
                if (_autopilot)
                {
                    _autopilot = false;
                    if (_autopilotItem != null) _autopilotItem.Checked = false;
                }
            }
        }
    }
}
