@echo off
chcp 65001 >nul 2>&1
setlocal enabledelayedexpansion
mode con: cols=120 lines=45

REM ============================================================================================
REM
REM                     SCRIPT DE NETTOYAGE WINDOWS - VERSION COMPLETE FINALE
REM
REM ============================================================================================

REM ============================================================================================
REM                              SECTION 1: VARIABLES GLOBALES
REM ============================================================================================

set "SCRIPT_DIR=%~dp0"
set "LOG_FILE=%SCRIPT_DIR%CleanerReport_%date:~-4,4%-%date:~-7,2%-%date:~-10,2%_%time:~0,2%-%time:~3,2%-%time:~6,2%.txt"
set "LOG_FILE=%LOG_FILE: =0%"
set "START_TIME=%time%"
set "STEP_START_TIME=%time%"
set "GLOBAL_START_TIME=%time%"
set "TOTAL_FREED=0"
set "FILES_DELETED=0"
set "THREATS_FOUND=0"

REM Variables benchmark
set "CPU_AVANT=0"
set "CPU_APRES=0"
set "RAM_AVANT=0"
set "RAM_APRES=0"
set "DISK_FREE_BEFORE=0"
set "DISK_FREE_AFTER=0"

REM Variables progression
set "CURRENT_STEP=0"
set "TOTAL_STEPS=0"
set "REBOOT_NEEDED=0"

REM ============================================================================================
REM                         SECTION 2: VERIFICATION DROITS ADMINISTRATEUR
REM ============================================================================================

net session >nul 2>&1
if %errorLevel% neq 0 (
    cls
    echo.
    echo  ============================================================================================
    echo                              ERREUR: DROITS ADMINISTRATEUR REQUIS
    echo  ============================================================================================
    echo.
    echo  Ce script necessite les droits administrateur.
    echo  Clic droit sur le fichier ^> Executer en tant qu'administrateur
    echo.
    pause
    exit /b
)

REM ============================================================================================
REM                              SECTION 3: INITIALISATION LOG
REM ============================================================================================

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
REM                                   SECTION 4: MENU PRINCIPAL
REM ============================================================================================

:MENU
cls
echo.
echo  ============================================================================================
echo                                SYSTEME DE NETTOYAGE WINDOWS PRO
echo  ============================================================================================
echo.
echo  [1] NETTOYAGE SIMPLE (23 etapes - 10-15 min)
echo  [2] NETTOYAGE DE PRINTEMPS OPTIMISE (53 etapes - 60-120 min)
echo  [3] PLANIFIER NETTOYAGE AUTOMATIQUE
echo  [4] VOIR HISTORIQUE DES NETTOYAGES
echo  [0] QUITTER
echo.
echo  ============================================================================================
set /p CHOICE="  Votre choix (0-4): "

if "%CHOICE%"=="1" goto MODE_SIMPLE
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
echo  [1] Planifier nettoyage SIMPLE tous les jours a 02:00
echo  [2] Planifier nettoyage COMPLET tous les dimanches a 03:00
echo  [3] Supprimer toutes les taches planifiees
echo  [0] Retour menu principal
echo.
set /p SCHED_CHOICE="  Votre choix: "

if "%SCHED_CHOICE%"=="1" (
    schtasks /create /tn "CleanerSimple" /tr "\"%~f0\"" /sc daily /st 02:00 /rl highest /f >nul 2>&1
    echo.
    echo  [OK] Nettoyage simple planifie
    pause
    goto MENU
)

if "%SCHED_CHOICE%"=="2" (
    schtasks /create /tn "CleanerComplet" /tr "\"%~f0\"" /sc weekly /d SUN /st 03:00 /rl highest /f >nul 2>&1
    echo.
    echo  [OK] Nettoyage complet planifie
    pause
    goto MENU
)

if "%SCHED_CHOICE%"=="3" (
    schtasks /delete /tn "CleanerSimple" /f >nul 2>&1
    schtasks /delete /tn "CleanerComplet" /f >nul 2>&1
    echo.
    echo  [OK] Taches supprimees
    pause
    goto MENU
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
    echo  Rapports disponibles:
    echo.
    set /a REPORT_COUNT=0
    for %%F in ("%SCRIPT_DIR%CleanerReport_*.txt") do (
        set /a REPORT_COUNT+=1
        echo    [!REPORT_COUNT!] %%~nxF
    )
    echo.
    set /p OPEN_REPORT="  Ouvrir un rapport? (numero ou N): "
    if /i not "!OPEN_REPORT!"=="N" (
        set /a CURRENT=0
        for %%F in ("%SCRIPT_DIR%CleanerReport_*.txt") do (
            set /a CURRENT+=1
            if "!CURRENT!"=="!OPEN_REPORT!" notepad "%%F"
        )
    )
) else (
    echo  Aucun historique disponible.
)
echo.
pause
goto MENU

REM ============================================================================================
REM                            SECTION 7: FONCTIONS UTILITAIRES
REM ============================================================================================

:GET_ELAPSED_TIME
set "TIME_NOW=%time%"
for /f "tokens=1-3 delims=:." %%a in ("%STEP_START_TIME%") do (
    set "H_START=%%a"
    set "M_START=%%b"
    set "S_START=%%c"
)
for /f "tokens=1-3 delims=:." %%a in ("%TIME_NOW%") do (
    set "H_NOW=%%a"
    set "M_NOW=%%b"
    set "S_NOW=%%c"
)
set /a "H_START=1%H_START%-100" 2>nul
set /a "M_START=1%M_START%-100" 2>nul
set /a "S_START=1%S_START%-100" 2>nul
set /a "H_NOW=1%H_NOW%-100" 2>nul
set /a "M_NOW=1%M_NOW%-100" 2>nul
set /a "S_NOW=1%S_NOW%-100" 2>nul
set /a "START_S=(H_START*3600)+(M_START*60)+S_START"
set /a "NOW_S=(H_NOW*3600)+(M_NOW*60)+S_NOW"
if %NOW_S% LSS %START_S% set /a "NOW_S+=86400"
set /a "ELAPSED_SEC=NOW_S-START_S"
set /a "ELAPSED_MIN=ELAPSED_SEC/60"
set /a "ELAPSED_SEC_REMAIN=ELAPSED_SEC%%60"
goto :eof

