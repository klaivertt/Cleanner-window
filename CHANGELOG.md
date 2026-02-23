# Changelog

## [1.0.0] - 2026-02-23

### Ajouté
- ✅ Interface graphique WPF moderne
- ✅ Mode Nettoyage Complet (26+ étapes)
- ✅ Mode Nettoyage de Printemps (50+ étapes)
- ✅ Architecture modulaire avec 7 modules de nettoyage
- ✅ Affichage en temps réel de la progression
- ✅ Génération automatique de rapports détaillés
- ✅ Support multi-disques
- ✅ Vérification des droits administrateur
- ✅ Gestion asynchrone pour interface non-bloquante
- ✅ Logs en temps réel
- ✅ Statistiques détaillées (fichiers, espace, durée)
- ✅ Support pour tous les navigateurs principaux
- ✅ Nettoyage caches de développement (Git, SVN, VS, node_modules, etc.)
- ✅ Optimisations réseau et DNS
- ✅ Scan antivirus intégré (Windows Defender)
- ✅ Défragmentation et vérification disque

### Modules implémentés
- **TempFilesModule**: Fichiers temporaires, prefetch, thumbnails
- **DevCacheModule**: SVN, Git, Visual Studio, node_modules, NuGet, npm, pip, Composer, Yarn, Docker
- **BrowserModule**: Firefox, Chrome, Edge, Brave, Opera
- **GamingModule**: Steam, DirectX cache, Epic Games, Battle.net
- **NetworkModule**: DNS flush, configuration Cloudflare, reset IP/Winsock/ARP
- **WindowsModule**: Corbeilles, journaux, cleanmgr, registre, défrag, Windows Update, DISM
- **SecurityModule**: Windows Defender (mise à jour + scan)

### Améliorations par rapport au script batch
- ✅ Interface graphique intuitive vs console
- ✅ Progression visuelle en temps réel
- ✅ Annulation possible pendant le nettoyage
- ✅ Rapports sauvegardés automatiquement
- ✅ Code maintainable et extensible
- ✅ Gestion d'erreurs robuste
- ✅ Opérations asynchrones (ne bloque pas l'interface)
