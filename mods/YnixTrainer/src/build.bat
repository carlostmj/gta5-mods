@echo off
echo ==============================================
echo Compilando Ynix Trainer v1.3.0.0...
echo ==============================================
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set OUT=..\scripts\YnixTrainer.dll
set REFS=/reference:..\ScriptHookVDotNet2.dll /reference:..\NativeUI.dll /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll

%CSC% /target:library /optimize+ /out:%OUT% %REFS% /recurse:*.cs

if %ERRORLEVEL% EQU 0 (
    echo [SUCESSO] YnixTrainer.dll v1.3.0.0 compilado com exito em scripts\YnixTrainer.dll!
) else (
    echo [ERRO] Falha na compilacao.
)
pause
