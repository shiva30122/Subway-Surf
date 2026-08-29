using UnityEngine;
using UnityEngine.UI;

namespace SubwayDash.Managers
{
    public class GameManager : MonoBehaviour
    {
        [Header("UI - Public")]
        public GameObject canvas;
        public Button playButton;

        [Header("WalkPath Prefabs")]
        public GameObject[] walkPathPrefabs;

        [Header("Spawn")]
        public Transform spawnRoot;
        public float segmentLength = 10f; // fallback if pivots missing

        private int spawned = 0;
        private Vector3 nextPos = Vector3.zero;
        private Transform lastEndPivot;

        private void Awake()
        {
            if (canvas != null) canvas.SetActive(true);
        }

        private void Start()
        {
            if (canvas != null) canvas.SetActive(true);
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayButtonClicked);
                playButton.onClick.AddListener(OnPlayButtonClicked);
            }
        }

        public void OnPlayButtonClicked()
        {
            HideCanvas();
            SpawnAllViaPivots();
        }

        public void ClearAll() => Clear();

        private void HideCanvas()
        {
            if (canvas != null) canvas.SetActive(false);
        }

        // Pivot-based spawning: uses StartPivot/EndPivot (A-B) on prefabs
        private void SpawnAllViaPivots()
        {
            if (walkPathPrefabs == null || walkPathPrefabs.Length == 0) return;
            Clear();
            nextPos = spawnRoot != null ? spawnRoot.position : Vector3.zero;
            lastEndPivot = null;

            for (int i = 0; i < walkPathPrefabs.Length; i++)
            {
                SpawnOneViaPivot(walkPathPrefabs[i]);
            }
        }

        // Keep old method for compatibility
        private void SpawnAll() => SpawnAllViaPivots();

        private void SpawnOneViaPivot(GameObject prefab)
        {
            if (prefab == null) return;
            Transform parent = spawnRoot != null ? spawnRoot : transform;

            // Calculate spawn pos: align StartPivot of next prefab to last EndPivot
            Vector3 spawnPos = nextPos;
            if (lastEndPivot != null)
            {
                // Find StartPivot offset in prefab (local pos)
                Vector3 startOffset = GetPivotLocalPos(prefab, "StartPivot");
                // next center = last EndPivot worldPos - startOffset
                spawnPos = lastEndPivot.position - startOffset;
            }

            GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity, parent);
            go.name = prefab.name;
            spawned++;

            // Update next pivot for next spawn
            Transform endPivot = FindDeep(go.transform, "EndPivot");
            if (endPivot != null)
            {
                lastEndPivot = endPivot;
                // For fallback nextPos (if next prefab missing StartPivot)
                nextPos = endPivot.position + Vector3.forward * (segmentLength / 2f);
            }
            else
            {
                // Fallback: use segmentLength
                nextPos += Vector3.forward * segmentLength;
            }
        }

        private void SpawnOne(GameObject prefab) => SpawnOneViaPivot(prefab);

        private Vector3 GetPivotLocalPos(GameObject prefab, string pivotName)
        {
            // Try find in prefab asset (instantiate temporary hidden)
            // Fallback: known square pivot at z = -5 / +5
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
            Transform parent = spawnRoot != null ? spawnRoot : transform;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                // keep canvas/playButton if they are under parent (they are not, but safe)
                if (canvas != null && child.gameObject == canvas) continue;
                DestroyImmediate(child.gameObject);
            }
            spawned = 0;
            lastEndPivot = null;
        }
    }
}
