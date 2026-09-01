using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SubwayDash.Collectables;

namespace SubwayDash.Managers
{
    public class GameManagerUI : MonoBehaviour
    {
        [Header("UI - Public on GameManagerUI")]
        public GameObject canvas; // MainMenu Canvas
        public Button playButton; // Play button moved from GameManager
        public GameObject playingUIRoot; // PlayingGame Canvas root to enable on start

        [Header("Pause - Public")]
        public Button pauseButton; // on PlayingGame Canvas
        public GameObject pausePanel; // hidden
        public Button resumeButton; // inside pausePanel
        public Button exitButton; // Exit to MainMenu

        [Header("Playing UI - Public")]
        public TMP_Text scoreText; // assign current score
        public TMP_Text coinsText; // assign current coins
        public TMP_Text highScoreText; // assign high score display on GameManagerUI
        public TMP_Text totalCoinsText; // assign total coins available display

        private GameManager gameManager;
        private CollectablesManager collectablesManager;

        private void Awake()
        {
            gameManager = FindObjectOfType<GameManager>();
            collectablesManager = FindObjectOfType<CollectablesManager>();
            // Fallback find if not assigned (moved from GameManager)
            if (canvas == null) canvas = GameObject.Find("Canvas");
            if (playButton == null)
            {
                var playGO = GameObject.Find("Play");
                if (playGO != null) playButton = playGO.GetComponent<Button>();
            }
            if (playingUIRoot == null)
            {
                var pg = GameObject.Find("PlayingUI");
                if (pg != null) playingUIRoot = pg;
                else playingUIRoot = GameObject.Find("GameUI");
            }
            if (canvas != null) canvas.SetActive(true);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (playingUIRoot != null) playingUIRoot.SetActive(false); // not enable until Play pressed
        }

        private void Start()
        {
            if (canvas != null) canvas.SetActive(true);
            if (playingUIRoot != null) playingUIRoot.SetActive(false); // stay hidden until Play
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayClicked);
                playButton.onClick.AddListener(OnPlayClicked);
            }
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(OnPauseClicked);
                pauseButton.onClick.AddListener(OnPauseClicked);
            }
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(OnResumeClicked);
                resumeButton.onClick.AddListener(OnResumeClicked);
            }
            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClicked);
                exitButton.onClick.AddListener(OnExitClicked);
            }
        }

        public void OnPlayClicked()
        {
            if (canvas != null) canvas.SetActive(false);
            if (playingUIRoot != null) playingUIRoot.SetActive(true);
            if (collectablesManager != null) collectablesManager.ResetRunCoins();
            if (gameManager != null) gameManager.OnPlayButtonClicked();
            // HowToPlay only on Play click (per requirement)
            var howToPlay = FindObjectOfType<HowToPlayUI>();
            if (howToPlay != null) howToPlay.TryShowOnPlay();
        }

        private void Update()
        {
            // Sync UI from managers if assigned - coinsText shows per-play runCoins starting 0
            if (scoreText != null && gameManager != null)
                scoreText.text = gameManager.score.ToString("N0");
            if (coinsText != null && collectablesManager != null)
                coinsText.text = collectablesManager.runCoins.ToString("N0");
            if (highScoreText != null)
                highScoreText.text = "HighScore: " + PlayerPrefs.GetInt("HighScore", 0).ToString("N0");
            if (totalCoinsText != null && collectablesManager != null)
                totalCoinsText.text = "Coins: " + collectablesManager.totalCoins.ToString("N0");
        }

        public void OnPauseClicked()
        {
            if (pausePanel != null) pausePanel.SetActive(true);
            Time.timeScale = 0f;
            if (gameManager != null) gameManager.SetMoving(false);
        }

        public void OnResumeClicked()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
            if (gameManager != null) gameManager.SetMoving(true);
        }

        public void OnExitClicked()
        {
            Time.timeScale = 1f;
            // Save high score
            if (gameManager != null)
            {
                int high = PlayerPrefs.GetInt("HighScore", 0);
                if (gameManager.score > high)
                {
                    PlayerPrefs.SetInt("HighScore", (int)gameManager.score);
                    PlayerPrefs.Save();
                }
            }
            // Full reset - hide all instanced WalkPaths/coins, reset player 0,0,0, enable startingModel
            if (gameManager != null)
            {
                gameManager.ClearAll();
                gameManager.startingModel.transform.position = new Vector3(0f, 0f, 0f);
                gameManager.startingModel.SetActive(true);
                gameManager.SetMoving(false);
                gameManager.score = 0;
                gameManager.speedMultiplier = 1f;
                // Reset player to 0,0,0 center
                var player = FindObjectOfType<SubwayDash.Player.PlayerController>();
                if (player != null) player.ResetPlayer();
                else
                {
                    var p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null) p.transform.position = new Vector3(0f, 1f, 0f);
                }
            }
            // Hide all coins via pool (fixes leak)
            if (collectablesManager != null)
            {
                var coins = FindObjectsOfType<Collectables.GoldCoin>();
                foreach (var c in coins) if (c.gameObject.activeSelf) collectablesManager.HideCoin(c.gameObject);
            }
            if (pausePanel != null) pausePanel.SetActive(false);
            if (playingUIRoot != null) playingUIRoot.SetActive(false);
            if (canvas != null) canvas.SetActive(true);
            if (highScoreText != null) highScoreText.text = "HighScore: " + PlayerPrefs.GetInt("HighScore", 0).ToString("N0");
            if (totalCoinsText != null && collectablesManager != null) totalCoinsText.text = "Coins: " + collectablesManager.totalCoins.ToString("N0");
            if (scoreText != null) scoreText.text = "0";
        }
    }
}
