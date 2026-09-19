using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bastion.Core;
using Bastion.Grid;

namespace Bastion.UI.Menu
{
    /// <summary>Carte d'un niveau : miniature, nom, étoiles de la difficulté choisie, record Sans fin, cadenas.</summary>
    public class LevelCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text starsText;
        [SerializeField] private TMP_Text recordText;
        [SerializeField] private RawImage preview;
        [SerializeField] private Button button;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private TMP_Text lockText;

        private LevelData level;
        private Texture2D texture;

        public void Bind(LevelData l, int index, Difficulty d, GameMode mode, LevelCatalog catalog, System.Action<LevelData> onPlay)
        {
            level = l;
            titleText.text = $"{index + 1}. {l.displayName}";
            subtitleText.text = mode == GameMode.Assault ? $"Assaut · {Mathf.RoundToInt(l.assaultDuration / 60f)} min pour percer · {l.description}" : $"{l.waves.Length} vagues · {l.description}";
            if (texture == null) { texture = LevelPreview.Render(l); preview.texture = texture; }
            var r = SaveSystem.GetResult(l.levelId, d, mode);
            int stars = r?.stars ?? 0;
            starsText.text = new string('★', stars) + new string('☆', 3 - stars);
            recordText.text = r != null && r.bestWave > l.waves.Length ? $"Sans fin : {r.bestWave} vagues" : r != null && r.bestWave > 0 ? $"Meilleure vague : {r.bestWave}" : "";
            bool unlocked = catalog.IsUnlocked(index, d, mode);
            lockOverlay.SetActive(!unlocked);
            button.interactable = unlocked;
            if (!unlocked)
                lockText.text = mode == GameMode.Assault && (SaveSystem.GetResult(l.levelId)?.completed ?? false) == false
                    ? "Défendez d'abord ce niveau"
                    : d > Difficulty.Normal
                    ? $"Terminez ce niveau en {DifficultySettings.Label(d - 1)}"
                    : SaveSystem.TotalStars() < l.starsToUnlock ? $"{l.starsToUnlock} ★ requises" : "Terminez le niveau précédent";
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onPlay(level));
        }

        private void OnDestroy() { if (texture != null) Destroy(texture); }
    }
}