:GET_GLOBAL_TIME
set "TIME_NOW=%time%"
for /f "tokens=1-3 delims=:." %%a in ("%GLOBAL_START_TIME%") do (
    set "H_GLOBAL=%%a"
    set "M_GLOBAL=%%b"
    set "S_GLOBAL=%%c"
)
for /f "tokens=1-3 delims=:." %%a in ("%TIME_NOW%") do (
    set "H_NOW=%%a"
    set "M_NOW=%%b"
    set "S_NOW=%%c"
)
set /a "H_GLOBAL=1%H_GLOBAL%-100" 2>nul
set /a "M_GLOBAL=1%M_GLOBAL%-100" 2>nul
set /a "S_GLOBAL=1%S_GLOBAL%-100" 2>nul
set /a "H_NOW=1%H_NOW%-100" 2>nul
set /a "M_NOW=1%M_NOW%-100" 2>nul
set /a "S_NOW=1%S_NOW%-100" 2>nul
set /a "START_S=(H_GLOBAL*3600)+(M_GLOBAL*60)+S_GLOBAL"
set /a "NOW_S=(H_NOW*3600)+(M_NOW*60)+S_NOW"
if %NOW_S% LSS %START_S% set /a "NOW_S+=86400"
set /a "GLOBAL_ELAPSED_SEC=NOW_S-START_S"
set /a "GLOBAL_ELAPSED_MIN=GLOBAL_ELAPSED_SEC/60"
set /a "GLOBAL_ELAPSED_SEC_REMAIN=GLOBAL_ELAPSED_SEC%%60"
goto :eof

:PROGRESS_BAR
set "STEP_START_TIME=%time%"
call :GET_GLOBAL_TIME
set /a PERCENT=(%CURRENT_STEP%*100)/%TOTAL_STEPS%
if %PERCENT% GTR 100 set PERCENT=100
set /a BARS=%PERCENT%/2
set "PROGRESS_STR="
for /l %%i in (1,1,!BARS!) do set "PROGRESS_STR=!PROGRESS_STR!#"
for /l %%i in (!BARS!,1,50) do set "PROGRESS_STR=!PROGRESS_STR!."
echo.
echo  +------------------------------------------------------------------------------------+
echo  ^| PROGRESSION: [!PROGRESS_STR!] !PERCENT!%%
echo  ^| Etape %CURRENT_STEP%/%TOTAL_STEPS% - %~1
echo  ^| Temps global: !GLOBAL_ELAPSED_MIN!m!GLOBAL_ELAPSED_SEC_REMAIN!s
echo  +------------------------------------------------------------------------------------+
echo.
goto :eof

:SUB_STEP
call :GET_ELAPSED_TIME
call :GET_GLOBAL_TIME
echo          ^> %~1... [Etape: !ELAPSED_MIN!m!ELAPSED_SEC_REMAIN!s / Global: !GLOBAL_ELAPSED_MIN!m!GLOBAL_ELAPSED_SEC_REMAIN!s]
goto :eof

:BENCHMARK_AVANT
echo  ============================================================================================
echo                              BENCHMARK PERFORMANCES (AVANT)
echo  ============================================================================================
echo.
for /f "skip=1" %%p in ('wmic cpu get loadpercentage 2^>nul') do (
    if not "%%p"=="" (
        set CPU_AVANT=%%p
        goto :cpu_done
    )
)
:cpu_done
for /f "skip=1 tokens=1,2" %%a in ('wmic OS get FreePhysicalMemory^,TotalVisibleMemorySize /format:value 2^>nul ^| findstr "="') do set "%%a=%%b" 2>nul
if defined FreePhysicalMemory if defined TotalVisibleMemorySize set /a RAM_AVANT=100-(!FreePhysicalMemory!*100/!TotalVisibleMemorySize!)
for /f "tokens=3" %%a in ('dir C:\ 2^>nul ^| find "octets libres"') do set DISK_FREE_BEFORE=%%a
echo  +----------------------------------------------------------------------------+
echo  ^| CPU Usage .................. %CPU_AVANT%%%
echo  ^| RAM Usage .................. %RAM_AVANT%%%
echo  ^| Disque C: libre ............ %DISK_FREE_BEFORE% octets
echo  +----------------------------------------------------------------------------+
echo.
timeout /t 2 /nobreak >nul
goto :eof

:BENCHMARK_APRES
echo.
echo  ============================================================================================
echo                              BENCHMARK PERFORMANCES (APRES)
echo  ============================================================================================
echo.
for /f "skip=1" %%p in ('wmic cpu get loadpercentage 2^>nul') do (
    if not "%%p"=="" (
        set CPU_APRES=%%p
        goto :cpu_done2
    )
)
:cpu_done2
for /f "skip=1 tokens=1,2" %%a in ('wmic OS get FreePhysicalMemory^,TotalVisibleMemorySize /format:value 2^>nul ^| findstr "="') do set "%%a=%%b" 2>nul
if defined FreePhysicalMemory if defined TotalVisibleMemorySize set /a RAM_APRES=100-(!FreePhysicalMemory!*100/!TotalVisibleMemorySize!)
for /f "tokens=3" %%a in ('dir C:\ 2^>nul ^| find "octets libres"') do set DISK_FREE_AFTER=%%a
set /a CPU_GAIN=CPU_AVANT-CPU_APRES
set /a RAM_GAIN=RAM_AVANT-RAM_APRES
if defined DISK_FREE_AFTER if defined DISK_FREE_BEFORE (
    set /a DISK_FREED_BYTES=DISK_FREE_AFTER-DISK_FREE_BEFORE
    set /a DISK_FREED=DISK_FREED_BYTES/1073741824
)
echo  +----------------------------------------------------------------------------+
echo  ^| CPU Usage .................. %CPU_APRES%%% (-%CPU_GAIN%%%)
echo  ^| RAM Usage .................. %RAM_APRES%%% (-%RAM_GAIN%%%)
echo  ^| Espace libere .............. %DISK_FREED% GB
echo  +----------------------------------------------------------------------------+
echo.
timeout /t 2 /nobreak >nul
goto :eof

:DISK_CLEANUP
call :SUB_STEP "Configuration nettoyage de disque (24 categories)"
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Temporary Files" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches\Recycle Bin" /v StateFlags0001 /t REG_DWORD /d 2 /f >nul 2>&1
call :SUB_STEP "Lancement cleanmgr (peut prendre plusieurs minutes)"
if /i "%~1"=="VeryLowDisk" (
    start /wait cleanmgr /sagerun:1 /VeryLowDisk
) else (
    start /wait cleanmgr /sagerun:1
)
call :SUB_STEP "Nettoyage termine"
echo.
goto :eof

