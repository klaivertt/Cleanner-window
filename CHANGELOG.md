# Changelog — PC Clean

---

## [v0.1.0-beta] — 2026-02-23

Première version beta de PC Clean — Application de nettoyage système Windows professionnelle.

### ✨ Fonctionnalités principales

- 🧹 **Nettoyage complet** — Fichiers temporaires (`%TEMP%`, `C:\Windows\Temp`), caches navigateurs (Chrome, Firefox, Edge, Brave, Vivaldi, Opera GX), miniatures, Prefetch, Corbeille, logs système
- ⚡ **Optimisations système** — SFC `/scannow`, DISM `/RestoreHealth`, reset réseau + DNS Cloudflare 1.1.1.1, rebuild cache polices, TRIM / défragmentation
- 🎮 **Gaming optimization** — Shader cache Steam, logs Epic Games / Battle.net, cache DirectX / GPU (D3DSCache), dumps et logs d'applications de gaming
- 📊 **Rapports détaillés** — Format TXT lisible et JSON structuré v3.0 (byCategory, skippedSteps, metadata, execution summary)
- 🔄 **Auto-update** — Système de mise à jour automatique via GitHub Releases
- 🌙 **Dark theme** — Interface WPF sombre et professionnelle, borderless WindowChrome
- 📋 **4 modes prédéfinis** — Rapide, Complet, Nettoyage de Printemps, Gaming + Disque
- ⚙️ **Mode personnalisé** — Sélection granulaire de chaque opération
- 🌐 **Multilingue** — Français, English, Español (chargé depuis JSON)
- 📱 **Gestion des applications** — Activation/désactivation par app (27 apps supportées), les apps désactivées sont ignorées pendant le nettoyage
- 🛡️ **Suppression bloatwares** — Candy Crush, Facebook, Xbox GameBar, Cortana, télémétrie Microsoft, etc.

### 📦 Installation

1. Télécharger `PCClean_v0.1.0-beta_win64.zip`
2. Extraire le ZIP dans un dossier (ex : `C:\Program Files\PC Clean\`)
3. Clic droit sur `NettoyerPc.exe` → **Exécuter en tant qu'administrateur**
4. Accepter le contrôle UAC

> ⚠️ **Droits administrateur requis** — Nécessaire pour le nettoyage système, SFC, DISM et la suppression de bloatwares.

### 🖥️ Configuration requise

| | |
|---|---|
| **OS** | Windows 10 / Windows 11 |
| **Architecture** | x64 |
| **Droits** | Administrateur |
| **Runtime** | .NET 8 (inclus dans le ZIP) |

---

*PC Clean est un outil open-source. Aucune donnée personnelle n'est collectée ou envoyée.*
