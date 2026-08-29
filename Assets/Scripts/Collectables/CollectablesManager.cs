using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SubwayDash.Collectables
{
    public class CollectablesManager : MonoBehaviour
    {
        [Header("Coin Prefab - Public")]
        public GameObject goldCoinPrefab;

        [Header("Lanes - Public")]
        public Transform laneLeft;
        public Transform laneCenter;
        public Transform laneRight;

        [Header("Tweaking - Public")]
        public int minCoinsPerLine = 3;
        public int maxCoinsPerLine = 7;
        public float coinSpacing = 1.6f;
        public float spawnChance = 0.7f; // random chance per segment
        public int maxCoinsSpawn = 25; // max coins to spawn total pooled
        [Range(0f,1f)] public float comboLeftRight = 0.2f;
        [Range(0f,1f)] public float comboCenter = 0.3f;
        // combos: 0=Center,1=Left,2=Right,3=Left+Right,4=Center+Left,5=Center+Right,6=All3

        [Header("UI - Public")]
        public TMP_Text coinsText; // assign on PlayingGame Canvas

        [Header("Save - Player Prefab Data")]
        public int totalCoins = 0;
        public int runCoins = 0; // coins in current play, starts 0
        private const string SaveKey = "GoldCoins";

        // Pooling - hide and reuse, no destroy
        private readonly Queue<GameObject> coinPool = new Queue<GameObject>();
        private readonly List<GameObject> activeCoins = new List<GameObject>();
        private Transform poolRoot;

        private void Awake()
        {
            totalCoins = PlayerPrefs.GetInt(SaveKey, 0);
            UpdateCoinsUI();
            if (laneLeft == null || laneCenter == null || laneRight == null)
                TryFindLanes();
            PrewarmCoins();
        }

        private void TryFindLanes()
        {
            var lp = GameObject.Find("LanePoints");
            if (lp != null)
            {
                laneLeft = lp.transform.Find("LeftPoint");
                laneCenter = lp.transform.Find("CenterPoint");
                laneRight = lp.transform.Find("RightPoint");
            }
        }

        private void PrewarmCoins()
        {
            if (goldCoinPrefab == null) return;
            poolRoot = transform;
            for (int i = 0; i < maxCoinsSpawn; i++)
            {
                GameObject go = Instantiate(goldCoinPrefab, Vector3.zero, Quaternion.identity, poolRoot);
                go.SetActive(false);
                // Ensure tag
                go.tag = "GoldCoin";
                coinPool.Enqueue(go);
            }
        }

        public void AddCoin(int amount)
        {
            runCoins += amount;
            totalCoins += amount;
            PlayerPrefs.SetInt(SaveKey, totalCoins);
            PlayerPrefs.Save();
            UpdateCoinsUI();
        }

        private void UpdateCoinsUI()
        {
            if (coinsText != null)
                coinsText.text = runCoins.ToString("N0");
        }

        public void ResetRunCoins()
        {
            runCoins = 0;
            UpdateCoinsUI();
            // Hide all active coins to pool for fresh start 0
            for (int i = activeCoins.Count - 1; i >= 0; i--)
                HideCoin(activeCoins[i]);
        }

        // Called by GameManager when new WalkPath spawned
        public void TrySpawnCoinsForSegment(Transform segment)
        {
            if (segment == null) return;
            if (goldCoinPrefab == null) return;
            if (Random.value > spawnChance) return;

            Transform startPivot = FindDeep(segment, "StartPivot");
            Transform endPivot = FindDeep(segment, "EndPivot");
            if (startPivot == null || endPivot == null) return;

            int coinCount = Random.Range(minCoinsPerLine, maxCoinsPerLine + 1);
            int combo = Random.Range(0, 7);
            List<int> lanes = GetLanesFromCombo(combo);

            float startZ = startPivot.position.z + 2f;
            float endZ = endPivot.position.z - 1f;
            float centerY = 0.9f; // floating Y above road

            for (int i = 0; i < coinCount; i++)
            {
                float z = startZ + i * coinSpacing;
                if (z > endZ) break;
                foreach (int lane in lanes)
                {
                    float x = GetLaneX(lane);
                    Vector3 pos = new Vector3(x, centerY, z);
                    SpawnCoin(pos);
                }
            }
        }

        private List<int> GetLanesFromCombo(int combo)
        {
            // Ensure at least one lane free if obstacle later, but for coins allow combos
            switch (combo)
            {
                case 0: return new List<int>{1}; // Center
                case 1: return new List<int>{0}; // Left
                case 2: return new List<int>{2}; // Right
                case 3: return new List<int>{0,2}; // Left+Right
                case 4: return new List<int>{0,1}; // Left+Center
                case 5: return new List<int>{1,2}; // Center+Right
                case 6: return new List<int>{0,1,2}; // All3
                default: return new List<int>{1};
            }
        }

        private float GetLaneX(int lane)
        {
            if (lane == 0 && laneLeft != null) return laneLeft.position.x;
            if (lane == 1 && laneCenter != null) return laneCenter.position.x;
            if (lane == 2 && laneRight != null) return laneRight.position.x;
            if (lane == 0) return -2f;
            if (lane == 2) return 2f;
            return 0f;
        }

        private void SpawnCoin(Vector3 pos)
        {
            // Find segment at pos to parent - coins must move alongside WalkPath same speed
            Transform segmentAtPos = FindSegmentAtZ(pos.z);

            GameObject go = null;
            if (coinPool.Count > 0) go = coinPool.Dequeue();
            else if (activeCoins.Count > 0)
            {
                go = activeCoins[0];
                activeCoins.RemoveAt(0);
                go.SetActive(false);
            }
            else
            {
                if (goldCoinPrefab == null) return;
                go = Instantiate(goldCoinPrefab, pos, Quaternion.identity, poolRoot);
            }
            // Parent to segment so it moves alongside WalkPath - same speed, no extra Move needed
            if (segmentAtPos != null)
            {
                go.transform.SetParent(segmentAtPos, true);
            }
            else
            {
                go.transform.SetParent(poolRoot, true);
            }
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            go.tag = "GoldCoin";
            // Ensure coins don't collide each other - trigger already, also ignore coin-coin collision
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var gc = go.GetComponent<GoldCoin>();
            if (gc != null) gc.ResetCoin(pos);
            else go.SetActive(true);
            if (!activeCoins.Contains(go)) activeCoins.Add(go);
        }

        private Transform FindSegmentAtZ(float z)
        {
            // Find active WalkPath segment whose bounds contain z
            var gm = FindObjectOfType<Managers.GameManager>();
            if (gm == null) return null;
            // Use reflection to get activeSegments if needed - fallback: find nearest segment by Z
            // Simple: find closest active WalkPath by distance
            GameObject best = null;
            float bestDist = float.MaxValue;
            foreach (var go in activeCoins) { } // dummy to avoid unused
            // Search in scene for WalkPath segments under spawnRoot
            var root = GameObject.Find("WalkPath");
            if (root == null) return null;
            foreach (Transform child in root.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                float cz = child.position.z;
                float dist = Mathf.Abs(cz - z);
                if (dist < bestDist && dist < 15f)
                {
                    bestDist = dist;
                    best = child.gameObject;
                }
            }
            return best != null ? best.transform : null;
        }

        public void HideCoin(GameObject go)
        {
            if (go == null) return;
            go.transform.SetParent(poolRoot, true);
            go.SetActive(false);
            if (!coinPool.Contains(go))
                coinPool.Enqueue(go);
            activeCoins.Remove(go);
        }

        // Dynamic pulling - coins always towards player, reuse to nearest WalkPath needing coins
        private float nextDynamicCheck = 0f;
        private void Update()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            var gm = FindObjectOfType<Managers.GameManager>();
            float despawn = gm != null ? gm.despawnBehindPlayer : 25f;
            float hideZ = player.transform.position.z - despawn;
            for (int i = activeCoins.Count - 1; i >= 0; i--)
            {
                var c = activeCoins[i];
                if (c == null) { activeCoins.RemoveAt(i); continue; }
                if (c.transform.position.z < hideZ)
                {
                    HideCoin(c);
                }
                if (c != null && c.transform.parent != null && !c.transform.parent.gameObject.activeSelf)
                {
                    HideCoin(c);
                }
            }
            // Dynamic pull: every 0.3s move pooled coins to nearest WalkPath ahead that needs coins
            if (Time.time > nextDynamicCheck && coinPool.Count > 0)
            {
                nextDynamicCheck = Time.time + 0.3f;
                TryDynamicPullToNearestWalkPath(player.transform);
            }
        }

        private void TryDynamicPullToNearestWalkPath(Transform player)
        {
            if (player == null) return;
            var root = GameObject.Find("WalkPath");
            if (root == null) return;
            // Find nearest segment ahead of player without coins, within 8-35m
            Transform best = null;
            float bestDist = float.MaxValue;
            foreach (Transform child in root.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                float cz = child.position.z;
                float ahead = cz - player.position.z;
                if (ahead < 5f || ahead > 35f) continue; // nearest WalkPath needed
                if (HasCoins(child)) continue; // already has coins
                float dist = Mathf.Abs(ahead);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = child;
                }
            }
            if (best != null && coinPool.Count > 0)
            {
                // Spawn coins on nearest empty segment instead of far end - no shortage
                int coinCount = Random.Range(minCoinsPerLine, maxCoinsPerLine + 1);
                int combo = Random.Range(0, 7);
                var lanes = GetLanesFromCombo(combo);
                Transform startPivot = FindDeep(best, "StartPivot");
                Transform endPivot = FindDeep(best, "EndPivot");
                if (startPivot == null || endPivot == null) return;
                float startZ = startPivot.position.z + 1.5f;
                float endZ = endPivot.position.z - 1f;
                float y = 0.9f;
                for (int i = 0; i < coinCount; i++)
                {
                    float z = startZ + i * coinSpacing;
                    if (z > endZ) break;
                    if (coinPool.Count == 0) break;
                    foreach (int lane in lanes)
                    {
                        if (coinPool.Count == 0) break;
                        float x = GetLaneX(lane);
                        Vector3 pos = new Vector3(x, y, z);
                        SpawnCoinOnSegment(pos, best);
                    }
                }
            }
        }

        private bool HasCoins(Transform segment)
        {
            foreach (Transform child in segment)
            {
                if (child.CompareTag("GoldCoin") && child.gameObject.activeSelf) return true;
            }
            // Also check activeCoins list parent
            foreach (var c in activeCoins)
                if (c != null && c.transform.parent == segment) return true;
            return false;
        }

        private void SpawnCoinOnSegment(Vector3 pos, Transform segment)
        {
            if (coinPool.Count == 0) return;
            GameObject go = coinPool.Dequeue();
            go.transform.SetParent(segment, true);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            go.tag = "GoldCoin";
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            var gc = go.GetComponent<GoldCoin>();
            if (gc != null) gc.ResetCoin(pos);
            else go.SetActive(true);
            if (!activeCoins.Contains(go)) activeCoins.Add(go);
        }

        // Called when WalkPath disabled - hide its child coins too
        public void HideCoinsForSegment(Transform segment)
        {
            if (segment == null) return;
            for (int i = activeCoins.Count - 1; i >= 0; i--)
            {
                var c = activeCoins[i];
                if (c != null && c.transform.parent == segment)
                {
                    HideCoin(c);
                }
            }
        }

        private Transform FindDeep(Transform root, string name)
        {
            foreach (Transform c in root.GetComponentsInChildren<Transform>(true))
                if (c.name == name) return c;
            return null;
        }
    }
}
