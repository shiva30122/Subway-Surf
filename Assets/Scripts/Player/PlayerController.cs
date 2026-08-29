using UnityEngine;

namespace SubwayDash.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float rotationSpeed = 720f;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundedGravity = -2f;

        [Header("Ground Check")]
        [SerializeField] private float groundedOffset = 0.1f;
        [SerializeField] private float groundedRadius = 0.4f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            HandleGroundCheck();
            HandleMovement();
            HandleJumpAndGravity();
        }

        private void HandleGroundCheck()
        {
            Vector3 spherePos = new Vector3(transform.position.x, transform.position.y - controller.height / 2f + groundedRadius + groundedOffset, transform.position.z);
            isGrounded = Physics.CheckSphere(spherePos, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
            if (isGrounded && velocity.y < 0f)
                velocity.y = groundedGravity;
        }

        private void HandleMovement()
        {
            float h = SafeGetAxis("Horizontal");
            float v = SafeGetAxis("Vertical");
            Vector3 input = new Vector3(h, 0f, v).normalized;
            float speed = IsSprintPressed() ? moveSpeed * sprintMultiplier : moveSpeed;
            if (input.magnitude > 0.01f)
            {
                Vector3 move = input * speed;
                Quaternion targetRot = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                controller.Move(move * Time.deltaTime);
            }
        }

        private void HandleJumpAndGravity()
        {
            if (isGrounded && IsJumpPressed())
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private float SafeGetAxis(string name)
        {
            try { return Input.GetAxisRaw(name); } catch { return 0f; }
        }

        private bool IsSprintPressed()
        {
            try { return Input.GetKey(KeyCode.LeftShift); } catch { return false; }
        }

        private bool IsJumpPressed()
        {
            try { return Input.GetButtonDown("Jump"); } catch { return false; }
        }

        private void OnDrawGizmosSelected()
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 pos = new Vector3(transform.position.x, transform.position.y - controller.height / 2f + groundedRadius + groundedOffset, transform.position.z);
            Gizmos.DrawWireSphere(pos, groundedRadius);
        }
    }
}