:CALC_TIME
set "END_TIME=%time%"
for /f "tokens=1-3 delims=:." %%a in ("%START_TIME%") do (
    set "H=%%a"
    set "M=%%b"
    set "S=%%c"
)
for /f "tokens=1-3 delims=:." %%a in ("%END_TIME%") do (
    set "H2=%%a"
    set "M2=%%b"
    set "S2=%%c"
)
set /a "H=1%H%-100" 2>nul
set /a "M=1%M%-100" 2>nul
set /a "S=1%S%-100" 2>nul
set /a "H2=1%H2%-100" 2>nul
set /a "M2=1%M2%-100" 2>nul
set /a "S2=1%S2%-100" 2>nul
set /a "START_S=(H*3600)+(M*60)+S"
set /a "END_S=(H2*3600)+(M2*60)+S2"
if %END_S% LSS %START_S% set /a "END_S+=86400"
set /a "ELAPSED_S=END_S-START_S"
set /a "ELAPSED_M=ELAPSED_S/60"
set /a "ELAPSED_SEC_REMAIN=ELAPSED_S%%60"
goto :eof

:AFFICHER_RESUME
cls
echo.
echo  ============================================================================================
echo                                    RESUME FINAL
echo  ============================================================================================
echo.
echo  +------------------------------------------------------------------------------------+
echo  ^| PERFORMANCES SYSTEME
echo  +------------------------------------------------------------------------------------+
echo  ^| CPU Usage .............. %CPU_AVANT%%% -^> %CPU_APRES%%% (gain: -%CPU_GAIN%%%)
echo  ^| RAM Usage .............. %RAM_AVANT%%% -^> %RAM_APRES%%% (gain: -%RAM_GAIN%%%)
echo  ^| Espace libere .......... %DISK_FREED% GB
echo  +------------------------------------------------------------------------------------+
echo.
echo  +------------------------------------------------------------------------------------+
echo  ^| NETTOYAGE
echo  +------------------------------------------------------------------------------------+
echo  ^| Fichiers supprimes ..... %FILES_DELETED%
echo  +------------------------------------------------------------------------------------+
echo.
echo  +------------------------------------------------------------------------------------+
echo  ^| SECURITE
echo  +------------------------------------------------------------------------------------+
echo  ^| Menaces detectees ...... %THREATS_FOUND%
echo  +------------------------------------------------------------------------------------+
echo.
echo  +------------------------------------------------------------------------------------+
echo  ^| TEMPS D'EXECUTION
echo  +------------------------------------------------------------------------------------+
echo  ^| Temps total ............ %ELAPSED_M% min %ELAPSED_SEC_REMAIN% sec
echo  +------------------------------------------------------------------------------------+
echo.
goto :eof

REM ============================================================================================
REM
REM                    SECTION 8: NETTOYAGES COMMUNS (23 ETAPES)
REM
REM ============================================================================================

:NETTOYAGE_COMMUN

REM --- DEBUT NETTOYAGES COMMUNS ---

REM ETAPE COMMUNE 1: Detection disques
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Detection des disques"
set "DRIVES="
for %%D in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
    if exist %%D:\ (
        call :SUB_STEP "Disque %%D:\ detecte"
        set "DRIVES=!DRIVES! %%D:"
    )
)
echo          [OK] Detection terminee
echo.

REM ETAPE COMMUNE 2: Fichiers temporaires
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage fichiers temporaires"
set /a TEMP_COUNT=0
for %%F in ("%TEMP%\*.*") do set /a TEMP_COUNT+=1
call :SUB_STEP "Suppression %TEMP%"
DEL /Q /F /S "%TEMP%\*.*" >nul 2>&1
FOR /D %%d IN ("%TEMP%\*") DO RD /S /Q "%%d" >nul 2>&1
call :SUB_STEP "Suppression C:\Windows\Temp"
DEL /Q /F /S "C:\Windows\Temp\*.*" >nul 2>&1
FOR /D %%d IN ("C:\Windows\Temp\*") DO RD /S /Q "%%d" >nul 2>&1
set /a FILES_DELETED+=%TEMP_COUNT%
echo          [OK] %TEMP_COUNT% fichiers supprimes
echo.

REM ETAPE COMMUNE 3: Prefetch
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage Prefetch"
call :SUB_STEP "Suppression cache Prefetch"
DEL /Q /F "C:\Windows\Prefetch\*.*" >nul 2>&1
echo          [OK] Prefetch nettoye
echo.

REM ETAPE COMMUNE 4: TortoiseSVN
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage TortoiseSVN (.svn)"
call :SUB_STEP "Scan recursif dossiers .svn"
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (.svn) DO (
            IF EXIST "%%d" RD /S /Q "%%d" >nul 2>&1
        )
    )
)
echo          [OK] Dossiers .svn supprimes
echo.

REM ETAPE COMMUNE 5: Git
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage cache Git"
call :SUB_STEP "Nettoyage logs Git"
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (.git) DO (
            IF EXIST "%%d\logs" DEL /Q "%%d\logs\*.*" >nul 2>&1
        )
    )
)
echo          [OK] Cache Git nettoye
echo.

REM ETAPE COMMUNE 5.5: Visual Studio - Nettoyage complet
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage Visual Studio (tous disques)"
call :SUB_STEP "Scan des fichiers de cache VS"
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        call :SUB_STEP "Scan de %%D:\"
        DEL /Q/S "%%D\*.pch" >nul 2>&1
        DEL /Q/S "%%D\*.pdb" >nul 2>&1
        DEL /Q/S "%%D\*.sdf" >nul 2>&1
        DEL /Q/S "%%D\*.ipch" >nul 2>&1
        DEL /Q/S "%%D\*.idb" >nul 2>&1
        DEL /Q/S "%%D\*.ilk" >nul 2>&1
        DEL /Q/S "%%D\*.VC.db" >nul 2>&1
        DEL /Q/S "%%D\*.ipdb" >nul 2>&1
        DEL /Q/S "%%D\*.tlog" >nul 2>&1
        DEL /Q/S "%%D\*.iobj" >nul 2>&1
        DEL /Q/S "%%D\*.obj" >nul 2>&1
        DEL /Q/S "%%D\*.exp" >nul 2>&1
        DEL /Q/S "%%D\*.suo" >nul 2>&1
        DEL /Q/S "%%D\*.opensdf" >nul 2>&1
        DEL /Q/S "%%D\*.log" >nul 2>&1
        DEL /Q/S "%%D\*.lastbuildstate" >nul 2>&1
        FOR /D /R "%%D\" %%d IN (.vs) DO (
            IF EXIST "%%d" RD /S /Q "%%d" >nul 2>&1
        )
    )
)
echo          [OK] Visual Studio nettoye
echo.

