namespace Bastion.Core
{
    /// <summary>Types de dégâts. Chaque tour en porte un, chaque ennemi a un multiplicateur par type.</summary>
    public enum DamageType { Physical, Fire, Frost, Magic }

    /// <summary>États globaux de la partie.</summary>
    public enum GameState { Menu, Building, WaveRunning, Paused, Victory, Defeat }
}
