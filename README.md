# 🎮 GTA V Mods Repository

Collection of custom, high-performance, and modular mods developed for **Grand Theft Auto V (PC Singleplayer)**.

Organized according to the [5Mods Upload Guidelines](https://github.com/5mods/tutorials/blob/master/5Mods%20Upload%20Guidelines.md).

---

## 📂 Repository Structure

The mods in this repository are structured in modular directories under `mods/<ModName>`:

```text
mods/
└── YnixTrainer/
    ├── README.md        # Complete 5Mods-compliant description & documentation
    ├── package/         # Ready-to-use drop-in mod files (drag & drop into GTA V)
    │   └── scripts/
    │       ├── YnixTrainer.dll
    │       └── YnixTrainer/
    │           ├── Config.ini
    │           ├── addons.json
    │           └── Languages/
    │               ├── pt-BR.ini
    │               └── en-US.ini
    └── src/             # Full C# source code and compiler scripts
        ├── Core/
        ├── Modules/
        └── build.bat
```

---

## 🚀 Available Mods

| Mod | Category | Current Version | Description |
| :--- | :--- | :--- | :--- |
| **[Ynix Trainer](mods/YnixTrainer/)** | Script / Trainer | `v1.3.0.0` | Comprehensive modular trainer featuring anti-despawn vehicle spawning, real-time manual LSC customization, skin changer, tactical bodyguards, superpowers (NoClip, Night/Thermal vision), animations, digital speedometer HUD, and multi-language support. |

---

## ⚠️ Important Singleplayer Notice

All mods in this repository are strictly developed and intended for **GTA V PC Singleplayer (Story Mode)**.
Modding GTA Online is against Rockstar Games' Terms of Service. Do not attempt to use these scripts in online modes.

---

## 📜 Credits & Acknowledgments

* **Mod Author**: Carlos
* **Script Hook V**: Alexander Blade
* **Community Script Hook V .NET**: crosire & contributors
* **NativeUI**: Guadmaz

---

## 📄 License

This repository is distributed under the [MIT License](LICENSE).
