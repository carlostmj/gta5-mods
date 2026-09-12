# Ynix Trainer for GTA V (PC Singleplayer)

[![Release](https://img.shields.io/badge/Release-v1.6.0.0-blue.svg)](https://github.com/carlostmj/gta5-mods/releases)
[![Platform](https://img.shields.io/badge/Platform-GTA%20V%20PC%20Singleplayer-brightgreen.svg)](https://www.gta5-mods.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](../../LICENSE)

---

## 🇬🇧 English Description (Mandatory for 5Mods)

### Short Description
**Ynix Trainer** is a high-performance, ultra-modular, and standalone script trainer designed specifically for **GTA V PC Singleplayer (Story Mode)**. It features an in-game Los Santos Customs manual tuning workshop, an anti-despawn vehicle spawner (resolving the vanishing DLC/Add-on car bug), a customizable **Digital Speedometer HUD with both KM/H and MPH support**, an advanced AI Autopilot with cruise control, tactical AI bodyguards with full gang squad warfare, complete skin changer (with safe aquatic animal survival), superpowers (No-Clip fly mode with zero-fall damage, Night/Thermal vision), extreme weapons (Teleport Gun, Force Gun, RPG Ammo), drift mode, seatbelt anti-ejection, and full English/Portuguese localization.

---

### Summary
* **Game**: Grand Theft Auto V (PC)
* **Mode**: Singleplayer / Story Mode Only
* **Version**: `v1.6.0.0`
* **Author**: Carlos (carlostmj)
* **Activation Key**: `F4` (Toggle Open/Close, customizable in `Config.ini`)

---

### What's New in v1.6.0.0
1. **Digital Speedometer HUD (KM/H & MPH Dual Support)**:
   - Real-time HUD displaying accurate vehicle speed and transmission gear (`1-8`, and `R` for reverse).
   - **Toggle between KM/H and MPH**: Switch units anytime in **Vehicle Options** or **Settings**.
   - Choice is automatically saved in `Config.ini` (`SpeedUnit=KM/H` or `SpeedUnit=MPH`).
   - Clean, lightweight display positioned unobtrusively on the screen.
2. **Autonomous AI Autopilot**:
   - Cruise speed selector (`60`, `90`, `130`, `180` KM/H).
   - Traffic rammer & anti-stuck watchdog to maneuver through Los Santos traffic to your map waypoint.
   - Touch `W`, `S`, `A`, `D`, or `Space` anytime to instantly take back manual control with no key lockups.
3. **Full Localization & Config Polish**:
   - Complete support for English (`en-US`) and Portuguese (`pt-BR`).
   - Correct versioning and keybind customization in `Config.ini`.

---

### What's New in v1.4.0.0 - v1.5.0.0
1. **Gang Squad Warfare**: Spawn full 3-5 member squads (Ballas, Families, Vagos, SWAT, Military) with tactical orders (*Attack My Target, Hold Position, Regroup*) and Gang War Battle Simulator.
2. **Extreme Weapons**: Teleport Gun (teleport where you shoot), Force Gun (shockwave blast throws vehicles and peds), and real explosive RPG Ammo.
3. **Vehicle Super Handling**: Drift Mode (reduced rear traction for drifting), Seatbelt (never fly through windshield and no falling off bikes), Super Torque (3x engine multiplier), and Auto-Repair.
4. **Clean LSC Tuning**: Tuning parts now display clean, clutter-free numbers (`Stock`, `1`, `2`, `3`...) matching your vehicle's mod capabilities.
5. **Marine Life Protection**: Marine animals (sharks, dolphins, whales) survive indefinitely on land without suffocating.
6. **Safe No-Clip**: Disabling No-Clip in mid-air provides temporary invincibility until you safely touch the ground, eliminating fall damage deaths.
7. **Cinematic Visual Filters**: Matrix (green tint), Noir (Black & White), Dramatic Sunset, and Foggy Silent Hill timecycle modifiers.

---

### Full Features List

* **🏎️ Digital Speedometer HUD**:
  - Real-time speed readout in **KM/H** or **MPH** (selectable in menu).
  - Real-time current transmission gear indicator (`1-8`, `R` for reverse).
* **🚗 Vehicle Spawner & Anti-Despawn**:
  - Spawns all original vehicles categorized cleanly + custom add-ons via `addons.json` + manual model name prompt.
  - Permanent Story Mode persistence decorator (`Player_Vehicle`) preventing DLC vehicle despawns.
* **🔧 Portable LSC Workshop**:
  - Clean numbered tuning for Engine, Brakes, Transmission, Suspension, Armor, Turbo, Xenon, Spoilers, Bumpers, Neons, Window Tint, and Custom Plate Text.
* **🤖 Smart Autopilot**:
  - Autonomous driving to waypoint with dynamic obstacle avoidance, ramming option, and instant manual takeover.
* **🦸 Superpowers & Vision**:
  - No-Clip (WASD/Shift/Ctrl/Space flight) with ground-safety landing, Night Vision, and Thermal Vision.
* **🎭 Skin Changer**:
  - Michael, Franklin, Trevor, story NPCs, law enforcement, and animals with aquatic suffocation protection.
* **🪖 Tactical Bodyguards & Gangs**:
  - Individual or full squads, tactical orders, gang warfare standoff, and gang armory.
* **🔫 Weapons & Combat**:
  - Teleport Gun, Force Gun, RPG Ammo, Give All, Infinite Ammo, Bottomless Clip (no reload), and 1-Hit Kill.
* **🎨 Visuals & World**:
  - Matrix, Noir, Sunset, and Fog filters, Weather and Time control, Moon Gravity, and Clear Area.

---

### Installation Instructions (Singleplayer)
1. Install **Script Hook V**, **ScriptHookVDotNet v2.10.13+**, and **NativeUI v1.9+** into your GTA V directory.
2. Extract the `scripts` folder from `YnixTrainer-v1.6.0.0.zip` into your Grand Theft Auto V main directory:
   - `Grand Theft Auto V\scripts\YnixTrainer.dll`
   - `Grand Theft Auto V\scripts\YnixTrainer\Config.ini`
   - `Grand Theft Auto V\scripts\YnixTrainer\addons.json`
   - `Grand Theft Auto V\scripts\YnixTrainer\Languages\pt-BR.ini`
   - `Grand Theft Auto V\scripts\YnixTrainer\Languages\en-US.ini`
3. Start GTA V Story Mode and press **F4** to open the menu.

---

### Controls
* **F4**: Open / Close Trainer (Toggle)
* **Numpad 8 / Up Arrow**: Move Up
* **Numpad 2 / Down Arrow**: Move Down
* **Numpad 4 / Left Arrow**: Option Left / Decrease
* **Numpad 6 / Right Arrow**: Option Right / Increase
* **Numpad 5 / Enter**: Select
* **Numpad 0 / Backspace**: Back

---

### Dependencies & Requirements
* [Script Hook V](http://www.dev-c.com/gtav/scripthookv/) by Alexander Blade
* [Community Script Hook V .NET](https://github.com/crosire/scripthookvdotnet/releases) by crosire & contributors
* [NativeUI](https://github.com/Guad/NativeUI/releases) by Guadmaz

---

### Credits
* **Ynix Trainer Developer**: Carlos (carlostmj)
* **Script Hook V**: Alexander Blade
* **ScriptHookVDotNet**: crosire & contributors
* **NativeUI**: Guadmaz

---

## 🇧🇷 Descrição em Português

### Resumo
O **Ynix Trainer v1.6.0.0** é um trainer completo, modular e autônomo para o Modo História do GTA V no PC. Conta com velocímetro digital em tempo real configurável em **KM/H** ou **MPH** com indicador de marcha, piloto automático inteligente com desvio de tráfego, oficina LSC portátil integrada, correção anti-despawn definitiva para carros de DLC/Add-on, esquadrões de gangues com ordens táticas e modo guerra urbana, armas especiais (Teleport Gun, Force Gun, RPG Ammo), modo drift, cinto de segurança anti-ejeção do pára-brisa, animais aquáticos que sobrevivem em terra, No-Clip seguro sem dano de queda, filtros de câmera cinematográficos e localização em Português e Inglês.
