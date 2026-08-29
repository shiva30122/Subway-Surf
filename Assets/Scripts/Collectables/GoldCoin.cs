using UnityEngine;

namespace SubwayDash.Collectables
{
    public class GoldCoin : MonoBehaviour
    {
        [Header("Animation")]
        public float rotationSpeed = 90f; // simple Y rotation
        public float bobSpeed = 2f;
        public float bobHeight = 0.15f;
        private Vector3 startPos;
        private bool collected = false;

        private void OnEnable()
        {
            collected = false;
            startPos = transform.position;
        }

        private void Update()
        {
            // All coins same rotation - use unscaled Time so all look same
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
            // Bob in local Y relative to parent (segment) so moving alongside keeps Y stable
            // Use shared Time.time so all coins bob in sync - same animation
            if (transform.parent != null)
            {
                Vector3 local = transform.localPosition;
                local.y = 0.9f + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.localPosition = local;
            }
            else
            {
                float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                Vector3 p = transform.position;
                p.y = startPos.y + bob;
                transform.position = new Vector3(transform.position.x, startPos.y + bob, transform.position.z);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;
            if (other.CompareTag("Player"))
            {
                collected = true;
                var mgr = FindObjectOfType<CollectablesManager>();
                if (mgr != null) mgr.AddCoin(1);
                // Hide and reuse instead of destroy
                gameObject.SetActive(false);
            }
        }

        public void ResetCoin(Vector3 pos)
        {
            startPos = pos;
            transform.position = pos;
            collected = false;
            gameObject.SetActive(true);
        }

#if UNITY_EDITOR
        // Editor button helpers - preview animation in Edit mode
        public void PreviewAnimation()
        {
            // Quick spin in editor to test how coin animation looks
            transform.Rotate(Vector3.up * 45f, Space.World);
            Debug.Log($"[GoldCoin Preview] rotationSpeed={rotationSpeed} bobSpeed={bobSpeed} bobHeight={bobHeight} - Tweak sliders and press again. Play mode shows continuous spin.");
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
