@echo off
chcp 65001 >nul 2>&1
setlocal enabledelayedexpansion
mode con: cols=120 lines=45

REM ============================================================================================
REM                     SCRIPT DE NETTOYAGE WINDOWS - VERSION FINALE
REM                                    Par klaivertt
REM ============================================================================================

set "SCRIPT_DIR=%~dp0"
set "LOG_FILE=%SCRIPT_DIR%CleanerReport_%date:~-4,4%-%date:~-7,2%-%date:~-10,2%_%time:~0,2%-%time:~3,2%-%time:~6,2%.txt"
set "LOG_FILE=%LOG_FILE: =0%"
set "START_TIME=%time%"
set "STEP_START_TIME=%time%"
set "GLOBAL_START_TIME=%time%"
set "FILES_DELETED=0"
set "THREATS_FOUND=0"
set "CURRENT_STEP=0"
set "TOTAL_STEPS=0"
set "REBOOT_NEEDED=0"
set "DRIVES="

net session >nul 2>&1
if %errorLevel% neq 0 (
    cls
    echo.
    echo  ============================================================================================
    echo                              ERREUR: DROITS ADMINISTRATEUR REQUIS
    echo  ============================================================================================
    echo.
    echo  Clic droit sur le fichier ^> Executer en tant qu'administrateur
    echo.
    pause
    exit /b
)

echo ==================================================================================== > "%LOG_FILE%"
echo                          RAPPORT DE NETTOYAGE WINDOWS >> "%LOG_FILE%"
echo ==================================================================================== >> "%LOG_FILE%"
echo Date: %date% >> "%LOG_FILE%"
echo Heure debut: %time% >> "%LOG_FILE%"
echo Utilisateur: %USERNAME% >> "%LOG_FILE%"
echo Ordinateur: %COMPUTERNAME% >> "%LOG_FILE%"
echo ==================================================================================== >> "%LOG_FILE%"
echo. >> "%LOG_FILE%"

REM ============================================================================================
REM                              FONCTIONS
REM ============================================================================================

goto :AFTER_FUNCTIONS

:LOG
echo %~1 >> "%LOG_FILE%"
goto :eof

:LOG_OK
echo [OK] %~1
echo [OK] %~1 >> "%LOG_FILE%"
goto :eof

:LOG_INFO
echo [INFO] %~1
echo [INFO] %~1 >> "%LOG_FILE%"
goto :eof

:LOG_WARN
echo [WARN] %~1
echo [WARN] %~1 >> "%LOG_FILE%"
goto :eof

:GET_GLOBAL
set "TIME_NOW=%time%"
for /f "tokens=1-3 delims=:." %%a in ("%GLOBAL_START_TIME%") do (
    set /a "HG=1%%a-100" 2>nul
    set /a "MG=1%%b-100" 2>nul
    set /a "SG=1%%c-100" 2>nul
)
for /f "tokens=1-3 delims=:." %%a in ("%TIME_NOW%") do (
    set /a "HN=1%%a-100" 2>nul
    set /a "MN=1%%b-100" 2>nul
    set /a "SN=1%%c-100" 2>nul
)
set /a "T1=(HG*3600)+(MG*60)+SG"
set /a "T2=(HN*3600)+(MN*60)+SN"
if %T2% LSS %T1% set /a "T2+=86400"
set /a "GL=T2-T1"
set /a "GL_M=GL/60"
set /a "GL_S=GL%%60"
goto :eof

:GET_ELAPSED
set "TIME_NOW=%time%"
for /f "tokens=1-3 delims=:." %%a in ("%STEP_START_TIME%") do (
    set /a "HS=1%%a-100" 2>nul
    set /a "MS=1%%b-100" 2>nul
    set /a "SS=1%%c-100" 2>nul
)
for /f "tokens=1-3 delims=:." %%a in ("%TIME_NOW%") do (
    set /a "HN=1%%a-100" 2>nul
    set /a "MN=1%%b-100" 2>nul
    set /a "SN=1%%c-100" 2>nul
)
set /a "T1=(HS*3600)+(MS*60)+SS"
set /a "T2=(HN*3600)+(MN*60)+SN"
if %T2% LSS %T1% set /a "T2+=86400"
set /a "EL=T2-T1"
set /a "EL_M=EL/60"
set /a "EL_S=EL%%60"
goto :eof

:STEP
set "STEP_START_TIME=%time%"
call :GET_GLOBAL
set /a "PCT=(%CURRENT_STEP%*100)/%TOTAL_STEPS%"
if %PCT% GTR 100 set PCT=100
set /a "BARS=PCT/2"
set "PB="
for /l %%i in (1,1,!BARS!) do set "PB=!PB!#"
for /l %%i in (!BARS!,1,50) do set "PB=!PB!."
echo.
echo  +------------------------------------------------------------------------------------+
echo  ^| PROGRESSION: [!PB!] !PCT!%%
echo  ^| Etape %CURRENT_STEP%/%TOTAL_STEPS% - %~1
echo  ^| Temps global: !GL_M!m!GL_S!s
echo  +------------------------------------------------------------------------------------+
echo. >> "%LOG_FILE%"
echo ==================================================================================== >> "%LOG_FILE%"
echo ETAPE %CURRENT_STEP%/%TOTAL_STEPS%: %~1 >> "%LOG_FILE%"
echo Temps: !GL_M!m!GL_S!s >> "%LOG_FILE%"
echo ==================================================================================== >> "%LOG_FILE%"
echo.
goto :eof

:SS
call :GET_ELAPSED
call :GET_GLOBAL
echo          ^> %~1... [Etape: !EL_M!m!EL_S!s / Global: !GL_M!m!GL_S!s]
echo    ^> %~1 >> "%LOG_FILE%"
goto :eof

:CALC_TOTAL
set "END_TIME=%time%"
for /f "tokens=1-3 delims=:." %%a in ("%START_TIME%") do (
    set /a "H1=1%%a-100" 2>nul
    set /a "M1=1%%b-100" 2>nul
    set /a "S1=1%%c-100" 2>nul
)
for /f "tokens=1-3 delims=:." %%a in ("%END_TIME%") do (
    set /a "H2=1%%a-100" 2>nul
    set /a "M2=1%%b-100" 2>nul
    set /a "S2=1%%c-100" 2>nul
)
set /a "T1=(H1*3600)+(M1*60)+S1"
set /a "T2=(H2*3600)+(M2*60)+S2"
if %T2% LSS %T1% set /a "T2+=86400"
set /a "TOTAL_S=T2-T1"
set /a "TOTAL_M=TOTAL_S/60"
set /a "TOTAL_SR=TOTAL_S%%60"
goto :eof

