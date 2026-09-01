using System.Collections.Generic;
using SubwayDash.Collectables;
using UnityEngine;
using UnityEngine.UI;

namespace SubwayDash.Managers
{
    public class GameManager : MonoBehaviour
    {
        [Header("Starting Model - Public")]
        public GameObject startingModel; // has StartPivot/EndPivot

        [Header("WalkPath Prefabs")]
        public GameObject[] walkPathPrefabs;

        [Header("Spawn")]
        public Transform spawnRoot; // WalkPath 1338611459 at 0,0,0 - ONLY holder
        public float segmentLength = 10f;

        [Header("Pooling - Public")]
        public Transform playerTransform; // assign Player, auto-finds Tag=Player if null
        public int maxPoolSize = 10; // total pooled - auto = copiesPerPrefab * walkPathPrefabs.Length
        public int copiesPerPrefab = 4; // 4× per prefab = long distance, set 7-8 for extra long
        public float moveSpeed = 12f; // base track move speed toward player
        public float despawnBehindPlayer = 25f; // hide when EndPivot behind player
        public bool autoMove = true; // move tracks
        public bool isMoving = false;

        [Header("Score - Public")]
        public long score = 0; // BigInt style Subway Surfers score
        public float speedMultiplier = 1f; // current multiplier
        public float maxSpeed = 25f; // max speed cap
        public float speedIncreaseRate = 0.04f; // per second
        private float baseMoveSpeed;

        // Internal
        private readonly Queue<GameObject> poolQueue = new Queue<GameObject>();
        private readonly List<GameObject> activeSegments = new List<GameObject>();
        private int spawned = 0;
        private Vector3 nextPos = Vector3.zero;
        private Transform lastEndPivot;
        private int nextPrefabIndex = 0;
        private Vector3 startingModelInitialPos;

        private void Awake()
        {
            if (startingModel != null) startingModelInitialPos = startingModel.transform.position;
            if (playerTransform == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }
        }

        private void Start()
        {
            baseMoveSpeed = moveSpeed;
            PrewarmPool();
        }

        private void Update()
        {
            if (!isMoving)
            {
                // Keep score UI visible even when not moving
                return;
            }
            // Score + speed multiplier (Subway Surfers style) - runs only when moving
            if (autoMove)
            {
                speedMultiplier = Mathf.Clamp(speedMultiplier + speedIncreaseRate * Time.deltaTime, 1f, maxSpeed);
                moveSpeed = Mathf.Clamp(baseMoveSpeed * speedMultiplier, baseMoveSpeed, maxSpeed);
            }
            // BigInt score: distance * multiplier
            long add = (long)(moveSpeed * Time.deltaTime * 10f * speedMultiplier);
            score += add; // 50% slower UI update

            if (!autoMove) return;
            if (activeSegments.Count == 0) return;
            if (playerTransform == null) TryFindPlayer();

            // Move all active segments toward player (negative Z = toward player if forward is +Z)
            float delta = moveSpeed * Time.deltaTime;
            for (int i = 0; i < activeSegments.Count; i++)
            {
                if (activeSegments[i] != null)
                    activeSegments[i].transform.position += Vector3.back * delta;
            }
            // lastEndPivot also moves with its segment, so front position moves back correctly
            // Check recycle behind player
            CheckRecycle();
        }

        public void OnPlayButtonClicked()
        {
            isMoving = false;
            score = 0;
            speedMultiplier = 1f;
            moveSpeed = baseMoveSpeed > 0 ? baseMoveSpeed : moveSpeed;
            SpawnAllViaPivots();
            isMoving = true;
        }

        public void ClearAll() => Clear();
        public void SetMoving(bool moving) => isMoving = moving;

        // Public Hide API - called when segment reaches player endpoint
        public void HideWalkPath(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            if (!poolQueue.Contains(go))
                poolQueue.Enqueue(go);
            // Debug.Log($"[Pool] HideWalkPath '{go.name}' pooled={poolQueue.Count} active={activeSegments.Count}", go);
        }

        // Public Get pooled
        public GameObject GetPooledWalkPath()
        {
            if (poolQueue.Count > 0) return poolQueue.Dequeue();
            return null;
        }