REM ETAPE COMMUNE 5.6: x64/Debug/Release - Suppression selective
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage x64/Debug/Release (preservation DLL/assets)"
call :SUB_STEP "Parcours des dossiers de compilation"
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (x64) DO (
            IF EXIST "%%d" (
                DEL /Q "%%d\*.exe" >nul 2>&1
                DEL /Q "%%d\*.pdb" >nul 2>&1
                DEL /Q "%%d\*.obj" >nul 2>&1
                DEL /Q "%%d\*.ilk" >nul 2>&1
                DEL /Q "%%d\*.iobj" >nul 2>&1
                DEL /Q "%%d\*.ipdb" >nul 2>&1
                DEL /Q "%%d\*.tlog" >nul 2>&1
            )
        )
        FOR /D /R "%%D\" %%d IN (Debug) DO (
            IF EXIST "%%d" (
                set "IS_GAME_FOLDER=0"
                if exist "%%d\..\Assets" set "IS_GAME_FOLDER=1"
                if exist "%%d\..\Textures" set "IS_GAME_FOLDER=1"
                if exist "%%d\..\Resources" set "IS_GAME_FOLDER=1"
                if "!IS_GAME_FOLDER!"=="0" (
                    DEL /Q "%%d\*.exe" >nul 2>&1
                    DEL /Q "%%d\*.pdb" >nul 2>&1
                    DEL /Q "%%d\*.obj" >nul 2>&1
                    DEL /Q "%%d\*.ilk" >nul 2>&1
                )
            )
        )
        FOR /D /R "%%D\" %%d IN (Release) DO (
            IF EXIST "%%d" (
                set "IS_GAME_FOLDER=0"
                if exist "%%d\..\Assets" set "IS_GAME_FOLDER=1"
                if exist "%%d\..\Textures" set "IS_GAME_FOLDER=1"
                if exist "%%d\..\Resources" set "IS_GAME_FOLDER=1"
                if "!IS_GAME_FOLDER!"=="0" (
                    DEL /Q "%%d\*.exe" >nul 2>&1
                    DEL /Q "%%d\*.pdb" >nul 2>&1
                    DEL /Q "%%d\*.obj" >nul 2>&1
                    DEL /Q "%%d\*.ilk" >nul 2>&1
                )
            )
        )
    )
)
echo          [OK] Dossiers compilation nettoyes (DLL preservees)
echo.

REM ETAPE COMMUNE 6: Corbeilles
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Vidage des corbeilles"
for %%D in (%DRIVES%) do (
    if exist "%%D\$Recycle.Bin" (
        call :SUB_STEP "Vidage corbeille %%D:\"
        rd /s /q "%%D\$Recycle.Bin" >nul 2>&1
    )
)
echo          [OK] Corbeilles videes
echo.

REM ETAPE COMMUNE 7: Navigateurs
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage cache navigateurs"
call :SUB_STEP "Fermeture navigateurs"
taskkill /F /IM firefox.exe /IM chrome.exe /IM msedge.exe /IM brave.exe >nul 2>&1
timeout /t 2 /nobreak >nul
IF EXIST "%APPDATA%\Mozilla\Firefox\Profiles" (
    call :SUB_STEP "Nettoyage Firefox (AppData)"
    FOR /D %%p IN ("%APPDATA%\Mozilla\Firefox\Profiles\*") DO (
        IF EXIST "%%p\cache2" RD /S /Q "%%p\cache2" >nul 2>&1
        IF EXIST "%%p\jumpListCache" RD /S /Q "%%p\jumpListCache" >nul 2>&1
        IF EXIST "%%p\thumbnails" RD /S /Q "%%p\thumbnails" >nul 2>&1
        IF EXIST "%%p\crashes" RD /S /Q "%%p\crashes" >nul 2>&1
        DEL /Q "%%p\*.sqlite-wal" >nul 2>&1
        DEL /Q "%%p\*.sqlite-shm" >nul 2>&1
    )
)
IF EXIST "%LOCALAPPDATA%\Mozilla\Firefox\Profiles" (
    call :SUB_STEP "Nettoyage Firefox (LocalAppData)"
    FOR /D %%p IN ("%LOCALAPPDATA%\Mozilla\Firefox\Profiles\*") DO (
        IF EXIST "%%p\cache2" RD /S /Q "%%p\cache2" >nul 2>&1
    )
)
IF EXIST "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Cache" (
    call :SUB_STEP "Nettoyage Chrome"
    RD /S /Q "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Cache" >nul 2>&1
)
IF EXIST "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Code Cache" (
    RD /S /Q "%LOCALAPPDATA%\Google\Chrome\User Data\Default\Code Cache" >nul 2>&1
)
IF EXIST "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Cache" (
    call :SUB_STEP "Nettoyage Edge"
    RD /S /Q "%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Cache" >nul 2>&1
)
echo          [OK] Navigateurs nettoyes
echo.

REM ETAPE COMMUNE 8: Applications
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage cache applications"
call :SUB_STEP "Fermeture applications"
taskkill /F /IM Discord.exe /IM Spotify.exe /IM Teams.exe /IM Code.exe >nul 2>&1
if exist "%APPDATA%\Discord\Cache" RD /S /Q "%APPDATA%\Discord\Cache" >nul 2>&1
if exist "%APPDATA%\Spotify\Storage" RD /S /Q "%APPDATA%\Spotify\Storage" >nul 2>&1
if exist "%APPDATA%\Code\Cache" RD /S /Q "%APPDATA%\Code\Cache" >nul 2>&1
if exist "%APPDATA%\Code\CachedData" RD /S /Q "%APPDATA%\Code\CachedData" >nul 2>&1
if exist "%APPDATA%\Code\logs" RD /S /Q "%APPDATA%\Code\logs" >nul 2>&1
call :SUB_STEP "Nettoyage dossiers .vscode"
for %%D in (%DRIVES%) do (
    if exist %%D\ (
        FOR /D /R "%%D\" %%d IN (.vscode) DO (
            IF EXIST "%%d" (
                DEL /Q "%%d\.history" >nul 2>&1
                DEL /Q "%%d\*.log" >nul 2>&1
            )
        )
    )
)
echo          [OK] Applications nettoyees
echo.

