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

        private MenuPool _pool;
        private bool _vehicleGodMode = false;
        private bool _speedBoost = false;
        private bool _hornJump = false;
        private bool _vehicleJump = false;
        private bool _driveOnWater = false;
        private bool _speedometer = false;
        private bool _driftMode = false;
        private bool _seatbelt = true;
        private bool _autoRepair = false;
        private bool _superTorque = false;
        private bool _autoFlip = false;

        // Autopilot State
        private bool _autopilotActive = false;
        private int _autopilotStartTime = 0;
        private int _autopilotStyle = 786603; // Safe / Legal by default
        private float _autopilotSpeed = 28.0f; // ~100 km/h
        private Vector3 _autopilotDestination = Vector3.Zero;
        private int _brakeHoldCounter = 0;
        private readonly List<Prop> _spawnedRamps = new List<Prop>();

        public void Initialize(UIMenu mainMenu, MenuPool menuPool)
        {
            _pool = menuPool;
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

            // Auto Repair
            var autoRepItem = new UIMenuCheckboxItem("Auto-Reparo Contínuo", _autoRepair, "Conserta o veículo instantaneamente sempre que sofrer dano");
            autoRepItem.CheckboxEvent += (sender, state) => { _autoRepair = state; };
            vehMenu.AddItem(autoRepItem);

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

            // Seatbelt (Anti-Fly through windshield & anti-fall from bikes)
            var seatItem = new UIMenuCheckboxItem("Cinto de Segurança (Anti-Ejeção)", _seatbelt, "Nunca é ejetado pelo pára-brisa em batidas e não cai da moto");
            seatItem.CheckboxEvent += (sender, state) =>
            {
                _seatbelt = state;
                Ped player = Game.Player.Character;
                if (player != null && player.Exists())
                {
                    Function.Call(Hash.SET_PED_CONFIG_FLAG, player.Handle, 32, !_seatbelt);
                    Function.Call(Hash.SET_PED_CAN_BE_KNOCKED_OFF_VEHICLE, player.Handle, _seatbelt ? 1 : 0);
                }
            };
            vehMenu.AddItem(seatItem);

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

            // Speed Boost / NOS
            var boostItem = new UIMenuCheckboxItem("Nitro Boost / NOS (Segure SHIFT ou X)", _speedBoost, "Aceleração insana com efeito de velocidade ao segurar SHIFT ou X");
            boostItem.CheckboxEvent += (sender, state) => { _speedBoost = state; };
            vehMenu.AddItem(boostItem);

            // Super Torque
            var torqueItem = new UIMenuCheckboxItem("Super Motor (Torque 3x)", _superTorque, "Multiplica o torque do motor para aceleração brutal");
            torqueItem.CheckboxEvent += (sender, state) => { _superTorque = state; };
            vehMenu.AddItem(torqueItem);

            // Vehicle Jump (Ruiner 2000 style)
            var jumpItem = new UIMenuCheckboxItem("Pulo de Veículo (Espaço)", _vehicleJump, "Pressione ESPAÇO para saltar por cima de obstáculos e outros carros");
            jumpItem.CheckboxEvent += (sender, state) => { _vehicleJump = state; };
            vehMenu.AddItem(jumpItem);

            // Horn Jump
            var hornItem = new UIMenuCheckboxItem(Localization.Get("Vehicle", "HornJump", "Pulo com Buzina"), _hornJump, Localization.Get("Vehicle", "HornJumpDesc", "Buzine para dar um salto"));
            hornItem.CheckboxEvent += (sender, state) => { _hornJump = state; };
            vehMenu.AddItem(hornItem);

            // Auto-Flip
            var autoFlipItem = new UIMenuCheckboxItem("Auto-Desvirar (Anti-Capotamento)", _autoFlip, "Corrige e desvira o veículo automaticamente se capotar");
            autoFlipItem.CheckboxEvent += (sender, state) => { _autoFlip = state; };
            vehMenu.AddItem(autoFlipItem);

            // Drift Mode
            var driftItem = new UIMenuCheckboxItem("Modo Drift", _driftMode, "Reduz a aderência traseira para manobras de drift fluidas");
            driftItem.CheckboxEvent += (sender, state) => { _driftMode = state; };
            vehMenu.AddItem(driftItem);

            // Drive on Water (Jesus Car)
            var waterItem = new UIMenuCheckboxItem(Localization.Get("Vehicle", "DriveOnWater", "Dirigir sobre a Água (Jesus Car)"), _driveOnWater, Localization.Get("Vehicle", "DriveOnWaterDesc", "O carro navega sobre a água sem afundar ou quebrar o motor"));
            waterItem.CheckboxEvent += (sender, state) => { _driveOnWater = state; };
            vehMenu.AddItem(waterItem);

            // Spawn Instant Stunt Ramp
            var rampItem = new UIMenuItem("Criar Rampa Acrobática à Frente", "Spawna uma rampa de salto perfeita 15 metros à frente do carro");
            rampItem.Activated += (sender, selected) =>
            {
                Ped player = Game.Player.Character;
                if (!player.IsInVehicle())
                {
                    UI.Notify("~r~Você precisa estar em um veículo!");
                    return;
                }
                Vehicle v = player.CurrentVehicle;
                Vector3 rampPos = v.Position + v.ForwardVector * 16.0f;
                Model rampModel = new Model("prop_mp_ramp_01");
                rampModel.Request(1500);
                if (rampModel.IsInCdImage && rampModel.IsValid)
                {
                    Prop ramp = World.CreateProp(rampModel, rampPos, new Vector3(0, 0, v.Heading), false, false);
                    if (ramp != null && ramp.Exists())
                    {
                        Function.Call(Hash.PLACE_OBJECT_ON_GROUND_PROPERLY, ramp.Handle);
                        ramp.HasCollision = true;
                        _spawnedRamps.Add(ramp);
                        UI.Notify("~g~Rampa de salto criada! Acelere!");
                    }
                }
                rampModel.MarkAsNoLongerNeeded();
            };
            vehMenu.AddItem(rampItem);

            var clearRamps = new UIMenuItem("Remover Rampas Criadas", "Limpa todas as rampas acrobáticas do mapa");
            clearRamps.Activated += (sender, selected) =>
            {
                int count = 0;
                foreach (var r in _spawnedRamps)
                {
                    if (r != null && r.Exists()) { r.Delete(); count++; }
                }
                _spawnedRamps.Clear();
                UI.Notify("~y~" + count + " rampa(s) removida(s).");
            };
            vehMenu.AddItem(clearRamps);

            // -------------------------------------------------------------
            // AUTOPILOT SUBMENU
            // -------------------------------------------------------------
            var autoMenu = menuPool.AddSubMenu(vehMenu, "Piloto Automático Inteligente (Waypoint)");

            var autoToggle = new UIMenuItem("Iniciar Piloto Automático", "Conduz autonomamente até o marcador GPS usando IA avançada");
            autoToggle.Activated += (sender, selected) =>
            {
                StartAutopilot();
            };
            autoMenu.AddItem(autoToggle);

            var autoCancel = new UIMenuItem("Cancelar Piloto Automático", "Assume o controle manual do carro imediatamente");
            autoCancel.Activated += (sender, selected) =>
            {
                CancelAutopilot(true, "~g~Piloto automático cancelado manualmente.");
            };
            autoMenu.AddItem(autoCancel);

            var styleList = new List<dynamic> { "Seguro / Legal (Obedece Regras)", "Rápido / Desvia de Trânsito", "Fuga / Agressivo (Velocidade Total)" };
            var styleItem = new UIMenuListItem("Estilo de Condução", styleList, 0, "Altera o comportamento da IA no trânsito");
            styleItem.OnListChanged += (sender, index) =>
            {
                if (index == 0) _autopilotStyle = 786603;      // Safe, obeys traffic lights, avoids cars
                else if (index == 1) _autopilotStyle = 1074528293; // Fast, weaves through traffic, ignores red lights
                else _autopilotStyle = 2883621;                   // Rushing, ultra aggressive
                UI.Notify("~b~Estilo do Piloto Automático: ~w~" + styleList[index]);
            };
            autoMenu.AddItem(styleItem);

            var speedList = new List<dynamic> { "60 KM/H (Passeio)", "90 KM/H (Padrão)", "130 KM/H (Rápido)", "180 KM/H (Máximo)" };
            var speedVals = new[] { 17.0f, 25.0f, 36.0f, 50.0f };
            var speedItem = new UIMenuListItem("Velocidade de Cruzeiro", speedList, 1, "Define a velocidade alvo da IA");
            speedItem.OnListChanged += (sender, index) =>
            {
                _autopilotSpeed = speedVals[index];
                UI.Notify("~b~Velocidade do Piloto: ~w~" + speedList[index]);
            };
            autoMenu.AddItem(speedItem);

            // Speedometer
            var speedoItem = new UIMenuCheckboxItem(Localization.Get("Vehicle", "Speedometer", "Velocímetro Digital (KM/H)"), _speedometer, Localization.Get("Vehicle", "SpeedometerDesc", "Exibe velocidade e marcha na tela"));
            speedoItem.CheckboxEvent += (sender, state) => { _speedometer = state; };
            vehMenu.AddItem(speedoItem);

            // Flip Vehicle
            var flipItem = new UIMenuItem(Localization.Get("Vehicle", "FlipVehicle", "Desvirar Veículo Manualmente"), Localization.Get("Vehicle", "FlipVehicleDesc", "Desvira o carro"));
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
                    CancelAutopilot(true);
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

            var origCatsMenu = menuPool.AddSubMenu(spawnerMenu, Localization.Get("Vehicle", "SpawnCategories", "Categorias Originais do GTA V"));

            AddCategoryMenu(origCatsMenu, menuPool, "Super", new[]
            {
                "adder", "autarch", "banshee2", "bullet", "cheetah", "cyclone", "deveste", "entity2", "entityxf",
                "emerus", "fmj", "furia", "gp1", "infernus", "italigtb", "italigtb2", "krieger", "le7b", "nero",
                "nero2", "osiris", "penetrator", "pfister811", "proto", "reaper", "sc1", "sheava", "sultanrs",
                "t20", "taipan", "tempesta", "tezeract", "thrax", "tigon", "turismor", "tyrant", "tyrus",
                "vacca", "vagner", "visione", "voltic", "xa21", "zentorno", "zorrusso"
            });

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

            AddCategoryMenu(origCatsMenu, menuPool, "Clássicos Esportivos", new[]
            {
                "arwing", "casco", "cheetah2", "coquette2", "coquette3", "deluxo", "dynasty", "gt500", "infernusclassic",
                "jb700", "jb7002", "mamba", "manana", "manana2", "michelli", "monroe", "nebula", "peyote", "peyote2",
                "pigalle", "retinue", "retinue2", "savestra", "stinger", "stingergt", "stromberg", "swinger", "torero",
                "tornadob", "turismo2", "viseris", "ztype", "zion3"
            });

            AddCategoryMenu(origCatsMenu, menuPool, "Muscle Cars", new[]
            {
                "blade", "buccaneer", "buccaneer2", "chino", "chino2", "clique", "coquette3", "deviant", "dominator",
                "dominator2", "dominator3", "dukes", "dukes2", "ellie", "faction", "faction2", "faction3", "gauntlet",
                "gauntlet2", "gauntlet3", "gauntlet4", "hermes", "hotknife", "hustler", "impaler", "imperator", "imperator2",
                "imperator3", "lurcher", "moonbeam", "moonbeam2", "nightshade", "phoenix", "picador", "ratloader", "ratloader2",
                "ruiner", "ruiner2", "sabregt", "sabregt2", "slamvan", "slamvan2", "slamvan3", "stalion", "tampa", "tampa3",
                "tulip", "vamos", "vigilante", "virgo", "virgo2", "virgo3", "voodoo", "voodoo2", "yosemite", "yosemite2"
            });

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

            AddCategoryMenu(origCatsMenu, menuPool, "Off-Road", new[]
            {
                "bfinjection", "bifta", "blazer", "blazer2", "blazer3", "blazer4", "blazer5", "bodhi2", "brawler", "bruiser",
                "bruiser2", "bruiser3", "caracara", "caracara2", "dloader", "dune", "dune2", "dune3", "dune4", "dune5",
                "everon", "freecrawler", "hellion", "insurgent", "insurgent2", "insurgent3", "kamacho", "kalahari", "marshall",
                "menacer", "monster", "nightshark", "outlaw", "rancherxl", "rebel", "rebel2", "riata", "sandking", "sandking2",
                "technical", "technical2", "technical3", "trophytruck", "trophytruck2", "vagrant", "wastelander", "zhaba"
            });

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

        private void StartAutopilot()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || !player.IsInVehicle())
            {
                UI.Notify("~r~Você precisa estar dentro de um veículo para ativar o piloto automático!");
                return;
            }

            if (!Game.IsWaypointActive)
            {
                UI.Notify("~r~Marque um destino no mapa (Waypoint) primeiro!");
                return;
            }

            Vehicle v = player.CurrentVehicle;
            if (v == null || !v.Exists()) return;

            Vector3 wp = World.GetWaypointPosition();
            OutputArgument groundZ = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, wp.X, wp.Y, 500.0f, groundZ, false))
            {
                wp.Z = groundZ.GetResult<float>();
            }
            else
            {
                wp.Z = player.Position.Z;
            }

            _autopilotDestination = wp;
            _autopilotStartTime = Game.GameTime;
            _brakeHoldCounter = 0;
            _autopilotActive = true;

            // Configure AI driver capability
            Function.Call(Hash.SET_DRIVER_ABILITY, player.Handle, 1.0f);
            Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, player.Handle, 0.0f);
            Function.Call(Hash.SET_PED_KEEP_TASK, player.Handle, true);

            // Execute long range navigation native
            Function.Call(Hash.TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE,
                player.Handle,
                v.Handle,
                wp.X, wp.Y, wp.Z,
                _autopilotSpeed,
                _autopilotStyle,
                20.0f
            );

            // Close all trainer menus so controls don't clash
            if (_pool != null)
            {
                _pool.CloseAllMenus();
            }

            int dist = (int)World.GetDistance(v.Position, wp);
            UI.Notify("~g~Piloto Automático Ativado!\n~w~Destino: ~y~" + dist + "m\n~w~Segure ~r~FREIO (S) ~w~ou buzine para cancelar.");
        }

        private void CancelAutopilot(bool clearTask, string notifyMsg = null)
        {
            if (!_autopilotActive) return;
            _autopilotActive = false;
            _brakeHoldCounter = 0;

            Ped player = Game.Player.Character;
            if (clearTask && player != null && player.Exists())
            {
                player.Task.ClearAll();
                if (player.IsInVehicle())
                {
                    player.CurrentVehicle.HandbrakeOn = false;
                }
            }

            if (!string.IsNullOrEmpty(notifyMsg))
            {
                UI.Notify(notifyMsg);
            }
        }

        private void AddCategoryMenu(UIMenu parent, MenuPool pool, string title, string[] models)
        {
            var catMenu = pool.AddSubMenu(parent, title);
            foreach (var modelName in models)
            {
                string m = modelName;
                var item = new UIMenuItem(m.ToUpper(), "Spawmar " + m);
                item.Activated += (sender, selected) => { SpawnVehicle(m); };
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

            // Seatbelt
            if (_seatbelt)
            {
                Function.Call(Hash.SET_PED_CONFIG_FLAG, player.Handle, 32, false);
                Function.Call(Hash.SET_PED_CAN_BE_KNOCKED_OFF_VEHICLE, player.Handle, 1);
            }

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

                    if (_autoRepair && (v.Health < 1000 || v.EngineHealth < 1000))
                    {
                        v.Repair();
                    }

                    // Nitro Boost / NOS (SHIFT or X)
                    if (_speedBoost && (Game.IsKeyPressed(Keys.ShiftKey) || Game.IsKeyPressed(Keys.X)))
                    {
                        v.ApplyForce(v.ForwardVector * 1.8f);
                        v.MaxSpeed = 400.0f;
                    }

                    if (_superTorque && Game.IsControlPressed(0, GTA.Control.VehicleAccelerate))
                    {
                        v.EnginePowerMultiplier = 2.5f;
                    }

                    if (_driftMode)
                    {
                        Function.Call(Hash.SET_VEHICLE_REDUCE_GRIP, v.Handle, true);
                    }

                    // Horn Jump
                    if (_hornJump && Game.IsControlJustPressed(0, GTA.Control.VehicleHorn))
                    {
                        v.ApplyForce(new Vector3(0.0f, 0.0f, 9.0f));
                    }

                    // Vehicle Jump (Space)
                    if (_vehicleJump && Game.IsControlJustPressed(0, GTA.Control.VehicleHandbrake))
                    {
                        if (v.IsOnAllWheels || v.HeightAboveGround < 1.5f)
                        {
                            v.ApplyForce(new Vector3(0.0f, 0.0f, 9.5f));
                        }
                    }

                    // Auto-Flip (if upside down)
                    if (_autoFlip)
                    {
                        if (v.UpVector.Z < -0.1f || Math.Abs(v.Rotation.Y) > 75.0f || Math.Abs(v.Rotation.X) > 75.0f)
                        {
                            v.Rotation = new Vector3(0f, 0f, v.Rotation.Z);
                            v.PlaceOnGround();
                        }
                    }

                    // Drive on Water (Jesus Car)
                    if (_driveOnWater)
                    {
                        OutputArgument outHeight = new OutputArgument();
                        bool hasWater = Function.Call<bool>(Hash.GET_WATER_HEIGHT, v.Position.X, v.Position.Y, v.Position.Z, outHeight);
                        if (hasWater)
                        {
                            float waterHeight = outHeight.GetResult<float>();
                            if (v.Position.Z <= waterHeight + 1.2f && v.Position.Z >= waterHeight - 4.0f)
                            {
                                Vector3 vel = v.Velocity;
                                v.Velocity = new Vector3(vel.X, vel.Y, 0f);
                                v.Position = new Vector3(v.Position.X, v.Position.Y, waterHeight + 0.2f);
                                v.Rotation = new Vector3(0f, 0f, v.Rotation.Z);
                                Function.Call(Hash.SET_VEHICLE_ENGINE_ON, v.Handle, true, true, false);
                                Function.Call(Hash.SET_VEHICLE_UNDRIVEABLE, v.Handle, false);
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

                    // ---------------------------------------------------------
                    // Robust Autopilot Loop
                    // ---------------------------------------------------------
                    if (_autopilotActive)
                    {
                        // Check if player erased waypoint
                        if (!Game.IsWaypointActive)
                        {
                            CancelAutopilot(true, "~y~Piloto automático finalizado: Marcador removido.");
                            return;
                        }

                        // Check arrival distance
                        float dist = World.GetDistance(v.Position, _autopilotDestination);
                        if (dist < 22.0f)
                        {
                            CancelAutopilot(true, "~g~Destino alcançado com sucesso!");
                            return;
                        }

                        // Draw Autopilot HUD status
                        float currentKmh = v.Speed * 3.6f;
                        string hudText = string.Format("~b~PILOTO AUTOMÁTICO ~w~| Distância: ~y~{0:0}m ~w~| Velocidade: ~g~{1:0} km/h ~w~| Segure ~r~[S] ~w~ou buzine", dist, currentKmh);
                        new UIText(hudText, new System.Drawing.Point(UI.WIDTH / 2, UI.HEIGHT - 35), 0.45f, System.Drawing.Color.White, GTA.Font.ChaletComprimeCologne, true, false, true).Draw();

                        // Cancellation check (only after 1.5s grace period)
                        if (Game.GameTime - _autopilotStartTime > 1500)
                        {
                            // 1. Horn key
                            if (Game.IsControlJustPressed(0, GTA.Control.VehicleHorn) || Game.IsKeyPressed(Keys.E))
                            {
                                CancelAutopilot(true, "~g~Piloto automático cancelado pela buzina.");
                                return;
                            }

                            // 2. Handbrake
                            if (Game.IsControlJustPressed(0, GTA.Control.VehicleHandbrake) || Game.IsKeyPressed(Keys.Space))
                            {
                                CancelAutopilot(true, "~g~Freio de mão puxado: Controle reassumido.");
                                return;
                            }

                            // 3. Holding brake / reverse
                            if (Game.IsControlPressed(0, GTA.Control.VehicleBrake) || Game.IsKeyPressed(Keys.S))
                            {
                                _brakeHoldCounter++;
                                if (_brakeHoldCounter > 15) // ~250ms hold
                                {
                                    CancelAutopilot(true, "~g~Freio acionado: Controle manual restabelecido.");
                                    return;
                                }
                            }
                            else
                            {
                                _brakeHoldCounter = 0;
                            }
                        }
                    }
                }
            }
            else
            {
                if (_autopilotActive)
                {
                    CancelAutopilot(false);
                }
            }
        }
    }
}
