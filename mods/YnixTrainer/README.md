# Ynix Trainer for GTA V (PC Singleplayer)

[![Release](https://img.shields.io/badge/Release-v1.3.0.0-blue.svg)](https://github.com/carlostmj/gta5-mods/releases)
[![Platform](https://img.shields.io/badge/Platform-GTA%20V%20PC%20Singleplayer-brightgreen.svg)](https://www.gta5-mods.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](../../LICENSE)

---

## 🇬🇧 English Description (Mandatory for 5Mods)

### Short Description
**Ynix Trainer** is a modern, ultra-modular, and feature-rich script trainer designed specifically for **GTA V PC Singleplayer (Story Mode)**. Built with high performance in mind, it features a native GTA-style UI (using NativeUI), full multi-language localization (English and Portuguese), a built-in Los Santos Customs workshop for tuning anywhere, an anti-despawn vehicle spawner (fixing vanishing DLC cars), tactical AI bodyguards, a skin changer, superpowers (No-Clip, Thermal/Night Vision), scenarios, and digital HUD speedometer.

---

### Summary
* **Game**: Grand Theft Auto V (PC)
* **Mode**: Singleplayer / Story Mode Only
* **Version**: `v1.3.0.0` (First Official GitHub Release)
* **Author**: Carlos
* **Activation Key**: `F4` (Toggle Open/Close, customizable in `Config.ini`)

---

### Features & Capabilities

1. **🚗 Vehicle Spawner & Anti-Despawn Fix**:
   - Spawns any original GTA V vehicle categorized cleanly: *Super, Sports, Sports Classics, Muscle, Sedans/SUVs, Motorcycles, Off-Road, Aircraft, Boats/Military*.
   - **Anti-Despawn Engine**: Resolves the infamous GTA V singleplayer bug where spawned DLC/Add-on cars despawn after 0.2 seconds. Registers decorator `Player_Vehicle`, applies mission entity status, and sets permanent ownership.
   - **Custom Add-Ons via JSON**: Reads `scripts/YnixTrainer/addons.json` dynamically. Add any custom add-on car without modifying game code!
   - **Manual Add-On Input**: Spawn any model by typing its name in the in-game input prompt.
   - **Vehicle Utilities**: Fix & Wash, Vehicle God Mode, Max Performance Tuning, Nitro Boost (Hold SHIFT), Horn Jump, Flip Upright, Open/Close all doors, and Vehicle Delete.
   - **Drive on Water**: Drive any car or motorcycle across the surface of oceans and rivers.
   - **Autopilot (Waypoint Navigation)**: Vehicle drives itself automatically through traffic directly to your map waypoint.
   - **Digital Speedometer HUD**: Real-time HUD displaying speed in **KM/H** and current gear.

2. **🔧 In-Field Los Santos Customs (Manual Tuning Anywhere)**:
   - Tune and customize every single component of your current car on the fly:
     - **Performance**: Engine (Levels 1-4), Brakes, Transmission, Suspension, Armor (up to 100%), Turbo, and Xenon Headlights.
     - **Bodywork**: Spoilers, Front/Rear Bumpers, Side Skirts, Exhausts, Grilles, Hoods, Fenders, Roofs, and Liveries.
     - **Paint & Colors**: Primary and Secondary colors (Metallic, Matte, Pure Gold, Formula Red, etc.).
     - **Neon Underglow**: 9 vibrant neon colors with master toggle under the chassis.
     - **Window Tint (Insulfilm)**: Pure Black, Dark Smoke, Light Smoke, Limo, and Green.
     - **Wheels & Tires**: Bulletproof tires toggle.
     - **License Plate**: Plate styles and **custom in-game text input** (put your own name on the plate).

3. **🦸 Superpowers & Vision Modes**:
   - **No-Clip / Fly Mode**: Fly through the air and pass through walls. Controlled via `W`, `A`, `S`, `D`, `Space` (ascend), `Ctrl` (descend), and `Shift` (speed boost).
   - **Night Vision**: Military-grade green night vision filter.
   - **Thermal Vision**: Infrared body heat signature vision.

4. **🎭 Skin Changer**:
   - Switch models seamlessly:
     - **Main Characters**: Michael, Franklin, Trevor, Lamar, Lester, Amanda, Jimmy, Tracey, Dave, Solomon.
     - **Authorities & Military**: SWAT Officer, Marine / Army, Air Force Pilot, FIB Agent, Paramedic, Firefighter, Security Guard, Prison Guard.
     - **Animals**: Chop, Cat, Chimp, Seagull, Pigeon, Crow, Hen, Deer, Husky, Golden Retriever, German Shepherd, Poodle, Pug, Coyote, Boar, Rabbit, Tiger Shark, Dolphin, Killer Whale.
     - **Restore Original**: Returns safely to your original story character.

5. **🪖 Tactical Bodyguards**:
   - Summon armed AI companions: **SWAT Team**, **Military Marine**, or **Secret Agent**.
   - Bodyguards join the player's group, enter vehicles together, defend the player automatically, and have maximum armor.
   - **Dismiss All**: Safely dismisses and removes all active guards.

6. **🎬 Animations & Ambient Scenarios**:
   - Perform scenarios: Smoke cigarette, drink beer, party/dance, sit on ground/picnic, push-ups, yoga, play guitar, cheer, flex muscles, binoculars, sit on chair, guard stance.
   - **Stop Animation**: Instantly cancels any scenario and returns control.

7. **🔫 Weapons & Combat**:
   - Give All Weapons with full ammunition, Remove All Weapons, Infinite Ammo (no reload), Explosive Ammo, Fire / Incendiary Ammo, and 1-Hit Kill Super Damage.

8. **📍 Teleportation**:
   - Teleport to Waypoint, Forward Teleport (3m through locked doors/walls), and famous landmarks (Maze Bank roof, Mount Chiliad, LS Airport, Fort Zancudo, Del Perro Pier, etc.).

9. **☀️ World & Environment**:
   - Change Weather (Clear, Sunny, Clouds, Rain, Thunder, Snow, Blizzard, Halloween).
   - Change Time of Day (Morning, Noon, Evening, Midnight) and Freeze Time.
   - Moon Gravity and Clear Area (clears traffic and pedestrians).

10. **🌐 Multi-Language & Settings**:
    - Complete runtime language toggle between **Português (pt-BR)** and **English (en-US)**.
    - Reload `addons.json` on the fly without restarting the game.

---

### Known Issues & Limitations
* **Singleplayer Only**: This mod requires ScriptHookV and ScriptHookVDotNet, which automatically disable when GTA Online is launched.
* **Dependencies Required**: Must have Script Hook V, Script Hook V .NET, and NativeUI installed in your GTA V root directory.

---

### Requirements & Dependencies
Make sure you have installed the following prerequisites:
1. [Script Hook V](http://www.dev-c.com/gtav/scripthookv/) by Alexander Blade (v1.0.3351.0 or newer)
2. [Community Script Hook V .NET](https://github.com/crosire/scripthookvdotnet/releases) (v2.10.13 or newer)
3. [NativeUI](https://github.com/Guad/NativeUI/releases) (v1.9 or newer)

---

### Installation Instructions (Singleplayer)
1. Download the latest release from the [Releases](https://github.com/carlostmj/gta5-mods/releases) tab.
2. Ensure you have installed **Script Hook V**, **ScriptHookVDotNet**, and **NativeUI** in your Grand Theft Auto V root folder (`Grand Theft Auto V\`).
3. Copy the contents of the `package/scripts/` folder (or from the downloaded zip) directly into your `Grand Theft Auto V\scripts\` folder.
   - Resulting structure:
     - `Grand Theft Auto V\scripts\YnixTrainer.dll`
     - `Grand Theft Auto V\scripts\YnixTrainer\Config.ini`
     - `Grand Theft Auto V\scripts\YnixTrainer\addons.json`
     - `Grand Theft Auto V\scripts\YnixTrainer\Languages\pt-BR.ini`
     - `Grand Theft Auto V\scripts\YnixTrainer\Languages\en-US.ini`
4. Start GTA V Story Mode.
5. Press **F4** on your keyboard to open/close the trainer.

---

### Controls
* **F4**: Open / Close Trainer (Toggle)
* **Numpad 8 / Up Arrow**: Menu Up
* **Numpad 2 / Down Arrow**: Menu Down
* **Numpad 4 / Left Arrow**: Decrease Value / Option Left
* **Numpad 6 / Right Arrow**: Increase Value / Option Right
* **Numpad 5 / Enter**: Select
* **Numpad 0 / Backspace**: Back / Close Submenu

---

## 🇧🇷 Descrição em Português

### Resumo do Mod
O **Ynix Trainer** é um trainer completo, modular e de altíssimo desempenho para o **GTA V PC (Modo História)**. Possui interface nativa limpa, sistema de tradução dinâmica (Português e Inglês), oficina Los Santos Customs portátil em tempo real, correção de despawn para veículos DLC/Add-on, guarda-costas com inteligência artificial, troca de personagens (skins), superpoderes (No-Clip, Visão Noturna e Térmica), cenários/animações e velocímetro digital.

### Principais Recursos
* **Spawner com Anti-Despawn**: Corrige carros de DLC que somem sozinhos. Suporta `addons.json` e digitação manual de modelos.
* **Customização Manual Completa (LSC em Qualquer Lugar)**: Modifique motor, freios, turbo, aerofólio, cor primária/secundária, luzes de neon, insulfilm e digite sua própria placa.
* **Superpoderes**: Modo voo (No-Clip), visão noturna e visão térmica infravermelha.
* **Guarda-Costas**: Convoque soldados da SWAT, militares ou agentes que te protegem e entram no seu carro.
* **Troca de Skins**: Jogue como Michael, Franklin, Trevor, personagens secundários, autoridades ou animais.
* **Direção na Água e Piloto Automático**: Ande sobre a água e viaje até seu destino no mapa sozinho.

---

### Tags
`Trainer`, `Script`, `Add-On`, `Vehicle`, `Player`, `Weapons`, `Teleport`, `Singleplayer`, `NativeUI`

---

### Credits
* **Ynix Trainer Author**: Carlos
* **Script Hook V**: Alexander Blade
* **ScriptHookVDotNet**: crosire & contributors
* **NativeUI Library**: Guadmaz
