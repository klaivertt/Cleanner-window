# 📦 Guide de Déploiement - PC Clean

**Version**: 2.0.0  
**Framework**: .NET 8.0  
**License**: Propriétaire  

---

## 📋 Table des matières

1. [Prérequis](#prérequis)
2. [Préparation du système](#préparation-du-système)
3. [Premiere installation / Build initial](#première-installationbuild-initial)
4. [Création du premier package (Release)](#création-du-premier-package-release)
5. [Configuration GitHub Releases](#configuration-github-releases)
6. [Déploiement initial aux utilisateurs](#déploiement-initial-aux-utilisateurs)
7. [Système de mise à jour automatique](#système-de-mise-à-jour-automatique)
8. [Gestion des versions](#gestion-des-versions)

---

## 🔧 Prérequis

### Pour développer/compiler:
- Windows 10/11 (version 22H2 ou plus récente recommandée)
- .NET 8.0 SDK ou ultérieur [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
- Visual Studio 2022 Community Edition (gratuit) ou Visual Studio Code
- Git / GitHub Desktop
- Droits administrateur sur le PC de développement

### Pour exécuter l'application:
- Windows 10 ou 11
- .NET 8.0 Runtime (sera fourni avec le package autonome)
- Droits administrateur (requis pour les opérations de nettoyage)

---

## 🖥️ Préparation du système

### 1. Installer .NET 8.0 SDK

```powershell
# Vérifier l'installation
dotnet --version

# Devrait afficher: 8.0.X ou ultérieur
```

### 2. Cloner le repository

```powershell
cd $HOME/Bureau
git clone https://github.com/Scryl/Cleanner-window.git
cd Cleanner-window
```

### 3. Vérifier la structure du projet

```powershell
ls -la

# Doit contenir:
# - NettoyerPc.sln
# - NettoyerPc/ (dossier principal)
# - README.md
# - CHANGELOG.md
```

---

## 🚀 Première installation/Build initial

### Via Visual Studio 2022

1. Ouvrir `NettoyerPc.sln`
2. Attendre la restauration automatique des packages NuGet
3. **Build → Build Solution** (Ctrl+Shift+B)
4. Appuyer sur **F5** pour lancer en debug (ou Ctrl+F5 en Release)

### Via .NET CLI (PowerShell)

```powershell
# Se placer dans le dossier du projet
cd NettoyerPc

# Restaurer les dépendances
dotnet restore

# Compiler en mode Debug
dotnet build -c Debug

# Compiler en mode Release
dotnet build -c Release

# Lancer l'application (Debug)
dotnet run -c Debug

# IMPORTANT: L'application doit être lancée EN TANT QU'ADMINISTRATEUR
```

### Résolution des problèmes courants

**Erreur**: "C# language version is not supported"
```powershell
# Mettre à jour Visual Studio ou le SDK .NET
dotnet sdk check
dotnet tools update --global
```

**Erreur**: "The project doesn't know how to run"
```powershell
# Vérifier le fichier .csproj
cat NettoyerPc/NettoyerPc.csproj | sls OutputType
# Doit avoir: <OutputType>WinExe</OutputType>
```

---

## 📦 Création du premier package (Release)

### Option 1: Package autonome (RECOMMANDÉ)

```powershell
cd NettoyerPc

# Publier comme application autonome 64-bit
dotnet publish -c Release -r win-x64 --self-contained true -o .\publish\

# Optimiser la taille
dotnet publish -c Release -r win-x64 --self-contained true \
    -p:PublishTrimmed=true \
    -p:PublishReadyToRun=true \
    -o .\publish\
```

**Résultat**: Dossier `publish/` contenant:
- `NettoyerPc.exe` (executable principal, ~5-8 MB)
- Fichiers de support .NET 8.0
- Ressources et manifests

### Option 2: Installer Visual Studio Installer Projects (pour MSI)

```powershell
# Installer l'extension
dotnet package add wix

# Créer une configuration MSI (optionnel, plus complexe)
```

### Créer le package ZIP distributable

```powershell
# Depuis le dossier du projet
cd ..

# Créer le ZIP avec tous les fichiers
Compress-Archive -Path "NettoyerPc\publish\*" `
                 -DestinationPath "NettoyerPC_2.0.0_win64.zip" `
                 -Force

# Vérifier le contenu
$zipFile = "NettoyerPC_2.0.0_win64.zip"
Expand-Archive -Path $zipFile -DestinationPath "test_extract" -Force
ls test_extract
```

---

## 🐙 Configuration GitHub Releases

### 1. Créer un Release sur GitHub

1. Aller sur [https://github.com/Scryl/Cleanner-window/releases](https://github.com/Scryl/Cleanner-window/releases)
2. Cliquer sur **"Create a new release"**
3. **Tag version**: `v2.0.0`
4. **Release title**: `PC Clean v2.0.0`
5. **Description** (Changelog):

```markdown
## 🎉 PC Clean - v2.0.0

### ✨ Nouvelles fonctionnalités
- Interface WPF sombre professionnelle
- Rapports JSON détaillés
- Système d'auto-update via GitHub Releases
- Nettoyage des navigateurs amélioré
- Force-close d'applications avant nettoyage

### 🐛 Corrections
- Correction du bug VS Code
- Amélioration des descriptions
- Optimisation de la détection des menaces

### 💾 Téléchargement & Installation
1. Télécharger `NettoyerPC_2.0.0_win64.zip`
2. Extraire le ZIP
3. Clic droit sur `NettoyerPc.exe` → Exécuter en tant qu'administrateur
4. Les futures mises à jour se feront automatiquement via le bouton "Mises à jour"
```

6. **Attacher les fichiers **:
   - Cliquer sur "Attach binaries" ou drag-drop:
     - `NettoyerPC_2.0.0_win64.zip`
     - `NettoyerPC_2.0.0_win64.exe` (si créé avec Visual Studio)

7. ✅ Cliquer **"Publish release"**

### 2. Vérifier que tout fonctionne

```powershell
# Récupérer le changelog et URL de téléchargement
$url = "https://api.github.com/repos/Scryl/Cleanner-window/releases/latest"
$release = Invoke-RestMethod -Uri $url
$release | Select-Object tag_name, name, body, assets | Format-List

# Doit renvoyer:
# tag_name: v2.0.0
# name: PC Clean v2.0.0
# body: [votre description]
# assets: liste des fichiers attachés
```

---

## 👥 Déploiement initial aux utilisateurs

### Méthode 1: Lien GitHub Releases (SIMPLE)

Partager le lien direct:
```
https://github.com/Scryl/Cleanner-window/releases/download/v2.0.0/NettoyerPC_2.0.0_win64.zip
```

### Méthode 2: Site web / Portail

```html
<a href="https://github.com/Scryl/Cleanner-window/releases/latest">
  📥 Télécharger PC Clean (v2.0.0)
</a>
```

### Méthode 3: Guide d'installation pour utilisateurs

**Créer un fichier `INSTALL_USER.md`:**

```markdown
# 🧹 Installation - PC Clean

## Étapes d'installation

1. **Télécharger** le fichier ZIP depuis GitHub
   - Lien: [https://github.com/Scryl/Cleanner-window/releases](https://github.com/Scryl/Cleanner-window/releases)

2. **Extraire** le fichier ZIP
   - Clic droit sur le ZIP → "Extraire tout"
   - Choisir un dossier (ex: `C:\Program Files\PC Clean`)

3. **Lancer l'application**
   - Clic droit sur `NettoyerPc.exe` → "Exécuter en tant qu'administrateur"
   - Accepter le contrôle UAC si demandé

4. **Premières utilisation**
   - Lire les descriptions des étapes avant de nettoyer
   - Commencer par "Mode Complet" pour un premier test (20-40 min)
   - **NE PAS FERMER** l'application durant le nettoyage

5. **Après nettoyage**
   - Consulter le rapport généré
   - Un redémarrage peut être nécessaire (l'app vous le dira)

## ⚙️ Mise à jour automatique

Una fois installée, l'app détecte automatiquement les nouvelles versions:
- Un bouton **"Mises à jour"** est disponible dans le menu principal
- Cliquer pour vérifier et télécharger les mises à jour
- L'installation se fait automatiquement et l'app redémarre

## ❓ Support

Visiter: https://github.com/Scryl/Cleanner-window/issues
```

---

## 🔄 Système de mise à jour automatique

### Comment ça fonctionne?

1. **Vérification**: L'app compare sa version (2.0.0.0) avec le tag GitHub (`v2.0.0`)
2. **Détection**: Si une version plus récente existe, le bouton s'allume
3. **Téléchargement**: L'app télécharge le nouvel `.exe` en silence
4. **Installation**: Un script batch remplace l'ancien exe et relance l'app
5. **Redémarrage**: L'application redémarre avec la nouvelle version

### Arborescence interne

```
%TEMP%/
├── NettoyerPc_update_XXXX.exe   (nouveau exe téléchargé)
├── NettoyerPc_install.bat       (script de remplacement)
└── NettoyerPc_2.0.0.old.exe     (ancien exe sauvegardé)
```

### Code de mise à jour (UpdateManager.cs)

Vérifier que `CurrentVersion` dans `Core/UpdateManager.cs` est à jour:

```csharp
static readonly Version CurrentVersion = new(2, 0, 0, 0);
```

**IMPORTANT**: Modifier cette version **avant** de créer chaque nouveau release!

---

## 📈 Gestion des versions

### Numérotation sémantique

Format: `v{MAJOR}.{MINOR}.{PATCH}.{BUILD}`

Exemples:
- `v2.0.0` - Première version de production
- `v2.0.1` - Patch/bugfix (aucune nouvelle fonctionnalité)
- `v2.1.0` - Nouvelle fonctionnalité mineure
- `v3.0.0` - Changement majeur (breaking changes)

### Processus de création d'une nouvelle version

#### 1. Modifier le code

```powershell
# ...faire les modifications...
# Tester localement
dotnet run -c Debug
```

#### 2. Mettre à jour la version

**Fichier**: `NettoyerPc/NettoyerPc.csproj`

```xml
<PropertyGroup>
    <Version>2.0.1</Version>
    <AssemblyVersion>2.0.1.0</AssemblyVersion>
    <FileVersion>2.0.1.0</FileVersion>
</PropertyGroup>
```

**Fichier**: `NettoyerPc/Core/UpdateManager.cs`

```csharp
static readonly Version CurrentVersion = new(2, 0, 1, 0);
```

#### 3. Compiler en Release

```powershell
cd NettoyerPc

# Nettoyer les anciens builds
dotnet clean -c Release

# Publier le nouveau build
dotnet publish -c Release -r win-x64 --self-contained true \
    -p:PublishTrimmed=true \
    -o .\publish\

# Créer le package ZIP
cd ..
Compress-Archive -Path "NettoyerPc\publish\*" `
                 -DestinationPath "NettoyerPC_2.0.1_win64.zip" `
                 -Force
```

#### 4. Créer le Release sur GitHub

```powershell
# (Manual):
# 1. https://github.com/Scryl/Cleanner-window/releases
# 2. "Create new release"
# 3. Tag: v2.0.1
# 4. Title: PC Clean v2.0.1
# 5. Description: changelog
# 6. Upload: NettoyerPC_2.0.1_win64.zip
# 7. Publish
```

#### 5. Mettre à jour CHANGELOG.md

```markdown
## [2.0.1] - 2026-02-24

### Fixed
- Correction du bug XXX
- Amélioration de la performance YYY

### Added
- Nouvelle fonctionnalité ZZZ

### Changed
- Interface légèrement modifiée
```

#### 6. Commiter et pusher

```powershell
git add .
git commit -m "Version 2.0.1 - Bugfixes et améliorations"
git push origin main
```

---

## 🧪 Vérification pré-déploiement

Avant chaque release, exécuter cette checklist:

- [ ] Tous les fichiers compilent sans erreur (`dotnet build -c Release`)
- [ ] L'application peut être lancée en tant qu'administrateur
- [ ] Les modes de nettoyage fonctionnent (au moins un test rapide)
- [ ] Le rapport se génère correctement (texte + JSON)
- [ ] Le système de mise à jour détecte les nouvelles versions
- [ ] Version mises à jour dans `.csproj` et `UpdateManager.cs`
- [ ] Le ZIP peut être extrait et l'app lancée
- [ ] CHANGELOG.md est rempli avec les changements
- [ ] Le Release GitHub contient la bonne description et les bons fichiers

---

## 📊 Monitoring et Support

### Où surveiller les problèmes:

1. **GitHub Issues**: https://github.com/Scryl/Cleanner-window/issues
2. **Feedback utilisateurs**
3. **Crash logs** dans `Reports/`

### Informations de débogage

Les utilisateurs peuvent envoyer leurs rapports:
```
C:\Users\[USERNAME]\AppData\Local\NettoyerPc\Reports\
```

Chaque rapport contient:
- Fichier `.txt` (formatage lisible)
- Fichier `.json` (données structurées pour analyse)

---

## 🔐 Sécurité

- ✅ Toujours compiler en **Release** pour la distribution
- ✅ Vérifier les certificats de signature de code (si possible)
- ✅ Ne jamais partager les credentials GitHub
- ✅ Utiliser des tokens GitHub avec permissions limitées
- ✅ Mettre à jour .NET SDK régulièrement

---

## 📞 Dépannage

### Problème: L'app ne trouve pas les mises à jour

```powershell
# Vérifier la version
dotnet publish --version-suffix debug

# Vérifier le manifest
cat NettoyerPc/app.manifest | grep requestedExecutionLevel
# Doit avoir: requestedExecutionLevel level="requireAdministrator"
```

### Problème: Le ZIP téléchargé s'ouvre mal

```powershell
# Windows Defender peut bloquer les fichiers ZIP téléchargés
# Solution: Propriétés du ZIP → Sécurité → Débloquer → Appliquer
```

### Problème: Erreur lors de la mise à jour ("Access denied")

- L'ancien exe est peut-être encore verrouillé
- Redémarrer le PC ou attendre quelques secondes
- Les antivirus/malware peuvent bloquer le remplacement de l'exe

---

## 📝 Licences et crédits

**PC Clean** © 2026 - Propriétaire  
Développé avec .NET 8.0 et WPF

---

**Version de ce guide**: 1.0  
**Dernière mise à jour**: Février 2026  
**Prochaine étape**: [Déployer v2.0.1](#)