:RESUME
cls
echo.
echo  ============================================================================================
echo                                    RESUME FINAL
echo  ============================================================================================
echo.
echo  +------------------------------------------------------------------------------------+
echo  ^| Fichiers supprimes ..... %FILES_DELETED%
echo  ^| Menaces detectees ...... %THREATS_FOUND%
echo  ^| Temps total ............ %TOTAL_M% min %TOTAL_SR% sec
echo  +------------------------------------------------------------------------------------+
echo.
echo. >> "%LOG_FILE%"
echo ==================================================================================== >> "%LOG_FILE%"
echo RESUME: Fichiers=%FILES_DELETED% Menaces=%THREATS_FOUND% Temps=%TOTAL_M%m%TOTAL_SR%s >> "%LOG_FILE%"
echo ==================================================================================== >> "%LOG_FILE%"
goto :eof

:KILL_APPS
call :LOG "Fermeture forcee applications..."
echo          ^> Fermeture navigateurs...
taskkill /F /IM firefox.exe >nul 2>&1
taskkill /F /IM chrome.exe >nul 2>&1
taskkill /F /IM msedge.exe >nul 2>&1
taskkill /F /IM brave.exe >nul 2>&1
taskkill /F /IM opera.exe >nul 2>&1
echo          ^> Fermeture IDE...
taskkill /F /IM devenv.exe >nul 2>&1
taskkill /F /IM MSBuild.exe >nul 2>&1
taskkill /F /IM VBCSCompiler.exe >nul 2>&1
taskkill /F /IM Code.exe >nul 2>&1
echo          ^> Fermeture applications...
taskkill /F /IM Discord.exe >nul 2>&1
taskkill /F /IM Spotify.exe >nul 2>&1
taskkill /F /IM Teams.exe >nul 2>&1
taskkill /F /IM slack.exe >nul 2>&1
timeout /t 5 /nobreak >nul
call :LOG_OK "Applications fermees"
goto :eof

:DETECT_DRIVES
set "DRIVES="
for %%D in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
    if exist %%D:\ set "DRIVES=!DRIVES! %%D:"
)
call :LOG_OK "Disques detectes: !DRIVES!"
goto :eof

:DO_DISK_CLEANUP
call :SS "Configuration cleanmgr (32 categories)"
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Active Setup Temp Folders" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\BranchCache" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Content Indexer Cleaner" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\D3D Shader Cache" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Delivery Optimization Files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Device Driver Packages" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Diagnostic Data Viewer database files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Downloaded Program Files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\DownloadsFolder" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Feedback Hub Archive log files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Internet Cache Files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Language Pack" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Microsoft Defender Antivirus" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Old ChkDsk Files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Previous Installations" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Recycle Bin" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\RetailDemo Offline Content" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Service Pack Cleanup" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Setup Log Files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error memory dump files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\System error minidump files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Setup Files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Sync Files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Thumbnail Cache" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Update Cleanup" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Upgrade Discarded Files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\User file versions" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Defender" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Error Reporting Files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows ESD installation files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Windows Upgrade Log Files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
call :LOG_OK "32 categories configurees"
call :SS "Lancement cleanmgr automatique"
cleanmgr /sagerun:1 /d C:
call :SS "Nettoyage fichiers systeme"
if exist "C:\Windows.old" (
    takeown /F "C:\Windows.old\*" /R /A /D Y >nul 2>&1
    icacls "C:\Windows.old\*" /grant administrators:F /T /C /Q >nul 2>&1
    rd /s /q "C:\Windows.old" >nul 2>&1
)
if exist "C:\$Windows.~BT" (
    takeown /F "C:\$Windows.~BT\*" /R /A /D Y >nul 2>&1
    rd /s /q "C:\$Windows.~BT" >nul 2>&1
)
if exist "C:\$Windows.~WS" (
    takeown /F "C:\$Windows.~WS\*" /R /A /D Y >nul 2>&1
    rd /s /q "C:\$Windows.~WS" >nul 2>&1
)
if exist "C:\Windows\Installer\$PatchCache$" del /f /s /q "C:\Windows\Installer\$PatchCache$\*.*" >nul 2>&1
if exist "C:\Windows\memory.dmp" del /f /q "C:\Windows\memory.dmp" >nul 2>&1
if exist "C:\Windows\Minidump" del /f /s /q "C:\Windows\Minidump\*.*" >nul 2>&1
if exist "C:\Windows\Logs\CBS\CBS.log" del /f /q "C:\Windows\Logs\CBS\CBS.log" >nul 2>&1
net stop dosvc >nul 2>&1
if exist "C:\Windows\ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache" (
    del /f /s /q "C:\Windows\ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache\*.*" >nul 2>&1
)
net start dosvc >nul 2>&1
call :SS "Nettoyage temporaires residuels"
del /f /s /q "%temp%\*.*" >nul 2>&1
del /f /s /q "C:\Windows\Temp\*.*" >nul 2>&1
call :SS "DISM composants"
Dism.exe /online /Cleanup-Image /StartComponentCleanup >nul 2>&1
call :LOG_OK "Nettoyage disque complet termine"
goto :eof

:AFTER_FUNCTIONS

REM ============================================================================================
REM                                   MENU
REM ============================================================================================

:MENU
cls
color 0A
echo.
echo  ============================================================================================
echo                                SYSTEME DE NETTOYAGE WINDOWS PRO
echo  ============================================================================================
echo.
echo  [1] NETTOYAGE COMPLET (26 etapes - 20-60 min)
echo  [2] NETTOYAGE DE PRINTEMPS (50 etapes - 60-120 min)
echo  [3] PLANIFIER NETTOYAGE AUTOMATIQUE
echo  [4] VOIR HISTORIQUE DES NETTOYAGES
echo  [0] QUITTER
echo.
echo  ============================================================================================
set /p CHOICE="  Votre choix (0-4): "

if "%CHOICE%"=="1" goto MODE_COMPLET
if "%CHOICE%"=="2" goto MODE_PRINTEMPS
if "%CHOICE%"=="3" goto PLANIFICATION
if "%CHOICE%"=="4" goto HISTORIQUE
if "%CHOICE%"=="0" exit /b
goto MENU