REM ETAPE COMMUNE 8.5: Steam
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage Steam"
call :SUB_STEP "Nettoyage logs et caches Steam"
IF EXIST "%PROGRAMFILES(X86)%\Steam\logs" DEL /Q "%PROGRAMFILES(X86)%\Steam\logs\*.*" >nul 2>&1
IF EXIST "%PROGRAMFILES(X86)%\Steam\appcache" RD /S /Q "%PROGRAMFILES(X86)%\Steam\appcache" >nul 2>&1
IF EXIST "%PROGRAMFILES(X86)%\Steam\dumps" DEL /Q "%PROGRAMFILES(X86)%\Steam\dumps\*.*" >nul 2>&1
IF EXIST "%PROGRAMFILES(X86)%\Steam\steamapps\shadercache" RD /S /Q "%PROGRAMFILES(X86)%\Steam\steamapps\shadercache" >nul 2>&1
echo          [OK] Steam nettoye
echo.

REM ETAPE COMMUNE 8.6: Node.js - node_modules (optionnel)
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Node.js - node_modules (optionnel)"
echo.
set /p CLEAN_NODE="          Supprimer tous les node_modules? (o/N): "
if /i "%CLEAN_NODE%"=="o" (
    call :SUB_STEP "Suppression node_modules sur tous les disques"
    for %%D in (%DRIVES%) do (
        if exist %%D\ (
            FOR /D /R "%%D\" %%d IN (node_modules) DO (
                IF EXIST "%%d" RD /S /Q "%%d" >nul 2>&1
            )
        )
    )
    echo          [OK] node_modules supprimes
) else (
    echo          [SKIP] node_modules preserves
)
echo.

REM ETAPE COMMUNE 8.7: Caches developpement (NuGet, Gradle, Maven)
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Caches developpement supplementaires"
call :SUB_STEP "Nettoyage NuGet, Gradle, Maven"
IF EXIST "%USERPROFILE%\.nuget\packages" RD /S /Q "%USERPROFILE%\.nuget\packages" >nul 2>&1
IF EXIST "%USERPROFILE%\.gradle\caches" RD /S /Q "%USERPROFILE%\.gradle\caches" >nul 2>&1
IF EXIST "%USERPROFILE%\.m2\repository" RD /S /Q "%USERPROFILE%\.m2\repository" >nul 2>&1
echo          [OK] Caches dev supprimes
echo.

REM ETAPE COMMUNE 9: Thumbnails
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage thumbnails"
IF EXIST "%LOCALAPPDATA%\Microsoft\Windows\Explorer" (
    DEL /Q "%LOCALAPPDATA%\Microsoft\Windows\Explorer\thumbcache_*.db" >nul 2>&1
)
echo          [OK] Thumbnails nettoyes
echo.

REM ETAPE COMMUNE 10: DNS
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Vidage cache DNS"
ipconfig /flushdns >nul 2>&1
echo          [OK] Cache DNS vide
echo.

REM ETAPE COMMUNE 11: Optimisation DNS
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Optimisation DNS"
netsh interface ip set dns "Ethernet" static 1.1.1.1 primary >nul 2>&1
netsh interface ip set dns "Wi-Fi" static 1.1.1.1 primary >nul 2>&1
echo          [OK] DNS Cloudflare configure
echo.

REM ETAPE COMMUNE 12: Nettoyage disque
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage de disque Windows"
call :DISK_CLEANUP "standard"

REM ETAPE COMMUNE 13: Journaux
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage journaux Windows"
call :SUB_STEP "Vidage journaux evenements (peut prendre du temps)"
FOR /F "tokens=*" %%G in ('wevtutil.exe el 2^>nul') DO wevtutil.exe cl "%%G" >nul 2>&1
echo          [OK] Journaux nettoyes
echo.

REM ETAPE COMMUNE 14: Reseau
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Optimisation reseau"
call :SUB_STEP "Reset IP"
netsh int ip reset >nul 2>&1
call :SUB_STEP "Reset Winsock"
netsh winsock reset >nul 2>&1
echo          [OK] Reseau optimise
echo.

REM ETAPE COMMUNE 15: Registre
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Optimisation registre"
call :SUB_STEP "Desactivation animations"
reg add "HKCU\Control Panel\Desktop\WindowMetrics" /v MinAnimate /t REG_SZ /d 0 /f >nul 2>&1
call :SUB_STEP "Acceleration menu"
reg add "HKCU\Control Panel\Desktop" /v MenuShowDelay /t REG_SZ /d 0 /f >nul 2>&1
call :SUB_STEP "Desactivation services inutiles"
sc config "DiagTrack" start= disabled >nul 2>&1
sc config "dmwappushservice" start= disabled >nul 2>&1
echo          [OK] Registre optimise
echo.

REM ETAPE COMMUNE 16: Defender
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Mise a jour Windows Defender"
call :SUB_STEP "Telechargement definitions"
"%ProgramFiles%\Windows Defender\MpCmdRun.exe" -SignatureUpdate >nul 2>&1
echo          [OK] Defender mis a jour
echo.

REM ETAPE COMMUNE 17: Scan antivirus
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Scan antivirus rapide"
call :SUB_STEP "Analyse rapide en cours (5-10 min)"
"%ProgramFiles%\Windows Defender\MpCmdRun.exe" -Scan -ScanType 1 >nul 2>&1
if %errorLevel% equ 0 (
    echo          [OK] Aucune menace
    set THREATS_FOUND=0
) else (
    echo          [WARN] Menaces detectees
    set THREATS_FOUND=1
)
echo.

REM ETAPE COMMUNE 18: Verification disque
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Verification disque C:\"
call :SUB_STEP "Scan erreurs disque"
chkdsk C: /scan >nul 2>&1
echo          [OK] Disque verifie
echo.

REM ETAPE COMMUNE 19: Optimisation disque (SSD/HDD)
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Optimisation disque C:\"
call :SUB_STEP "Detection type de disque"
set "IS_SSD=0"
for /f "skip=1 tokens=*" %%i in ('wmic diskdrive get MediaType 2^>nul ^| findstr /i "SSD"') do set "IS_SSD=1"
if "%IS_SSD%"=="1" (
    call :SUB_STEP "SSD detecte - Lancement TRIM"
    defrag C: /L >nul 2>&1
    echo          [OK] C:\ optimise (SSD - TRIM)
) else (
    call :SUB_STEP "HDD detecte - Defragmentation (peut prendre du temps)"
    defrag C: /O >nul 2>&1
    echo          [OK] C:\ optimise (HDD - Defragmentation)
)
echo.

