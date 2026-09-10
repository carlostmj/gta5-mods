using System;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using YnixTrainer.Core;

namespace YnixTrainer.Modules
{
    public class WorldModule : IYnixModule
    {
        public string Name { get { return "World"; } }

        private bool _freezeTime = false;
        private bool _moonGravity = false;

        public void Initialize(UIMenu mainMenu, MenuPool menuPool)
        {
            var worldMenu = menuPool.AddSubMenu(mainMenu, Localization.Get("General", "SubmenuWorld", "Mundo e Tempo"));

            // Weather Submenu
            var weatherMenu = menuPool.AddSubMenu(worldMenu, Localization.Get("World", "Weather", "Alterar Clima"));
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetExtraSunny", "Muito Ensolarado"), Weather.ExtraSunny);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetClear", "Limpo"), Weather.Clear);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetClouds", "Nublado"), Weather.Clouds);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetRain", "Chuva"), Weather.Raining);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetThunder", "Tempestade"), Weather.ThunderStorm);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetSnow", "Neve"), Weather.Snowing);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetBlizzard", "Nevasca"), Weather.Blizzard);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetHalloween", "Halloween"), Weather.Halloween);

            // Time Submenu
            var timeMenu = menuPool.AddSubMenu(worldMenu, Localization.Get("World", "Time", "Alterar Horário"));
            AddTimeOption(timeMenu, Localization.Get("World", "Morning", "Manhã (06:00)"), 6);
            AddTimeOption(timeMenu, Localization.Get("World", "Noon", "Meio-dia (12:00)"), 12);
            AddTimeOption(timeMenu, Localization.Get("World", "Evening", "Entardecer (18:00)"), 18);
            AddTimeOption(timeMenu, Localization.Get("World", "Midnight", "Meia-noite (00:00)"), 0);

            // Freeze Time
            var freezeItem = new UIMenuCheckboxItem(Localization.Get("World", "FreezeTime", "Congelar Horário"), _freezeTime, Localization.Get("World", "FreezeTimeDesc", "Trava o relógio"));
            freezeItem.CheckboxEvent += (sender, state) =>
            {
                _freezeTime = state;
                Function.Call(Hash.PAUSE_CLOCK, _freezeTime);
            };
            worldMenu.AddItem(freezeItem);

            // Clear Area
            var clearAreaItem = new UIMenuItem(Localization.Get("World", "ClearArea", "Limpar Tráfego e Pedestres"), Localization.Get("World", "ClearAreaDesc", "Remove veículos e NPCs"));
            clearAreaItem.Activated += (sender, selected) =>
            {
                Vector3 p = Game.Player.Character.Position;
                Function.Call(Hash.CLEAR_AREA_OF_VEHICLES, p.X, p.Y, p.Z, 300.0f, false, false, false, false, false);
                Function.Call(Hash.CLEAR_AREA_OF_PEDS, p.X, p.Y, p.Z, 300.0f, 1);
                UI.Notify("~g~Área ao redor limpa com sucesso!");
            };
            worldMenu.AddItem(clearAreaItem);

            // Moon Gravity
            var gravityItem = new UIMenuCheckboxItem(Localization.Get("World", "MoonGravity", "Gravidade da Lua"), _moonGravity, Localization.Get("World", "MoonGravityDesc", "Gravidade ultraleve"));
            gravityItem.CheckboxEvent += (sender, state) =>
            {
                _moonGravity = state;
                Function.Call(Hash.SET_GRAVITY_LEVEL, _moonGravity ? 1 : 0);
            };
            worldMenu.AddItem(gravityItem);
        }

        private void AddWeatherOption(UIMenu menu, string label, Weather weather)
        {
            var item = new UIMenuItem(label, "Definir clima para " + label);
            item.Activated += (sender, selected) =>
            {
                World.Weather = weather;
                UI.Notify("~y~Clima alterado para: " + label);
            };
            menu.AddItem(item);
        }

        private void AddTimeOption(UIMenu menu, string label, int hour)
        {
            var item = new UIMenuItem(label, "Ajustar hora para " + label);
            item.Activated += (sender, selected) =>
            {
                World.CurrentDayTime = new TimeSpan(hour, 0, 0);
                UI.Notify("~y~Horário ajustado: " + label);
            };
            menu.AddItem(item);
        }

        public void OnTick()
        {
            if (_freezeTime)
            {
                Function.Call(Hash.PAUSE_CLOCK, true);
            }
            if (_moonGravity)
            {
                Function.Call(Hash.SET_GRAVITY_LEVEL, 1);
            }
        }
    }
}
