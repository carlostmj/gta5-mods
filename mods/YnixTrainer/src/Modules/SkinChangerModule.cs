using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using NativeUI;
using YnixTrainer.Core;

namespace YnixTrainer.Modules
{
    public class SkinChangerModule : IYnixModule
    {
        public string Name { get { return "SkinChanger"; } }

        private int _originalModelHash = 0;

        public void Initialize(UIMenu mainMenu, MenuPool menuPool)
        {
            var skinMenu = menuPool.AddSubMenu(mainMenu, Localization.Get("General", "SubmenuSkin", "Troca de Personagem (Skins)"));

            if (Game.Player.Character != null && Game.Player.Character.Exists())
            {
                _originalModelHash = Game.Player.Character.Model.Hash;
            }

            var restoreItem = new UIMenuItem(
                Localization.Get("Skins", "RestoreOriginal", "Restaurar Personagem Original"),
                Localization.Get("Skins", "RestoreOriginalDesc", "Retorna ao personagem inicial da história")
            );
            restoreItem.Activated += (sender, selected) =>
            {
                if (_originalModelHash != 0)
                {
                    ChangeSkinByHash(_originalModelHash, "Original");
                }
                else
                {
                    ChangeSkin("player_zero", "Michael");
                }
            };
            skinMenu.AddItem(restoreItem);

            var mainCharsMenu = menuPool.AddSubMenu(skinMenu, Localization.Get("Skins", "MainCharacters", "Personagens Principais"));
            AddSkinOption(mainCharsMenu, "Michael De Santa", "player_zero");
            AddSkinOption(mainCharsMenu, "Franklin Clinton", "player_one");
            AddSkinOption(mainCharsMenu, "Trevor Philips", "player_two");
            AddSkinOption(mainCharsMenu, "Lamar Davis", "ig_lamardavis");
            AddSkinOption(mainCharsMenu, "Lester Crest", "ig_lestercrest");
            AddSkinOption(mainCharsMenu, "Amanda De Santa", "ig_amanda_townley");
            AddSkinOption(mainCharsMenu, "Jimmy De Santa", "ig_jimmydisanto");
            AddSkinOption(mainCharsMenu, "Tracey De Santa", "ig_tracie");
            AddSkinOption(mainCharsMenu, "Dave Norton", "ig_davenorton");
            AddSkinOption(mainCharsMenu, "Solomon Richards", "ig_solomon");

            var authMenu = menuPool.AddSubMenu(skinMenu, Localization.Get("Skins", "Authorities", "Autoridades e Militares"));
            AddSkinOption(authMenu, "Policial SWAT", "s_m_y_swat_01");
            AddSkinOption(authMenu, "Fuzileiro / Exército", "s_m_y_marine_01");
            AddSkinOption(authMenu, "Piloto da Força Aérea", "s_m_m_pilot_02");
            AddSkinOption(authMenu, "Agente FIB", "s_m_m_fbi_01");
            AddSkinOption(authMenu, "Paramédico", "s_m_m_paramedic_01");
            AddSkinOption(authMenu, "Bombeiro", "s_m_y_fireman_01");
            AddSkinOption(authMenu, "Segurança Particular", "s_m_m_security_01");
            AddSkinOption(authMenu, "Guarda Prisional", "s_m_m_prisguard_01");

            var animalsMenu = menuPool.AddSubMenu(skinMenu, Localization.Get("Skins", "Animals", "Animais"));
            AddSkinOption(animalsMenu, "Chop (Rottweiler)", "a_c_chop");
            AddSkinOption(animalsMenu, "Gato", "a_c_cat_01");
            AddSkinOption(animalsMenu, "Chimpanzé", "a_c_chimp");
            AddSkinOption(animalsMenu, "Gaivota", "a_c_seagull");
            AddSkinOption(animalsMenu, "Pombo", "a_c_pigeon");
            AddSkinOption(animalsMenu, "Corvo", "a_c_crow");
            AddSkinOption(animalsMenu, "Galinha", "a_c_hen");
            AddSkinOption(animalsMenu, "Veado", "a_c_deer");
            AddSkinOption(animalsMenu, "Husky", "a_c_husky");
            AddSkinOption(animalsMenu, "Golden Retriever", "a_c_retriever");
            AddSkinOption(animalsMenu, "Pastor Alemão", "a_c_shepherd");
            AddSkinOption(animalsMenu, "Poodle", "a_c_poodle");
            AddSkinOption(animalsMenu, "Pug", "a_c_pug");
            AddSkinOption(animalsMenu, "Coiote", "a_c_coyote");
            AddSkinOption(animalsMenu, "Javali", "a_c_boar");
            AddSkinOption(animalsMenu, "Coelho", "a_c_rabbit_01");
            AddSkinOption(animalsMenu, "Tubarão Tigre", "a_c_sharktiger");
            AddSkinOption(animalsMenu, "Golfinho", "a_c_dolphin");
            AddSkinOption(animalsMenu, "Orca (Baleia Assassina)", "a_c_killerwhale");
        }

        private void AddSkinOption(UIMenu parent, string displayName, string modelName)
        {
            var item = new UIMenuItem(displayName, "Transformar em " + displayName);
            item.Activated += (sender, selected) =>
            {
                ChangeSkin(modelName, displayName);
            };
            parent.AddItem(item);
        }

        private void ChangeSkin(string modelName, string displayName)
        {
            try
            {
                Model model = new Model(modelName);
                model.Request(2500);
                if (model.IsInCdImage && model.IsValid)
                {
                    Function.Call(Hash.SET_PLAYER_MODEL, Game.Player.Handle, model.Hash);
                    Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, Game.Player.Character.Handle);
                    UI.Notify("~g~Skin alterada para ~w~" + displayName);
                }
                else
                {
                    UI.Notify("~r~Falha ao carregar modelo: " + modelName);
                }
                model.MarkAsNoLongerNeeded();
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Erro ao alterar skin: " + ex.Message);
            }
        }

        private void ChangeSkinByHash(int modelHash, string displayName)
        {
            try
            {
                Model model = new Model(modelHash);
                model.Request(2500);
                if (model.IsInCdImage && model.IsValid)
                {
                    Function.Call(Hash.SET_PLAYER_MODEL, Game.Player.Handle, model.Hash);
                    Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, Game.Player.Character.Handle);
                    UI.Notify("~g~Skin alterada para ~w~" + displayName);
                }
                else
                {
                    UI.Notify("~r~Falha ao restaurar modelo original.");
                }
                model.MarkAsNoLongerNeeded();
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Erro: " + ex.Message);
            }
        }

        public void OnTick()
        {
        }
    }
}