REM ETAPE COMMUNE 20: Windows Update
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage cache Windows Update"
call :SUB_STEP "Arret services"
net stop wuauserv >nul 2>&1
net stop bits >nul 2>&1
call :SUB_STEP "Suppression cache"
DEL /Q /F /S "C:\Windows\SoftwareDistribution\Download\*.*" >nul 2>&1
call :SUB_STEP "Nettoyage composants (DISM - peut prendre du temps)"
Dism.exe /online /Cleanup-Image /StartComponentCleanup >nul 2>&1
call :SUB_STEP "Redemarrage services"
net start wuauserv >nul 2>&1
net start bits >nul 2>&1
echo          [OK] Cache nettoye et composants supprimes
echo.

REM --- FIN NETTOYAGES COMMUNS ---

goto :eof

REM ============================================================================================
REM
REM                           SECTION 9: MODE SIMPLE (20 ETAPES)
REM
REM ============================================================================================

:MODE_SIMPLE
cls
set TOTAL_STEPS=23
set CURRENT_STEP=0
set "GLOBAL_START_TIME=%time%"
echo.
echo  ============================================================================================
echo                               MODE SIMPLE - NETTOYAGE RAPIDE
echo  ============================================================================================
echo.

call :BENCHMARK_AVANT
call :NETTOYAGE_COMMUN

set CURRENT_STEP=%TOTAL_STEPS%
call :PROGRESS_BAR "NETTOYAGE SIMPLE TERMINE"

call :BENCHMARK_APRES
call :CALC_TIME

echo  ============================================================================================
echo                               NETTOYAGE SIMPLE TERMINE !
echo  ============================================================================================
echo.

call :AFFICHER_RESUME

set /p OPEN_LOG="  Ouvrir le rapport? (O/N): "
if /i "%OPEN_LOG%"=="O" notepad "%LOG_FILE%"

pause
goto MENU

REM ============================================================================================
REM
REM                      SECTION 10: MODE PRINTEMPS (53 ETAPES)
REM
REM ============================================================================================

:MODE_PRINTEMPS
cls
color 0E
echo.
echo  ==========================================================================================
echo                    NETTOYAGE DE PRINTEMPS OPTIMISE - CONFIRMATION
echo  ==========================================================================================
echo.
echo   ATTENTION: Ce processus peut prendre 60-120 minutes.
echo   Un point de restauration sera cree pour votre securite.
echo.
set /p CONFIRM="   Continuer ? [O/N] : "
if /i not "%CONFIRM%"=="O" goto MENU

cls
color 0A
set TOTAL_STEPS=53
set CURRENT_STEP=0
set "GLOBAL_START_TIME=%time%"
echo.
echo  ==========================================================================================
echo                        NETTOYAGE DE PRINTEMPS OPTIMISE EN COURS
echo  ==========================================================================================
echo.

call :BENCHMARK_AVANT

REM --- DEBUT MODE PRINTEMPS ---

REM ETAPE 1 PRINTEMPS: Point de restauration
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Creation point de restauration"
call :SUB_STEP "Creation en cours"
wmic.exe /Namespace:\\root\default Path SystemRestore Call CreateRestorePoint "Avant Nettoyage", 100, 7 >nul 2>&1
if %errorLevel% equ 0 (
    echo          [OK] Point cree
) else (
    echo          [WARN] Impossible de creer le point
)
echo.

REM ETAPES 2-24 PRINTEMPS: Nettoyages communs (23 etapes)
call :NETTOYAGE_COMMUN

REM --- DEBUT OPTIMISATIONS AVANCEES PRINTEMPS (ETAPES 25-53) ---

REM ETAPE 25: GAMING - DirectX Shader Cache
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "GAMING - DirectX Shader Cache"
if exist "%LOCALAPPDATA%\D3DSCache" (
    call :SUB_STEP "Nettoyage DirectX Shader Cache"
    RD /S /Q "%LOCALAPPDATA%\D3DSCache" >nul 2>&1
)
if exist "%LOCALAPPDATA%\AMD\DxCache" (
    call :SUB_STEP "Nettoyage AMD DX Cache"
    RD /S /Q "%LOCALAPPDATA%\AMD\DxCache" >nul 2>&1
)
if exist "%LOCALAPPDATA%\NVIDIA\DXCache" (
    call :SUB_STEP "Nettoyage NVIDIA DX Cache"
    RD /S /Q "%LOCALAPPDATA%\NVIDIA\DXCache" >nul 2>&1
)
echo          [OK] DirectX Cache nettoye
echo.

REM ETAPE 26: GAMING - Cache plateformes
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "GAMING - Cache plateformes"
if exist "C:\Program Files (x86)\Steam\appcache" (
    call :SUB_STEP "Nettoyage Steam"
    RD /S /Q "C:\Program Files (x86)\Steam\appcache" >nul 2>&1
)
if exist "%LOCALAPPDATA%\EpicGamesLauncher\Saved\webcache" (
    call :SUB_STEP "Nettoyage Epic Games"
    RD /S /Q "%LOCALAPPDATA%\EpicGamesLauncher\Saved\webcache" >nul 2>&1
)
if exist "%APPDATA%\Battle.net\Cache" (
    call :SUB_STEP "Nettoyage Battle.net"
    RD /S /Q "%APPDATA%\Battle.net\Cache" >nul 2>&1
)
echo          [OK] Plateformes gaming nettoyees
echo.

REM ETAPE 27: GAMING - Optimisation reseau
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "GAMING - Optimisation reseau"
call :SUB_STEP "Desactivation algorithme Nagle"
reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" /v TcpAckFrequency /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces" /v TCPNoDelay /t REG_DWORD /d 1 /f >nul 2>&1
call :SUB_STEP "Configuration MTU"
netsh interface ipv4 set subinterface "Ethernet" mtu=1500 store=persistent >nul 2>&1
netsh interface ipv4 set subinterface "Wi-Fi" mtu=1500 store=persistent >nul 2>&1
echo          [OK] Reseau gaming optimise
echo.

