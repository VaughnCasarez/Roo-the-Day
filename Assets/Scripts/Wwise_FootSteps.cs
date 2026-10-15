using Unity.XR.CoreUtils;
using UnityEngine;

[RequireComponent(typeof(AkGameObj), typeof(CharacterController))]
public class XRFootsteps : MonoBehaviour
{
    [Header("Wwise")]
    [SerializeField] private AK.Wwise.Event footstepEvent;
    [SerializeField] private AK.Wwise.Switch defaultSurface;

    [Header("Footstep Settings")]
    [Tooltip("Track this object's world velocity. Defaults to the XR Origin camera to include tracked headset movement.")]
    [SerializeField] private Transform movementSource;
    [SerializeField, Min(0.05f)] private float stepInterval = 0.45f;
    [SerializeField, Min(0.01f)] private float minimumMoveSpeed = 0.1f;
    [Tooltip("Ignore faster movement caused by teleporting or recentering (metres per second).")]
    [SerializeField, Min(0.1f)] private float maximumMoveSpeed = 12f;
    [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;
    [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.3f;

    private CharacterController character;
    private readonly RaycastHit[] groundHits = new RaycastHit[8];
    private Vector3 previousPosition;
    private float stepTimer;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        if (movementSource == null)
        {
            var origin = GetComponentInParent<XROrigin>();
            movementSource = origin != null && origin.Camera != null ? origin.Camera.transform : transform;
        }
    }

    private void OnEnable()
    {
        ResetFootsteps();
    }

    private void Start()
    {
        if (footstepEvent == null || !footstepEvent.IsValid())
        {
            Debug.LogError("Assign Footsteps_SFX to XRFootsteps.", this);
            enabled = false;
            return;
        }

        if (defaultSurface != null && defaultSurface.IsValid())
            defaultSurface.SetValue(gameObject);
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        Vector3 position = movementSource.position;
        Vector3 displacement = position - previousPosition;
        previousPosition = position;
        displacement.y = 0f;

        // World-space camera velocity includes both locomotion and room-scale /
        // simulator headset translation. Controller.velocity misses the latter.
        Vector3 velocity = deltaTime > 0f ? displacement / deltaTime : Vector3.zero;
        float speedSquared = velocity.sqrMagnitude;

        bool isMoving =
            deltaTime > 0f &&
            character.enabled &&
            speedSquared > minimumMoveSpeed * minimumMoveSpeed &&
            speedSquared <= maximumMoveSpeed * maximumMoveSpeed &&
            IsOnGround(position);

        if (!isMoving)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= deltaTime;

        if (stepTimer <= 0f)
        {
            footstepEvent.Post(gameObject);
            stepTimer = stepInterval;
        }
    }

    private bool IsOnGround(Vector3 position)
    {
        // isGrounded is updated by CharacterController.Move. Tracked headset
        // movement doesn't call Move, so also probe below the tracked player.
        Vector3 feet = position;
        feet.y = character.bounds.min.y;
        int hitCount = Physics.RaycastNonAlloc(feet + Vector3.up * groundCheckDistance,
            Vector3.down, groundHits, groundCheckDistance * 2f, groundLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            if (!groundHits[i].collider.transform.IsChildOf(transform))
                return true;
        }

        // A capsule can be supported at its edge even when the ray misses.
        Vector3 bodyOffset = position - character.bounds.center;
        bodyOffset.y = 0f;
        return bodyOffset.sqrMagnitude <= character.radius * character.radius && character.isGrounded;
    }

    public void ResetFootsteps()
    {
        previousPosition = movementSource != null ? movementSource.position : transform.position;
        stepTimer = 0f;
    }
}
