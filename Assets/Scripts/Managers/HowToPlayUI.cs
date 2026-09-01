using System.Collections;
using UnityEngine;

namespace SubwayDash.Managers
{
    /// HowToPlay - shows ONLY on Play click per new requirement (not on Start).
    /// Attach to GameManagerUI GO. Assign howToPlayCanvas (must be independent Canvas/not child of MainMenu Canvas).
    /// Fading via CanvasGroup (auto-added). Public maxDisplaySeconds controls how long.
    public class HowToPlayUI : MonoBehaviour
    {
        [Header("How To Play - Public")]
        [Tooltip("Assign Canvas/Panel to show")]
        public GameObject howToPlayCanvas;

        [Tooltip("Max seconds shown on first time")]
        public float maxDisplaySeconds = 4.5f;

        [Tooltip("Fading duration little bit fade in/out")]
        public float fadeDuration = 0.45f;

        [Header("Condition - Public")]
        [Tooltip("If true show when HighScore==0 OR GoldCoins==0 (any one zero). False = need both zero.")]
        public bool showIfAnyZero = true;

        [Tooltip("Force always show on Play click for testing (ignores zero check)")]
        public bool debugForceShow = false;

        [Tooltip("Always show on Play click even if not zero (true = always, false = only if zero condition)")]
        public bool alwaysShowOnPlay = false;

        [Tooltip("Allow tap/click to dismiss early")]
        public bool dismissOnInput = true;

        [Tooltip("Also check runtime score/coins zero")]
        public bool checkRuntimeZero = true;

        [Header("Trigger - Public")]
        [Tooltip("If true, ONLY shows when Play clicked (new requirement). If false, also auto-shows on Start like before.")]
        public bool showOnlyOnPlayButton = true;

        private CanvasGroup canvasGroup;
        private Coroutine fadeRoutine;
        private bool isShowing = false;

        private const string HighScoreKey = "HighScore";
        private const string GoldCoinsKey = "GoldCoins";

        private void Awake()
        {
            if (howToPlayCanvas == null)
            {
                var found = GameObject.Find("HowToPlayCanvas");
                if (found != null) howToPlayCanvas = found;
                else
                {
                    found = GameObject.Find("HowToPlayPanel");
                    if (found != null) howToPlayCanvas = found;
                }
            }
            if (howToPlayCanvas != null)
            {
                canvasGroup = howToPlayCanvas.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = howToPlayCanvas.AddComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            if (howToPlayCanvas == null)
            {
                Debug.LogWarning("[HowToPlayUI] howToPlayCanvas not assigned. Create Canvas/Panel named HowToPlayCanvas and assign it.", this);
                return;
            }
            // New requirement: only on Play click, so Start just hides
            if (showOnlyOnPlayButton)
            {
                howToPlayCanvas.SetActive(false);
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                return;
            }
            if (ShouldShowHowToPlay()) ShowHowToPlay();
            else
            {
                howToPlayCanvas.SetActive(false);
                if (canvasGroup != null) canvasGroup.alpha = 0f;
            }
        }

        private void Update()
        {
            if (!isShowing || !dismissOnInput) return;
            if (Input.GetMouseButtonDown(0)) Dismiss();
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) Dismiss();
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Dismiss();
        }

        /// Called by GameManagerUI.OnPlayClicked() - only entry point when showOnlyOnPlayButton=true
        public bool TryShowOnPlay()
        {
            if (howToPlayCanvas == null) return false;
            // Ensure detached from hidden MainMenu Canvas if needed
            if (howToPlayCanvas.transform.parent != null && !howToPlayCanvas.transform.parent.gameObject.activeInHierarchy)
            {
                // Keep world position but ensure canvas not hidden by parent
                // Note: ideally HowToPlayCanvas should be top-level Canvas, not child of MainMenu Canvas that gets disabled on Play
            }
            if (alwaysShowOnPlay)
            {
                ShowHowToPlay();
                return true;
            }
            if (ShouldShowHowToPlay())
            {
                ShowHowToPlay();
                return true;
            }
            // Not zero and not forced -> do not show, keep hidden
            howToPlayCanvas.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            return false;
        }

        public void ShowHowToPlay()
        {
            if (howToPlayCanvas == null) return;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeSequence());
        }

        public void HideImmediately()
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            if (howToPlayCanvas != null) howToPlayCanvas.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            isShowing = false;
        }

        public void Dismiss()
        {
            if (!isShowing) return;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeOutAndHide());
        }

        private bool ShouldShowHowToPlay()
        {
            if (debugForceShow) return true;
            int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            int totalCoins = PlayerPrefs.GetInt(GoldCoinsKey, 0);
            bool prefsAnyZero = showIfAnyZero ? (highScore == 0 || totalCoins == 0) : (highScore == 0 && totalCoins == 0);
            if (prefsAnyZero) return true;
            if (checkRuntimeZero)
            {
                var gm = FindObjectOfType<GameManager>();
                var cm = FindObjectOfType<Collectables.CollectablesManager>();
                bool runtimeAnyZero = false;
                if (gm != null && gm.score == 0) runtimeAnyZero = true;
                if (cm != null && cm.runCoins == 0 && cm.totalCoins == 0) runtimeAnyZero = true;
                if (showIfAnyZero && runtimeAnyZero) return true;
                if (!showIfAnyZero && gm != null && cm != null && gm.score == 0 && cm.runCoins == 0) return true;
            }
            return false;
        }

        private System.Collections.IEnumerator FadeSequence()
        {
            isShowing = true;
            // Ensure canvas on top - if parent hidden, force active
            howToPlayCanvas.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            yield return FadeTo(0f, 1f, fadeDuration);
            float elapsed = 0f;
            while (elapsed < maxDisplaySeconds)
            {
                if (!isShowing) yield break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return FadeTo(1f, 0f, fadeDuration);
            howToPlayCanvas.SetActive(false);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            isShowing = false;
        }

        private System.Collections.IEnumerator FadeOutAndHide()
        {
            yield return FadeTo(canvasGroup.alpha, 0f, fadeDuration * 0.7f);
            howToPlayCanvas.SetActive(false);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            isShowing = false;
        }

        private System.Collections.IEnumerator FadeTo(float from, float to, float duration)
        {
            if (duration <= 0f) { canvasGroup.alpha = to; yield break; }
            float t = 0f;
            canvasGroup.alpha = from;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                k = k * k * (3f - 2f * k);
                canvasGroup.alpha = Mathf.Lerp(from, to, k);
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }
}
