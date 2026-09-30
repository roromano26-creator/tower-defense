using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Bastion.EditorTools
{
    /// <summary>
    /// Compte-rendu de génération écrit dans un fichier, à côté du projet.
    ///
    /// GameCI diffuse la sortie d'Unity dans le journal du workflow sans l'écrire nulle
    /// part : une étape qui cherche dans des fichiers *.log ne trouve rien lors d'une
    /// construction réussie. Deux tentatives de diagnostic ont été perdues ainsi. Un
    /// fichier que nous écrivons nous-mêmes est lisible à coup sûr.
    /// </summary>
    public static class Diagnostic
    {
        private static readonly List<string> lignes = new();

        public static void Noter(string ligne)
        {
            lignes.Add(ligne);
            Debug.Log(ligne);
        }

        public static void Ecrire()
        {
            string racine = Path.GetDirectoryName(Application.dataPath);
            File.WriteAllLines(Path.Combine(racine, "diagnostic-generation.txt"), lignes);
        }
    }
}
