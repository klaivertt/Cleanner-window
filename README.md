# 🧹 PC Clean

**Application de nettoyage système Windows professionnel – Interface WPF optimisée**

**Version**: 2.0.0 | **Build**: February 2026 | **Plateforme**: Windows 10/11

---

## 📌 À propos

**PC Clean** est une application complète de maintenance Windows conçue pour :
- 🚀 **Libérer de l'espace disque** disque (des dizaines de GB possibles)
- ⚡ **Optimiser les performances** système (défragmentation, TRIM SSD, nettoyage registre)
- 🎮 **Optimiser gaming** (shader cache, plateformes de jeux, éléments inutiles)
- 🔒 **Améliorer la sécurité** (scan antivirus, nettoyage navigateurs)
- 📊 **Générer des rapports détaillés** (JSON + texte formaté)
- 🔄 **Mise à jour automatique** via GitHub

**GARANTIE DE SÉCURITÉ**: Aucun fichier personnel, paramètres, jeux ou documents ne sera jamais supprimé. Seuls les fichiers temporaires et caches se recréant automatiquement sont nettoyés.

---

## ✨ Fonctionnalités principales

### 🎯 4 Modes de nettoyage prédéfinis

| Mode | Durée | Description |
|------|-------|-------------|
| **Mode Rapide** | 10-15 min | Nettoyage léger : temp, cache navigateurs, jeux 🟢 |
| **Mode Complet** | 20-40 min | Nettoyage approfondi : + Windows update, journaux, DNS 🟠 |
| **Mode Printemps** | 60-90 min | Nettoyage total : + SFC, DISM, défrag, restauration 🟡 |
| **Mode Gaming** | 30-50 min | Optimisation gamers : shader cache, Epic, Steam, etc. 🔴 |

### 📂 Catégories de nettoyage

#### 🌟 Bases (activé par défaut)
- **Fichiers temporaires**: %TEMP%, Windows\Temp, prefetch, thumbnails
- **Navigateurs**: Firefox, Chrome, Edge, Brave, Opera, Vivaldi (cache + tous profils)
- **Applications populaires**: Discord, Spotify, Teams, OBS, Slack
- **Gaming**: Steam cache, DirectX shader, Epic Games, Battle.net
- **Réseau**: Flush DNS, configuration IP, cache ARP

#### 🔧 Avancé (optionnel)
- **Développement**: SVN, Git logs, Visual Studio, node_modules, caches package managers
- **Optimisation système**: SFC, DISM, défragmentation HDD, TRIM SSD
- **VS Code**: Cache GPU, CachedData, historique
- **Restauration**: Points de restauration, Windows Update
- **Sécurité**: Antivirus Defender, scans rapide/complet

#### ⚠️ Critique (très optionnel)
- **Bloatware**: Jeux pré-installés (Candy Crush), apps sociales, Xbox GameBar
- **Nettoyage Docker**: Images, conteneurs, volumes inutilisés

### 📊 Rapports professionnels

Chaque nettoyage génère des rapports dans le dossier `Reports/` :

```
CleanerReport_2026-02-23_16-05-51.txt   ← Rapport formaté lisible
CleanerReport_2026-02-23_16-05-51.json  ← Données structurées pour analyse
```

**Contenu détaillé** :
- 📅 Date, heure, durée (précision à la seconde)
- 💾 Espace libéré (en bytes + formaté)
- 📁 Nombre de fichiers supprimés + liste partielle
- ✓ Étapes réussies vs échouées
- ⚠️ Menaces détectées
- 🖥️ Informations système (OS, version Windows, build)
- 📈 Statistiques par étape (archives détaillées JSON)

### 🔄 Mise à jour automatique

- ✅ Vérification dans le menu via bouton **"Mises à jour"**
- 📥 Téléchargement et installation silencieux
- 🔄 Redémarrage automatique de l'application
- 📝 Changelog affiché avant installation

---

## 🛠️ Architecture technique

