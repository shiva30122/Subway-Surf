using UnityEngine;

namespace SubwayDash.Collectables
{
    public class GoldCoin : MonoBehaviour
    {
        [Header("Animation")]
        public float rotationSpeed = 90f;
        public float bobSpeed = 2f;
        public float bobHeight = 0.15f;
        private Vector3 startPos;
        [Header("Coin Y - Public offset")]
        [Tooltip("Base Y height, set from CollectablesManager coinYOffset on spawn")]
        public float baseY = 0.9f;
        private bool collected = false;

        private Vector3 originalScale;
        private Quaternion originalRotation;
        private void Awake()
        {
            originalScale = transform.localScale;
            originalRotation = transform.rotation;
            // Keep prefab as is - only ensure trigger if missing, dont add extra collider if already correct
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var rb = GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        }

        private void OnEnable()
        {
            collected = false;
            // Do not overwrite startPos here; ResetCoin will set it correctly after spawn
            // Keep baseY as is until ResetCoin
        }

        private void Update()
        {
            // Live sync Y from manager so inspector coinYOffset change applies instantly (fix 50 not changing)
            var mgr = FindObjectOfType<CollectablesManager>();
            if (mgr != null) baseY = mgr.coinYOffset;
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            Vector3 p = transform.position;
            p.y = baseY + bob;
            transform.position = p;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            // Support both tag and component check (PlayerController) to avoid missed counts
            bool isPlayer = other.CompareTag("Player") || other.GetComponent<SubwayDash.Player.PlayerController>() != null || other.transform.GetComponentInParent<SubwayDash.Player.PlayerController>() != null;
            if (!isPlayer)
            {
                // Fallback: check root Player tag
                if (!other.transform.root.CompareTag("Player")) return;
                isPlayer = true;
            }
            if (isPlayer)
            {
                collected = true;
                var mgr = FindObjectOfType<CollectablesManager>();
                if (mgr != null)
                {
                    mgr.AddCoin(1);
                    mgr.HideCoin(gameObject); // proper pool return, fixes 250 leak + scale
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void ResetCoin(Vector3 pos)
        {
            startPos = pos;
            baseY = pos.y;
            transform.position = pos;
            // Use prefab as is - restore original scale/rotation
            transform.rotation = originalRotation;
            transform.localScale = originalScale;
            collected = false;
            gameObject.SetActive(true);
        }

#if UNITY_EDITOR
        public void PreviewAnimation()
        {
            transform.Rotate(Vector3.up * 45f, Space.World);
            Debug.Log($"[GoldCoin Preview] rotationSpeed={rotationSpeed} bobSpeed={bobSpeed} bobHeight={bobHeight}");
        }

        public void ResetPreview()
        {
            transform.rotation = Quaternion.identity;
            transform.position = startPos;
            Debug.Log("[GoldCoin] Reset transform to startPos");
        }
#endif
    }
}
