# Architecture technique — Tower Defense « Bastion » (nom de code)

Unity 6 LTS (6000.3), URP mobile, C#, Android API 26+.

## Principe directeur

**Le code ne connaît jamais un modèle 3D.** Chaque tour, ennemi et projectile est un prefab
« logique » (collider, scripts, points d'ancrage) qui contient un enfant `Visual/`.
Le placeholder géométrique vit dans `Visual/` ; remplacer un asset Piloto Studio = remplacer
cet enfant, et rien d'autre. Voir `ASSETS_IMPORT.md`.

**Toutes les données sont des ScriptableObjects** (`Assets/Data/`) : une tour, un ennemi,
une vague ou un niveau se règle dans l'Inspector, sans toucher au code.

**Tout est généré par un script Éditeur** (`Assets/Scripts/Editor/ProjectBootstrap.cs`,
menu *Bastion → 1. Générer le projet*) : URP asset, matériaux, prefabs, données, scène,
réglages Android. Le dépôt ne contient donc aucun YAML Unity fragile écrit à la main ;
il contient la *recette* qui les fabrique, rejouable à volonté.

## Arborescence

```
Assets/
  Scripts/
    Core/      GameManager (machine à états), GameEvents (bus d'événements statique),
               ResourceManager (or, vies), SaveSystem (JSON dans PlayerPrefs), ObjectPool
    Grid/      GridManager (cellules constructibles / chemin), LevelData, PathFollower
    Towers/    TowerData (SO, niveaux), Tower, TowerTargeting, Projectile*, TowerPlacer,
               TowerManager, RangeIndicator
    Enemies/   EnemyData (SO), Enemy (vie, résistances, vol, statuts), EnemyManager,
               HealthBar (world-space)
    Waves/     WaveData (SO), WaveManager (spawn progressif, boss tous les N)
    Camera/    CameraController (drag, pinch, bornes, secousse)
    UI/        HUD, TowerBuildMenu, TowerUpgradePanel, PauseMenu, EndScreen, DamagePopup
    VFX/       VFXLibrary (SO), VFXManager (pool de particules), AuraRing
    Editor/    ProjectBootstrap (génération), AndroidBuildSettings, PlaceholderFactory
  Prefabs/     produits par le bootstrap ; Towers/, Enemies/, Projectiles/, VFX/, UI/
  Data/        ScriptableObjects : Towers/, Enemies/, Waves/, Levels/
  Materials/   matériaux URP/Lit stylisés (palette cartoon PBR)
  Art/         Placeholders/ (meshes primitives) · Imported/ (vos packs, non versionnés)
  Settings/URP URP asset mobile + Renderer + Volume profile (bloom, color grading)
  Scenes/      Main.unity (généré)
```

## Systèmes principaux et leurs responsabilités

| Système | Rôle | Parle à |
|---|---|---|
| `GameManager` | États `Menu → Building → WaveRunning → Paused → Victory/Defeat`, vitesse de jeu | tous, via `GameEvents` |
| `GameEvents` | Événements statiques (`OnGoldChanged`, `OnLivesChanged`, `OnWaveStarted`, `OnEnemyKilled`, `OnTowerPlaced`…) | découple UI et gameplay |
| `ResourceManager` | Or et vies du joueur, règles de gain/dépense | `GameEvents` |
| `GridManager` | Grille de cellules (constructible / chemin / bloquée), conversion monde ↔ cellule, waypoints du chemin | `LevelData`, `TowerPlacer` |
| `TowerPlacer` | Entrée tactile : tap sur cellule libre → menu de construction → instancie la tour ; tap sur tour → panneau d'upgrade/vente | `GridManager`, `TowerManager`, UI |
| `TowerManager` | Registre des tours, catalogue `TowerData[]`, achat / upgrade / vente | `ResourceManager` |
| `Tower` | Portée, cadence, ciblage (`TowerTargeting` : premier / plus proche / plus faible / volant) ; délègue le tir à un `IAttackBehaviour` (direct, zone, ralentissement, sniper) | `Projectile`, `VFXManager` |
| `Enemy` | Suit le chemin (`PathFollower`), vie, armure, résistances par type de dégâts, vol (ignore les tours sol-seulement), statuts (ralenti), récompense | `WaveManager`, `ResourceManager` |
| `WaveManager` | Lit `LevelData.waves`, spawne les groupes avec intervalles, vague boss tous les N, bonus de vague, mise à l'échelle progressive | `EnemyManager`, `GameManager` |
| `EnemyManager` | Registre des ennemis vivants, requêtes de ciblage (`GetInRange`) sans `FindObjectsOfType` | `Tower` |
| `CameraController` | Un doigt : déplacement · deux doigts : zoom · bornes du niveau · secousse à l'impact | — |
| `VFXManager` | Pools de particules (tir, impact, mort, construction, upgrade), `Play(kind, pos)` | tous |
| `SaveSystem` | Profil joueur : niveau max atteint, étoiles, réglages (JSON, PlayerPrefs) | `GameManager` |
| UI | Canvas Screen-Space, safe area, boutons ≥ 44 pt, panneaux animés | `GameEvents` |

## Progression et modes

- **`LevelCatalog`** (`Resources/`) : liste ordonnée des niveaux + améliorations permanentes. Le menu
  (`UI/Menu/MainMenu`) génère une carte par entrée, miniature dessinée depuis `LevelData`.
- **`LevelSession`** : niveau, difficulté et mode choisis, transmis à la scène de jeu via PlayerPrefs.
- **`SaveSystem`** : un résultat par clé `levelId[@difficulté][#a]` (étoiles, meilleure vague), gemmes,
  rangs d'améliorations. `MetaProgression` en déduit les multiplicateurs que le gameplay lit.
- **Sans fin** : `WaveManager` génère des `WaveData` à la volée (budget de menace, boss périodique,
  RNG déterministe par niveau/difficulté pour que les records soient comparables).
- **Assaut** (`Assault/`) : `AssaultController` (mana, chrono, déploiement) et `AIDefender`
  (construit sur la case qui couvre le plus de chemin, pondère par le prix, monte en anti-aérien si
  le joueur envoie des volants). Réutilise intégralement grille, tours, ennemis, économie : en Assaut
  `ResourceManager` est le trésor de l'IA et les vies sont celles du bastion à faire tomber.

## Types de dégâts et résistances

`DamageType { Physical, Fire, Frost, Magic }`. Chaque `EnemyData` porte un multiplicateur par
type (1 = normal, 0.5 = résistant, 1.5 = vulnérable). Une tour porte un type. Cela suffit à
exprimer « résistant à un type » sans tableau de règles séparé.

## Tours livrées (placeholders inspirés des visuels Piloto)

| Tour | Comportement | Type | Style placeholder |
|---|---|---|---|
| Arbalète | dégâts directs, cadence élevée | Physique | bois/ocre, arbalètes en croix |
| Canon | zone d'effet, lent, sol uniquement | Physique | pierre grise, canon noir, crânes |
| Cristal de givre | ralentissement + dégâts légers, touche le vol | Givre | blanc/bleu givré, cristal |
| Fournaise | zone brûlante, dégâts sur la durée | Feu | rouge sombre, anneau de lave |
| Tour arcane (sniper) | portée énorme, cadence lente, dégâts massifs | Magie | violet/or, cristal flottant |

Chaque tour a 3 niveaux (`TowerLevel[]`) : stats + variante visuelle (le bootstrap construit un
placeholder qui grandit et gagne des détails, comme les progressions des images de référence).

## Ennemis livrés

| Ennemi | Profil |
|---|---|
| Gobelin | rapide, fragile |
| Ogre | lent, très résistant, armure physique |
| Chauve-souris | volant : ignore le Canon et la Fournaise |
| Golem de givre | résistant au givre, vulnérable au feu |
| Boss : Seigneur de guerre | vague boss, très lent, énorme vie, aura visuelle |

## Boucle de jeu

1. `Building` : le joueur pose des tours, un bouton « Lancer la vague » (ou décompte auto).
2. `WaveRunning` : spawn → ennemis suivent le chemin → tours tirent → or gagné à la mort.
3. Fin de vague : bonus d'or, retour à `Building`. Dernière vague terminée → `Victory`.
4. Vies à 0 → `Defeat`. Pause à tout moment (timeScale 0, UI seule).

## Performance mobile (choix faits dans le bootstrap)

- URP asset : ombres 1 cascade 30 m, HDR on (bloom), MSAA 2x, render scale 1, SRP Batcher,
  Dynamic Batching off (SRP Batcher suffit), GPU instancing sur tous les matériaux.
- Post-process : Bloom léger, Color Adjustments (contraste/saturation), Vignette discrète.
  Pas de SSAO, pas de Depth of Field.
- Un seul directional light avec ombres, éclairage ambiant en dégradé ciel/sol, pas de lumières
  ponctuelles temps réel dans les tours (émission + bloom fait le travail).
- Pools d'objets pour projectiles, ennemis, VFX, popups. Aucun `Instantiate` pendant une vague.
- Ciblage par registre (`EnemyManager`) et distance au carré, jamais `Physics.OverlapSphere`
  par frame.
- Cible 60 fps sur milieu de gamme ; `Application.targetFrameRate = 60`.
