using System;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using NativeUI;
using YnixTrainer.Core;

namespace YnixTrainer.Modules
{
    public class VehicleCustomizationModule : IYnixModule
    {
        public string Name { get { return "Customization"; } }

        public void Initialize(UIMenu mainMenu, MenuPool menuPool)
        {
            var customMenu = menuPool.AddSubMenu(mainMenu, Localization.Get("General", "SubmenuCustomization", "Oficina e Customização (LSC)"));

            // 1. Performance
            var perfMenu = menuPool.AddSubMenu(customMenu, Localization.Get("Customization", "PerformanceMenu", "Desempenho e Motor"));
            AddModListItem(perfMenu, Localization.Get("Customization", "Engine", "Nível do Motor"), VehicleMod.Engine);
            AddModListItem(perfMenu, Localization.Get("Customization", "Brakes", "Freios"), VehicleMod.Brakes);
            AddModListItem(perfMenu, Localization.Get("Customization", "Transmission", "Transmissão"), VehicleMod.Transmission);
            AddModListItem(perfMenu, Localization.Get("Customization", "Suspension", "Suspensão"), VehicleMod.Suspension);
            AddModListItem(perfMenu, Localization.Get("Customization", "Armor", "Blindagem"), VehicleMod.Armor);
            AddToggleModItem(perfMenu, Localization.Get("Customization", "Turbo", "Turbo Tuning"), VehicleToggleMod.Turbo);
            AddToggleModItem(perfMenu, Localization.Get("Customization", "XenonHeadlights", "Faróis Xenon"), VehicleToggleMod.XenonHeadlights);

            // 2. Bodywork
            var bodyMenu = menuPool.AddSubMenu(customMenu, Localization.Get("Customization", "BodyworkMenu", "Lataria e Peças Visuais"));
            AddModListItem(bodyMenu, Localization.Get("Customization", "Spoilers", "Aerofólio"), VehicleMod.Spoilers);
            AddModListItem(bodyMenu, Localization.Get("Customization", "FrontBumper", "Pára-choque Dianteiro"), VehicleMod.FrontBumper);
            AddModListItem(bodyMenu, Localization.Get("Customization", "RearBumper", "Pára-choque Traseiro"), VehicleMod.RearBumper);
            AddModListItem(bodyMenu, Localization.Get("Customization", "SideSkirt", "Saias Laterais"), VehicleMod.SideSkirt);
            AddModListItem(bodyMenu, Localization.Get("Customization", "Exhaust", "Escapamento"), VehicleMod.Exhaust);
            AddModListItem(bodyMenu, Localization.Get("Customization", "Grille", "Grade Dianteira"), VehicleMod.Grille);
            AddModListItem(bodyMenu, Localization.Get("Customization", "Hood", "Capô"), VehicleMod.Hood);
            AddModListItem(bodyMenu, Localization.Get("Customization", "Fender", "Pára-lama"), VehicleMod.Fender);
            AddModListItem(bodyMenu, Localization.Get("Customization", "Roof", "Teto"), VehicleMod.Roof);
            AddModListItem(bodyMenu, Localization.Get("Customization", "Livery", "Pintura Especial / Decalque"), VehicleMod.Livery);

            // 3. Paint & Colors
            var paintMenu = menuPool.AddSubMenu(customMenu, Localization.Get("Customization", "PaintMenu", "Pintura e Cores"));
            AddColorSelector(paintMenu, Localization.Get("Customization", "PrimaryColor", "Cor Primária"), true);
            AddColorSelector(paintMenu, Localization.Get("Customization", "SecondaryColor", "Cor Secundária"), false);

            // 4. Neons
            var neonMenu = menuPool.AddSubMenu(customMenu, Localization.Get("Customization", "NeonsMenu", "Luzes e Kits de Neon"));
            var toggleNeons = new UIMenuCheckboxItem(Localization.Get("Customization", "ToggleAllNeons", "Ligar/Desligar Neons"), false, "Ativa luzes sob o chassi");
            toggleNeons.CheckboxEvent += (sender, state) =>
            {
                Vehicle v = GetPlayerVehicle();
                if (v != null)
                {
                    v.SetNeonLightsOn(VehicleNeonLight.Left, state);
                    v.SetNeonLightsOn(VehicleNeonLight.Right, state);
                    v.SetNeonLightsOn(VehicleNeonLight.Front, state);
                    v.SetNeonLightsOn(VehicleNeonLight.Back, state);
                }
            };
            neonMenu.AddItem(toggleNeons);

            var neonColorList = new List<dynamic> { "Azul", "Vermelho", "Verde", "Amarelo", "Roxo", "Laranja", "Branco", "Rosa", "Ciano" };
            var neonColors = new[] { Color.DeepSkyBlue, Color.Red, Color.LimeGreen, Color.Yellow, Color.Purple, Color.Orange, Color.White, Color.HotPink, Color.Cyan };
            var neonColorItem = new UIMenuListItem(Localization.Get("Customization", "NeonColor", "Cor do Neon"), neonColorList, 0, "Altera a cor do neon");
            neonColorItem.OnListChanged += (sender, index) =>
            {
                Vehicle v = GetPlayerVehicle();
                if (v != null)
                {
                    v.NeonLightsColor = neonColors[index];
                }
            };
            neonMenu.AddItem(neonColorItem);

            // 5. Window Tint
            var tintMenu = menuPool.AddSubMenu(customMenu, Localization.Get("Customization", "WindowTintMenu", "Fumê dos Vidros (Insulfilm)"));
            var tintList = new List<dynamic> { "Nenhum", "Fumê Claro", "Fumê Escuro", "Limo", "Preto Puro", "Verde" };
            var tintValues = new[] { VehicleWindowTint.None, VehicleWindowTint.LightSmoke, VehicleWindowTint.DarkSmoke, VehicleWindowTint.Limo, VehicleWindowTint.PureBlack, VehicleWindowTint.Green };
            var tintItem = new UIMenuListItem(Localization.Get("Customization", "WindowTint", "Tom do Fumê"), tintList, 0, "Altera o insulfilm");
            tintItem.OnListChanged += (sender, index) =>
            {
                Vehicle v = GetPlayerVehicle();
                if (v != null)
                {
                    v.WindowTint = tintValues[index];
                }
            };
            tintMenu.AddItem(tintItem);

            // 6. Wheels & Tires
            var wheelsMenu = menuPool.AddSubMenu(customMenu, Localization.Get("Customization", "WheelsMenu", "Rodas e Pneus"));
            var bpTires = new UIMenuCheckboxItem(Localization.Get("Customization", "BulletproofTires", "Pneus à Prova de Balas"), false, "Nunca furam");
            bpTires.CheckboxEvent += (sender, state) =>
            {
                Vehicle v = GetPlayerVehicle();
                if (v != null)
                {
                    v.CanTiresBurst = !state;
                }
            };
            wheelsMenu.AddItem(bpTires);

            // 7. License Plates
            var plateMenu = menuPool.AddSubMenu(customMenu, Localization.Get("Customization", "PlateMenu", "Placa do Veículo"));
            var plateStyles = new List<dynamic> { "Azul no Branco", "Amarelo no Preto", "Amarelo no Azul", "Azul no Branco 2", "North Yankton", "Preto San Andreas" };
            var plateItem = new UIMenuListItem(Localization.Get("Customization", "PlateStyle", "Estilo da Placa"), plateStyles, 0, "Altera o modelo da placa");
            plateItem.OnListChanged += (sender, index) =>
            {
                Vehicle v = GetPlayerVehicle();
                if (v != null)
                {
                    v.NumberPlateType = (NumberPlateType)index;
                }
            };
            plateMenu.AddItem(plateItem);

            var plateTextItem = new UIMenuItem(Localization.Get("Customization", "CustomPlateText", "Digitar Texto da Placa"), Localization.Get("Customization", "CustomPlateTextDesc", "Altera o texto da placa"));
            plateTextItem.Activated += (sender, selected) =>
            {
                Vehicle v = GetPlayerVehicle();
                if (v != null)
                {
                    string input = Game.GetUserInput(8);
                    if (!string.IsNullOrEmpty(input))
                    {
                        v.NumberPlate = input.ToUpper();
                        UI.Notify("~g~Placa alterada para: ~w~" + input.ToUpper());
                    }
                }
            };
            plateMenu.AddItem(plateTextItem);
        }

