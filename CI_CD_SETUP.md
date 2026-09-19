# Construire Bastion sans installer Unity

Trois workflows GitHub Actions font tout le travail dans le nuage. Vous n'avez
besoin ni d'Unity, ni d'Android Studio, ni d'un ordinateur puissant.

| Workflow | Produit | Déclenché par |
|---|---|---|
| `unity-activation.yml` | le fichier de licence, **une seule fois** | manuellement |
| `build-apk.yml` | un **APK** à installer sur le téléphone | tout push sur `main` |
| `build-webgl.yml` | un **build navigateur**, déployé sur Vercel | tout push sur `main` |

Chaque workflow commence par régénérer le projet (`ProjectBootstrap.GenerateAll()`,
c'est-à-dire l'équivalent en ligne de commande du menu *Bastion → 1. Générer*),
puis construit. Le dépôt contient la recette, pas les fichiers Unity fragiles :
c'est ce qui rend la construction dans le nuage possible.

---

## Étape 1 — Obtenir la licence Unity (une seule fois, ~5 minutes)

Une licence **Personal** suffit, elle est gratuite. Il faut la transformer en
fichier texte que GitHub pourra utiliser.

1. Créez un compte sur https://id.unity.com si vous n'en avez pas.
2. Sur GitHub : onglet **Actions** → workflow **Unity – Demander le fichier
   d'activation** → **Run workflow**.
3. Comptez quelques minutes, le temps de télécharger l'image Unity. Ouvrez
   ensuite l'exécution et téléchargez l'artefact **licence-unity-alf**.
   Décompressez-le : vous obtenez un fichier `.alf`.
4. Allez sur https://license.unity3d.com/manual, connectez-vous, déposez le `.alf`,
   choisissez **Unity Personal** puis *Personal Edition*.
5. Unity vous renvoie un fichier `.ulf`. Ouvrez-le dans un éditeur de texte et
   **copiez tout son contenu** (c'est du XML, gardez les premières et dernières lignes).

---

## Étape 2 — Déclarer les secrets GitHub

**Settings → Secrets and variables → Actions → New repository secret.**

Obligatoires :

| Secret | Contenu |
|---|---|
| `UNITY_LICENSE` | tout le contenu du fichier `.ulf` de l'étape 1 |
| `UNITY_EMAIL` | l'adresse de votre compte Unity |
| `UNITY_PASSWORD` | le mot de passe de ce compte |

Facultatifs, uniquement pour publier le WebGL en ligne automatiquement :

| Secret | Où le trouver |
|---|---|
| `VERCEL_TOKEN` | https://vercel.com/account/tokens |
| `VERCEL_ORG_ID` | Vercel → Settings du compte ou de l'équipe → *ID* |
| `VERCEL_PROJECT_ID` | Vercel → le projet → Settings → General → *Project ID* |

Sans ces trois-là le workflow WebGL fonctionne quand même : il s'arrête juste
avant le déploiement et le build reste téléchargeable depuis l'onglet Actions.

---

## Étape 3 — Créer le projet Vercel (facultatif)

1. https://vercel.com/new → importez ce dépôt.
2. **Framework Preset** : *Other*. Laissez la commande de build et le dossier de
   sortie vides : c'est le workflow qui téléverse les fichiers déjà construits.
3. Déployez une fois à vide, puis récupérez le *Project ID* dans les réglages
   pour le secret de l'étape 2.

Aucune configuration d'en-têtes n'est nécessaire : le build active
`decompressionFallback`, donc le navigateur décompresse lui-même les fichiers
Brotli. C'est un peu de JavaScript en plus, contre la garantie que le jeu
fonctionne sur n'importe quel hébergement statique.

---

## Étape 4 — Lancer une construction

Automatique à chaque push sur `main` :

```bash
git add .
git commit -m "Nouvelle tour"
git push origin main
```

Ou manuellement : **Actions** → choisissez le workflow → **Run workflow**.

**Comptez 60 à 90 minutes la première fois.** L'import des assets et la
compilation IL2CPP sont longs. Les exécutions suivantes réutilisent le cache et
tombent à 15–30 minutes.

---

## Étape 5 — Récupérer le jeu

**L'APK** : onglet Actions → l'exécution terminée → section *Artifacts* →
`bastion-apk`. Décompressez, transférez `Bastion.apk` sur le téléphone et
ouvrez-le (il faudra autoriser l'installation depuis cette source). L'APK est
signé avec la clé de débogage : il s'installe, mais ne se publie pas sur le
Play Store — c'est `Bastion/4. Construire l'AAB` qui sert à ça.

**Le WebGL** : si Vercel est configuré, l'adresse est affichée dans le résumé de
l'exécution. Sinon, artefact `bastion-webgl`, à ouvrir avec n'importe quel
serveur statique local.

---

## Pièges déjà rencontrés

- **Une licence Personal ne vaut que pour un poste à la fois.** Si Unity tourne
  sur votre machine avec le même compte, l'activation peut échouer dans le nuage.
- **La version d'Unity doit exister chez GameCI.** Les workflows lisent
  `ProjectSettings/ProjectVersion.txt`, actuellement `6000.3.0f1`. Les images
  `android`, `webgl` et `base` de cette version ont été vérifiées comme
  publiées. Si vous changez de version d'Unity, vérifiez d'abord que les images
  correspondantes existent, sinon la construction s'arrête sur une image
  introuvable.
- **N'utilisez pas `game-ci/unity-request-activation-file`.** Cette action a été
  retirée et échoue en six secondes sur « This action is no longer supported ».
  Le workflow d'activation appelle directement l'image Unity à la place.
- **Un push n'est pas une livraison.** Le workflow peut être rouge pendant que
  tout le reste est vert. Regardez l'onglet Actions avant d'annoncer une version.
- **En cas d'échec, l'artefact `journal-unity-*` contient le log Unity complet** :
  c'est la seule trace exploitable, les messages de l'action elle-même sont
  rarement suffisants.
- **Ne collez jamais un secret dans un fichier du dépôt.** Les workflows ne les
  lisent que via `${{ secrets.* }}`, qui n'apparaît pas dans les journaux.
