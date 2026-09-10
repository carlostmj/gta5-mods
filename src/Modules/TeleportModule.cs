using System;
using GTA;
using GTA.Math;
using NativeUI;
using YnixTrainer.Core;

namespace YnixTrainer.Modules
{
    public class TeleportModule : IYnixModule
    {
        public string Name { get { return "Teleport"; } }

        public void Initialize(UIMenu mainMenu, MenuPool menuPool)
        {
            var teleMenu = menuPool.AddSubMenu(mainMenu, Localization.Get("General", "SubmenuTeleport", "Teletransporte"));

            // Waypoint
            var wpItem = new UIMenuItem(Localization.Get("Teleport", "TeleportWaypoint", "Teleportar para Marcador"), Localization.Get("Teleport", "TeleportWaypointDesc", "Teleporta para o ponto marcado no mapa"));
            wpItem.Activated += (sender, selected) =>
            {
                Vector3 wp = World.GetWaypointPosition();
                if (wp != Vector3.Zero)
                {
                    float gz = World.GetGroundHeight(new Vector2(wp.X, wp.Y));
                    if (gz <= 0.0f) gz = wp.Z;
                    if (gz <= 0.0f) gz = 40.0f;
                    Vector3 dest = new Vector3(wp.X, wp.Y, gz + 1.2f);
                    Teleport(dest);
                    UI.Notify("~g~Teleportado para o Marcador!");
                }
                else
                {
                    UI.Notify("~r~Marque um ponto no mapa primeiro!");
                }
            };
            teleMenu.AddItem(wpItem);

            // Forward Teleport
            var fwdItem = new UIMenuItem(Localization.Get("Teleport", "ForwardTeleport", "Teleportar 3m para Frente"), Localization.Get("Teleport", "ForwardTeleportDesc", "Avança através de portas"));
            fwdItem.Activated += (sender, selected) =>
            {
                Ped player = Game.Player.Character;
                Teleport(player.Position + player.ForwardVector * 3.0f);
            };
            teleMenu.AddItem(fwdItem);

            // Famous Locations
            AddLocation(teleMenu, Localization.Get("Teleport", "MazeBank", "Topo do Maze Bank"), new Vector3(-75.0f, -818.0f, 326.0f));
            AddLocation(teleMenu, Localization.Get("Teleport", "VinewoodSign", "Letreiro de Vinewood"), new Vector3(711.0f, 1198.0f, 348.0f));
            AddLocation(teleMenu, Localization.Get("Teleport", "MountChiliad", "Topo do Mount Chiliad"), new Vector3(501.0f, 5604.0f, 797.0f));
            AddLocation(teleMenu, Localization.Get("Teleport", "Airport", "Aeroporto Internacional LS"), new Vector3(-1037.0f, -2737.0f, 20.0f));
            AddLocation(teleMenu, Localization.Get("Teleport", "DelPerroPier", "Pier Del Perro"), new Vector3(-1600.0f, -1041.0f, 13.0f));
            AddLocation(teleMenu, Localization.Get("Teleport", "FortZancudo", "Base Militar Fort Zancudo"), new Vector3(-2047.0f, 3132.0f, 33.0f));
            AddLocation(teleMenu, Localization.Get("Teleport", "Prison", "Presídio Bolingbroke"), new Vector3(1846.0f, 2585.0f, 46.0f));
            AddLocation(teleMenu, Localization.Get("Teleport", "SandyShoresAirfield", "Aeroporto Sandy Shores"), new Vector3(1734.0f, 3710.0f, 34.0f));
            AddLocation(teleMenu, Localization.Get("Teleport", "HumaneLabs", "Laboratório Humane Labs"), new Vector3(3615.0f, 3740.0f, 28.0f));
            AddLocation(teleMenu, Localization.Get("Teleport", "GalileoObservatory", "Observatório Galileo"), new Vector3(-438.0f, 1076.0f, 328.0f));
        }

        private void AddLocation(UIMenu menu, string label, Vector3 coords)
        {
            var item = new UIMenuItem(label, "Teleportar para " + label);
            item.Activated += (sender, selected) =>
            {
                Teleport(coords);
                UI.Notify("~b~Teleportado: " + label);
            };
            menu.AddItem(item);
        }

        private void Teleport(Vector3 pos)
        {
            Ped player = Game.Player.Character;
            if (player.IsInVehicle())
            {
                player.CurrentVehicle.Position = pos;
            }
            else
            {
                player.Position = pos;
            }
        }

        public void OnTick() { }
    }
}