        private void PrewarmPool()
        {
            if (walkPathPrefabs == null || walkPathPrefabs.Length == 0) return;
            Transform parent = spawnRoot != null ? spawnRoot : transform;
            // Long distance: total = copiesPerPrefab * walkPathPrefabs.Length (e.g. 4*4=16, 8*4=32)
            int total = Mathf.Max(maxPoolSize, copiesPerPrefab * walkPathPrefabs.Length);
            maxPoolSize = total;
            // Clear old pool if replay in editor
            poolQueue.Clear();
            for (int i = 0; i < total; i++)
            {
                GameObject prefab = walkPathPrefabs[i % walkPathPrefabs.Length];
                GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
                go.name = prefab.name + "_Pooled";
                go.SetActive(false);
                poolQueue.Enqueue(go);
            }
            // Debug.Log($"[Pool] Prewarmed {poolQueue.Count} objects ({copiesPerPrefab}×{walkPathPrefabs.Length})");
        }



        private void SpawnAllViaPivots()
        {
            if (walkPathPrefabs == null || walkPathPrefabs.Length == 0) return;

            // Return actives to pool before respawn
            Clear();

            if (startingModel != null)
            {
                // Reset to initial 0,0,0 front and enable - one-time enable
                startingModel.transform.position = startingModelInitialPos;
                if (startingModel.transform.parent != spawnRoot && spawnRoot != null)
                    startingModel.transform.SetParent(spawnRoot, true);
                startingModel.SetActive(true);
                Transform startEnd = FindDeep(startingModel.transform, "EndPivot");
                if (startEnd != null)
                {
                    lastEndPivot = startEnd;
                    nextPos = startEnd.position;
                    // Add to activeSegments only for one-time move, will hide and not re-pool
                    if (!activeSegments.Contains(startingModel))
                        activeSegments.Add(startingModel);
                    Debug.Log($"[Spawn] START reset to initial {startingModelInitialPos} End={startEnd.position}", startingModel);
                }
                else
                {
                    Debug.LogWarning($"[Spawn] startingModel has NO EndPivot! Add child 'EndPivot'", startingModel);
                    nextPos = startingModel.transform.position;
                    lastEndPivot = null;
                }
            }
            else
            {
                Debug.LogWarning("[Spawn] startingModel NULL! Using spawnRoot");
                nextPos = spawnRoot != null ? spawnRoot.position : Vector3.zero;
                lastEndPivot = null;
            }

            // Long distance: spawn all pooled = copiesPerPrefab × prefabs
            for (int i = 0; i < maxPoolSize; i++)
            {
                GameObject prefab = walkPathPrefabs[i % walkPathPrefabs.Length];
                SpawnOneViaPivot(prefab);
            }
        }

        private void SpawnAll() => SpawnAllViaPivots();

        private CollectablesManager collectablesMgr;
        private void EnsureCollectablesMgr() { if (collectablesMgr == null) collectablesMgr = FindObjectOfType<CollectablesManager>(); }
        private void SpawnOneViaPivot(GameObject prefab)
        {
            if (prefab == null) return;
            Transform parent = spawnRoot != null ? spawnRoot : transform;

            GameObject go = GetPooledWalkPath();
            bool isNew = false;
            if (go == null)
            {
                go = Instantiate(prefab, nextPos, Quaternion.identity, parent);
                isNew = true;
            }
            else
            {
                go.transform.SetParent(parent);
                go.transform.position = nextPos;
                go.transform.rotation = Quaternion.identity;
                go.SetActive(true);
            }
            // Ensure correct prefab type name if reused different type - keep original type for variety
            if (isNew) go.name = prefab.name;
            else if (go.name.Contains("_Pooled")) go.name = prefab.name;

            // Exact pivot snap: StartPivot == lastEndPivot
            if (lastEndPivot != null)
            {
                Transform startPivot = FindDeep(go.transform, "StartPivot");
                if (startPivot != null)
                {
                    Vector3 delta = lastEndPivot.position - startPivot.position;
                    go.transform.position += delta;
                    if (spawned == 0)
                        Debug.Log($"[Spawn 1st] prevEnd={lastEndPivot.position} delta={delta} newRoot={go.transform.position}", go);
                }
                else
                {
                    Vector3 startOffset = GetPivotLocalPos(prefab, "StartPivot");
                    Vector3 scaledOffset = Vector3.Scale(go.transform.localScale, startOffset);
                    go.transform.position = lastEndPivot.position - scaledOffset;
                }
            }

            if (!activeSegments.Contains(go))
                activeSegments.Add(go);
            spawned++;

            Transform endPivot = FindDeep(go.transform, "EndPivot");
            if (endPivot != null)
            {
                lastEndPivot = endPivot;
                nextPos = endPivot.position;
            }
            else
            {
                nextPos = go.transform.position + Vector3.forward * (segmentLength * Mathf.Max(1f, go.transform.localScale.z));
            }
            nextPrefabIndex = (nextPrefabIndex + 1) % walkPathPrefabs.Length;
            EnsureCollectablesMgr();
            if (collectablesMgr != null) collectablesMgr.TrySpawnCoinsForSegment(go.transform);
        }

