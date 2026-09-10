# Ynix Trainer (GTA V) - v1.5.0.0

Trainer completo, moderno, estável e traduzido para Grand Theft Auto V (Modo História / Singleplayer).

Desenvolvido em C# com ScriptHookVDotNet e NativeUI.

---

## 🚀 Novidades da v1.5.0.0
- **Piloto Automático Inteligente Reconstruído**:
  - Navegação de longa distância real via `TASK_VEHICLE_DRIVE_TO_COORD_LONGRANGE`.
  - Cálculo de altitude de solo (`Z`) para nunca bugar em viadutos ou morros.
  - Fechamento automático de menus ao iniciar para evitar conflito de botões.
  - HUD na tela com distância restante, velocidade da IA e aviso de cancelamento.
  - Cancelamento suave segurando o freio (`S`), buzina (`E`) ou freio de mão (`Espaço`).
  - 3 Estilos de condução: Seguro/Legal, Rápido/Desvio de Tráfego e Fuga/Agressivo.
- **Super Nitro Boost (NOS)**:
  - Impulso veloz ao segurar `Shift` ou `X` com limite de velocidade liberado.
- **Pulo de Veículo (Car Jump)**:
  - Pressione `Espaço` no carro para saltar por cima de obstáculos estilo Ruiner 2000.
- **Auto-Desvirar (Anti-Capotamento)**:
  - Corrige automaticamente o veículo caso ele fique de cabeça para baixo.
- **Rampa Acrobática Instantânea**:
  - Cria rampas de salto 15 metros à frente com 1 clique para decolar em acrobacias.
- **Câmera Lenta / Bullet Time (Modo Matrix)**:
  - 3 intensidades de câmera lenta no mundo (0.7x, 0.4x, 0.15x Matrix) para cenas de ação épicas.
- **Rádio Portátil a Pé (Mobile Radio)**:
  - Escute as estações de rádio do GTA V enquanto anda, corre ou nada.
- **Gerador de Dinheiro Ampliado**:
  - +$100.000, +$1.000.000, +$10.000.000, +$100.000.000 e Dinheiro Máximo ($2.147.483.647).
- **Consistência de God Mode**:
  - Desativar No-Clip não remove mais o God Mode se o jogador já o tinha ligado.

---

## 📦 Instalação
1. Certifique-se de ter instalado no diretório do GTA V:
   - **ScriptHookV** (`ScriptHookV.dll`)
   - **ScriptHookVDotNet** (`ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll`)
   - **NativeUI** (`scripts/NativeUI.dll`)
2. Copie o conteúdo da pasta `scripts` do arquivo zip para a pasta `Grand Theft Auto V/scripts/`.
3. Pressione **F4** dentro do jogo para abrir o menu!

---

## 🎮 Teclas de Atalho
- **F4**: Abrir / Fechar o menu Ynix Trainer
- **Setas / Enter / Backspace**: Navegação do menu
- **Shift** ou **X** (no veículo): Nitro Boost / NOS
- **Espaço** (no veículo): Pulo Acrobático
- **S** (segurar 0.25s) ou **E**: Cancelar Piloto Automático

---

## 📜 Créditos
- **Desenvolvido por**: Carlos (carlostmj)
- **Ferramentas**: Alexander Blade (ScriptHookV), crosire (ScriptHookVDotNet), Guadmaz (NativeUI)
