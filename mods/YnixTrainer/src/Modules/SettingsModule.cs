using System;
using System.Collections.Generic;
using GTA;
using NativeUI;
using YnixTrainer.Core;

namespace YnixTrainer.Modules
{
    public class SettingsModule : IYnixModule
    {
        public string Name { get { return "Settings"; } }

        public void Initialize(UIMenu mainMenu, MenuPool menuPool)
        {
            var settingsMenu = menuPool.AddSubMenu(mainMenu, Localization.Get("General", "SubmenuSettings", "Configurações"));

            // Language selector
            var langList = new List<dynamic> { "Português (pt-BR)", "English (en-US)" };
            int currentIdx = ConfigManager.Language == "en-US" ? 1 : 0;
            var langItem = new UIMenuListItem(Localization.Get("Settings", "Language", "Idioma"), langList, currentIdx, "Selecione o idioma / Select language");
            langItem.OnListChanged += (sender, index) =>
            {
                string selectedLang = index == 1 ? "en-US" : "pt-BR";
                ConfigManager.Language = selectedLang;
                ConfigManager.Save();
                Localization.LoadLanguage(selectedLang);
                UI.Notify("~g~Idioma alterado / Language changed: " + selectedLang);
            };
            settingsMenu.AddItem(langItem);

            // Reload addons.json
            var reloadAddonsItem = new UIMenuItem(Localization.Get("Settings", "ReloadAddons", "Recarregar addons.json"), "Atualiza a lista de carros de scripts/YnixTrainer/addons.json");
            reloadAddonsItem.Activated += (sender, selected) =>
            {
                AddonManager.Load();
                UI.Notify("~g~addons.json recarregado! " + AddonManager.Addons.Count + " veículos encontrados.");
            };
            settingsMenu.AddItem(reloadAddonsItem);

            // Version info
            var verItem = new UIMenuItem(Localization.Get("Settings", "VersionLabel", "Versão: v1.3.0.0"), "Versão oficial do Ynix Trainer");
            settingsMenu.AddItem(verItem);

            // Creator info
            var authorItem = new UIMenuItem(Localization.Get("Settings", "CreatorLabel", "Desenvolvido por Carlos"), "Ynix Trainer Oficial");
            settingsMenu.AddItem(authorItem);
        }

        public void OnTick() { }
    }
}
