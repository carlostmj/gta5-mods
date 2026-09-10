using System;
using GTA;
using GTA.Native;
using YnixPolice.Core;
using YnixPolice.Systems;

namespace YnixPolice
{
    public class YnixPoliceScript : Script
    {
        private TrafficStopSystem _trafficStop;
        private SurrenderSystem _surrender;
        private PoliceEscalationSystem _escalation;

        public YnixPoliceScript()
        {
            try
            {
                ConfigManager.Load();
                Localization.LoadLanguage(ConfigManager.Language);

                _trafficStop = new TrafficStopSystem();
                _surrender = new SurrenderSystem();
                _escalation = new PoliceEscalationSystem();

                Tick += OnTick;
                Interval = 0;

                UI.Notify("~b~Ynix Realistic Police v1.0.0.0 ~w~carregado!\nAbordagens de trânsito e rendição ativadas.");
            }
            catch (Exception ex)
            {
                UI.Notify("~r~Erro ao iniciar Ynix Police: " + ex.Message);
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                if (_trafficStop != null) _trafficStop.OnTick();
                if (_surrender != null) _surrender.OnTick();
                if (_escalation != null) _escalation.OnTick();
            }
            catch { }
        }
    }
}
