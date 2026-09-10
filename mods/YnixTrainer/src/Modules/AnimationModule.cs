using System;
using GTA;
using GTA.Native;
using NativeUI;
using YnixTrainer.Core;

namespace YnixTrainer.Modules
{
    public class AnimationModule : IYnixModule
    {
        public string Name { get { return "Animations"; } }

        public void Initialize(UIMenu mainMenu, MenuPool menuPool)
        {
            var animMenu = menuPool.AddSubMenu(mainMenu, Localization.Get("General", "SubmenuAnimations", "Animações e Cenários"));

            var stopItem = new UIMenuItem(
                Localization.Get("Animations", "StopAnim", "Parar Animação"),
                Localization.Get("Animations", "StopAnimDesc", "Retorna à postura normal")
            );
            stopItem.Activated += (sender, selected) =>
            {
                Ped p = Game.Player.Character;
                if (p != null && p.Exists())
                {
                    p.Task.ClearAllImmediately();
                    p.Task.ClearAll();
                    Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, p.Handle);
                    UI.Notify("~y~Animação cancelada.");
                }
            };
            animMenu.AddItem(stopItem);

            AddScenarioItem(animMenu, Localization.Get("Animations", "Smoke", "Fumar Cigarro"), "WORLD_HUMAN_SMOKING");
            AddScenarioItem(animMenu, Localization.Get("Animations", "DrinkBeer", "Beber Cerveja"), "WORLD_HUMAN_DRINKING");
            AddScenarioItem(animMenu, Localization.Get("Animations", "Party", "Festejar / Dançar"), "WORLD_HUMAN_PARTYING");
            AddScenarioItem(animMenu, Localization.Get("Animations", "Sit", "Sentar no Chão"), "WORLD_HUMAN_PICNIC");
            AddScenarioItem(animMenu, Localization.Get("Animations", "Pushups", "Fazer Flexões"), "WORLD_HUMAN_PUSH_UPS");
            AddScenarioItem(animMenu, "Fazer Yoga", "WORLD_HUMAN_YOGA");
            AddScenarioItem(animMenu, "Tocar Violão", "WORLD_HUMAN_MUSICIAN");
            AddScenarioItem(animMenu, "Comemorar / Aplaudir", "WORLD_HUMAN_CHEERING");
            AddScenarioItem(animMenu, "Exibir Músculos", "WORLD_HUMAN_MUSCLE_FLEX");
            AddScenarioItem(animMenu, "Binóculos", "WORLD_HUMAN_BINOCULARS");
            AddScenarioItem(animMenu, "Sentar na Cadeira", "PROP_HUMAN_SEAT_CHAIR");
            AddScenarioItem(animMenu, "Postura de Guarda", "WORLD_HUMAN_GUARD_STAND");
        }

        private void AddScenarioItem(UIMenu menu, string label, string scenarioName)
        {
            var item = new UIMenuItem(label, "Executar cenário " + label);
            item.Activated += (sender, selected) =>
            {
                Ped p = Game.Player.Character;
                if (p != null && p.Exists())
                {
                    p.Task.ClearAllImmediately();
                    Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, p.Handle, scenarioName, 0, true);
                    UI.Notify("~g~Executando: ~w~" + label);
                }
            };
            menu.AddItem(item);
        }

        public void OnTick()
        {
        }
    }
}
