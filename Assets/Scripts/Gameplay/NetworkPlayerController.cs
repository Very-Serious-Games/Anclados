using UnityEngine;
using System.Collections;

/// <summary>
/// Network-aware player controller. Handles input/state split for multiplayer.
/// Local players send inputs to server, remote players apply received state.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class NetworkPlayerController : MonoBehaviour
{
    [Header("Network")]
    public bool isLocalPlayer = false;
    public int playerId = -1;
    private int inputSequenceNumber = 0;

    [Header("Movement")]
    public float moveSpeed = 18f;
    public float acceleration = 14f;
    public float deceleration = 12f;

    [Header("Rudder")]
    public float rudderMaxAngle = 35f;
    public float rudderChangeSpeed = 60f;
    public float rudderReturnSpeed = 35f;
    public float turnSpeed = 90f;
    private float rudderAngle;

    [Header("Anchor")]
    public KeyCode anchorKey = KeyCode.F;
    public float anchorDropTime = 1.5f;
    public float anchorLiftTime = 1.5f;

    [Header("Reconciliation")]
    public float positionThreshold = 3f;
    public float rotationThreshold = 15f;

    [Header("Behavior")]
    public float lateralDrag = 2f;
    public bool lockHeight = true;

    // State
    private Rigidbody rb;
    public bool anchorActive = false;
    private bool anchorChanging = false;

    // Input state for server processing
    private struct PlayerInput
    {
        public bool forward, backward, turnLeft, turnRight;
        public bool anchorToggle;
    }

    private PlayerInput currentInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = false;

        if (lockHeight)
            rb.constraints |= RigidbodyConstraints.FreezePositionY;
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            // Capture input
            CaptureInput();
            // Send input to server
            SendInputToServer();
        }
    }

    void FixedUpdate()
    {
        // Server processes physics for all players
        if (GameManager.Instance.connectionType == ConnectionType.Host)
        {
            ProcessPhysics();
        }
        // Clients: only local player processes physics (client prediction)
        else if (isLocalPlayer)
        {
            ProcessPhysics();
        }
        // Remote players on clients apply received state in ApplyNetworkState()
    }

    private void CaptureInput()
    {
        currentInput.forward = Input.GetKey(KeyCode.W);
        currentInput.backward = Input.GetKey(KeyCode.S);
        currentInput.turnLeft = Input.GetKey(KeyCode.A);
        currentInput.turnRight = Input.GetKey(KeyCode.D);

        if (Input.GetKeyDown(anchorKey) && !anchorChanging)
        {
            currentInput.anchorToggle = true;
            StartCoroutine(ToggleAnchor());
        }
        else
        {
            currentInput.anchorToggle = false;
        }
    }

    private void SendInputToServer()
    {
        if (GameManager.Instance.gameClient == null) return;

        // Only send if there's actual input
        bool hasInput = currentInput.forward || currentInput.backward || 
                       currentInput.turnLeft || currentInput.turnRight || 
                       currentInput.anchorToggle;

        if (!hasInput) return;

        PlayerInputMessage inputMsg = new PlayerInputMessage(
            playerId,
            currentInput.forward,
            currentInput.backward,
            currentInput.turnLeft,
            currentInput.turnRight,
            currentInput.anchorToggle,
            Time.time,
            inputSequenceNumber++
        );

        Debug.Log($"[NetworkPlayerController] Sending input to server - Forward:{currentInput.forward} Backward:{currentInput.backward}");
        GameManager.Instance.gameClient.Send(inputMsg);
    }

    private void ProcessPhysics()
    {
        if (!anchorActive && !anchorChanging)
        {
            HandleMovement();
            HandleRudder();
            HandleRotation();
            ApplyLateralDamping();
        }
    }

    IEnumerator ToggleAnchor()
    {
        anchorChanging = true;

        if (!anchorActive)
        {
            yield return new WaitForSeconds(anchorDropTime);
            anchorActive = true;
        }
        else
        {
            yield return new WaitForSeconds(anchorLiftTime);
            anchorActive = false;
        }

        anchorChanging = false;
    }

    private void HandleMovement()
    {
        float v = 0f;
        if (currentInput.forward) v += 1f;
        if (currentInput.backward) v -= 1f;

        Vector3 desiredVelocity = transform.forward * v * moveSpeed;
        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float accel = Mathf.Abs(v) > 0.01f ? acceleration : deceleration;

        Vector3 newVelocity = Vector3.MoveTowards(
            currentVelocity,
            desiredVelocity,
            accel * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(newVelocity.x, rb.linearVelocity.y, newVelocity.z);
    }

    private void HandleRudder()
    {
        float h = 0f;
        if (currentInput.turnRight) h += 1f;
        if (currentInput.turnLeft) h -= 1f;

        if (Mathf.Abs(h) > 0.01f)
            rudderAngle += h * rudderChangeSpeed * Time.fixedDeltaTime;
        else
            rudderAngle = Mathf.MoveTowards(rudderAngle, 0f, rudderReturnSpeed * Time.fixedDeltaTime);

        rudderAngle = Mathf.Clamp(rudderAngle, -rudderMaxAngle, rudderMaxAngle);
    }

    private void HandleRotation()
    {
        float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / moveSpeed);
        float rudderNormalized = rudderAngle / rudderMaxAngle;

        float turnAmount = rudderNormalized * turnSpeed * speedFactor * Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount, 0f));
    }

    private void ApplyLateralDamping()
    {
        Vector3 lateral = Vector3.Dot(rb.linearVelocity, transform.right) * transform.right;
        Vector3 force = -lateral * lateralDrag * Time.fixedDeltaTime;
        rb.AddForce(force, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Server applies input received from client for physics processing
    /// </summary>
    public void ApplyInputFromServer(PlayerInputMessage input)
    {
        // Update current input state from message
        currentInput.forward = input.forward;
        currentInput.backward = input.backward;
        currentInput.turnLeft = input.turnLeft;
        currentInput.turnRight = input.turnRight;
        currentInput.anchorToggle = input.anchorToggle;

        // Handle anchor toggle
        if (input.anchorToggle && !anchorChanging)
            StartCoroutine(ToggleAnchor());
    }

    /// <summary>
    /// Apply state received from server (for remote players or reconciliation)
    /// </summary>
    public void ApplyNetworkState(PlayerStateMessage state)
    {
        if (isLocalPlayer)
        {
            // Client-side reconciliation
            float dist = Vector3.Distance(transform.position, state.position);
            float angleDiff = Quaternion.Angle(transform.rotation, state.rotation);

            if (dist < positionThreshold && angleDiff < rotationThreshold)
            {
                return;
            }
            
            Debug.LogWarning($"[NetworkPlayerController] Corrección necesaria. Desviación: {dist:F2}m");
        }

        // Apply position/rotation
        transform.position = state.position;
        transform.rotation = state.rotation;
        rb.linearVelocity = state.velocity;
        anchorActive = state.anchorActive;
    }

    /// <summary>
    /// Get current state for server broadcasting
    /// </summary>
    public PlayerStateMessage GetCurrentState(int lastProcessedInputSeq)
    {
        return new PlayerStateMessage(
            playerId,
            transform.position,
            transform.rotation,
            rb.linearVelocity,
            anchorActive,
            Time.time,
            lastProcessedInputSeq
        );
    }
}