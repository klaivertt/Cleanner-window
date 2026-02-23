# Release Convention — PC Clean

Guide à suivre à chaque nouvelle release GitHub.

---

## 1. Incrémenter la version dans le code

Deux fichiers à mettre à jour **avant** de builder/publier :

### `NettoyerPc/Core/AppConstants.cs`
```csharp
public const string AppVersion  = "0.2.0-beta";               // ← chaîne lisible
public static readonly Version VersionNumber = new(0, 2, 0, 0); // ← X.Y.Z.0
```

### `NettoyerPc/PCClean.csproj`
```xml
<Version>0.2.0-beta</Version>
<AssemblyVersion>0.2.0.0</AssemblyVersion>
<FileVersion>0.2.0.0</FileVersion>
```

---

## 2. Builder en Release

```powershell
dotnet publish NettoyerPc/PCClean.csproj -c Release -r win-x64 --self-contained true
```

---

## 3. Créer le ZIP

Zipper le contenu du dossier `publish/` (ou `bin/Release/net8.0-windows/`).

**Format du nom :**
```
PC.Clean.V-X.Y.Z-Beta.zip
```
> Exemple : `PC.Clean.V-0.2.0-Beta.zip`

Le ZIP peut contenir les fichiers à la racine **ou** dans un unique sous-dossier.
Les deux structures sont supportées par l'auto-updater.

---

## 4. Créer la release sur GitHub

| Champ | Format | Exemple |
|-------|--------|---------|
| **Tag** | `vX.Y.Z` | `v0.2.0` |
| **Titre** | `PC Clean V-X.Y.Z-Beta` | `PC Clean V-0.2.0-Beta` |
| **Asset** | Le ZIP ci-dessus | `PC.Clean.V-0.2.0-Beta.zip` |

> ⚠️ Un seul asset `.zip` par release — l'updater prend le premier trouvé.

---

## 5. Checklist rapide

- [ ] `AppConstants.AppVersion` mis à jour
- [ ] `AppConstants.VersionNumber` mis à jour
- [ ] `PCClean.csproj` (`Version`, `AssemblyVersion`, `FileVersion`) mis à jour
- [ ] Build Release OK (`0 erreur`)
- [ ] ZIP créé avec le bon nom
- [ ] Tag GitHub au format `vX.Y.Z`
- [ ] Asset ZIP uploadé dans la release

---

## Schéma de versioning

```
vMAJOR.MINOR.PATCH

MAJOR → changement incompatible / refonte majeure
MINOR → nouvelles fonctionnalités
PATCH → corrections de bugs
```