:PLANIFICATION
cls
echo.
echo  ============================================================================================
echo                           PLANIFICATION NETTOYAGE AUTOMATIQUE
echo  ============================================================================================
echo.
echo  [1] Planifier nettoyage COMPLET tous les jours a 02:00
echo  [2] Planifier nettoyage DE PRINTEMPS tous les dimanches a 03:00
echo  [3] Supprimer toutes les taches planifiees
echo  [0] Retour
echo.
set /p SC="  Votre choix: "
if "%SC%"=="1" (
    schtasks /create /tn "CleanerComplet" /tr "\"%~f0\"" /sc daily /st 02:00 /rl highest /f >nul 2>&1
    echo  [OK] Planifie & pause & goto MENU
)
if "%SC%"=="2" (
    schtasks /create /tn "CleanerPrintemps" /tr "\"%~f0\"" /sc weekly /d SUN /st 03:00 /rl highest /f >nul 2>&1
    echo  [OK] Planifie & pause & goto MENU
)
if "%SC%"=="3" (
    schtasks /delete /tn "CleanerComplet" /f >nul 2>&1
    schtasks /delete /tn "CleanerPrintemps" /f >nul 2>&1
    echo  [OK] Supprime & pause & goto MENU
)
goto MENU

:HISTORIQUE
cls
echo.
echo  ============================================================================================
echo                              HISTORIQUE DES NETTOYAGES
echo  ============================================================================================
echo.
if exist "%SCRIPT_DIR%CleanerReport_*.txt" (
    set /a RC=0
    for %%F in ("%SCRIPT_DIR%CleanerReport_*.txt") do (
        set /a RC+=1
        echo    [!RC!] %%~nxF
    )
    echo.
    set /p OR="  Ouvrir un rapport? (numero ou N): "
    if /i not "!OR!"=="N" (
        set /a CC=0
        for %%F in ("%SCRIPT_DIR%CleanerReport_*.txt") do (
            set /a CC+=1
            if "!CC!"=="!OR!" notepad "%%F"
        )
    )
) else (
    echo  Aucun historique.
)
echo.
pause
goto MENU

REM ============================================================================================
REM ============================================================================================
REM                           MODE COMPLET - 26 ETAPES
REM ============================================================================================
REM ============================================================================================

:MODE_COMPLET
cls
set TOTAL_STEPS=26
set CURRENT_STEP=0
set FILES_DELETED=0
set THREATS_FOUND=0
set "GLOBAL_START_TIME=%time%"
set "START_TIME=%time%"
echo.
echo  ============================================================================================
echo                               MODE COMPLET - 26 ETAPES
echo  ============================================================================================
echo.
call :LOG "==================== DEBUT MODE COMPLET (26 ETAPES) ===================="

call :KILL_APPS

REM === C1 ===
set /a CURRENT_STEP+=1
call :STEP "Detection des disques"
call :DETECT_DRIVES

REM === C2 ===
set /a CURRENT_STEP+=1
call :STEP "Fichiers temporaires"
call :SS "Suppression TEMP utilisateur"
FOR /D %%d IN ("%TEMP%\*") DO RD /S /Q "%%d" 2>nul
DEL /Q /F /S "%TEMP%\*.*" 2>nul
call :SS "Suppression Windows Temp"
FOR /D %%d IN ("C:\Windows\Temp\*") DO RD /S /Q "%%d" 2>nul
DEL /Q /F /S "C:\Windows\Temp\*.*" 2>nul
call :LOG_OK "Fichiers temporaires supprimes"

REM === C3 ===
set /a CURRENT_STEP+=1
call :STEP "Prefetch"
call :SS "Suppression Prefetch"
DEL /Q /F "C:\Windows\Prefetch\*.*" 2>nul
call :LOG_OK "Prefetch nettoye"

REM === C4 ===
set /a CURRENT_STEP+=1
call :STEP "Thumbnails"
call :SS "Suppression thumbcache"
IF EXIST "%LOCALAPPDATA%\Microsoft\Windows\Explorer" (
    for %%F in ("%LOCALAPPDATA%\Microsoft\Windows\Explorer\thumbcache_*.db") do del /f /q "%%F" 2>nul
)
call :LOG_OK "Thumbnails nettoyes"

REM === C5 ===
set /a CURRENT_STEP+=1
call :STEP "SVN - TOUS DISQUES"
call :SS "Scan .svn"
set /a SVN_C=0
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (.svn) DO (
            IF EXIST "%%d" ( RD /S /Q "%%d" 2>nul & set /a SVN_C+=1 )
        )
    )
)
call :LOG_OK "!SVN_C! .svn supprimes"

REM === C6 ===
set /a CURRENT_STEP+=1
call :STEP "Git - TOUS DISQUES"
call :SS "Nettoyage logs Git"
set /a GIT_C=0
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (.git) DO (
            IF EXIST "%%d\logs" (
                DEL /Q "%%d\logs\*.*" 2>nul
                FOR /D %%s IN ("%%d\logs\*") DO RD /S /Q "%%s" 2>nul
                set /a GIT_C+=1
            )
        )
    )
)
call :LOG_OK "!GIT_C! depots Git nettoyes"

REM === C7 ===
set /a CURRENT_STEP+=1
call :STEP "Visual Studio - TOUS DISQUES"
call :SS "Scan VS"
set /a VS_C=0
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (.vs) DO (
            IF EXIST "%%d" ( RD /S /Q "%%d" 2>nul & set /a VS_C+=1 )
        )
        FOR /D /R "%%D\" %%d IN (x64 Debug Release ipch) DO (
            IF EXIST "%%d" (
                set "ISV=0"
                if exist "%%d\..\*.sln" set "ISV=1"
                if exist "%%d\..\*.vcxproj" set "ISV=1"
                if exist "%%d\..\..\*.sln" set "ISV=1"
                if exist "%%d\..\..\*.vcxproj" set "ISV=1"
                if "!ISV!"=="1" (
                    set "ISG=0"
                    if exist "%%d\..\Assets" set "ISG=1"
                    if exist "%%d\..\Textures" set "ISG=1"
                    if exist "%%d\..\Resources" set "ISG=1"
                    if "!ISG!"=="0" (
                        for %%E in (pch pdb sdf ipch idb ilk VC.db ipdb tlog iobj obj exp suo opensdf log lastbuildstate) do (
                            for %%F in ("%%d\*.%%E") do (
                                if exist "%%F" ( del /f /q "%%F" 2>nul & set /a VS_C+=1 )
                            )
                        )
                    )
                )
            )
        )
    )
)
call :LOG_OK "VS nettoye (!VS_C! elements)"

