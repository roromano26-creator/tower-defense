using UnityEngine;
using Bastion.Core;

namespace Bastion.Grid
{
    /// <summary>Applique le biome du niveau à la lumière, à l'ambiance et au brouillard : une scène, plusieurs climats.</summary>
    [DefaultExecutionOrder(-170)]
    public class LevelEnvironment : MonoBehaviour
    {
        [SerializeField] private Light sun;
        [SerializeField] private Camera cam;

        private void Awake()
        {
            var level = GameManager.Instance != null ? GameManager.Instance.Level : null;
            if (level == null) return;
            if (sun != null) sun.color = level.sunColor;
            RenderSettings.ambientSkyColor = level.ambientSky;
            RenderSettings.ambientEquatorColor = Color.Lerp(level.ambientSky, level.ambientGround, 0.5f);
            RenderSettings.ambientGroundColor = level.ambientGround;
            RenderSettings.fogColor = level.fogColor;
            if (cam != null) cam.backgroundColor = level.fogColor;
        }
    }
}
