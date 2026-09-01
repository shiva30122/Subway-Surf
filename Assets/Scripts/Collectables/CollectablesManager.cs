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
        public float spawnChance = 0.7f;
        public int maxCoinsSpawn = 25;
        [Range(0f,1f)] public float comboLeftRight = 0.2f;
        [Range(0f,1f)] public float comboCenter = 0.3f;

        [Header("Coin Y - Public (offset)")]
        [Tooltip("Public Y base height for coins above road. Tweak if y not correctly set.")]
        public float coinYOffset = 0.9f;

        [Header("UI - Public")]
        public TMP_Text coinsText;

        [Header("Save - Player Prefab Data")]
        public int totalCoins = 0;
        public int runCoins = 0;
        private const string SaveKey = "GoldCoins";

        // Pooling - hide and reuse, no destroy
        private readonly Queue<GameObject> coinPool = new Queue<GameObject>();
        private readonly List<GameObject> activeCoins = new List<GameObject>();
        private readonly Dictionary<GameObject, Transform> coinSegmentMap = new Dictionary<GameObject, Transform>();
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
            Vector3 prefabScale = goldCoinPrefab != null ? goldCoinPrefab.transform.localScale : Vector3.one;
            Quaternion prefabRot = goldCoinPrefab != null ? goldCoinPrefab.transform.rotation : Quaternion.identity;
            for (int i = 0; i < maxCoinsSpawn; i++)
            {
                GameObject go = Instantiate(goldCoinPrefab, Vector3.zero, Quaternion.identity, poolRoot);
                go.SetActive(false);
                go.tag = "GoldCoin";
                // Use prefab as is - keep original scale/rotation
                go.transform.localScale = prefabScale;
                go.transform.rotation = prefabRot;
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
            for (int i = activeCoins.Count - 1; i >= 0; i--)
                HideCoin(activeCoins[i]);
            coinSegmentMap.Clear();
        }

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
            float centerY = coinYOffset; // public offset

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
            switch (combo)
            {
                case 0: return new List<int>{1};
                case 1: return new List<int>{0};
                case 2: return new List<int>{2};
                case 3: return new List<int>{0,2};
                case 4: return new List<int>{0,1};
                case 5: return new List<int>{1,2};
                case 6: return new List<int>{0,1,2};
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
            Transform segmentAtPos = FindSegmentAtZ(pos.z);

            GameObject go = null;
            if (coinPool.Count > 0) go = coinPool.Dequeue();
            else if (activeCoins.Count > 0)
            {
                go = activeCoins[0];
                activeCoins.RemoveAt(0);
                coinSegmentMap.Remove(go);
                go.SetActive(false);
            }
            else
            {
                if (goldCoinPrefab == null) return;
                go = Instantiate(goldCoinPrefab, pos, Quaternion.identity, poolRoot);
            }
            // Fix: do NOT parent to non-uniform scaled WalkPath segment (causes scale+rotation skew)
            // Keep under poolRoot (uniform) and move manually - use prefab as is
            go.transform.SetParent(poolRoot, true);
            go.transform.position = pos;
            // Restore prefab rotation/scale as is
            if (goldCoinPrefab != null)
            {
                go.transform.rotation = goldCoinPrefab.transform.rotation;
                go.transform.localScale = goldCoinPrefab.transform.localScale;
            }
            else
            {
                go.transform.rotation = Quaternion.identity;
            }
            go.tag = "GoldCoin";
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            if (segmentAtPos != null) coinSegmentMap[go] = segmentAtPos;
            else coinSegmentMap.Remove(go);
            var gc = go.GetComponent<GoldCoin>();
            if (gc != null) gc.ResetCoin(pos);
            else go.SetActive(true);
            if (!activeCoins.Contains(go)) activeCoins.Add(go);
        }

        private Transform FindSegmentAtZ(float z)
        {
            var gm = FindObjectOfType<Managers.GameManager>();
            if (gm == null) return null;
            GameObject best = null;
            float bestDist = float.MaxValue;
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
            coinSegmentMap.Remove(go);
            go.transform.SetParent(poolRoot, true);
            // Keep prefab scale as is (dont force to one)
            if (goldCoinPrefab != null) go.transform.localScale = goldCoinPrefab.transform.localScale;
            go.SetActive(false);
            if (!coinPool.Contains(go))
                coinPool.Enqueue(go);
            activeCoins.Remove(go);
        }

        private float nextDynamicCheck = 0f;
        private void Update()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            var gm = FindObjectOfType<Managers.GameManager>();
            float despawn = gm != null ? gm.despawnBehindPlayer : 25f;
            float hideZ = player.transform.position.z - despawn;

            // Move coins with track (since not parented to avoid non-uniform scale skew)
            if (gm != null && gm.isMoving && gm.autoMove)
            {
                float delta = gm.moveSpeed * Time.deltaTime;
                for (int i = 0; i < activeCoins.Count; i++)
                {
                    var c = activeCoins[i];
                    if (c != null && c.activeSelf)
                        c.transform.position += Vector3.back * delta;
                }
            }
            // Force Y to public offset so 50 updates instantly even for active pooled coins
            for (int i = 0; i < activeCoins.Count; i++)
            {
                var c = activeCoins[i];
                if (c != null && c.activeSelf)
                {
                    var gc = c.GetComponent<GoldCoin>();
                    if (gc != null) gc.baseY = coinYOffset;
                    // Also correct x/z not needed, just ensure y base
                }
            }

            for (int i = activeCoins.Count - 1; i >= 0; i--)
            {
                var c = activeCoins[i];
                if (c == null) { activeCoins.RemoveAt(i); coinSegmentMap.Remove(c); continue; }
                if (c.transform.position.z < hideZ)
                {
                    HideCoin(c);
                    continue;
                }
                // If segment this coin was spawned on got recycled, hide it
                if (coinSegmentMap.TryGetValue(c, out Transform seg) && seg != null && !seg.gameObject.activeSelf)
                {
                    HideCoin(c);
                }
            }
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
            Transform best = null;
            float bestDist = float.MaxValue;
            foreach (Transform child in root.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                float cz = child.position.z;
                float ahead = cz - player.position.z;
                if (ahead < 5f || ahead > 35f) continue;
                if (HasCoins(child)) continue;
                float dist = Mathf.Abs(ahead);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = child;
                }
            }
            if (best != null && coinPool.Count > 0)
            {
                int coinCount = Random.Range(minCoinsPerLine, maxCoinsPerLine + 1);
                int combo = Random.Range(0, 7);
                var lanes = GetLanesFromCombo(combo);
                Transform startPivot = FindDeep(best, "StartPivot");
                Transform endPivot = FindDeep(best, "EndPivot");
                if (startPivot == null || endPivot == null) return;
                float startZ = startPivot.position.z + 1.5f;
                float endZ = endPivot.position.z - 1f;
                float y = coinYOffset;
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
            // Check via map (since coins not parented to segment anymore)
            foreach (var kv in coinSegmentMap)
            {
                if (kv.Value == segment && kv.Key != null && kv.Key.activeSelf) return true;
            }
            // Fallback old parent check (for any legacy)
            foreach (Transform child in segment)
            {
                if (child.CompareTag("GoldCoin") && child.gameObject.activeSelf) return true;
            }
            foreach (var c in activeCoins)
                if (c != null && c.transform.parent == segment) return true;
            return false;
        }

        private void SpawnCoinOnSegment(Vector3 pos, Transform segment)
        {
            if (coinPool.Count == 0) return;
            GameObject go = coinPool.Dequeue();
            go.transform.SetParent(poolRoot, true);
            go.transform.position = pos;
            if (goldCoinPrefab != null)
            {
                go.transform.rotation = goldCoinPrefab.transform.rotation;
                go.transform.localScale = goldCoinPrefab.transform.localScale;
            }
            else
            {
                go.transform.rotation = Quaternion.identity;
            }
            go.tag = "GoldCoin";
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            coinSegmentMap[go] = segment;
            var gc = go.GetComponent<GoldCoin>();
            if (gc != null) gc.ResetCoin(pos);
            else go.SetActive(true);
            if (!activeCoins.Contains(go)) activeCoins.Add(go);
        }

        public void HideCoinsForSegment(Transform segment)
        {
            if (segment == null) return;
            for (int i = activeCoins.Count - 1; i >= 0; i--)
            {
                var c = activeCoins[i];
                if (c != null && coinSegmentMap.TryGetValue(c, out Transform seg) && seg == segment)
                {
                    HideCoin(c);
                }
                else if (c != null && c.transform.parent == segment)
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