REM === C8 ===
set /a CURRENT_STEP+=1
call :STEP "node_modules - TOUS DISQUES"
call :SS "Suppression node_modules"
set /a ND_C=0
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (node_modules) DO (
            IF EXIST "%%d" ( RD /S /Q "%%d" 2>nul & set /a ND_C+=1 )
        )
    )
)
call :LOG_OK "!ND_C! node_modules supprimes"

REM === C9 ===
set /a CURRENT_STEP+=1
call :STEP "Caches developpement"
call :SS "NuGet Gradle Maven npm pip Composer Yarn"
IF EXIST "%USERPROFILE%\.nuget\packages" RD /S /Q "%USERPROFILE%\.nuget\packages" 2>nul
IF EXIST "%USERPROFILE%\.gradle\caches" RD /S /Q "%USERPROFILE%\.gradle\caches" 2>nul
IF EXIST "%USERPROFILE%\.m2\repository" RD /S /Q "%USERPROFILE%\.m2\repository" 2>nul
IF EXIST "%APPDATA%\npm-cache" RD /S /Q "%APPDATA%\npm-cache" 2>nul
IF EXIST "%LOCALAPPDATA%\pip\Cache" RD /S /Q "%LOCALAPPDATA%\pip\Cache" 2>nul
IF EXIST "%APPDATA%\Composer\cache" RD /S /Q "%APPDATA%\Composer\cache" 2>nul
IF EXIST "%LOCALAPPDATA%\Yarn\Cache" RD /S /Q "%LOCALAPPDATA%\Yarn\Cache" 2>nul
call :LOG_OK "Caches dev supprimes"

REM === C10 ===
set /a CURRENT_STEP+=1
call :STEP "VS Code - TOUS DISQUES"
call :SS "Cache VS Code"
if exist "%APPDATA%\Code\Cache" RD /S /Q "%APPDATA%\Code\Cache" 2>nul
if exist "%APPDATA%\Code\CachedData" RD /S /Q "%APPDATA%\Code\CachedData" 2>nul
if exist "%APPDATA%\Code\logs" RD /S /Q "%APPDATA%\Code\logs" 2>nul
if exist "%USERPROFILE%\.vscode\extensions" (
    for /d %%d in ("%USERPROFILE%\.vscode\extensions\*") do (
        if exist "%%d\logs" DEL /Q /F "%%d\logs\*" 2>nul
    )
)
call :SS ".vscode tous disques"
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (.vscode) DO (
            IF EXIST "%%d" ( DEL /Q "%%d\.history" 2>nul & DEL /Q "%%d\*.log" 2>nul )
        )
    )
)
call :LOG_OK "VS Code nettoye"

REM === C11 ===
set /a CURRENT_STEP+=1
call :STEP "Docker"
call :SS "Docker prune"
docker system prune -af >nul 2>&1
if !errorLevel! equ 0 ( call :LOG_OK "Docker nettoye" ) else ( call :LOG_INFO "Docker absent" )

REM === C12 ===
set /a CURRENT_STEP+=1
call :STEP "Navigateurs"
call :SS "Firefox"
IF EXIST "%APPDATA%\Mozilla\Firefox\Profiles" (
    FOR /D %%p IN ("%APPDATA%\Mozilla\Firefox\Profiles\*") DO (
        IF EXIST "%%p\cache2" RD /S /Q "%%p\cache2" 2>nul
        IF EXIST "%%p\jumpListCache" RD /S /Q "%%p\jumpListCache" 2>nul
        IF EXIST "%%p\thumbnails" RD /S /Q "%%p\thumbnails" 2>nul
        IF EXIST "%%p\crashes" RD /S /Q "%%p\crashes" 2>nul
        DEL /Q "%%p\*.sqlite-wal" 2>nul
        DEL /Q "%%p\*.sqlite-shm" 2>nul
    )
)
IF EXIST "%LOCALAPPDATA%\Mozilla\Firefox\Profiles" (
    FOR /D %%p IN ("%LOCALAPPDATA%\Mozilla\Firefox\Profiles\*") DO (
        IF EXIST "%%p\cache2" RD /S /Q "%%p\cache2" 2>nul
    )
)
call :SS "Chrome"
IF EXIST "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Cache" RD /S /Q "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Cache" 2>nul
IF EXIST "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Code Cache" RD /S /Q "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Code Cache" 2>nul
IF EXIST "%LOCALAPPDATA%\Google\Chrome\User Data\Default\GPUCache" RD /S /Q "%LOCALAPPDATA%\Google\Chrome\User Data\Default\GPUCache" 2>nul
call :SS "Edge"
IF EXIST "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Cache" RD /S /Q "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Cache" 2>nul
IF EXIST "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Code Cache" RD /S /Q "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Code Cache" 2>nul
call :LOG_OK "Navigateurs nettoyes"

REM === C13 ===
set /a CURRENT_STEP+=1
call :STEP "Applications"
call :SS "Discord Spotify"
if exist "%APPDATA%\Discord\Cache" RD /S /Q "%APPDATA%\Discord\Cache" 2>nul
if exist "%APPDATA%\Discord\Code Cache" RD /S /Q "%APPDATA%\Discord\Code Cache" 2>nul
if exist "%APPDATA%\Spotify\Storage" RD /S /Q "%APPDATA%\Spotify\Storage" 2>nul
call :LOG_OK "Applications nettoyees"

