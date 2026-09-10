@echo off
echo ==============================================
echo Compilando Ynix Realistic Police v1.0.0.0...
echo ==============================================
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set OUT=..\scripts\YnixPolice.dll
set REFS=/reference:..\ScriptHookVDotNet2.dll /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll

%CSC% /target:library /optimize+ /out:%OUT% %REFS% /recurse:*.cs

if %ERRORLEVEL% EQU 0 (
    echo [SUCESSO] YnixPolice.dll v1.0.0.0 compilado com exito em scripts\YnixPolice.dll!
) else (
    echo [ERRO] Falha na compilacao.
)
