using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SubwayDash.Managers
{
    /// CheatManager - fast debugging/testing at runtime.
    /// Attach to empty GO (e.g. CheatManager) in scene. Assign 4 Buttons in Inspector or call methods via Button OnClick.
    /// Works always at runtime (no #if UNITY_EDITOR guard).
    public class CheatManager : MonoBehaviour
    {
        [Header("Cheat Amounts - Public")]
        [Tooltip("Coins added per AddCoin() click")]
        public int coinAddAmount = 100;
        [Tooltip("Score added per AddHighScore() click")]
        public int highScoreAddAmount = 5000;

        [Header("Current Values - Public (readout)")]
        [Tooltip("Live total coins (PlayerPrefs GoldCoins)")]
        public int currentCoins;
        [Tooltip("Live high score (PlayerPrefs HighScore)")]
        public int currentHighScore;
        [Tooltip("Runtime score (GameManager.score)")]
        public long currentRunScore;

        [Header("Buttons - Public (optional auto-wire)")]
        public Button addCoinButton;
        public Button deleteCoinsButton;
        public Button addHighScoreButton;
        public Button deleteHighScoreButton;

        [Header("Debug UI - Optional")]
        public TMP_Text debugText;

        private const string CoinsKey = "GoldCoins";
        private const string HighScoreKey = "HighScore";

        private void Awake()
        {
            RefreshReadout();
        }

        private void Start()
        {
            // Auto-wire buttons if assigned (so 1-click setup)
            if (addCoinButton != null) { addCoinButton.onClick.RemoveListener(AddCoin); addCoinButton.onClick.AddListener(AddCoin); }
            if (deleteCoinsButton != null) { deleteCoinsButton.onClick.RemoveListener(DeleteCoins); deleteCoinsButton.onClick.AddListener(DeleteCoins); }
            if (addHighScoreButton != null) { addHighScoreButton.onClick.RemoveListener(AddHighScore); addHighScoreButton.onClick.AddListener(AddHighScore); }
            if (deleteHighScoreButton != null) { deleteHighScoreButton.onClick.RemoveListener(DeleteHighScore); deleteHighScoreButton.onClick.AddListener(DeleteHighScore); }
            RefreshReadout();
            Debug.Log("[CheatManager] Ready - 4 buttons wired. Amounts: coin+" + coinAddAmount + " score+" + highScoreAddAmount, this);
        }

        private void Update()
        {
            // Optional live readout every half sec
            // RefreshReadout();
        }

        // ========== 4 PUBLIC BUTTON METHODS (assign to Button OnClick) ==========

        /// Add Coin - public button method
        public void AddCoin()
        {
            int before = PlayerPrefs.GetInt(CoinsKey, 0);
            int after = before + Mathf.Max(1, coinAddAmount);
            PlayerPrefs.SetInt(CoinsKey, after);
            PlayerPrefs.Save();

            // Sync CollectablesManager if present
            var cm = FindObjectOfType<Collectables.CollectablesManager>();
            if (cm != null)
            {
                cm.totalCoins = after;
                // keep runCoins as is, but refresh UI via cm.AddCoin(0) trick or direct
                // We call AddCoin(0) path: just update PlayerPrefs and force UI refresh by re-reading
                // Instead directly set and invoke UI update via reflection/refresh
                cm.totalCoins = after;
            }

            RefreshReadout();
            Debug.Log($"[Cheat] AddCoin +{coinAddAmount} : {before} -> {after}", this);
        }

        /// Delete Coins - public button method (resets to 0)
        public void DeleteCoins()
        {
            int before = PlayerPrefs.GetInt(CoinsKey, 0);
            PlayerPrefs.SetInt(CoinsKey, 0);
            PlayerPrefs.DeleteKey(CoinsKey); // full delete as requested (prefab delete)
            PlayerPrefs.SetInt(CoinsKey, 0);
            PlayerPrefs.Save();

            var cm = FindObjectOfType<Collectables.CollectablesManager>();
            if (cm != null)
            {
                cm.totalCoins = 0;
                cm.runCoins = 0;
            }

            // Also reset any UI texts
            RefreshReadout();
            Debug.Log($"[Cheat] DeleteCoins : {before} -> 0 (PlayerPrefs deleted)", this);
        }

        /// Add High Score - public button method
        public void AddHighScore()
        {
            int before = PlayerPrefs.GetInt(HighScoreKey, 0);
            int after = before + Mathf.Max(1, highScoreAddAmount);
            PlayerPrefs.SetInt(HighScoreKey, after);
            PlayerPrefs.Save();

            // Also bump runtime score so HowToPlay condition flips immediately if needed
            var gm = FindObjectOfType<GameManager>();
            if (gm != null) gm.score = System.Math.Max(gm.score, (long)after);

            RefreshReadout();
            Debug.Log($"[Cheat] AddHighScore +{highScoreAddAmount} : {before} -> {after}", this);
        }

        /// Delete High Score - public button method (resets to 0)
        public void DeleteHighScore()
        {
            int before = PlayerPrefs.GetInt(HighScoreKey, 0);
            PlayerPrefs.SetInt(HighScoreKey, 0);
            PlayerPrefs.DeleteKey(HighScoreKey);
            PlayerPrefs.SetInt(HighScoreKey, 0);
            PlayerPrefs.Save();

            var gm = FindObjectOfType<GameManager>();
            if (gm != null) gm.score = 0;

            RefreshReadout();
            Debug.Log($"[Cheat] DeleteHighScore : {before} -> 0 (PlayerPrefs deleted)", this);
        }

        // ========== Helpers ==========

        public void RefreshReadout()
        {
            currentCoins = PlayerPrefs.GetInt(CoinsKey, 0);
            currentHighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            var gm = FindObjectOfType<GameManager>();
            currentRunScore = gm != null ? gm.score : 0;
            if (debugText != null)
                debugText.text = $"Coins:{currentCoins:N0} High:{currentHighScore:N0} Run:{currentRunScore:N0}";
        }

        // Extra: delete both at once (useful for HowToPlay re-test)
        [ContextMenu("Delete Both (Coins+HighScore)")]
        public void DeleteBoth()
        {
            DeleteCoins();
            DeleteHighScore();
        }

        [ContextMenu("Add Both")]
        public void AddBoth()
        {
            AddCoin();
            AddHighScore();
        }

#if UNITY_EDITOR
        [ContextMenu("Refresh Readout")]
        private void EditorRefresh() => RefreshReadout();
#endif
    }
}