REM === C14 ===
set /a CURRENT_STEP+=1
call :STEP "Steam - TOUS DISQUES"
call :SS "Steam cache"
IF EXIST "%PROGRAMFILES(X86)%\Steam\logs" DEL /Q "%PROGRAMFILES(X86)%\Steam\logs\*.*" 2>nul
IF EXIST "%PROGRAMFILES(X86)%\Steam\appcache" RD /S /Q "%PROGRAMFILES(X86)%\Steam\appcache" 2>nul
IF EXIST "%PROGRAMFILES(X86)%\Steam\dumps" DEL /Q "%PROGRAMFILES(X86)%\Steam\dumps\*.*" 2>nul
IF EXIST "%PROGRAMFILES(X86)%\Steam\steamapps\shadercache" RD /S /Q "%PROGRAMFILES(X86)%\Steam\steamapps\shadercache" 2>nul
call :SS "SteamLibrary tous disques"
for %%D in (%DRIVES%) do (
    if exist "%%D\SteamLibrary\steamapps\shadercache" RD /S /Q "%%D\SteamLibrary\steamapps\shadercache" 2>nul
    if exist "%%D\steamapps\shadercache" RD /S /Q "%%D\steamapps\shadercache" 2>nul
)
call :LOG_OK "Steam nettoye"

REM === C15 ===
set /a CURRENT_STEP+=1
call :STEP "Corbeilles - TOUS DISQUES"
call :SS "Vidage"
for %%D in (%DRIVES%) do (
    if exist "%%D\$Recycle.Bin" rd /s /q "%%D\$Recycle.Bin" 2>nul
)
call :LOG_OK "Corbeilles videes"

REM === C16 ===
set /a CURRENT_STEP+=1
call :STEP "Cache DNS"
call :SS "Flush DNS"
ipconfig /flushdns >nul 2>&1
call :LOG_OK "DNS vide"

REM === C17 ===
set /a CURRENT_STEP+=1
call :STEP "DNS Cloudflare"
call :SS "Configuration 1.1.1.1"
netsh interface show interface | findstr /i "Ethernet" >nul 2>&1
if !errorLevel! equ 0 (
    netsh interface ip set dns "Ethernet" static 1.1.1.1 primary >nul 2>&1
    netsh interface ip add dns "Ethernet" 1.0.0.1 index=2 >nul 2>&1
)
netsh interface show interface | findstr /i "Wi-Fi" >nul 2>&1
if !errorLevel! equ 0 (
    netsh interface ip set dns "Wi-Fi" static 1.1.1.1 primary >nul 2>&1
    netsh interface ip add dns "Wi-Fi" 1.0.0.1 index=2 >nul 2>&1
)
call :LOG_OK "DNS configure"

REM === C18 ===
set /a CURRENT_STEP+=1
call :STEP "Optimisation reseau"
call :SS "Reset IP"
netsh int ip reset >nul 2>&1
call :SS "Reset Winsock"
netsh winsock reset >nul 2>&1
call :SS "Vidage ARP"
netsh interface ip delete arpcache >nul 2>&1
call :LOG_OK "Reseau optimise"

REM === C19 ===
set /a CURRENT_STEP+=1
call :STEP "Nettoyage disque Windows COMPLET"
call :DO_DISK_CLEANUP

REM === C20 ===
set /a CURRENT_STEP+=1
call :STEP "Journaux Windows"
call :SS "Vidage journaux"
set /a JC=0
FOR /F "tokens=*" %%G in ('wevtutil.exe el 2^>nul') DO (
    wevtutil.exe cl "%%G" >nul 2>&1
    set /a JC+=1
)
call :LOG_OK "!JC! journaux nettoyes"

REM === C21 ===
set /a CURRENT_STEP+=1
call :STEP "Registre"
call :SS "Animations"
reg add "HKCU\Control Panel\Desktop\WindowMetrics" /v MinAnimate /t REG_SZ /d 0 /f >nul 2>&1
call :SS "Menu"
reg add "HKCU\Control Panel\Desktop" /v MenuShowDelay /t REG_SZ /d 0 /f >nul 2>&1
call :SS "Telemetrie"
sc config "DiagTrack" start= disabled >nul 2>&1
sc config "dmwappushservice" start= disabled >nul 2>&1
call :LOG_OK "Registre optimise"

REM === C22 ===
set /a CURRENT_STEP+=1
call :STEP "Windows Defender MAJ"
call :SS "Definitions"
"%ProgramFiles%\Windows Defender\MpCmdRun.exe" -SignatureUpdate >nul 2>&1
if !errorLevel! equ 0 ( call :LOG_OK "MAJ OK" ) else ( call :LOG_WARN "Erreur MAJ" )

REM === C23 ===
set /a CURRENT_STEP+=1
call :STEP "Scan antivirus rapide"
call :SS "Scan rapide (5-10 min)"
"%ProgramFiles%\Windows Defender\MpCmdRun.exe" -Scan -ScanType 1 >nul 2>&1
if !errorLevel! equ 0 ( call :LOG_OK "Aucune menace" & set THREATS_FOUND=0 ) else ( call :LOG_WARN "Menaces" & set THREATS_FOUND=1 )

REM === C24 ===
set /a CURRENT_STEP+=1
call :STEP "Verification disque C:\"
call :SS "chkdsk scan"
chkdsk C: /scan >nul 2>&1
call :LOG_OK "C:\ verifie"

REM === C25 ===
set /a CURRENT_STEP+=1
call :STEP "Optimisation TOUS DISQUES"
call :SS "defrag"
for %%D in (%DRIVES%) do (
    if exist %%D:\ (
        echo          ^> %%D:\...
        defrag %%D: /O >nul 2>&1
    )
)
call :LOG_OK "Disques optimises"

REM === C26 ===
set /a CURRENT_STEP+=1
call :STEP "Windows Update cache"
call :SS "Arret services"
net stop wuauserv >nul 2>&1
net stop bits >nul 2>&1
timeout /t 2 /nobreak >nul
call :SS "Suppression cache"
DEL /Q /F /S "C:\Windows\SoftwareDistribution\Download\*.*" >nul 2>&1
for /d %%d in ("C:\Windows\SoftwareDistribution\Download\*") do rd /s /q "%%d" >nul 2>&1
call :SS "Redemarrage services"
net start wuauserv >nul 2>&1
net start bits >nul 2>&1
call :LOG_OK "Windows Update nettoye"

REM === FIN MODE COMPLET ===
set CURRENT_STEP=%TOTAL_STEPS%
call :STEP "NETTOYAGE COMPLET TERMINE"
call :CALC_TOTAL
echo.
echo  ============================================================================================
echo                               NETTOYAGE COMPLET TERMINE !
echo  ============================================================================================
echo.
call :RESUME
set /p OL="  Ouvrir le rapport? (O/N): "
if /i "%OL%"=="O" notepad "%LOG_FILE%"
pause
goto MENU

