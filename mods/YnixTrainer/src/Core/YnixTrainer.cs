using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using NativeUI;
using YnixTrainer.Core;
using YnixTrainer.Modules;

namespace YnixTrainer
{
    public class YnixTrainerScript : Script
    {
        private MenuPool _menuPool;
        private UIMenu _mainMenu;
        private List<IYnixModule> _modules;
        private Keys _toggleKey = Keys.F4;
        private int _lastToggleTime = 0;

        public YnixTrainerScript()
        {
            try
            {
                ConfigManager.Load();
                Localization.LoadLanguage(ConfigManager.Language);

                try
                {
                    _toggleKey = (Keys)Enum.Parse(typeof(Keys), ConfigManager.MenuKey, true);
                }
                catch
                {
                    _toggleKey = Keys.F4;
                }

                _menuPool = new MenuPool();
                _menuPool.MouseEdgeEnabled = false;
                _menuPool.ResetCursorOnOpen = false;

                string title = Localization.Get("General", "MenuTitle", "Ynix Trainer");
                string subtitle = Localization.Get("General", "MenuSubtitle", "v1.4.0.0");
                _mainMenu = new UIMenu(title, subtitle);
                _menuPool.Add(_mainMenu);

                _modules = new List<IYnixModule>
                {
                    new PlayerModule(),
                    new VehicleModule(),
                    new VehicleCustomizationModule(),
                    new SkinChangerModule(),
                    new BodyguardModule(),
                    new MiscPowersModule(),
                    new AnimationModule(),
                    new WeaponModule(),
                    new TeleportModule(),
                    new WorldModule(),
                    new SettingsModule()
                };

                foreach (var module in _modules)
                {
                    try
                    {
                        module.Initialize(_mainMenu, _menuPool);
                    }
                    catch { }
                }

                _menuPool.RefreshIndex();

                Tick += OnTick;
                KeyDown += OnKeyDown;

                Interval = 0;

                UI.Notify("~b~Ynix Trainer v1.4.0.0 ~w~carregado!\nPressione ~g~" + _toggleKey.ToString() + " ~w~para abrir.");
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Erro ao iniciar Ynix Trainer: " + ex.Message);
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (_menuPool != null)
            {
                _menuPool.ProcessControl();
                _menuPool.Draw();

                if (_menuPool.IsAnyMenuOpen())
                {
                    Function.Call(Hash._SHOW_CURSOR_THIS_FRAME, false);
                }
            }

            if (_modules != null)
            {
                for (int i = 0; i < _modules.Count; i++)
                {
                    try
                    {
                        _modules[i].OnTick();
                    }
                    catch { }
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == _toggleKey)
            {
                // Debounce to prevent menu flickering
                int currentTime = Game.GameTime;
                if (currentTime - _lastToggleTime < 300) return;
                _lastToggleTime = currentTime;

                if (_menuPool != null)
                {
                    if (_menuPool.IsAnyMenuOpen())
                    {
                        _menuPool.CloseAllMenus();
                    }
                    else
                    {
                        _mainMenu.Visible = true;
                    }
                }
            }
        }
    }
}
