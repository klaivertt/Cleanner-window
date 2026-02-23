# 📥 Guide d'installation utilisateur - PC Clean

Bienvenue! Voici comment installer et utiliser **PC Clean** en quelques minutes.

---

## ✅ Prérequis

- Windows 10 ou 11
- 100 MB d'espace disque libre
- **Connexion Internet** (pour télécharger + mises à jour)
- **Droits administrateur** (obligatoire)

---

## 🚀 Installation en 3 étapes

### Étape 1⃣ : Télécharger

1. Aller sur [GitHub Releases](https://github.com/Scryl/Cleanner-window/releases)
2. Cliquer sur la **version la plus récente** (ex: v2.0.0)
3. Télécharger le fichier **`NettoyerPC_2.0.0_win64.zip`**

![Télécharger le ZIP]()

### Étape 2⃣ : Extraire

1. Localiser le fichier ZIP téléchargé (généralement dans `Téléchargements/`)
2. **Clic droit** → **"Extraire tout"**
   - Ou **Double-clic** sur le ZIP et drag-drop dans un dossier
3. Choisir un dossier de destination (ex: `C:\Program Files\` ou `C:\Users\[Votre User]\Applications\`)
4. Valider

### Étape 3⃣ : Lancer l'application

1. **Naviguer** dans le dossier extrait
2. **Clic droit** sur `NettoyerPc.exe`
3. **Sélectionner** "Exécuter en tant qu'administrateur"
4. **Cliquer "Oui"** au contrôle UAC (sécurité Windows)

✅ **L'application se lance!**

---

## 🎯 Guide d'utilisation (premier lancement)

### 1️⃣ Lire les informations

L'application affiche d'abord:
- 🔒 **Garantie de sécurité**: Explique ce qui SERA supprimé vs ce qui ne le sera JAMAIS
- 4 modes de nettoyage prédéfinis

### 2️⃣ Choisir un mode

| Mode | Pour qui? | Temps |
|------|-----------|-------|
| 🟢 Rapide | Test initial, utilisateurs légers | 10-15 min |
| 🟠 Complet | 95% des utilisateurs, utilisation courante | 20-40 min |
| 🟡 Printemps | Nettoyage profond annuel, vieux PC | 60-90 min |
| 🔴 Gaming | Gamers, problèmes de performances jeux | 30-50 min |

**Recommandation pour 1er lancement**: Commencer par **Mode Rapide** pour tester

### 3️⃣ (Optionnel) Personnaliser

Si vous voulez contrôler chaque détail:
- Cliquer sur **"Sélection avancée"**
- Lire les descriptions (avec emojis 🟢🟠🟡 indiquant le risque)
- Cocher/décocher les catégories
- Cliquer **"Confirmer"**

### 4️⃣ Lancer le nettoyage

1. Cliquer **"Commencer"**
2. **NE PAS ÉTEINDRE** le PC ou fermer l'app pendant le nettoyage!
3. L'interface affiche en temps réel:
   - Fichiers supprimés
   - Espace libéré
   - Étapes réussies
   - Menaces détectées

### 5️⃣ Consulter le rapport

Après le nettoyage:
1. Cliquer **"Voir le rapport"** (en bas)
2. L'application affiche:
   - 📊 Statistiques (fichiers, espace, durée)
   - 📝 Détail par étape
   - 💾 Fichiers/dossiers supprimés (premier 500)

3. **Rapport sauvegardé automatiquement** dans le dossier `Reports/` :
   - `.txt` : Version lisible humain
   - `.json` : Données structurées (pour analyse)

---

## ⚙️ Mises à jour automatiques

### Vérifier les mises à jour

1. Dans le menu principal, cliquer **"🔄 Mises à jour"**
2. L'app vérifie auprès de GitHub
3. Résultats possibles:
   - ✅ "Vous êtes à jour" → Fermer et continuer
   - 📥 "Mise à jour disponible" → Voir les changements et cliquer **"Installer"**
   - ⚠️ "Erreur" → Vérifier votre connexion Internet

### Installation automatique

Une fois "Installer" cliqué:
1. ✅ Téléchargement en arrière-plan (~5-30 MB selon la version)
2. 📥 Barre de progression affichée
3. 🔄 Application redémarre automatiquement
4. ✅ Vous êtes à jour!

**Aucune action manuelle requise!**

---

## ❓ FAQ

### Q: L'app dit "Accès refusé" ou ne se lance pas

**Réponse**: 
- Vérifier que **Admin est activé** (Clic droit → Exécuter en tant qu'administrateur)
- Vérifier que l'antivirus n'a pas mis en quarantaine le fichier
  - Si en quarantaine: Restaurer depuis votre antivirus
- Redémarrer le PC

### Q: Après nettoyage, mon espace disque ne baisse pas

**Réponse**:
- Windows **cache le changement** jusqu'au redémarrage
- **Redémarrer le PC** → l'espace apparaît
- L'app nettoie réellement beaucoup (vérifier le rapport JSON)

### Q: Je peux relancer le nettoyage directement?

**Réponse**: 
- Oui! L'app se réinitialise automatiquement après chaque nettoyage
- Vous pouvez enchaîner plusieurs modes si vous voulez

### Q: Puis-je supprimer le dossier après installation?

**Réponse**:
- **OUI** - L'app est autonome (standalone)
- Elle ne crée pas de registre ni de dépendances système
- Vous pouvez la mettre où vous voulez (USB, SSD, etc.)
- Pour désinstaller: Supprimez le dossier, c'est tout

### Q: Où sont les rapports sauvegardés?

**Réponse**:
- Dossier `Reports/` **à côté du programme**
- Ou: Cliquer **"Mes rapports"** → Voir les fichiers.txt et .json
- Vous pouvez les copier/archiver où vous voulez

### Q: Puis-je lancer l'app en mode silencieux?

**Réponse**: 
- Pas actuellement (il n'y a pas d'arguments CLI)
- Version future prévue avec `--silent`, `--mode complete`, etc.

### Q: L'app utilise combien de RAM/CPU?

**Réponse**:
- **Avant nettoyage**: ~50-80 MB RAM, 0% CPU
- **Pendant nettoyage**: ~100-150 MB RAM, 20-80% CPU (normal)
- Pas de memory leak connu

---

## 🔒 Sécurité & Confidentialité

### Ce que l'app FAIT:
- ✅ Supprime fichiers temporaires locaux
- ✅ Vide caches (navigateurs, applications)
- ✅ Nettoie registre (orphelins seulement)
- ✅ Sauvegarde rapports localement

### Ce que l'app NE fait PAS:
- ❌ N'envoie aucune donnée à serveur
- ❌ N'accède pas Internet pendant nettoyage (sauf mise à jour vérification)
- ❌ N'accède pas à vos fichiers personnels
- ❌ Ne supprime jamais vos jeux/documents/photos
- ❌ Aucune télémétrie ou tracking

### Antivirus false-positive?

Si votre antivirus signale l'app comme suspect:
- C'est normal! Les tools de nettoyage sont souvent flaggés (false positive courant)
- **Solution**: Mettre l'app en whitelist / exceptions
  - Ex: Ajouter le dossier NettoyerPC à la liste blanche de Windows Defender
- Ou: Compiler vous-même depuis le code source (voir README.md)

---

## 🆘 Problèmes avancés

### Erreur "The system cannot find the path specified"

```
Solution:
- L'app essaie d'accéder à un chemin qui n'existe plus
- Normal si votre PC a des configurations spéciales
- Consulter le rapport pour le détail de l'erreur
- Vérifier: Disques externes non branché, lecteur Z: mappé à un dossier qui n'existe plus
```

### Nettoyage s'arrête sur une étape

```
Causes possibles:
1. Une application vérouille des fichiers (ex: VS Code, Chrome ouvert)
   → Fermer l'application et relancer le nettoyage
   
2. Les fichiers sont en utilisation système
   → Redémarrer le PC et réessayer
   
3. Permissions insuffisantes sur un dossier
   → S'assurer d'avoir Admin sur TOUTES les partitions
   
4. Antivirus interfère
   → Ajouter l'app en exception antivirus temporairement
```

### Redémarrage demandé mais dont faire?

```
Si message "Redémarrage requis":
1. ✅ Sauvegarder vos fichiers ouverts
2. ✅ Fermer toutes les applications
3. ✅ Cliquer OUI au redémarrage
4. ✅ Windows redémarre et finit les opérations
5. ✅ Vous êtes dans un état optimal!

Si vous appuyez NON:
- Les changements ne sont qu'à moitié appliqués
- Redémarrez vous-même dès que possible
```

---

## 📞 Support & Contact

Si vous rencontrez un problème:

1. **Vérifier le rapport d'erreur** (fichier .txt)
   - Contient généralement l'explication et la solution

2. **Ouvrir une issue GitHub**
   - https://github.com/Scryl/Cleanner-window/issues
   - Inclure:
     - Votre version Windows (ex: Windows 11 22H2, build 22621)
     - Version de l'app (ex: v2.0.0)
     - Description du problème
     - Le fichier rapport d'erreur (anonymisé)

3. **Vérifier les issues existantes**
   - Votre problème est peut-être déjà résolu

---

## 📊 Statistiques typiques

Voici ce qu'on peut espérer après nettoyage (selon utilisation):

| Profil utilisateur | Espace libéré | Temps |
|-------------------|---------------|-------|
| **Gamer léger** | 5-15 GB | 20-30 min |
| **Dev/VS Code** | 10-25 GB | 30-45 min |
| **Professionnel** | 15-40 GB | 40-60 min |
| **Gaming intensif** | 30-80 GB | 60-90 min |
| **PC très ancien** | 50-200+ GB | 90+ min |

*Chiffres indicatifs basés sur data réelle - Votre résultat dépend de votre usage!*

---

## 🎓 Conseils d'expert

1. **Créer un point de restauration avant** (Windows n'en crée pas automatiquement):
   ```
   Windows + R → rstrui.exe → Créer point de restauration
   ```

2. **Planifier les nettoyages**:
   - Mode Rapide: 1x par semaine
   - Mode Complet: 1x par mois  
   - Mode Printemps: 1x par an

3. **Archiver les rapports** pour suivi:
   - Copier régulièrement `Reports/` ailleurs
   - Voir l'évolution de votre système au fil du temps

4. **Pour les devs**:
   - Exclure `node_modules` si vous développez (risque: perdre dépendances)
   - `npm install` resynchronisera après oubli accidentel

---

## ✨ Merci d'utiliser PC Clean!

- ⭐ Si ça vous a plu: **Starrez le repo GitHub**
- 🐛 Si ça bug: **Ouvrez une issue**
- 💡 Si vous avez une idée: **Partagez vos suggestions**
- 👥 Si vous êtes dev: **Contribuez au code!**

---

**Questions?** → https://github.com/Scryl/Cleanner-window/discussions

**Besoin de générer un Release?** → Voir [DEPLOYMENT.md](DEPLOYMENT.md)

**Merci d'avoir choisi PC Clean - Votre application de nettoyage de confiance! 🧹✨**
