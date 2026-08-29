using TMPro;
using UnityEngine;

namespace SubwayDash.Managers
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("MainMenuCanvas - Public")]
        public GameObject MainMenuCanvas; // rename: MainMenuCanvas
        public TMP_Text highScoreText; // assign high score display
        public TMP_Text coinsAvailableText; // assign total coins display

        private void Start()
        {
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            int coins = PlayerPrefs.GetInt("GoldCoins", 0);
            if (highScoreText != null) highScoreText.text = "HighScore: " + highScore.ToString("N0");
            if (coinsAvailableText != null) coinsAvailableText.text = "Coins: " + coins.ToString("N0");
        }

        // Optional: call to refresh after coin earn
        public void Refresh()
        {
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            int coins = PlayerPrefs.GetInt("GoldCoins", 0);
            if (highScoreText != null) highScoreText.text = "HighScore: " + highScore.ToString("N0");
            if (coinsAvailableText != null) coinsAvailableText.text = "Coins: " + coins.ToString("N0");
        }
    }
}
