# Bastion — Tower Defense mobile (Unity 6, URP)

Jeu de défense de tours stylisé (cartoon PBR) pour Android, avec placeholders géométriques
soignés en attendant vos vrais assets 3D. Le projet est **généré par un script Éditeur** :
le dépôt contient la recette, pas les fichiers YAML fragiles.

## Démarrage — sans rien installer

**Unity n'est pas nécessaire pour jouer ni pour construire le jeu.** Trois workflows
GitHub Actions s'en chargent dans le nuage, à chaque push sur `main` :

- l'**APK Android**, à récupérer dans l'onglet *Actions* et à installer sur le téléphone ;
- la **version navigateur**, déployée sur Vercel et jouable sur PC comme sur mobile.

Une seule mise en place est requise, elle-même sans installation : la licence Unity
s'obtient via le workflow *Unity – Demander le fichier d'activation*, qui produit le
fichier à déposer sur une page web. Marche à suivre et pièges : **[CI_CD_SETUP.md](CI_CD_SETUP.md)**.

## Développer dans l'éditeur Unity (facultatif)

Utile seulement pour itérer vite : dans l'éditeur, un changement se teste en appuyant sur
Play, là où le nuage demande une construction complète à chaque essai.


1. Installez **Unity 6 LTS** (6000.3.x) via Unity Hub, avec le module **Android Build Support**
   (+ OpenJDK, SDK & NDK). Comptez plusieurs Go.
2. Ouvrez ce dépôt comme projet Unity. Unity installe les packages du `manifest.json`
   (URP, uGUI/TextMeshPro).
3. Menu **Bastion → 1. Générer le projet**. Cela crée :
   - `Assets/Settings/URP/` : pipeline asset mobile, renderer, profil de post-process ;
   - `Assets/Materials/` et `Assets/Art/Placeholders/` : palette et meshes procéduraux ;
   - `Assets/Prefabs/` : tours (5 × 3 niveaux), ennemis (5), projectiles, VFX, UI ;
   - `Assets/Data/` : ScriptableObjects tours, ennemis, 10 vagues, niveau « La Vallée » ;
   - `Assets/Scenes/Main.unity` : la scène jouable ;
   - les Player Settings Android.
4. Ouvrez `Assets/Scenes/Menu.unity`, **Play**. (`Main.unity` lancé seul joue le niveau de secours
   du `GameManager`, pratique pour itérer sur un niveau précis.)

Si Unity demande d'importer les *TMP Essential Resources*, acceptez puis relancez l'étape 3.

## Contenu et durée de vie

| Axe | Ce qui existe | Comment l'étendre |
|---|---|---|
| **Niveaux** | 3 (Vallée 10 vagues · Marais gelés 14 · Forges 18), biomes distincts | Create → Bastion → Niveau, ajouter au `LevelCatalog` (Resources/) |
| **Modes** | **Défense** (classique) · **Assaut** (inversé : vous déployez les monstres, une IA construit) | `GameMode` + un contrôleur par mode |
| **Difficultés** | Normal · Difficile · Cauchemar, étoiles séparées, déverrouillage en chaîne | `DifficultySettings` |
| **Sans fin** | après la dernière vague scriptée, vagues générées par budget de menace, record sauvegardé, gemmes par paliers | `endlessPool` / `endlessBossPool` par niveau |
| **Méta-progression** | gemmes (10 par étoile nouvelle × difficulté) → 8 améliorations permanentes à 10 rangs | Create → Bastion → Amélioration permanente |
| **Étoiles** | 3 niveaux × 3 difficultés × 2 modes × 3 étoiles = 54 | automatique avec un niveau de plus |

Le mode Assaut d'un niveau s'ouvre quand on l'a défendu une fois. Les monstres tués rapportent
de l'or à l'IA : spammer des gobelins nourrit la défense ; les volants passent les canons ; les
boss ont un cooldown. Les améliorations *Horde* et *Flux* ne servent qu'à l'Assaut.

## Jouer dans l'éditeur

| Geste mobile | Équivalent éditeur |
|---|---|
| Tap sur une case | clic gauche |
| Drag un doigt = caméra | clic droit maintenu |
| Pinch = zoom | molette |
| Bouton retour Android = pause | Échap |

Tap sur une case libre → menu de construction. Un premier tap sur une tour la prévisualise
(fantôme + portée), un second la construit. Tap sur une tour posée → améliorer / vendre / priorité de cible.

## Structure

Voir `ARCHITECTURE.md` (systèmes, flux, choix de perf) et `ASSETS_IMPORT.md` (remplacer les placeholders).

```
.
  Assets/Scripts/{Core,Grid,Towers,Enemies,Waves,Camera,UI,VFX,Editor}
  Assets/{Prefabs,Data,Materials,Art,Settings,Scenes}   ← générés
  Packages/manifest.json
  ProjectSettings/ProjectVersion.txt
```


## Tester sur un téléphone

1. Sur le téléphone : Paramètres → À propos → tapez 7 fois sur *Numéro de build*, puis
   Options pour les développeurs → **Débogage USB**. Branchez en USB, acceptez l'empreinte.
2. Dans Unity : File → Build Settings → Android → **Switch Platform** (une fois).
3. **Bastion → 3. Build & Run sur l'appareil USB** : APK de développement compilé, installé et
   lancé. Le Profiler (Window → Analysis → Profiler) se connecte automatiquement.
4. Logs : Window → Analysis → **Android Logcat** (package installé), filtre `Unity`.

Sans câble : récupérez `Builds/Bastion-dev.apk`, envoyez-le au téléphone (Drive, mail) et
ouvrez-le ; autorisez l'installation depuis cette source.

## Build Android

- **Bastion → 2. Appliquer les Player Settings Android** : `fr.veloute.bastion`, API 26 min,
  IL2CPP ARM64+ARMv7, paysage, Vulkan → GLES3, aucune permission forcée, ASTC, icône temporaire.
- **Bastion → 4. Construire l'AAB** → `Builds/Bastion.aab`. Pour un APK de test :
  File → Build Settings, décochez *Build App Bundle*.
- Signature : renseignez votre keystore dans Player Settings → Publishing Settings avant un
  envoi Play Store (le build de test utilise la clé debug).

## Performance visée

60 fps sur milieu de gamme. Les leviers : SRP Batcher + GPU instancing, une seule lumière avec
ombres (1 cascade, 30 m), MSAA 2x, bloom sans filtrage HQ, aucun `Instantiate` pendant une vague
(tout est poolé), ciblage par registre sans physique. Le Profiler (Window → Analysis) sur un
appareil réel reste le juge : `Development Build` + `Autoconnect Profiler`.

## Ajouter du contenu sans coder

- **Une tour** : clic droit dans `Assets/Data/Towers` → Create → Bastion → Tour ; remplissez les
  3 niveaux, glissez un visuel par niveau, ajoutez-la au `catalog` du `TowerManager` dans la scène.
- **Un ennemi** : Create → Bastion → Ennemi, réglez résistances / vol / boss, glissez un visuel.
- **Une vague** : Create → Bastion → Vague, listez des groupes ; ajoutez-la à `waves` du niveau.
- **Un niveau** : Create → Bastion → Niveau, dessinez `path` (cases consécutives, sans diagonale),
  réglez le biome (couleurs), les pools Sans fin / Assaut, puis ajoutez-le à `Resources/LevelCatalog`.
  Sa miniature dans le menu est dessinée depuis ses données.
- **Une amélioration permanente** : Create → Bastion → Amélioration permanente ; pour un nouvel
  effet, ajoutez une valeur à `MetaEffect` et lisez-la dans `MetaProgression`.
