using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class TopDownPlayerController : MonoBehaviour
{
    [Header("Widok kamery")]
    public CameraViewTransition cameraRig;
    [Header("Ruch")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 20f;

    // Legacy serialized references are retained for later reuse. Manual attacks are off.
    [HideInInspector] public GameObject spellPrefab;
    [HideInInspector] public Transform castPoint;
    [HideInInspector] public float attackCooldown = 0.5f;

    [Header("Podłoże")]
    public LayerMask groundLayer;

    private CharacterController controller;
    private PlayerStats stats;
    private CharacterPanelUI characterPanel;
    public float CurrentMoveSpeed => moveSpeed * (stats ? Mathf.Max(.01f, stats.movementSpeedMultiplier) : 1f);
    private Animator animator;
    private Vector2 moveInput;
    private Camera mainCamera;
    private Vector3 lookDirection;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();
        characterPanel = Object.FindAnyObjectByType<CharacterPanelUI>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnAttack(InputValue value)
    {
        // Keep the PlayerInput message receiver; StormSpell handles automatic combat.
    }

    void Update()
    {
        if ((stats && !stats.IsAlive) || (characterPanel && characterPanel.panel && characterPanel.panel.activeInHierarchy) || (cameraRig != null && (cameraRig.IsTransitioning || cameraRig.MenuOpen)))
        {
            controller.SimpleMove(Vector3.zero);
            if (animator != null)
            {
                animator.SetFloat("MoveX", 0, 0.1f, Time.deltaTime);
                animator.SetFloat("MoveZ", 0, 0.1f, Time.deltaTime);
            }
            return;
        }
        HandleRotation();
        HandleMovement();
    }

    void HandleRotation()
    {
        if (mainCamera == null || Mouse.current == null) return;
        if (cameraRig != null && cameraRig.IsFirstPerson) return;
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        Vector3 targetPosition;
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            targetPosition = hit.point;
        }
        else
        {
            // Rendering layers must not prevent aiming. Fall back to a horizontal
            // plane through the player's feet when the Ground ray finds nothing.
            Plane aimPlane = new Plane(Vector3.up, transform.position);
            if (!aimPlane.Raycast(ray, out float distance)) return;
            targetPosition = ray.GetPoint(distance);
        }
        targetPosition.y = transform.position.y;
        lookDirection = (targetPosition - transform.position).normalized;
        if (lookDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    void HandleMovement()
    {
        // 1. Ruch względem widoku kamery
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 worldMove = camRight * moveInput.x + camForward * moveInput.y;
        controller.SimpleMove(Vector3.ClampMagnitude(worldMove, 1f) * CurrentMoveSpeed);

        // 2. Przeliczamy kierunek ruchu na lokalną przestrzeń postaci (do Blend Tree Strafe!)
        if (animator != null)
        {
            Vector3 localMove = transform.InverseTransformDirection(worldMove);
            animator.SetFloat("MoveX", localMove.x, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveZ", localMove.z, 0.1f, Time.deltaTime);
        }
    }

}

