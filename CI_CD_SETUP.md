# Construire Bastion sans installer Unity

Deux workflows GitHub Actions font tout le travail dans le nuage. Vous n'avez
besoin ni d'Unity, ni d'Android Studio, ni d'un ordinateur puissant.

| Workflow | Produit | Déclenché par |
|---|---|---|
| `build-apk.yml` | un **APK** à installer sur le téléphone | tout push sur `main` |
| `build-webgl.yml` | un **build navigateur**, déployé sur Vercel | tout push sur `main` |

Chaque workflow commence par régénérer le projet (`ProjectBootstrap.GenerateAll()`,
c'est-à-dire l'équivalent en ligne de commande du menu *Bastion → 1. Générer*),
puis construit. Le dépôt contient la recette, pas les fichiers Unity fragiles :
c'est ce qui rend la construction dans le nuage possible.

---

## Étape 1 — Déclarer les identifiants Unity

Deux secrets, aucun fichier, aucune installation.

**Settings → Secrets and variables → Actions → New repository secret.**

| Secret | Contenu |
|---|---|
| `UNITY_EMAIL` | l'adresse du compte Unity |
| `UNITY_PASSWORD` | le mot de passe de ce compte |

GameCI demande alors un siège Personal directement au service de licence
d'Unity. **Ne déclarez pas `UNITY_LICENSE`** : dès que ce secret existe sans
`UNITY_SERIAL`, GameCI bascule en activation par fichier et réclame un `.ulf`
qu'un compte gratuit ne peut plus obtenir.

C'est là qu'aboutit l'ancienne procédure `.alf` vers `.ulf`, qui ne fonctionne
plus : **Unity a supprimé l'activation hors ligne des licences Personal.** La
page de dépôt ne propose plus que les sièges Enterprise et Industry, ou un
numéro de série Plus/Pro.

Trois conditions sur le compte, sans quoi l'activation échoue en tête de
construction :

- **Pas de double authentification.** Une activation sans interface ne peut
  répondre ni à un code, ni à une validation d'appareil.
- **Un vrai compte Unity ID avec mot de passe.** Une connexion via Google,
  Facebook ou Apple n'en a pas.
- **Un compte dédié à l'intégration continue de préférence.** Les exécutions
  changent d'adresse IP à chaque fois, et Unity peut envoyer une demande de
  confirmation de nouvel appareil.

Un siège Personal reste retenu pendant toute la construction et le compte en
compte très peu. GameCI le rend via un piège de sortie, y compris quand la
construction échoue. Les deux workflows partagent pour cette raison un même
groupe de concurrence : ils se suivent au lieu de se disputer le siège.

---

## Étape 2 — Secrets Vercel, facultatifs

Uniquement pour publier le WebGL en ligne automatiquement :

| Secret | Où le trouver |
|---|---|
| `VERCEL_TOKEN` | https://vercel.com/account/tokens |
| `VERCEL_ORG_ID` | Vercel → Settings du compte ou de l'équipe → *ID* |
| `VERCEL_PROJECT_ID` | Vercel → le projet → Settings → General → *Project ID* |

Sans eux le workflow WebGL fonctionne quand même : il s'arrête juste avant le
déploiement et le build reste téléchargeable depuis l'onglet Actions.

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
- **L'action doit être en `v6`.** Les versions `v4` et `v5` refusent de démarrer
  sans `UNITY_LICENSE` ou `UNITY_SERIAL`, sur le message « Missing Unity License
  File and no Serial was found ». Elles bloquent sur leur propre validation,
  avant même de lancer Unity, alors que l'activation par identifiants existe.
  Seule `v6` laisse passer.
- **La voie `.alf` vers `.ulf` est morte.** L'action
  `game-ci/unity-request-activation-file` a été retirée, et Unity a de toute
  façon supprimé l'activation hors ligne des licences Personal. L'activation se
  fait désormais par identifiants, décrite à l'étape 1.
- **Un push n'est pas une livraison.** Le workflow peut être rouge pendant que
  tout le reste est vert. Regardez l'onglet Actions avant d'annoncer une version.
- **En cas d'échec, l'artefact `journal-unity-*` contient le log Unity complet** :
  c'est la seule trace exploitable, les messages de l'action elle-même sont
  rarement suffisants.
- **Ne collez jamais un secret dans un fichier du dépôt.** Les workflows ne les
  lisent que via `${{ secrets.* }}`, qui n'apparaît pas dans les journaux.
