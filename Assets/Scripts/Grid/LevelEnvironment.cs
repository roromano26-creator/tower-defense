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

        private bool pilote;

        private void Awake()
        {
            var level = GameManager.Instance != null ? GameManager.Instance.Level : null;
            if (level == null) return;
            pilote = true;
            if (sun != null) sun.color = level.sunColor;
            RenderSettings.ambientSkyColor = level.ambientSky;
            RenderSettings.ambientEquatorColor = Color.Lerp(level.ambientSky, level.ambientGround, 0.5f);
            RenderSettings.ambientGroundColor = level.ambientGround;
            RenderSettings.fogColor = level.fogColor;
            if (cam != null) cam.backgroundColor = level.fogColor;
        }

        /// <summary>
        /// Le brouillard suit la caméra au lieu de supposer sa distance.
        ///
        /// Les distances étaient fixes, 48 m à 100 m, calibrées sur le cadrage paysage où
        /// la caméra se tient à 23,8 m : le brouillard commençait donc juste après le
        /// plateau et n'assombrissait que les bords de la dalle de fond, ce qui était
        /// l'intention. Mais en portrait le cadrage doit reculer jusqu'à 52 m pour
        /// embrasser le plateau, et ces mêmes 48 m tombaient alors AVANT son centre :
        /// tout le damier passait dans le brouillard et la partie se jouait dans la
        /// pénombre. Le décalage reproduit les deux valeurs d'origine à la distance
        /// paysage — 23,8 + 24 ≈ 48 et 23,8 + 76 ≈ 100 — donc le cas qui fonctionnait
        /// ne change pas d'un pixel.
        ///
        /// LateUpdate, pour lire la distance après que la caméra l'a appliquée dans son
        /// propre Update ; la caméra étant enfant du rig, sa position locale EST la
        /// distance de recul.
        /// </summary>
        private void LateUpdate()
        {
            if (!pilote || cam == null) return;
            float recul = cam.transform.localPosition.magnitude;
            RenderSettings.fogStartDistance = recul + 24f;
            RenderSettings.fogEndDistance = recul + 76f;
        }
    }
}
