# CLAUDE.md

Mémoire du dépôt **Bastion** (tower defense Unity 6 / URP, Android + WebGL).
Tout le texte visible par le joueur, les commentaires et les messages de commit
sont en **français** ; gardez-le ainsi.

`README.md` explique comment construire, `ARCHITECTURE.md` décrit les systèmes.
Ce fichier-ci ne répète ni l'un ni l'autre : il consigne ce qui a déjà coûté
une erreur, et les règles qui n'ont pas de place ailleurs.

## Le projet est une recette, pas un dépôt d'assets

Rien de ce que l'éditeur Unity sérialise n'est versionné : ni `.meta`, ni
`Library/`, ni `Assets/Materials`, ni `Assets/Prefabs`, ni `ProjectSettings`.
`Assets/Scripts/Editor/` **reconstruit tout** à chaque exécution —
`ProjectBootstrap.GenerateAll()` fabrique l'URP, les matériaux, les meshes, les
prefabs, les données, les scènes et les réglages Android. La construction CI
appelle cette génération avant de compiler.

Conséquences :

- **Un changement visuel se fait dans la fabrique, jamais dans un asset.** Un
  matériau modifié « à la main » serait écrasé à la génération suivante.
- **`XVisual()` sort tôt si le prefab existe déjà.** En local, régénérez via
  `Bastion/Régénérer les placeholders` ; en CI le dossier est toujours vierge.
- **Pas d'Unity disponible dans un conteneur d'agent.** Aucune vérification de
  types, aucun test ne peut tourner localement : la compilation C# n'existe
  qu'en CI. Relisez votre diff en conséquence, et n'utilisez que des API dont
  un appel identique figure déjà dans le fichier. Les treize premières erreurs
  de compilation du projet sont toutes parties de ce point aveugle.

## Deux pipelines, et une relance qui se trompe de bouton

Un push sur `main` déclenche deux constructions indépendantes
(`.github/workflows/build-apk.yml` et `build-webgl.yml`), l'une pouvant passer
au vert pendant que l'autre échoue. **Un push n'est pas une livraison :**
vérifiez le run Actions, pas seulement le site. Trois pushes consécutifs ont
déjà cassé la construction Android pendant que Vercel restait vert, et le
travail a été annoncé livré alors qu'il était absent du téléphone.

- **Ne relancez jamais « Re-run failed jobs » sur une exécution WebGL.** Le job
  rejoué dépose un **second** artefact `github-pages`, et `deploy-pages` refuse
  alors de choisir : « Multiple artifacts named "github-pages" … Artifact count
  is 2. » L'échec est instantané, donc facile à confondre avec celui qu'on
  voulait corriger. Passez par **Actions → Run workflow**, qui part d'une
  exécution vierge.
- Un push ne contenant que des `.md` ne déclenche rien (`paths-ignore`) : il
  n'annulera pas une exécution en cours.
- Le cache `Library` est évincé après sept jours sans usage. Après une pause,
  comptez 15 à 20 minutes au lieu de 5.
- Les autres pièges de la chaîne (licence Unity, `cliVersion`, « Build
  succeeded! », `androidTargetSdkVersion`, dossiers du client Vercel) sont dans
  `CI_CD_SETUP.md`, section « Pièges déjà rencontrés ».

## Lire les journaux d'une construction

GameCI **diffuse** la sortie d'Unity dans le journal du workflow et ne l'écrit
nulle part : une étape qui cherche dans des `*.log` ne trouve rien lors d'une
construction réussie. Deux diagnostics ont été perdus ainsi.

La génération écrit donc elle-même son compte-rendu (`Diagnostic.cs` →
`diagnostic-generation.txt`), que les deux workflows affichent **en toute
dernière étape** : l'API ne rend que la fin du journal, et une étape placée au
milieu reste hors de portée. Un troisième diagnostic a été perdu comme ça.

Pour le lire depuis un agent : `get_job_logs` avec `return_content: true` ; le
résultat est sauvé dans un fichier local qu'on filtre ensuite. L'URL signée
qu'il propose n'est pas joignable (politique réseau), et `github.io` non plus —
**on ne peut pas charger la page déployée depuis le conteneur.** La preuve
s'arrête à l'étape de déploiement réussie ; le reste se vérifie à l'œil.

## Direction artistique : ce qui est acquis

Trois cycles ont été dépensés sur un liseré invisible avant de mesurer plutôt
que de supposer. Ce que la mesure a établi :

- **`Bastion/Contour`** est un contour par coque inversée, posé en **second
  matériau** de chaque renderer. L'épaisseur est en espace **monde**, jamais
  objet : les pièces sont des primitives aux échelles très inégales, et un
  décalage en espace objet donnerait un contour énorme sur un axe et nul sur
  l'autre. La passe porte `Tags { "LightMode" = "SRPDefaultUnlit" }`.
