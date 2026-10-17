using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class SimpleTopDownCharacterController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Normal walking speed. Keep this lower for comfortable room movement.")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("Speed reached while holding Shift. This only works after the coffee machine unlock enables it.")]
    [SerializeField] private float shiftMoveSpeed = 10f;
    [Tooltip("How quickly the player accelerates toward the Shift speed.")]
    [SerializeField] private float acceleration = 18f;
    [Tooltip("How quickly the player returns to normal walking speed.")]
    [SerializeField] private float deceleration = 24f;
    [SerializeField] private float rotationSpeed = 720f; // degrees per second

    [Header("Walking Sound")]
    [Tooltip("Fallback sound used when a directional sound is not assigned.")]
    [SerializeField] private AudioClip walkSound;
    [SerializeField] private AudioClip leftSound;
    [SerializeField] private AudioClip rightSound;
    [SerializeField] private AudioClip topDownSound;
    [SerializeField] private AudioSource walkAudioSource;
    [SerializeField] [Range(0f, 1f)] private float stepVolume = 0.8f;
    [SerializeField] private Vector2 stepPitchRange = new Vector2(0.95f, 1.05f);

    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;
    private Vector2 moveInput;
    private float currentMoveSpeed;
    private bool shiftAccelerationUnlocked;

    private PlayerControls controls;

    public void setMovementSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
        if (!shiftAccelerationUnlocked)
        {
            currentMoveSpeed = moveSpeed;
        }
    }

    public void SetShiftAcceleration(bool unlocked, float speed)
    {
        shiftAccelerationUnlocked = unlocked;
        shiftMoveSpeed = Mathf.Max(moveSpeed, speed);
        if (!shiftAccelerationUnlocked)
        {
            currentMoveSpeed = moveSpeed;
        }
    }

    public void ConfigureMovement(float normalSpeed, bool shiftUnlocked, float acceleratedSpeed)
    {
        moveSpeed = Mathf.Max(0f, normalSpeed);
        shiftAccelerationUnlocked = shiftUnlocked;
        shiftMoveSpeed = Mathf.Max(moveSpeed, acceleratedSpeed);
        if (!shiftAccelerationUnlocked)
        {
            currentMoveSpeed = moveSpeed;
        }
    }

    private void Awake()
    {
        controls = new PlayerControls();

        controls.Player.Move.performed += context => moveInput = context.ReadValue<Vector2>();

        controls.Player.Move.canceled += context => moveInput = Vector2.zero;


        rb = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();
        currentMoveSpeed = moveSpeed;

        if (walkAudioSource == null)
        {
            walkAudioSource = gameObject.AddComponent<AudioSource>();
        }
        walkAudioSource.playOnAwake = false;
        walkAudioSource.loop = true;

        // Ensure 2D top-down behaviour
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }
    private void OnEnable() { controls.Player.Enable(); }

    private void OnDisable()
    {
        controls.Player.Disable();
        StopMovement();
        if (walkAudioSource != null)
        {
            walkAudioSource.Stop();
        }
    }

    private void Update()
    {
        UpdateWalkingSound();
    }

    private void UpdateWalkingSound()
    {
        bool isWalking = moveInput.sqrMagnitude > 0.0001f
            && GameController.GameState == GameState.Running
            && !MissionObjects.IsInMission;

        AudioClip directionalSound = GetDirectionalWalkSound();
        if (!isWalking || directionalSound == null || walkAudioSource == null)
        {
            if (walkAudioSource != null && walkAudioSource.isPlaying)
            {
                walkAudioSource.Stop();
            }
            return;
        }

        if (walkAudioSource.isPlaying && walkAudioSource.clip == directionalSound)
        {
            return;
        }

        walkAudioSource.Stop();
        walkAudioSource.clip = directionalSound;
        walkAudioSource.volume = stepVolume;
        float minimumPitch = Mathf.Min(stepPitchRange.x, stepPitchRange.y);
        float maximumPitch = Mathf.Max(stepPitchRange.x, stepPitchRange.y);
        walkAudioSource.pitch = Random.Range(minimumPitch, maximumPitch);
        walkAudioSource.Play();
    }

    private AudioClip GetDirectionalWalkSound()
    {
        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            if (moveInput.x < 0f)
            {
                return leftSound != null ? leftSound : walkSound;
            }

            if (moveInput.x > 0f)
            {
                return rightSound != null ? rightSound : walkSound;
            }
        }
        else if (Mathf.Abs(moveInput.y) > 0.0001f)
        {
            return topDownSound != null ? topDownSound : walkSound;
        }

        return walkSound;
    }
    private void FixedUpdate()
    {
        if (GameController.GameState == GameState.Stopped || MissionObjects.IsInMission)
        {
            StopMovement();
            return;
        }

        bool isTryingToMove = moveInput.sqrMagnitude > 0.0001f;
        bool shiftHeld = Keyboard.current != null
            && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        float targetSpeed = shiftAccelerationUnlocked && shiftHeld ? shiftMoveSpeed : moveSpeed;
        float speedChangeRate = targetSpeed > currentMoveSpeed ? acceleration : deceleration;
        currentMoveSpeed = Mathf.MoveTowards(
            currentMoveSpeed,
            targetSpeed,
            speedChangeRate * Time.fixedDeltaTime);

        Vector2 movementDirection = Vector2.ClampMagnitude(moveInput, 1f);

        // Move using Rigidbody2D for correct physics interactions
        rb.linearVelocity = movementDirection * currentMoveSpeed;

        // Rotate to face movement direction when moving
        if (isTryingToMove)
        {
            // Calculate target angle. Subtract 90 so the sprite's up points in movement direction
            float targetAngle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg - 90f;
            float newAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }
    }

    private void StopMovement()
    {
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        currentMoveSpeed = moveSpeed;
    }


    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<CircleCollider2D>();
        if (col == null)
            return;

        Gizmos.color = Color.yellow;
        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        float r = col.radius * scale;
        Gizmos.DrawWireSphere(transform.position, r);
    }
}
