using System;
using System.Collections.Generic;
using GTA;
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

            // Weather
            var weatherMenu = menuPool.AddSubMenu(worldMenu, Localization.Get("World", "Weather", "Alterar Clima"));
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetClear", "Limpo"), Weather.Clear);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetExtraSunny", "Muito Ensolarado"), Weather.ExtraSunny);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetClouds", "Nublado"), Weather.Clouds);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetRain", "Chuva"), Weather.Raining);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetThunder", "Tempestade com Trovões"), Weather.ThunderStorm);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetSnow", "Neve"), Weather.Snowing);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetBlizzard", "Nevasca"), Weather.Blizzard);
            AddWeatherOption(weatherMenu, Localization.Get("World", "SetHalloween", "Halloween"), Weather.Halloween);

            // Visual Filters
            var filterMenu = menuPool.AddSubMenu(worldMenu, "Filtros Visuais de Câmera (Timecycle)");
            AddFilterItem(filterMenu, "Efeito Matrix (Verde Futurista)", "spectator5");
            AddFilterItem(filterMenu, "Preto e Branco (Cinema Noir)", "cinema");
            AddFilterItem(filterMenu, "Pôr do Sol Dramático (Cores Vivas)", "v_sundown");
            AddFilterItem(filterMenu, "Neblina Sinistra (Silent Hill)", "prologue_fog");
            AddFilterItem(filterMenu, "Filtro Séphia / Velho Oeste", "hud_def_desat_cold");

            var resetFilterItem = new UIMenuItem("Restaurar Visual Normal", "Desativa qualquer filtro de câmera");
            resetFilterItem.Activated += (sender, selected) =>
            {
                Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
                UI.Notify("~g~Visual normal restaurado.");
            };
            filterMenu.AddItem(resetFilterItem);

            // Time
            var timeMenu = menuPool.AddSubMenu(worldMenu, Localization.Get("World", "Time", "Alterar Horário"));
            AddTimeOption(timeMenu, Localization.Get("World", "Morning", "Manhã (06:00)"), 6, 0);
            AddTimeOption(timeMenu, Localization.Get("World", "Noon", "Meio-dia (12:00)"), 12, 0);
            AddTimeOption(timeMenu, Localization.Get("World", "Evening", "Entardecer (18:00)"), 18, 0);
            AddTimeOption(timeMenu, Localization.Get("World", "Midnight", "Meia-noite (00:00)"), 0, 0);

            // Freeze Time
            var freezeItem = new UIMenuCheckboxItem(Localization.Get("World", "FreezeTime", "Congelar Horário"), _freezeTime, Localization.Get("World", "FreezeTimeDesc", "Trava o relógio"));
            freezeItem.CheckboxEvent += (sender, state) =>
            {
                _freezeTime = state;
                Function.Call(Hash.PAUSE_CLOCK, _freezeTime);
            };
            worldMenu.AddItem(freezeItem);

            // Clear Area
            var clearItem = new UIMenuItem(Localization.Get("World", "ClearArea", "Limpar Tráfego e Pedestres"), Localization.Get("World", "ClearAreaDesc", "Remove NPCs e carros"));
            clearItem.Activated += (sender, selected) =>
            {
                Ped player = Game.Player.Character;
                Function.Call(Hash.CLEAR_AREA_OF_PEDS, player.Position.X, player.Position.Y, player.Position.Z, 150.0f, 1);
                Function.Call(Hash.CLEAR_AREA_OF_VEHICLES, player.Position.X, player.Position.Y, player.Position.Z, 150.0f, false, false, false, false, false);
                UI.Notify("~g~Área ao redor limpa com sucesso!");
            };
            worldMenu.AddItem(clearItem);

            // Moon Gravity
            var moonItem = new UIMenuCheckboxItem(Localization.Get("World", "MoonGravity", "Gravidade da Lua"), _moonGravity, Localization.Get("World", "MoonGravityDesc", "Gravidade leve"));
            moonItem.CheckboxEvent += (sender, state) =>
            {
                _moonGravity = state;
                World.GravityLevel = _moonGravity ? 1 : 0;
            };
            worldMenu.AddItem(moonItem);
        }

        private void AddFilterItem(UIMenu menu, string label, string modifierName)
        {
            var item = new UIMenuItem(label, "Aplica o filtro de câmera " + label);
            item.Activated += (sender, selected) =>
            {
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER, modifierName);
                UI.Notify("~b~Filtro aplicado: ~w~" + label);
            };
            menu.AddItem(item);
        }

        private void AddWeatherOption(UIMenu menu, string label, Weather weather)
        {
            var item = new UIMenuItem(label, "Define o clima para " + label);
            item.Activated += (sender, selected) =>
            {
                World.Weather = weather;
                UI.Notify("~b~Clima alterado para: ~w~" + label);
            };
            menu.AddItem(item);
        }

        private void AddTimeOption(UIMenu menu, string label, int hours, int minutes)
        {
            var item = new UIMenuItem(label, "Ajusta o relógio para " + label);
            item.Activated += (sender, selected) =>
            {
                World.CurrentDayTime = new TimeSpan(hours, minutes, 0);
                UI.Notify("~b~Horário ajustado para: ~w~" + label);
            };
            menu.AddItem(item);
        }

        public void OnTick() { }
    }
}