REM ============================================================================================
REM ============================================================================================
REM                           MODE PRINTEMPS - 50 ETAPES
REM ============================================================================================
REM ============================================================================================

:MODE_PRINTEMPS
cls
color 0E
echo.
echo  ==========================================================================================
echo                    NETTOYAGE DE PRINTEMPS - CONFIRMATION
echo  ==========================================================================================
echo.
echo   Ce processus peut prendre 60-120 minutes.
echo   Un point de restauration sera cree.
echo.
set /p CF="   Continuer ? [O/N] : "
if /i not "%CF%"=="O" goto MENU

cls
color 0A
set TOTAL_STEPS=50
set CURRENT_STEP=0
set FILES_DELETED=0
set THREATS_FOUND=0
set "GLOBAL_START_TIME=%time%"
set "START_TIME=%time%"
echo.
echo  ==========================================================================================
echo                        NETTOYAGE DE PRINTEMPS - 50 ETAPES
echo  ==========================================================================================
echo.
call :LOG "==================== DEBUT MODE PRINTEMPS (50 ETAPES) ===================="

REM === P1: Restauration ===
set /a CURRENT_STEP+=1
call :STEP "Point de restauration"
call :SS "Creation"
wmic.exe /Namespace:\\root\default Path SystemRestore Call CreateRestorePoint "Avant Nettoyage", 100, 7 >nul 2>&1
if !errorLevel! equ 0 ( call :LOG_OK "Point cree" ) else ( call :LOG_WARN "Impossible" )

REM === P2: Kill apps ===
set /a CURRENT_STEP+=1
call :STEP "Fermeture applications"
call :KILL_APPS

REM === P3: Disques ===
set /a CURRENT_STEP+=1
call :STEP "Detection disques"
call :DETECT_DRIVES

REM === P4: Temp ===
set /a CURRENT_STEP+=1
call :STEP "Fichiers temporaires"
call :SS "TEMP utilisateur"
FOR /D %%d IN ("%TEMP%\*") DO RD /S /Q "%%d" 2>nul
DEL /Q /F /S "%TEMP%\*.*" 2>nul
call :SS "Windows Temp"
FOR /D %%d IN ("C:\Windows\Temp\*") DO RD /S /Q "%%d" 2>nul
DEL /Q /F /S "C:\Windows\Temp\*.*" 2>nul
call :LOG_OK "Temporaires supprimes"

REM === P5: Prefetch ===
set /a CURRENT_STEP+=1
call :STEP "Prefetch"
call :SS "Suppression"
DEL /Q /F "C:\Windows\Prefetch\*.*" 2>nul
call :LOG_OK "Prefetch nettoye"

REM === P6: Thumbnails ===
set /a CURRENT_STEP+=1
call :STEP "Thumbnails"
call :SS "thumbcache"
IF EXIST "%LOCALAPPDATA%\Microsoft\Windows\Explorer" (
    for %%F in ("%LOCALAPPDATA%\Microsoft\Windows\Explorer\thumbcache_*.db") do del /f /q "%%F" 2>nul
)
call :LOG_OK "Thumbnails nettoyes"

REM === P7: SVN ===
set /a CURRENT_STEP+=1
call :STEP "SVN - TOUS DISQUES"
call :SS "Scan .svn"
set /a SVN_C=0
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (.svn) DO (
            IF EXIST "%%d" ( RD /S /Q "%%d" 2>nul & set /a SVN_C+=1 )
        )
    )
)
call :LOG_OK "!SVN_C! .svn supprimes"

REM === P8: Git ===
set /a CURRENT_STEP+=1
call :STEP "Git - TOUS DISQUES"
call :SS "Logs Git"
set /a GIT_C=0
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (.git) DO (
            IF EXIST "%%d\logs" (
                DEL /Q "%%d\logs\*.*" 2>nul
                FOR /D %%s IN ("%%d\logs\*") DO RD /S /Q "%%s" 2>nul
                set /a GIT_C+=1
            )
        )
    )
)
call :LOG_OK "!GIT_C! depots Git nettoyes"

REM === P9: VS ===
set /a CURRENT_STEP+=1
call :STEP "Visual Studio - TOUS DISQUES"
call :SS "Scan VS"
set /a VS_C=0
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (.vs) DO (
            IF EXIST "%%d" ( RD /S /Q "%%d" 2>nul & set /a VS_C+=1 )
        )
        FOR /D /R "%%D\" %%d IN (x64 Debug Release ipch) DO (
            IF EXIST "%%d" (
                set "ISV=0"
                if exist "%%d\..\*.sln" set "ISV=1"
                if exist "%%d\..\*.vcxproj" set "ISV=1"
                if exist "%%d\..\..\*.sln" set "ISV=1"
                if exist "%%d\..\..\*.vcxproj" set "ISV=1"
                if "!ISV!"=="1" (
                    set "ISG=0"
                    if exist "%%d\..\Assets" set "ISG=1"
                    if exist "%%d\..\Textures" set "ISG=1"
                    if exist "%%d\..\Resources" set "ISG=1"
                    if "!ISG!"=="0" (
                        for %%E in (pch pdb sdf ipch idb ilk VC.db ipdb tlog iobj obj exp suo opensdf log lastbuildstate) do (
                            for %%F in ("%%d\*.%%E") do (
                                if exist "%%F" ( del /f /q "%%F" 2>nul & set /a VS_C+=1 )
                            )
                        )
                    )
                )
            )
        )
    )
)
call :LOG_OK "VS nettoye (!VS_C! elements)"

REM === P10: node_modules ===
set /a CURRENT_STEP+=1
call :STEP "node_modules - TOUS DISQUES"
call :SS "Suppression"
set /a ND_C=0
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (node_modules) DO (
            IF EXIST "%%d" ( RD /S /Q "%%d" 2>nul & set /a ND_C+=1 )
        )
    )
)
call :LOG_OK "!ND_C! node_modules supprimes"