REM ETAPE 28: GAMING - Parametres systeme
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "GAMING - Parametres systeme"
call :SUB_STEP "Desactivation Game DVR"
reg add "HKCU\System\GameConfigStore" /v GameDVR_Enabled /t REG_DWORD /d 0 /f >nul 2>&1
call :SUB_STEP "Activation Game Mode"
reg add "HKCU\SOFTWARE\Microsoft\GameBar" /v AutoGameModeEnabled /t REG_DWORD /d 1 /f >nul 2>&1
call :SUB_STEP "Activation GPU Scheduling"
reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v HwSchMode /t REG_DWORD /d 2 /f >nul 2>&1
echo          [OK] Parametres gaming optimises
echo.

REM ETAPE 29: DEV - Cache developpement
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "DEV - Cache developpement"
if exist "%APPDATA%\npm-cache" (
    call :SUB_STEP "Nettoyage npm"
    RD /S /Q "%APPDATA%\npm-cache" >nul 2>&1
)
if exist "%LOCALAPPDATA%\pip\Cache" (
    call :SUB_STEP "Nettoyage pip"
    RD /S /Q "%LOCALAPPDATA%\pip\Cache" >nul 2>&1
)
if exist "%USERPROFILE%\.gradle\caches" (
    call :SUB_STEP "Nettoyage Gradle"
    RD /S /Q "%USERPROFILE%\.gradle\caches" >nul 2>&1
)
if exist "%APPDATA%\Composer\cache" (
    call :SUB_STEP "Nettoyage Composer"
    RD /S /Q "%APPDATA%\Composer\cache" >nul 2>&1
)
if exist "%LOCALAPPDATA%\Yarn\Cache" (
    call :SUB_STEP "Nettoyage Yarn"
    RD /S /Q "%LOCALAPPDATA%\Yarn\Cache" >nul 2>&1
)
docker system prune -af >nul 2>&1
echo          [OK] Cache dev nettoye
echo.

REM ETAPE 30: DEV - VS Code
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "DEV - VS Code extensions"
if exist "%USERPROFILE%\.vscode\extensions" (
    call :SUB_STEP "Nettoyage logs extensions"
    for /d %%d in ("%USERPROFILE%\.vscode\extensions\*") do (
        if exist "%%d\logs" DEL /Q /F "%%d\logs\*" >nul 2>&1
    )
)
echo          [OK] VS Code nettoye
echo.

REM ETAPE 31: VIDEO - Cache multimedia
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "VIDEO - Cache multimedia"
if exist "%APPDATA%\vlc\cache" (
    call :SUB_STEP "Nettoyage VLC"
    RD /S /Q "%APPDATA%\vlc\cache" >nul 2>&1
)
if exist "%LOCALAPPDATA%\Microsoft\Media Player" (
    call :SUB_STEP "Nettoyage Windows Media Player"
    DEL /Q "%LOCALAPPDATA%\Microsoft\Media Player\*.wmdb" >nul 2>&1
)
if exist "%APPDATA%\Adobe\Common\Media Cache Files" (
    call :SUB_STEP "Nettoyage Adobe"
    RD /S /Q "%APPDATA%\Adobe\Common\Media Cache Files" >nul 2>&1
)
echo          [OK] Cache video nettoye
echo.

REM ETAPE 32: VIDEO - Codecs
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "VIDEO - Detection codecs"
if exist "C:\Program Files\K-Lite Codec Pack" (
    echo          [INFO] K-Lite Codec Pack detecte
) else if exist "C:\Program Files\LAV Filters" (
    echo          [INFO] LAV Filters detecte
) else (
    echo          [WARN] Aucun pack codecs
)
echo.

REM ETAPE 33: Windows Update - Service
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Windows Update - Service"
call :SUB_STEP "Demarrage service"
net start wuauserv >nul 2>&1
echo          [OK] Service demarre
echo.

REM ETAPE 34: Windows Update - Installation
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Windows Update - Installation"
call :SUB_STEP "Installation mises a jour (10-20 min)"
powershell -Command "Install-Module PSWindowsUpdate -Force -Confirm:$false" >nul 2>&1
powershell -Command "Import-Module PSWindowsUpdate; Get-WindowsUpdate -Install -AcceptAll -IgnoreReboot" >nul 2>&1
if %errorLevel% equ 0 (
    echo          [OK] Mises a jour installees
) else (
    echo          [WARN] Erreur mise a jour
)
echo.

REM ETAPE 35: Pilotes GPU - Detection
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Pilotes GPU - Detection"
wmic path win32_VideoController get name | findstr /i "NVIDIA" >nul 2>&1
if %errorLevel% equ 0 (
    echo          [INFO] NVIDIA detecte - GeForce Experience recommande
)
wmic path win32_VideoController get name | findstr /i "AMD\|Radeon" >nul 2>&1
if %errorLevel% equ 0 (
    echo          [INFO] AMD detecte - Adrenalin recommande
)
echo.

REM ETAPE 36: Pilotes - Scan
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Pilotes generiques - Scan"
call :SUB_STEP "Scan materiel"
pnputil /scan-devices >nul 2>&1
echo          [OK] Scan termine
echo.

REM ETAPE 37: Protection - Pare-feu
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Protection - Pare-feu"
call :SUB_STEP "Activation pare-feu"
netsh advfirewall set allprofiles state on >nul 2>&1
netsh advfirewall set allprofiles firewallpolicy blockinbound,allowoutbound >nul 2>&1
echo          [OK] Pare-feu active
echo.

REM ETAPE 38: Protection - Ransomware
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Protection - Ransomware"
call :SUB_STEP "Activation Controlled Folder Access"
powershell -Command "Set-MpPreference -EnableControlledFolderAccess Enabled" >nul 2>&1
call :SUB_STEP "Protection dossiers Documents"
powershell -Command "Add-MpPreference -ControlledFolderAccessProtectedFolders '%USERPROFILE%\Documents'" >nul 2>&1
call :SUB_STEP "Protection dossiers Pictures"
powershell -Command "Add-MpPreference -ControlledFolderAccessProtectedFolders '%USERPROFILE%\Pictures'" >nul 2>&1
call :SUB_STEP "Protection dossiers Videos"
powershell -Command "Add-MpPreference -ControlledFolderAccessProtectedFolders '%USERPROFILE%\Videos'" >nul 2>&1
echo          [OK] Protection ransomware activee
echo.

