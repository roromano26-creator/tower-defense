# Remplacer les placeholders par de vrais assets 3D

Le code ne référence **jamais** un modèle : il instancie un prefab « visuel » lu dans un
ScriptableObject. Remplacer un asset = changer une référence dans l'Inspector. Zéro code.

## Où vont les fichiers

```
Assets/Art/Imported/            ← vos packs (Piloto Studio, Synty, etc.), non versionnés (.gitignore)
Assets/Prefabs/Towers/Visuals/  ← V_<tour>_L<niveau>.prefab (placeholders générés)
Assets/Prefabs/Enemies/Visuals/ ← V_<ennemi>.prefab
Assets/Prefabs/Projectiles/     ← P_<projectile>.prefab
Assets/Prefabs/VFX/             ← VFX_<effet>.prefab
```

Importez le pack dans `Assets/Art/Imported/<NomDuPack>/`. Ne modifiez pas ses fichiers : créez
vos prefabs à côté (Prefab Variants ou nouveaux prefabs) pour pouvoir mettre le pack à jour.

## Tours (3 niveaux chacune)

1. Faites glisser le modèle du pack dans la scène, réglez échelle/orientation (la tour doit
   tenir dans une case de **2 m** et regarder +Z).
2. Ajoutez, si le modèle le permet, deux enfants vides nommés **exactement** :
   - `Turret` : la partie qui pivote vers la cible (tête d'arbalète, fût de canon, cristal) ;
   - `FirePoint` : l'origine des projectiles / du flash de tir (bouche du canon).
   Sans `Turret`, la tour ne pivote pas ; sans `FirePoint`, le tir part du centre.
3. Enregistrez comme prefab dans `Assets/Prefabs/Towers/Visuals/` (ex. `V_cannon_L2_Piloto`).
4. Ouvrez `Assets/Data/Towers/Tower_cannon.asset` → `Levels ▸ Element 1 ▸ Visual Prefab` → glissez.
5. Répétez pour les niveaux 1 et 3. Play : la tour posée utilise le nouveau visuel, l'upgrade
   bascule au suivant, le fantôme de placement le reprend avec le matériau `M_Ghost`.

Les matériaux du pack restent tels quels (URP/Lit). Si le pack est en Built-in, convertissez :
*Window → Rendering → Render Pipeline Converter → Built-in to URP → Material Upgrade*.

## Ennemis

1. Modèle dans la scène, pieds à y = 0, regard vers +Z, échelle ≈ 1 m de haut pour un gobelin.
2. Enregistrez comme prefab dans `Assets/Prefabs/Enemies/Visuals/`.
3. `Assets/Data/Enemies/Enemy_<id>.asset` → `Visual Prefab`. Ajustez `Visual Scale` si besoin.
4. La barre de vie et le `HitPoint` (0,7 m) sont dans le prefab logique `Enemy.prefab`, pas dans
   le visuel : ajustez-les là si vos modèles sont plus grands.

**Animations** : si le modèle a un Animator (Idle/Walk/Die), placez-le sur le visuel. Le code ne
pilote pas encore d'Animator ; branchez `Walk` en boucle par défaut. Un hook simple à ajouter :
dans `Enemy.Update`, `animator.SetFloat("Speed", Data.speed * slowFactor)`.

## Projectiles

`Assets/Data/Towers/Tower_<id>.asset ▸ Projectile Prefab`. Un prefab de projectile porte le script
`Projectile` (`Ballistic` coché pour un boulet en cloche) et un `TrailRenderer` facultatif. Créez un
Prefab Variant de `P_Cannonball.prefab` et remplacez l'enfant `Body` par votre mesh.

## VFX

`VFXManager` (scène) → `Entries` : un `ParticleSystem` par `VFXKind`. Remplacez le prefab d'une
entrée par le vôtre. Contraintes : `Play On Awake` désactivé, `Stop Action = None`, `Loop` seulement
pour `FrostStatus` / `BurnStatus`. La couleur `startColor` est teintée à l'exécution avec l'accent
de la tour/ennemi — laissez du blanc dans vos gradients si vous voulez cette teinte.

## Sol et décor

`GridManager` accepte trois prefabs de dalle (`Buildable / Path / Blocked Tile Prefab`) : glissez
des dalles de votre pack (2 m × 2 m, pivot au centre, dessus à y = 0). Sans prefab, les cubes
matérialisés sont utilisés. Le décor hors grille est libre : posez-le dans `Ground`, marquez-le
*Static* (batching statique + ombres bakées possibles).

## Icône et splash

`Assets/Art/Icon_Temp.png` est généré ; remplacez-le dans Player Settings → Icon (512 × 512,
adaptive icons recommandées) et supprimez le PNG temporaire.

## Ce qui ne bouge pas

- Les stats (`Assets/Data`) ne dépendent pas des visuels.
- `Tower.prefab` et `Enemy.prefab` (logiques) ne contiennent aucun mesh à remplacer.
- Les noms `Turret` / `FirePoint` sont les seuls contrats entre le code et un modèle.