REM === P11: Caches dev ===
set /a CURRENT_STEP+=1
call :STEP "Caches developpement"
call :SS "NuGet Gradle Maven npm pip Composer Yarn"
IF EXIST "%USERPROFILE%\.nuget\packages" RD /S /Q "%USERPROFILE%\.nuget\packages" 2>nul
IF EXIST "%USERPROFILE%\.gradle\caches" RD /S /Q "%USERPROFILE%\.gradle\caches" 2>nul
IF EXIST "%USERPROFILE%\.m2\repository" RD /S /Q "%USERPROFILE%\.m2\repository" 2>nul
IF EXIST "%APPDATA%\npm-cache" RD /S /Q "%APPDATA%\npm-cache" 2>nul
IF EXIST "%LOCALAPPDATA%\pip\Cache" RD /S /Q "%LOCALAPPDATA%\pip\Cache" 2>nul
IF EXIST "%APPDATA%\Composer\cache" RD /S /Q "%APPDATA%\Composer\cache" 2>nul
IF EXIST "%LOCALAPPDATA%\Yarn\Cache" RD /S /Q "%LOCALAPPDATA%\Yarn\Cache" 2>nul
call :LOG_OK "Caches dev supprimes"

REM === P12: VS Code ===
set /a CURRENT_STEP+=1
call :STEP "VS Code - TOUS DISQUES"
call :SS "Cache VS Code"
if exist "%APPDATA%\Code\Cache" RD /S /Q "%APPDATA%\Code\Cache" 2>nul
if exist "%APPDATA%\Code\CachedData" RD /S /Q "%APPDATA%\Code\CachedData" 2>nul
if exist "%APPDATA%\Code\logs" RD /S /Q "%APPDATA%\Code\logs" 2>nul
if exist "%USERPROFILE%\.vscode\extensions" (
    for /d %%d in ("%USERPROFILE%\.vscode\extensions\*") do (
        if exist "%%d\logs" DEL /Q /F "%%d\logs\*" 2>nul
    )
)
call :SS ".vscode tous disques"
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (.vscode) DO (
            IF EXIST "%%d" ( DEL /Q "%%d\.history" 2>nul & DEL /Q "%%d\*.log" 2>nul )
        )
    )
)
call :LOG_OK "VS Code nettoye"

REM === P13: Docker ===
set /a CURRENT_STEP+=1
call :STEP "Docker"
call :SS "Docker prune"
docker system prune -af >nul 2>&1
if !errorLevel! equ 0 ( call :LOG_OK "Docker nettoye" ) else ( call :LOG_INFO "Docker absent" )

REM === P14: Navigateurs ===
set /a CURRENT_STEP+=1
call :STEP "Navigateurs"
call :SS "Firefox"
IF EXIST "%APPDATA%\Mozilla\Firefox\Profiles" (
    FOR /D %%p IN ("%APPDATA%\Mozilla\Firefox\Profiles\*") DO (
        IF EXIST "%%p\cache2" RD /S /Q "%%p\cache2" 2>nul
        IF EXIST "%%p\jumpListCache" RD /S /Q "%%p\jumpListCache" 2>nul
        IF EXIST "%%p\thumbnails" RD /S /Q "%%p\thumbnails" 2>nul
        IF EXIST "%%p\crashes" RD /S /Q "%%p\crashes" 2>nul
        DEL /Q "%%p\*.sqlite-wal" 2>nul
        DEL /Q "%%p\*.sqlite-shm" 2>nul
    )
)
IF EXIST "%LOCALAPPDATA%\Mozilla\Firefox\Profiles" (
    FOR /D %%p IN ("%LOCALAPPDATA%\Mozilla\Firefox\Profiles\*") DO (
        IF EXIST "%%p\cache2" RD /S /Q "%%p\cache2" 2>nul
    )
)
call :SS "Chrome"
IF EXIST "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Cache" RD /S /Q "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Cache" 2>nul
IF EXIST "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Code Cache" RD /S /Q "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Code Cache" 2>nul
IF EXIST "%LOCALAPPDATA%\Google\Chrome\User Data\Default\GPUCache" RD /S /Q "%LOCALAPPDATA%\Google\Chrome\User Data\Default\GPUCache" 2>nul
call :SS "Edge"
IF EXIST "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Cache" RD /S /Q "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Cache" 2>nul
IF EXIST "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Code Cache" RD /S /Q "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Code Cache" 2>nul
call :LOG_OK "Navigateurs nettoyes"

REM === P15: Apps ===
set /a CURRENT_STEP+=1
call :STEP "Applications"
call :SS "Discord Spotify"
if exist "%APPDATA%\Discord\Cache" RD /S /Q "%APPDATA%\Discord\Cache" 2>nul
if exist "%APPDATA%\Discord\Code Cache" RD /S /Q "%APPDATA%\Discord\Code Cache" 2>nul
if exist "%APPDATA%\Spotify\Storage" RD /S /Q "%APPDATA%\Spotify\Storage" 2>nul
call :LOG_OK "Applications nettoyees"

REM === P16: Steam ===
set /a CURRENT_STEP+=1
call :STEP "Steam - TOUS DISQUES"
call :SS "Steam cache"
IF EXIST "%PROGRAMFILES(X86)%\Steam\logs" DEL /Q "%PROGRAMFILES(X86)%\Steam\logs\*.*" 2>nul
IF EXIST "%PROGRAMFILES(X86)%\Steam\appcache" RD /S /Q "%PROGRAMFILES(X86)%\Steam\appcache" 2>nul
IF EXIST "%PROGRAMFILES(X86)%\Steam\dumps" DEL /Q "%PROGRAMFILES(X86)%\Steam\dumps\*.*" 2>nul
IF EXIST "%PROGRAMFILES(X86)%\Steam\steamapps\shadercache" RD /S /Q "%PROGRAMFILES(X86)%\Steam\steamapps\shadercache" 2>nul
for %%D in (%DRIVES%) do (
    if exist "%%D\SteamLibrary\steamapps\shadercache" RD /S /Q "%%D\SteamLibrary\steamapps\shadercache" 2>nul
    if exist "%%D\steamapps\shadercache" RD /S /Q "%%D\steamapps\shadercache" 2>nul
)
call :LOG_OK "Steam nettoye"

REM === P17: Corbeilles ===
set /a CURRENT_STEP+=1
call :STEP "Corbeilles - TOUS DISQUES"
call :SS "Vidage"
for %%D in (%DRIVES%) do (
    if exist "%%D\$Recycle.Bin" rd /s /q "%%D\$Recycle.Bin" 2>nul
)
call :LOG_OK "Corbeilles videes"

REM === P18: DNS ===
set /a CURRENT_STEP+=1
call :STEP "Cache DNS"
call :SS "Flush"
ipconfig /flushdns >nul 2>&1
call :LOG_OK "DNS vide"

