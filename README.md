# Nettoyeur PC 2000

Application Windows de nettoyage système professionnel avec interface graphique WPF.

## 🎯 Fonctionnalités

### Mode Complet (20-40 minutes)
- ✅ Fichiers temporaires (utilisateur + système)
- ✅ Prefetch et thumbnails
- ✅ Caches de développement (SVN, Git, Visual Studio, node_modules, NuGet, npm, pip, etc.)
- ✅ VS Code cache
- ✅ Docker cleanup
- ✅ Navigateurs (Firefox, Chrome, Edge, Brave, Opera)
- ✅ Applications (Discord, Spotify)
- ✅ Steam cache (tous disques)
- ✅ Corbeilles (tous disques)
- ✅ DNS flush
- ✅ Nettoyage disque Windows (cleanmgr)
- ✅ Journaux Windows
- ✅ Windows Update cache
- ✅ Scan antivirus rapide

### Mode Printemps (60-120 minutes)
Tout le mode complet +
- ✅ Point de restauration système
- ✅ DirectX Shader Cache
- ✅ Gaming platforms (Epic, Battle.net)
- ✅ Configuration DNS Cloudflare (1.1.1.1)
- ✅ Reset IP / Winsock / ARP
- ✅ Optimisations registre
- ✅ Vérification disque (chkdsk)
- ✅ Défragmentation (tous disques)
- ✅ DISM cleanup
- ✅ Scan antivirus complet

## 🏗️ Architecture

```
NettoyerPc.sln
├── NettoyerPc/
│   ├── App.xaml                 ← Configuration WPF
│   ├── App.xaml.cs              ← Vérification admin
│   ├── MainForm.xaml            ← Menu principal
│   ├── MainForm.xaml.cs
│   ├── CleaningForm.xaml        ← Fenêtre de progression
│   ├── CleaningForm.xaml.cs
│   ├── Core/
│   │   ├── AdminHelper.cs       ← Vérification droits admin
│   │   ├── CleaningEngine.cs    ← Moteur de nettoyage (async)
│   │   ├── CleaningStep.cs      ← Modèle d'une étape
│   │   └── CleaningReport.cs    ← Génération du rapport
│   ├── Modules/
│   │   ├── ICleaningModule.cs
│   │   ├── TempFilesModule.cs
│   │   ├── DevCacheModule.cs    ← SVN, Git, VS, node_modules...
│   │   ├── BrowserModule.cs
│   │   ├── GamingModule.cs
│   │   ├── NetworkModule.cs
│   │   ├── WindowsModule.cs     ← Défrag, chkdsk, WU...
│   │   └── SecurityModule.cs    ← Defender, scan...
│   └── Resources/
│       └── icon.ico
```

## 🔧 Prérequis

- Windows 10/11
- .NET 8.0 SDK ou ultérieur
- Visual Studio 2022 (recommandé) ou Visual Studio Code
- Droits administrateur (requis pour l'exécution)

## 📦 Installation

### Avec Visual Studio 2022

1. Ouvrir `NettoyerPc.sln`
2. Restaurer les packages NuGet (automatique)
3. Build → Build Solution (Ctrl+Shift+B)
4. Clic droit sur le projet → Publish pour créer un exécutable autonome

### Avec .NET CLI

```powershell
# Restaurer les dépendances
dotnet restore NettoyerPc.sln

# Compiler en Debug
dotnet build NettoyerPc.sln

# Compiler en Release
dotnet build NettoyerPc.sln -c Release

# Publier une version autonome
dotnet publish NettoyerPc\NettoyerPc.csproj -c Release -r win-x64 --self-contained true
```

## 🚀 Utilisation

1. **Lancer l'application en tant qu'administrateur** (obligatoire)
   - Clic droit sur `NettoyerPc.exe` → Exécuter en tant qu'administrateur

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