- **Une épaisseur invisible ne prouve rien.** 2,5 puis 4,5 cm sur une case de
  2 m font moins d'un pixel à la distance de jeu — indistinguable d'une passe
  jamais dessinée. Valeur retenue : **6 cm**. Avant de soupçonner le rendu,
  demandez `ShaderUtil.ShaderHasError` : un shader qui ne compile pas reste
  trouvable par `Shader.Find`, et son magenta ne se voit pas sur un liseré.
- **Une pièce transparente ne reçoit pas de liseré** (halo, flaque d'ombre) :
  l'anneau noir obtenu serait le contraire de l'effet voulu.
- **La distance d'ombres se mesure depuis la caméra, pas depuis le plateau.**
  30 m paraissaient larges pour un plateau de 28 × 20 ; la caméra se tient à
  23,8 m de son centre, donc le bord éloigné était à 38 m, sans aucune ombre.
  Réglée à 70 m sur deux cascades.
- **Les tours portent une flaque d'ombre de contact**, mesurée sur les seules
  pièces qui touchent le sol : la silhouette entière donnait 2,06 m sous
  l'arbalète de niveau 3 et débordait sur les cases voisines. Plafond à 1,9 m
  pour une case de 2. Réservée aux tours — un ennemi volant porte sa hauteur
  sur son propre transform, une flaque enfant le suivrait en l'air.
- **Un socle par type de tour** (or, rouille, bleu profond, lave, violet) : vu
  de haut, le socle est la pièce la plus large et la seule que la tourelle ne
  masque jamais. Le canon et la tour arcanique partageaient le violet.
- **Le brouillard suit le recul de la caméra** (`LevelEnvironment.LateUpdate`).
  Figé à 48 m, il commençait avant le centre du plateau dès que le portrait
  faisait reculer la caméra à 52 m : toute la partie en pénombre.
- **Le cadrage est réglé et validé.** `CameraController.Recadrer` prend le
  cadrage paysage d'origine comme **plancher** et ne fait que reculer quand le
  format l'exige. Une tentative de « l'améliorer » l'a déjà cassé une fois.

## Ergonomie tactile : la sonde Playwright ne s'applique pas ici

La règle des 44 px tient, mais **l'interface est un canvas Unity : il n'y a
aucun élément DOM à sonder.** La technique `elementFromPoint` utilisée sur
Cuistop ne rend rien sur Bastion. L'équivalent se calcule depuis le
`CanvasScaler` de `UIFactory` (`ScaleWithScreenSize`, référence 1920 × 1080,
`matchWidthOrHeight = 0.5`) :

    échelle = (L/1920)^0.5 × (H/1080)^0.5

Sur un téléphone de 390 px de large, l'échelle vaut **≈ 0,40** dans les deux
orientations (le rapport d'écran se compense), et le facteur de pixels de
l'appareil s'annule. Donc :

> **Une cible tactile doit mesurer au moins 110 unités d'interface** pour
> atteindre 44 px sur un téléphone.

Audit du 10 novembre 2026 — la hauteur est presque partout le côté fautif :

| Bouton | Taille | Rendu sur 390 px | |
|---|---|---|---|
| `Close` (✕) | 64 × 64 | 25 × 25 | ✗ |
| `Pause` | 96 × 84 | 38 × 33 | ✗ |
| `Speed` | 110 × 84 | 44 × 33 | ✗ |
| `Quit` | 420 × 70 | 167 × 28 | ✗ |
| `Menu` | 420 × 84 | 167 × 33 | ✗ |
| `StartWave`, `Upgrade`, `Sell`, `Priority`, `Resume`, `Restart` | … × 96 | … × 38 | ✗ |
| `TowerButton` | 150 × 130 | 60 × 52 | ✓ |

Corriger une hauteur est sûr ; corriger une largeur ne l'est pas, plusieurs
boutons se partageant un panneau à la largeur comptée (`Upgrade` 300 +
`Sell` 190 + `Priority` 200 dans un panneau de 760). Toute reprise passe
panneau par panneau.

## Règles de travail

- **Développer sur une branche, montrer un aperçu, ne fusionner sur `main` que
  sur un « fusionne sur main » explicite.** Les commits `112c357`, `6e9e98f` et
  `175a1d1` sont partis directement sur `main` : c'était une erreur.
- **Aucun identifiant de modèle** dans un commit, une PR ou le code. Les trois
  commits ci-dessus en portent un dans leur `Co-Authored-By` ; l'historique de
  `main` ne se réécrit pas, mais cela ne doit plus se reproduire.
- Chaque changement **visible** fusionné sur `main` → une entrée dans
  `QUOI_DE_NEUF.md`, écrite pour un ami non technique.
- Aucune clé secrète dans le chat ni dans le dépôt ; elles vivent dans les
  secrets du dépôt GitHub (`UNITY_EMAIL`, `UNITY_PASSWORD`, `VERCEL_TOKEN`).
- **Mesurer avant de supposer.** Le compte-rendu de génération, l'arithmétique
  du `CanvasScaler` et le journal de construction ont chacun renversé une
  hypothèse confiante. Reproduisez avant de diagnostiquer.