        private void CheckRecycle()
        {
            if (playerTransform == null) return;
            if (activeSegments.Count == 0) return;

            for (int i = activeSegments.Count - 1; i >= 0; i--)
            {
                GameObject seg = activeSegments[i];
                if (seg == null) { activeSegments.RemoveAt(i); continue; }
                Transform endPivot = FindDeep(seg.transform, "EndPivot");
                Vector3 checkPos = endPivot != null ? endPivot.position : seg.transform.position;
                if (checkPos.z < playerTransform.position.z - despawnBehindPlayer)
                {
                    activeSegments.RemoveAt(i);
                    if (seg == startingModel)
                    {
                        // One-time: just hide and stay hidden, don't pull to front, don't pool
                        seg.SetActive(false);
                        Debug.Log($"[Recycle] StartingModel hidden one-time at {seg.transform.position}", seg);
                    }
                    else
                    {
                        EnsureCollectablesMgr();
                        if (collectablesMgr != null) collectablesMgr.HideCoinsForSegment(seg.transform);
                        HideWalkPath(seg);
                        ReuseAtFront();
                    }
                    break;
                }
            }
        }

        private void ReuseAtFront()
        {
            if (walkPathPrefabs == null || walkPathPrefabs.Length == 0) return;
            GameObject prefab = walkPathPrefabs[nextPrefabIndex % walkPathPrefabs.Length];
            SpawnOneViaPivot(prefab);
        }

        private void TryFindPlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else if (Camera.main != null) playerTransform = Camera.main.transform;
        }

        private Vector3 GetPivotLocalPos(GameObject prefab, string pivotName)
        {
            Transform t = FindDeep(prefab.transform, pivotName);
            if (t != null) return t.localPosition;
            if (pivotName == "StartPivot") return new Vector3(0, 0.5f, -5f);
            if (pivotName == "EndPivot") return new Vector3(0, 0.5f, 5f);
            return Vector3.zero;
        }

        private Transform FindDeep(Transform root, string name)
        {
            foreach (Transform c in root.GetComponentsInChildren<Transform>(true))
            {
                if (c.name == name) return c;
            }
            return null;
        }

        private void Clear()
        {
            for (int i = activeSegments.Count - 1; i >= 0; i--)
            {
                var go = activeSegments[i];
                if (go != null)
                {
                    if (go == startingModel)
                    {
                        go.SetActive(false);
                    }
                    else
                    {
                        HideWalkPath(go);
                    }
                }
            }
            activeSegments.Clear();
            // Hide remaining coins via pool (fixes leak)
            EnsureCollectablesMgr();
            if (collectablesMgr != null)
            {
                var coins = FindObjectsOfType<Collectables.GoldCoin>();
                foreach (var c in coins) if (c.gameObject.activeSelf) collectablesMgr.HideCoin(c.gameObject);
            }

            // Fallback: destroy any stray children under spawnRoot (except pooled) - NEVER destroy startingModel
            Transform parent = spawnRoot != null ? spawnRoot : transform;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (startingModel != null && child.gameObject == startingModel) continue;
                // Keep pooled inactive objects
                if (!child.gameObject.activeSelf && poolQueue.Contains(child.gameObject)) continue;
                // Destroy stray active not in list
                if (child.gameObject.activeSelf)
                    DestroyImmediate(child.gameObject);
            }
            spawned = 0;
            lastEndPivot = null;
            nextPrefabIndex = 0;
            // Reset startingModel for next play if it was hidden - will be re-added in SpawnAllViaPivots
            if (startingModel != null)
            {
                startingModel.SetActive(true);
                // Reset position to origin or its initial - will be repositioned via lastEndPivot
                Transform se = FindDeep(startingModel.transform, "EndPivot");
                if (se != null) lastEndPivot = se;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (playerTransform != null)
            {
                Gizmos.color = Color.red;
                Vector3 p = playerTransform.position - Vector3.forward * despawnBehindPlayer;
                Gizmos.DrawLine(p + Vector3.left * 5, p + Vector3.right * 5);
                Gizmos.DrawWireCube(p, new Vector3(10, 0.1f, 0.1f));
            }
            if (lastEndPivot != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(lastEndPivot.position, 0.5f);
            }
        }
    }
}
