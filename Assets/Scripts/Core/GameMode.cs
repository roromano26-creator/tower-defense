namespace Bastion.Core
{
    /// <summary>Défense : le joueur pose les tours. Assaut : le joueur déploie les monstres, une IA construit les tours.</summary>
    public enum GameMode { Defense = 0, Assault = 1 }

    public static class GameModes
    {
        public static string Label(GameMode m) => m == GameMode.Assault ? "Assaut" : "Défense";
        public static int Count => 2;
    }
}