```
NettoyerPc.sln
├── App.xaml / App.xaml.cs                ← Init WPF + vérification admin
├── MainForm.xaml / MainForm.xaml.cs      ← Menu principal (dark theme)
├── CleaningForm.xaml / CleaningForm.xaml.cs ← Fenêtre de progression (4 stat cards)
├── SelectionForm.xaml / SelectionForm.xaml.cs ← Sélection détaillée des étapes
├── ReportViewerForm.xaml / ReportViewerForm.xaml.cs ← Visualisation rapports JSON
├── UpdateCheckForm.xaml / UpdateCheckForm.xaml.cs ← Vérification mises à jour
├── Core/
│   ├── AdminHelper.cs ─────── Vérification & elevation droits admin
│   ├── CleaningEngine.cs ───── Orchestration asynchrone des modules
│   ├── CleaningStep.cs ────── Modèle d'une étape de nettoyage
│   ├── CleaningReport.cs ─── Génération rapports (TXT + JSON avancés)
│   └── UpdateManager.cs ─── GitHub Releases client (auto-update)
├── Modules/ (interfaces ICleaningModule)
│   ├── TempFilesModule.cs ────────── Fichiers temp + prefetch
│   ├── BrowserModule.cs ──────────── Navigateurs (11+ navigateurs)
│   ├── DevCacheModule.cs ─────────── Caches dev (SVN, Git, VS, npm, etc.)
│   ├── GamingModule.cs ───────────── Gaming optimization
│   ├── NetworkModule.cs ──────────── Réseau, DNS, IP
│   ├── SecurityModule.cs ────────── Antivirus + scans
│   ├── WindowsModule.cs ─────────── Windows system, registre, défrag
│   └── ProcessHelper.cs ──────────── Force-close applications
└── Resources/
    └── Icons, styles, app.manifest
```

### 🏗️ Patterns utilisés

