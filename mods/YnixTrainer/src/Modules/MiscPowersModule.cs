using System;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using NativeUI;
using YnixTrainer.Core;

namespace YnixTrainer.Modules
{
    public class MiscPowersModule : IYnixModule
    {
        public string Name { get { return "Powers"; } }

        private bool _noClip = false;
        private bool _nightVision = false;
        private bool _thermalVision = false;

        public void Initialize(UIMenu mainMenu, MenuPool menuPool)
        {
            var powerMenu = menuPool.AddSubMenu(mainMenu, Localization.Get("General", "SubmenuPowers", "Superpoderes e Visão"));

            // NoClip
            var noClipItem = new UIMenuCheckboxItem(Localization.Get("Powers", "NoClip", "Modo Voo (No-Clip)"), _noClip, Localization.Get("Powers", "NoClipDesc", "Voe livremente pelo ar e atravesse paredes"));
            noClipItem.CheckboxEvent += (sender, state) =>
            {
                _noClip = state;
                Ped player = Game.Player.Character;
                if (player != null && player.Exists())
                {
                    player.HasCollision = !_noClip;
                    Function.Call(Hash.SET_ENTITY_COLLISION, player.Handle, !_noClip, true);
                    player.FreezePosition = _noClip;
                    if (!_noClip)
                    {
                        player.IsInvincible = false;
                    }
                }
            };
            powerMenu.AddItem(noClipItem);

            // Night Vision
            var nightItem = new UIMenuCheckboxItem(Localization.Get("Powers", "NightVision", "Visão Noturna"), _nightVision, Localization.Get("Powers", "NightVisionDesc", "Filtro verde militar"));
            nightItem.CheckboxEvent += (sender, state) =>
            {
                _nightVision = state;
                Function.Call(Hash.SET_NIGHTVISION, _nightVision);
            };
            powerMenu.AddItem(nightItem);

            // Thermal Vision
            var thermItem = new UIMenuCheckboxItem(Localization.Get("Powers", "ThermalVision", "Visão Térmica"), _thermalVision, Localization.Get("Powers", "ThermalVisionDesc", "Filtro infravermelho de calor"));
            thermItem.CheckboxEvent += (sender, state) =>
            {
                _thermalVision = state;
                Function.Call(Hash.SET_SEETHROUGH, _thermalVision);
            };
            powerMenu.AddItem(thermItem);
        }

        public void OnTick()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            if (_noClip)
            {
                player.HasCollision = false;
                Function.Call(Hash.SET_ENTITY_COLLISION, player.Handle, false, true);
                player.FreezePosition = true;

                float speed = 0.8f;
                if (Game.IsKeyPressed(Keys.ShiftKey)) speed = 2.5f;

                Vector3 camDir = GameplayCamera.Direction;
                Vector3 rightDir = Vector3.Cross(camDir, new Vector3(0, 0, 1));
                Vector3 newPos = player.Position;

                if (Game.IsKeyPressed(Keys.W)) newPos += camDir * speed;
                if (Game.IsKeyPressed(Keys.S)) newPos -= camDir * speed;
                if (Game.IsKeyPressed(Keys.D)) newPos += rightDir * speed;
                if (Game.IsKeyPressed(Keys.A)) newPos -= rightDir * speed;
                if (Game.IsKeyPressed(Keys.Space)) newPos.Z += speed;
                if (Game.IsKeyPressed(Keys.ControlKey)) newPos.Z -= speed;

                player.Position = newPos;
                player.Heading = GameplayCamera.Rotation.Z;
            }
        }
    }
}