        private Vehicle GetPlayerVehicle()
        {
            Ped player = Game.Player.Character;
            if (player.IsInVehicle())
            {
                Vehicle v = player.CurrentVehicle;
                v.InstallModKit();
                return v;
            }
            UI.Notify("~r~" + Localization.Get("Customization", "NoVehicleMsg", "Você precisa estar dentro de um veículo!"));
            return null;
        }

        private void AddModListItem(UIMenu menu, string label, VehicleMod mod)
        {
            // Clean numbers: Stock, 1, 2, 3, 4, 5...
            var options = new List<dynamic>();
            options.Add("Stock");
            for (int i = 1; i <= 25; i++)
            {
                options.Add(i.ToString());
            }

            var item = new UIMenuListItem(label, options, 0, "Altera " + label);
            item.OnListChanged += (sender, index) =>
            {
                Vehicle v = GetPlayerVehicle();
                if (v != null)
                {
                    int modIndex = index - 1; // 0 -> Stock (-1 in GTA)
                    v.SetMod(mod, modIndex, false);
                }
            };
            menu.AddItem(item);
        }

        private void AddToggleModItem(UIMenu menu, string label, VehicleToggleMod toggleMod)
        {
            var item = new UIMenuCheckboxItem(label, false, "Ativa ou desativa " + label);
            item.CheckboxEvent += (sender, state) =>
            {
                Vehicle v = GetPlayerVehicle();
                if (v != null)
                {
                    v.ToggleMod(toggleMod, state);
                }
            };
            menu.AddItem(item);
        }

        private void AddColorSelector(UIMenu menu, string label, bool isPrimary)
        {
            var colorNames = new List<dynamic> { "Preto", "Branco", "Vermelho", "Azul", "Amarelo", "Verde Lima", "Laranja", "Ouro Puro", "Prata", "Roxo" };
            var colorValues = new[] { VehicleColor.MetallicBlack, VehicleColor.PureWhite, VehicleColor.MetallicFormulaRed, VehicleColor.MetallicBlue, VehicleColor.MetallicTaxiYellow, VehicleColor.MatteLimeGreen, VehicleColor.MetallicOrange, VehicleColor.PureGold, VehicleColor.MetallicSilver, VehicleColor.MetallicSpinnakerBlue };

            var item = new UIMenuListItem(label, colorNames, 0, "Altera " + label);
            item.OnListChanged += (sender, index) =>
            {
                Vehicle v = GetPlayerVehicle();
                if (v != null)
                {
                    if (isPrimary) v.PrimaryColor = colorValues[index];
                    else v.SecondaryColor = colorValues[index];
                }
            };
            menu.AddItem(item);
        }

        public void OnTick() { }
    }
}