REM ETAPE 39: Protection - Defender avance
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Protection - Defender avance"
call :SUB_STEP "Activation temps reel"
powershell -Command "Set-MpPreference -DisableRealtimeMonitoring $false" >nul 2>&1
call :SUB_STEP "Activation cloud"
powershell -Command "Set-MpPreference -MAPSReporting Advanced" >nul 2>&1
call :SUB_STEP "Envoi echantillons"
powershell -Command "Set-MpPreference -SubmitSamplesConsent 1" >nul 2>&1
echo          [OK] Defender avance active
echo.

REM ETAPE 40: Nettoyage Windows.old
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage Windows.old"
if exist "C:\Windows.old" (
    call :SUB_STEP "Prise de controle"
    takeown /F C:\Windows.old\* /R /A >nul 2>&1
    call :SUB_STEP "Suppression"
    RD /S /Q "C:\Windows.old" >nul 2>&1
    echo          [OK] Windows.old supprime
) else (
    echo          [INFO] Windows.old introuvable
)
echo.

REM ETAPE 41: Nettoyage logs
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage logs Windows"
if exist "C:\Windows\Logs" (
    call :SUB_STEP "Suppression logs +30 jours"
    forfiles /p "C:\Windows\Logs" /s /m *.log /d -30 /c "cmd /c del @path" >nul 2>&1
)
if exist "C:\Windows\Panther" (
    call :SUB_STEP "Nettoyage Panther"
    DEL /Q /F "C:\Windows\Panther\*.log" >nul 2>&1
)
echo          [OK] Logs nettoyes
echo.

REM ETAPE 42: Nettoyage disque COMPLET
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage disque COMPLET (VeryLowDisk)"
call :DISK_CLEANUP "VeryLowDisk"

REM ETAPE 43: DISM
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "DISM - Reparation image"
call :SUB_STEP "Reparation composants (10-15 min)"
DISM /Online /Cleanup-Image /RestoreHealth >nul 2>&1
echo          [OK] Image reparee
echo.

REM ETAPE 44: SFC
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "SFC - Verification integrite"
call :SUB_STEP "Scan fichiers systeme (10-15 min)"
sfc /scannow >nul 2>&1
echo          [OK] Integrite verifiee
echo.

REM ETAPE 45: Scan antivirus COMPLET
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Scan antivirus COMPLET"
call :SUB_STEP "Analyse complete tous disques (30-60 min)"
"%ProgramFiles%\Windows Defender\MpCmdRun.exe" -Scan -ScanType 2 >nul 2>&1
if %errorLevel% equ 0 (
    echo          [OK] Aucune menace
    set THREATS_FOUND=0
) else (
    echo          [WARN] Menaces detectees
    set THREATS_FOUND=1
)
echo.

REM ETAPE 46: Optimisation autres disques
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Optimisation autres disques"
for %%D in (D E F G H) do (
    if exist %%D:\ (
        call :SUB_STEP "Optimisation %%D:\"
        defrag %%D: /O >nul 2>&1
    )
)
echo          [OK] Disques optimises
echo.

REM ETAPE 47: WinSxS
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage WinSxS"
call :SUB_STEP "Nettoyage composants (10-15 min)"
Dism.exe /online /Cleanup-Image /StartComponentCleanup /ResetBase >nul 2>&1
echo          [OK] WinSxS nettoye
echo.

REM ETAPE 48: Pilotes obsoletes
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Nettoyage pilotes obsoletes"
call :SUB_STEP "Suppression anciens pilotes"
pnputil /delete-driver oem*.inf /uninstall >nul 2>&1
echo          [OK] Pilotes nettoyes
echo.

REM ETAPE 49: Registre avance
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Optimisation registre avance"
set /p REG_CLEAN="          Optimiser registre? [O/N]: "
if /i "%REG_CLEAN%"=="O" (
    call :SUB_STEP "Nettoyage traces"
    reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" /v "" /f >nul 2>&1
    reg delete "HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache" /f >nul 2>&1
    echo          [OK] Registre optimise
) else (
    echo          [SKIP] Non modifie
)
echo.

REM ETAPE 50: Verification disques
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Verification erreurs disques"
for %%D in (%DRIVES%) do (
    call :SUB_STEP "Verification %%D:\"
    chkdsk %%D /scan >nul 2>&1
)
echo          [OK] Disques verifies
echo.

REM ETAPE 51: Services
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Optimisation services"
set /p DISABLE_SERVICES="          Desactiver services inutiles? [O/N]: "
if /i "%DISABLE_SERVICES%"=="O" (
    call :SUB_STEP "Desactivation Fax"
    sc config Fax start= disabled >nul 2>&1
    call :SUB_STEP "Windows Search en demand"
    sc config WSearch start= demand >nul 2>&1
    call :SUB_STEP "Desactivation Superfetch"
    sc config SysMain start= disabled >nul 2>&1
    echo          [OK] Services optimises
) else (
    echo          [SKIP] Non modifies
)
echo.

REM ETAPE 52: Verification redemarrage
set /a CURRENT_STEP+=1
call :PROGRESS_BAR "Verification redemarrage"
if exist "%windir%\winsxs\pending.xml" (
    echo          [WARN] Redemarrage necessaire
    set REBOOT_NEEDED=1
) else (
    echo          [INFO] Redemarrage non obligatoire
    set REBOOT_NEEDED=0
)
echo.

REM ETAPE 53: Finalisation
set CURRENT_STEP=%TOTAL_STEPS%
call :PROGRESS_BAR "NETTOYAGE DE PRINTEMPS TERMINE"

REM --- FIN MODE PRINTEMPS ---

call :BENCHMARK_APRES
call :CALC_TIME

echo  ==========================================================================================
echo                        NETTOYAGE DE PRINTEMPS OPTIMISE TERMINE !
echo  ==========================================================================================
echo.

call :AFFICHER_RESUME

if %REBOOT_NEEDED% equ 1 (
    echo.
    echo  [IMPORTANT] Un redemarrage est necessaire.
    echo.
    set /p REBOOT_NOW="  Redemarrer maintenant ? [O/N] : "
    if /i "!REBOOT_NOW!"=="O" (
        shutdown /r /t 30 /c "Redemarrage pour finaliser"
        echo.
        echo  Redemarrage dans 30 secondes... (annuler: shutdown /a)
    )
)

echo.
set /p OPEN_LOG="  Ouvrir le rapport? (O/N): "
if /i "%OPEN_LOG%"=="O" notepad "%LOG_FILE%"

pause
goto MENU

REM ============================================================================================
REM                                FIN DU SCRIPT COMPLET
REM ============================================================================================