- **Modular architecture**: Chaque catégorie = module indépendant
- **Async/Await**: Opérations non-bloquantes (UI reste fluide)
- **MVVM-light**: Séparation logique/présentation
- **Dark XAML theme**: Cohérent (#1E1E2E, #12121F, couleurs émeude/bleu/orange)
- **JSON serialization**: Rapports exploitables programmatiquement

---

## ⚙️ Prérequis

### Pour utiliser l'application :
- Windows 10 ou 11 (22H2+ recommandé)
- 100 MB d'espace disque disponible
- **Droits administrateur** (obligatoire)

### Pour compiler/développer :
- .NET 8.0 SDK [télécharger](https://dotnet.microsoft.com/download)
- Visual Studio 2022 Community (gratuit) OU Visual Studio Code + C# DevKit
- Git
- (Optionnel) Windows SDK pour les ressources

---

## 🚀 Installation & Lancement

### Méthode 1 : Utilisateur final (version compilée)

1. **Télécharger** le ZIP depuis [GitHub Releases](https://github.com/Scryl/Cleanner-window/releases)
2. **Extraire** le ZIP dans un dossier (ex: `C:\Program Files\PC Clean\`)
3. **Clic droit** sur `NettoyerPc.exe` → **Exécuter en tant qu'administrateur**
4. **Accepter** le contrôle UAC

### Méthode 2 : Développeur (source code)

```powershell
# Cloner le repository
git clone https://github.com/Scryl/Cleanner-window.git
cd Cleanner-window/NettoyerPc

# Compiler en Debug
dotnet build -c Debug

# Lancer EN TANT QU'ADMINISTRATEUR
dotnet run -c Debug

# Ou publier version autonome
dotnet publish -c Release -r win-x64 --self-contained true -o .\publish\
```

---

## 📖 Guide d'utilisation

### Première utilisation

1. **Lire les avertissements** - Comprendre ce qui va être supprimé
2. **Choisir un mode** :
   - 🟢 **Mode Rapide** pour test initial
   - 🟠 **Mode Complet** pour usage courant
   - 🟡 **Mode Printemps** pour nettoyage profond annuel
   - 🔴 **Mode Gaming** si problèmes de jeux

3. **Optionnel**: Personnaliser via "Sélection avancée"

4. **Confirmer** et attendre la fin

5. **Consulter le rapport** dans "Mes rapports"

### Sélection avancée

Chaque catégorie peut être :
- ✅ Entièrement activée (tous les éléments)
- ⚪ Partiellement activée (cocher individuellement)
- ❌ Complètement désactivée (rien ne sera fait)

**Icônes de risque** :
- 🟢 **100% sûr** - Aucun risque, se recréera automatiquement
- 🟠 **Sur** - Très sûr mais consulter description
- 🟡 **Modéré** - Peut nécessiter redémarrage
- 🔴 **Attention** - Lisez bien la description avant

### Rapports

Après nettoyage : 
- 📄 **Fichier TXT** : Lecture facile avec formatage nice
- 📋 **Fichier JSON** : Analysable par d'autres outils/scripts
- 📊 **Visionneuse intégrée** : Interface dark theme avec stat cards

---

## 🐛 Dépannage

### L'application ne se lance pas
```powershell
# Vérifier l'installation de .NET 8
dotnet --version

# Vérifier les droits admin
# (Clic droit → Exécuter en tant qu'administrateur)
```

### Erreur "Access denied"
- Un antivirus peut bloquer le remplacement de fichiers
- Vérifier que l'application n'est pas en cours de suppression lors d'une mise à jour
- Redémarrer le PC et réessayer

### Nettoyage incomplet
- Certains fichiers ne sont pas supprimés = ils sont utilisés
- Fermer les applications (notamment navigateurs, VS Code)
- Utiliser "Force-close" avant "Mode Complet"

### Espace non libéré visible
- Windows cache l'espace disque libéré jusqu'au prochain démarrage
- Redémarrer le PC
- Vérifier avec `WinDirStat` ou `TreeView` l'espace réellement libéré

---

## 📝 Contribution

- 🐛 **Signaler un bug** : [Issues](https://github.com/Scryl/Cleanner-window/issues)
- 💡 **Suggérer une fonctionnalité** : Ouvrir une discussion
- 🔧 **Pull requests** acceptées pour amélioration
- 📚 **Documentation** : Aide bienvenue

### Développer un nouveau module

```csharp
using NettoyerPc.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MyCustomModule : ICleaningModule
{
    public string Name => "Mon Module Personnalisé";
    
    public List<CleaningStep> GetSteps(CleaningMode mode)
    {
        return new List<CleaningStep>
        {
            new CleaningStep
            {
                Name = "Ma première étape",
                Category = "custom",
                Status = "En attente"
            }
        };
    }
    
    public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            // Votre logique de nettoyage
            step.FilesDeleted = 42;
            step.SpaceFreed = 1024 * 1024 * 100; // 100 MB
        }, cancellationToken);
    }
}
```

Puis ajouter dans `CleaningEngine.InitializeModules()` :
```csharp
_modules.Add(new MyCustomModule());
```

---

## 🛡️ Sécurité & Confidentialité

- ✅ **Aucun envoi de données** - Tout reste sur votre PC
- ✅ **Code source ouvert** - Auditez le code
- ✅ **Pas de tracking** - Aucune telémétrie
- ✅ **Mises à jour vérifiées** - Via GitHub releases officielles seulement
- ✅ **Garantie zéro suppression** de fichiers personnels

---

## 📄 Licence & Légal

```
PC Clean - Application de nettoyage Windows
Copyright © 2026

AVERTISSEMENT :
Ce logiciel est fourni "tel quel" sans garantie d'aucune sorte.
L'utilisateur l'utilise à ses propres risques.

L'auteur ne peut pas être tenu responsable pour:
- Perte de données
- Corruption système
- Impossible à démarrer
- Mises à jour échouées

RECOMMANDATIONS:
- Créer un point de restauration avant nettoyage
- Utiliser une version à jour de Windows
- Archiver les données importantes
```

---

## 🤝 Support & Crédits

**Auteur** : klaivertt  
**GitHub** : https://github.com/Scryl/Cleanner-window  
**Issues & Support** : https://github.com/Scryl/Cleanner-window/issues

**Technos** :
- Framework: .NET 8.0
- UI: WPF (Windows Presentation Foundation)
- Langage: C# 12
- Icônes: Unicode emojis

---

## 📈 Roadmap futur

- [ ] Portabilité Linux/macOS via WinUI 3 ou autre
- [ ] Scan personnalisé (sélectionner des dossiers)
- [ ] Historique nettoyage (graphiques d'espace libéré)
- [ ] Cloud sync des rapports (OneDrive/Google Drive)
- [ ] Planification automatique (nettoyage nocturne)
- [ ] Plugin system (modules dynamiques)
- [ ] App Windows Store

---

**🔗 Liens utiles** :
- [Guide de déploiement](DEPLOYMENT.md)
- [Changelog complet](CHANGELOG.md)
- [GitHub Repository](https://github.com/Scryl/Cleanner-window)

---

**Version**: 2.0.0  
**Dernière mise à jour**: Février 2026  
**Statut**: Production-ready ✅

2. **Choisir un mode de nettoyage**
   - Mode Complet : Pour un nettoyage rapide (~30 min)
   - Mode Printemps : Pour un nettoyage approfondi (~90 min)

3. **Suivre la progression**
   - L'interface affiche en temps réel les étapes
   - Les statistiques sont mises à jour automatiquement
   - Un journal d'activité détaillé est disponible en bas

4. **Consulter le rapport**
   - Un rapport détaillé est automatiquement généré dans `Reports/`
   - Accessible via le bouton "Voir les rapports" du menu

## 📊 Rapports

Les rapports de nettoyage sont sauvegardés automatiquement dans le dossier `Reports/` avec :
- Date et heure du nettoyage
- Durée totale
- Nombre de fichiers supprimés
- Espace disque libéré
- Menaces détectées
- Détail de chaque étape

Format : `CleanerReport_YYYY-MM-DD_HH-mm-ss.txt`

## ⚠️ Avertissements

- **Toujours exécuter en tant qu'administrateur**
- **Fermer tous les programmes avant le nettoyage**
- Le mode Printemps peut prendre plus d'1h30
- Certaines opérations peuvent nécessiter un redémarrage
- Un point de restauration est créé en mode Printemps

## 🛠️ Technologies

- **Framework**: .NET 8.0
- **UI**: WPF (Windows Presentation Foundation)
- **Langage**: C# 12
- **Architecture**: MVVM-like avec modules séparés
- **Async/Await**: Pour des opérations non-bloquantes

## 📝 Notes de développement

### Ajouter un nouveau module

1. Créer une classe dans `Modules/` implémentant `ICleaningModule`
2. Implémenter `GetSteps()` et `ExecuteStepAsync()`
3. Ajouter le module dans `CleaningEngine.InitializeModules()`

```csharp
public class CustomModule : ICleaningModule
{
    public string Name => "Mon Module";
    
    public List<CleaningStep> GetSteps(CleaningMode mode)
    {
        return new List<CleaningStep>
        {
            new() { Name = "Ma tâche personnalisée" }
        };
    }
    
    public async Task ExecuteStepAsync(CleaningStep step, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            // Votre logique ici
        }, cancellationToken);
    }
}
```

## 🤝 Contribution

Les contributions sont les bienvenues ! N'hésitez pas à :
- Signaler des bugs
- Proposer de nouvelles fonctionnalités
- Améliorer la documentation
- Ajouter de nouveaux modules de nettoyage

## 📄 Licence

Ce projet est fourni "tel quel" sans garantie d'aucune sorte.
Utilisez-le à vos propres risques.

## 👤 Auteur

**klaivertt**

---

**Version**: 1.0.0  
**Dernière mise à jour**: Février 2026