REM === P19: DNS Cloudflare ===
set /a CURRENT_STEP+=1
call :STEP "DNS Cloudflare"
call :SS "Configuration"
netsh interface show interface | findstr /i "Ethernet" >nul 2>&1
if !errorLevel! equ 0 (
    netsh interface ip set dns "Ethernet" static 1.1.1.1 primary >nul 2>&1
    netsh interface ip add dns "Ethernet" 1.0.0.1 index=2 >nul 2>&1
)
netsh interface show interface | findstr /i "Wi-Fi" >nul 2>&1
if !errorLevel! equ 0 (
    netsh interface ip set dns "Wi-Fi" static 1.1.1.1 primary >nul 2>&1
    netsh interface ip add dns "Wi-Fi" 1.0.0.1 index=2 >nul 2>&1
)
call :LOG_OK "DNS configure"

REM === P20: Reseau ===
set /a CURRENT_STEP+=1
call :STEP "Optimisation reseau"
call :SS "Reset IP"
netsh int ip reset >nul 2>&1
call :SS "Winsock"
netsh winsock reset >nul 2>&1
call :SS "ARP"
netsh interface ip delete arpcache >nul 2>&1
call :LOG_OK "Reseau optimise"

REM === P21: Disk Cleanup ===
set /a CURRENT_STEP+=1
call :STEP "Nettoyage disque COMPLET"
call :DO_DISK_CLEANUP

REM === P22: Journaux ===
set /a CURRENT_STEP+=1
call :STEP "Journaux Windows"
call :SS "Vidage"
set /a JC=0
FOR /F "tokens=*" %%G in ('wevtutil.exe el 2^>nul') DO (
    wevtutil.exe cl "%%G" >nul 2>&1
    set /a JC+=1
)
call :LOG_OK "!JC! journaux nettoyes"

REM === P23: Registre ===
set /a CURRENT_STEP+=1
call :STEP "Registre"
call :SS "Animations"
reg add "HKCU\Control Panel\Desktop\WindowMetrics" /v MinAnimate /t REG_SZ /d 0 /f >nul 2>&1
call :SS "Menu"
reg add "HKCU\Control Panel\Desktop" /v MenuShowDelay /t REG_SZ /d 0 /f >nul 2>&1
call :SS "Telemetrie"
sc config "DiagTrack" start= disabled >nul 2>&1
sc config "dmwappushservice" start= disabled >nul 2>&1
call :LOG_OK "Registre optimise"

REM === P24: Defender MAJ ===
set /a CURRENT_STEP+=1
call :STEP "Defender MAJ"
call :SS "Definitions"
"%ProgramFiles%\Windows Defender\MpCmdRun.exe" -SignatureUpdate >nul 2>&1
if !errorLevel! equ 0 ( call :LOG_OK "MAJ OK" ) else ( call :LOG_WARN "Erreur MAJ" )

REM === P25: Scan rapide ===
set /a CURRENT_STEP+=1
call :STEP "Scan antivirus rapide"
call :SS "Scan (5-10 min)"
"%ProgramFiles%\Windows Defender\MpCmdRun.exe" -Scan -ScanType 1 >nul 2>&1
if !errorLevel! equ 0 ( call :LOG_OK "Aucune menace" & set THREATS_FOUND=0 ) else ( call :LOG_WARN "Menaces" & set THREATS_FOUND=1 )

REM === P26: chkdsk C ===
set /a CURRENT_STEP+=1
call :STEP "Verification C:\"
call :SS "chkdsk"
chkdsk C: /scan >nul 2>&1
call :LOG_OK "C:\ verifie"

REM === P27: Defrag ===
set /a CURRENT_STEP+=1
call :STEP "Optimisation TOUS DISQUES"
call :SS "defrag"
for %%D in (%DRIVES%) do (
    if exist %%D:\ (
        echo          ^> %%D:\...
        defrag %%D: /O >nul 2>&1
    )
)
call :LOG_OK "Disques optimises"

REM === P28: Windows Update ===
set /a CURRENT_STEP+=1
call :STEP "Windows Update cache"
call :SS "Arret services"
net stop wuauserv >nul 2>&1
net stop bits >nul 2>&1
timeout /t 2 /nobreak >nul
call :SS "Suppression cache"
DEL /Q /F /S "C:\Windows\SoftwareDistribution\Download\*.*" >nul 2>&1
for /d %%d in ("C:\Windows\SoftwareDistribution\Download\*") do rd /s /q "%%d" >nul 2>&1
call :SS "Redemarrage"
net start wuauserv >nul 2>&1
net start bits >nul 2>&1
call :LOG_OK "Windows Update nettoye"

REM ============================================================================================
REM                    ETAPES AVANCEES PRINTEMPS (29-50)
REM ============================================================================================

REM === P29: DirectX Shader ===
set /a CURRENT_STEP+=1
call :STEP "GAMING - DirectX Shader Cache"
call :SS "D3DSCache AMD NVIDIA"
if exist "%LOCALAPPDATA%\D3DSCache" RD /S /Q "%LOCALAPPDATA%\D3DSCache" 2>nul
if exist "%LOCALAPPDATA%\AMD\DxCache" RD /S /Q "%LOCALAPPDATA%\AMD\DxCache" 2>nul
if exist "%LOCALAPPDATA%\NVIDIA\DXCache" RD /S /Q "%LOCALAPPDATA%\NVIDIA\DXCache" 2>nul
call :LOG_OK "DirectX Cache nettoye"

REM === P30: Gaming platforms ===
set /a CURRENT_STEP+=1
call :STEP "GAMING - Epic Battle.net"
call :SS "Plateformes gaming"
if exist "%LOCALAPPDATA%\EpicGamesLauncher\Saved\webcache" RD /S /Q "%LOCALAPPDATA%\EpicGamesLauncher\Saved\webcache" 2>nul
if exist "%APPDATA%\Battle.net\Cache" RD /S /Q "%APPDATA%\Battle.net\Cache" 2>nul
call :LOG_OK "Plateformes nettoyees"

REM === P31: Reseau gaming ===
set /a CURRENT_STEP+=1
call :STEP "GAMING - Reseau"
call :SS "Nagle off"
reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" /v TcpAckFrequency /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" /v TCPNoDelay /t REG_DWORD /d 1 /f >nul 2>&1
call :SS "MTU 1500"
netsh interface show interface | findstr /i "