using UnityEngine;

namespace SubwayDash.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Lane Points - Public (MPC created)")]
        public Transform laneLeft;   // LeftPoint -2,0,0
        public Transform laneCenter; // CenterPoint 0,0,0
        public Transform laneRight;  // RightPoint 2,0,0

        [Header("Movement - Public")]
        public float laneSwitchSpeed = 12f;
        public float laneOffsetX = 2f;
        public float playerFloatY = 1f; // public max Y for gravity - no ground touch needed
        private int currentLane = 1; // 0=Left 1=Center 2=Right
        private float targetX;
        private float lockedZ;
        private float debounceTime = 0.12f;
        private float lastSwitchTime = -10f;

        [Header("Jump - Public")]
        public float jumpHeight = 1.8f;
        public float jumpDuration = 0.6f;
        public float gravity = -22f;
        public float groundedGravity = -1f;

        [Header("Slide - Public")]
        public float slideDuration = 0.85f;
        public float slideHeight = 1f;
        public float slideScaleY = 0.55f;
        private bool isSliding = false;
        private float slideTimer = 0f;
        private float originalHeight;
        private Vector3 originalCenter;
        private Vector3 originalModelScale;
        public Transform modelTransform;

        [Header("Ground Check")]
        public float groundedOffset = 0.1f;
        public float groundedRadius = 0.4f;
        public LayerMask groundLayers = ~0;

        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded = true;
        private bool isJumping = false;
        private float jumpTimer = 0f;

        public void ResetPlayer()
        {
            currentLane = 1;
            targetX = GetLaneX(currentLane);
            velocity = Vector3.zero;
            isJumping = false;
            isSliding = false;
            slideTimer = 0f;
            jumpTimer = 0f;
            controller.height = originalHeight;
            controller.center = originalCenter;
            if (modelTransform != null) modelTransform.localScale = originalModelScale;
            controller.enabled = false;
            transform.position = new Vector3(0f, playerFloatY, 0f);
            controller.enabled = true;
            lockedZ = 0f;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            originalHeight = controller.height;
            originalCenter = controller.center;
            if (modelTransform == null)
            {
                var m = transform.Find("PlayerModel");
                if (m != null) modelTransform = m;
            }
            if (modelTransform != null) originalModelScale = modelTransform.localScale;
            lockedZ = 0f;
            targetX = GetLaneX(currentLane);
            if (laneLeft == null || laneCenter == null || laneRight == null)
                CreateLanePointsFallback();
            Invoke(nameof(IgnoreWalkPathCollisions), 0.2f);
        }

        private void Start()
        {
            // Force float Y on start
            Vector3 p = transform.position;
            p.y = playerFloatY + controller.height / 2f - controller.center.y;
            // Set directly bypassing controller
            controller.enabled = false;
            transform.position = new Vector3(GetLaneX(currentLane), playerFloatY, lockedZ);
            controller.enabled = true;
            targetX = GetLaneX(currentLane);
        }

        private void Update()
        {
            HandleInput();
            HandleLaneMovementDirect(); // no collider block
            HandleJumpAndGravityFloating(); // uses playerFloatY, no ground ray
            HandleSlide();
            LockZDirect(); // no collider push
        }

        private void HandleInput()
        {
            bool canSwitch = Time.time - lastSwitchTime > debounceTime;
            if (canSwitch)
            {
                if (IsRightPressed())
                {
                    if (currentLane < 2) { currentLane++; targetX = GetLaneX(currentLane); lastSwitchTime = Time.time; Debug.Log($"[Input] Right -> lane {currentLane} targetX {targetX}"); }
                }
                else if (IsLeftPressed())
                {
                    if (currentLane > 0) { currentLane--; targetX = GetLaneX(currentLane); lastSwitchTime = Time.time; Debug.Log($"[Input] Left -> lane {currentLane} targetX {targetX}"); }
                }
            }
            if (IsUpPressed())
            {
                if (!isSliding && !isJumping)
                {
                    isJumping = true;
                    jumpTimer = 0f;
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    Debug.Log($"[Input] Jump velocity {velocity.y}");
                }
            }
            if (IsDownPressed())
            {
                if (!isJumping && !isSliding)
                {
                    StartSlide();
                    Debug.Log("[Input] Slide start");
                }
            }
        }

        private void HandleLaneMovementDirect()
        {
            // Direct transform lerp - no CharacterController collision with WallLeft/Right
            float newX = Mathf.Lerp(transform.position.x, targetX, laneSwitchSpeed * Time.deltaTime);
            Vector3 pos = transform.position;
            pos.x = newX;
            // Keep Y as is (handled by jump), Z as locked
            transform.position = pos;
        }

        private void HandleJumpAndGravityFloating()
        {
            if (isJumping)
            {
                jumpTimer += Time.deltaTime;
                velocity.y += gravity * Time.deltaTime;
                float newY = transform.position.y + velocity.y * Time.deltaTime;
                // Clamp to floatY when landing
                if (newY <= playerFloatY && velocity.y < 0)
                {
                    newY = playerFloatY;
                    velocity.y = groundedGravity;
                    isJumping = false;
                    jumpTimer = 0f;
                }
                Vector3 pos = transform.position;
                pos.y = newY;
                transform.position = pos;
                // Also move controller for collision with future obstacles
                // Use simple translate without ground check
            }
            else
            {
                // Always float at playerFloatY when not jumping
                Vector3 pos = transform.position;
                if (Mathf.Abs(pos.y - playerFloatY) > 0.01f)
                {
                    pos.y = Mathf.Lerp(pos.y, playerFloatY, 10f * Time.deltaTime);
                    transform.position = pos;
                }
                velocity.y = groundedGravity;
            }
        }

        private void HandleSlide()
        {
            if (!isSliding) return;
            slideTimer += Time.deltaTime;
            if (slideTimer >= slideDuration)
                EndSlide();
        }

        private void StartSlide()
        {
            isSliding = true;
            slideTimer = 0f;
            controller.height = slideHeight;
            controller.center = new Vector3(originalCenter.x, originalCenter.y - (originalHeight - slideHeight) / 2f, originalCenter.z);
            if (modelTransform != null)
                modelTransform.localScale = new Vector3(originalModelScale.x, originalModelScale.y * slideScaleY, originalModelScale.z);
        }

        private void EndSlide()
        {
            isSliding = false;
            controller.height = originalHeight;
            controller.center = originalCenter;
            if (modelTransform != null)
                modelTransform.localScale = originalModelScale;
            slideTimer = 0f;
        }

        private void LockZDirect()
        {
            Vector3 pos = transform.position;
            if (Mathf.Abs(pos.z - lockedZ) > 0.001f)
            {
                pos.z = lockedZ;
                transform.position = pos;
            }
        }

        private void IgnoreWalkPathCollisions()
        {
            // Ignore all WalkPath Road/Wall colliders so player not pushed -Z and walls don't block <>
            var cols = FindObjectsOfType<Collider>();
            foreach (var c in cols)
            {
                if (c == null || c.gameObject == gameObject) continue;
                string n = c.gameObject.name;
                if (n == "Road" || n == "WallLeft" || n == "WallRight" || n == "WallLeft (1)" || n == "WallRight (1)" || n.Contains("Road") || n.Contains("Wall"))
                {
                    try { Physics.IgnoreCollision(controller, c, true); } catch {}
                }
            }
            Debug.Log("[Player] Ignored WalkPath collisions for floating + lane fix");
        }

        private float GetLaneX(int lane)
        {
            if (lane == 0 && laneLeft != null) return laneLeft.position.x;
            if (lane == 1 && laneCenter != null) return laneCenter.position.x;
            if (lane == 2 && laneRight != null) return laneRight.position.x;
            if (lane == 0) return -laneOffsetX;
            if (lane == 2) return laneOffsetX;
            return 0f;
        }

        private void CreateLanePointsFallback()
        {
            GameObject holder = new GameObject("LanePoints");
            holder.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            holder.transform.SetParent(transform.parent);
            if (laneLeft == null) { var go = new GameObject("LeftPoint"); go.transform.SetParent(holder.transform); go.transform.localPosition = new Vector3(-laneOffsetX, 0, 0); laneLeft = go.transform; }
            if (laneCenter == null) { var go = new GameObject("CenterPoint"); go.transform.SetParent(holder.transform); go.transform.localPosition = new Vector3(0, 0, 0); laneCenter = go.transform; }
            if (laneRight == null) { var go = new GameObject("RightPoint"); go.transform.SetParent(holder.transform); go.transform.localPosition = new Vector3(laneOffsetX, 0, 0); laneRight = go.transform; }
        }

        private bool IsRightPressed()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.RightArrow)) return true;
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rightArrowKey.wasPressedThisFrame) return true;
            } catch {}
            return false;
        }
        private bool IsLeftPressed()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) return true;
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.leftArrowKey.wasPressedThisFrame) return true;
            } catch {}
            return false;
        }
        private bool IsUpPressed()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.UpArrow)) return true;
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.upArrowKey.wasPressedThisFrame) return true;
            } catch {}
            return false;
        }
        private bool IsDownPressed()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.DownArrow)) return true;
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.downArrowKey.wasPressedThisFrame) return true;
            } catch {}
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            Gizmos.color = Color.green;
            Vector3 pos = new Vector3(transform.position.x, playerFloatY, transform.position.z);
            Gizmos.DrawWireSphere(pos, 0.5f);
            Gizmos.color = Color.yellow;
            if (laneLeft != null) Gizmos.DrawWireCube(laneLeft.position, Vector3.one * 0.5f);
            if (laneCenter != null) Gizmos.DrawWireCube(laneCenter.position, Vector3.one * 0.5f);
            if (laneRight != null) Gizmos.DrawWireCube(laneRight.position, Vector3.one * 0.5f);
        }
    }
}
