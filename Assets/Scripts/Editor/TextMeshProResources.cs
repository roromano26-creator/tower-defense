using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bastion.EditorTools
{
    /// <summary>
    /// Importe les ressources essentielles de TextMeshPro avant toute création de texte.
    ///
    /// Dans l'éditeur, Unity ouvre une fenêtre au premier texte créé et attend un clic.
    /// En ligne de commande personne ne clique : rien n'est importé, aucune police ni
    /// shader n'existe, et tous les textes du jeu s'affichent vides. Le jeu se lance
    /// alors normalement mais sans un seul caractère visible, ce qui est exactement le
    /// symptôme observé sur la première version mise en ligne.
    /// </summary>
    public static class TextMeshProResources
    {
        private const string NomPaquet = "TMP Essential Resources.unitypackage";

        public static void EnsureImported()
        {
            if (Presentes())
            {
                Debug.Log("[TMP] Ressources essentielles déjà présentes.");
                return;
            }

            string paquet = TrouverPaquet();
            if (paquet == null)
            {
                Debug.LogWarning($"[TMP] « {NomPaquet} » introuvable. Les textes seront invisibles.");
                return;
            }

            Debug.Log($"[TMP] Import de {paquet}");
            AssetDatabase.ImportPackage(paquet, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Journalisé dans les deux cas : c'est la seule façon de savoir, depuis le
            // journal d'une construction, si les textes sortiront lisibles ou vides.
            Debug.Log(Presentes()
                ? "[TMP] Ressources essentielles importées, police par défaut disponible."
                : "[TMP] Import effectué mais aucune police par défaut : textes encore invisibles.");
        }

        /// <summary>
        /// L'accès passe par les réglages TextMeshPro, qui n'existent pas tant que les
        /// ressources ne sont pas importées : l'absence peut donc lever autant que
        /// renvoyer nul, et les deux signifient la même chose ici.
        /// </summary>
        private static bool Presentes()
        {
            try { return TMPro.TMP_Settings.defaultFontAsset != null; }
            catch { return false; }
        }

        /// <summary>
        /// Le paquet vit dans le cache de packages, sous un dossier dont le nom porte un
        /// hachage qui change à chaque version : il faut le chercher, pas le coder en dur.
        /// </summary>
        private static string TrouverPaquet()
        {
            foreach (var racine in new[] { "Library/PackageCache", "Packages" })
            {
                if (!Directory.Exists(racine)) continue;
                var trouves = Directory.GetFiles(racine, NomPaquet, SearchOption.AllDirectories);
                if (trouves.Length > 0) return trouves[0];
            }
            return null;
        }
    }
}
