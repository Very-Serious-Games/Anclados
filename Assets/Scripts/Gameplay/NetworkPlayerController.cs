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
    private float networkUpdateRate = 0.05f; // 20Hz
    private float timeSinceLastNetworkUpdate = 0f;

    [Header("Movement")]
    public float acceleration = 8f;
    public float maxSpeed = 12f;
    public float reverseSpeed = 4f;
    public float dragWater = 0.8f;

    [Header("Anchor")]
    public KeyCode anchorKey = KeyCode.F;
    public float anchorDropTime = 2f;
    public float anchorLiftTime = 2f;
    public float anchorExtraDrag = 10f;

    [Header("Rudder")]
    public float rudderMaxAngle = 35f;
    public float rudderChangeSpeed = 40f;
    public float rudderReturnSpeed = 15f;
    private float rudderAngle = 0f;

    [Header("Reconciliation")]
    public float positionThreshold = 0.5f; // Accept small deviations, blend larger ones
    public float rotationThreshold = 5.0f;
    public float interpolationSpeed = 10f; // How fast to lerp to server position
    public float localCorrectionSpeed = 8f; // Speed for local player corrections

    [Header("Cannons")]
    public Transform cannonLeft;
    public Transform cannonRight;
    public KeyCode fireLeftKey = KeyCode.Z;
    public KeyCode fireRightKey = KeyCode.X;
    public float fireCooldown = 1.2f;
    private float nextFireLeft = 0f;
    private float nextFireRight = 0f;

    [Header("Behavior")]
    public float lateralDrag = 2f;
    public bool lockHeight = true;

    // State
    private Rigidbody rb;
    public bool anchorActive = false;
    private bool anchorChanging = false;

    // Interpolation (for both local corrections and remote players)
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 targetVelocity;
    private bool hasTargetState = false;
    private bool isLocalCorrection = false; // Use slower lerp for local corrections

    // Input state for server processing
    private struct PlayerInput
    {
        public bool forward, backward, turnLeft, turnRight;
        public bool anchorToggle, fireLeft, fireRight;
    }
    private PlayerInput currentInput;
    
    void Awake()
    {
        // Initialize Rigidbody early to avoid null reference
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (lockHeight)
            rb.constraints |= RigidbodyConstraints.FreezePositionY;
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            // Capture input every frame
            CaptureInput();
            
            // Accumulate time for fixed network update rate
            timeSinceLastNetworkUpdate += Time.deltaTime;
            
            // Send input to server at fixed 20Hz rate
            if (timeSinceLastNetworkUpdate >= networkUpdateRate)
            {
                SendInputToServer();
                timeSinceLastNetworkUpdate = 0f;
            }
            
            // Handle cannons locally (will validate on server)
            HandleCannons();
        }
        // Remote players don't process input
    }

    void FixedUpdate()
    {
        // Server processes physics for all players
        if (GameManager.Instance.connectionType == ConnectionType.Host)
        {
            ProcessPhysics();
        }
        // Clients: local player always processes physics (client prediction)
        else if (isLocalPlayer)
        {
            ProcessPhysics();
            
            // If we have a target state (server correction), blend toward it
            if (hasTargetState)
            {
                ApplyLocalCorrection();
            }
        }
        else if (hasTargetState)
        {
            // Remote players interpolate to server state
            InterpolateToTargetState();
        }
    }

    private void ApplyLocalCorrection()
    {
        // Gently blend toward server position without stopping physics
        float blendFactor = localCorrectionSpeed * Time.fixedDeltaTime;
        
        transform.position = Vector3.Lerp(transform.position, targetPosition, blendFactor);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blendFactor);
        
        // Check if correction is complete
        float dist = Vector3.Distance(transform.position, targetPosition);
        if (dist < 0.1f) // Larger threshold since physics is still running
        {
            hasTargetState = false;
            isLocalCorrection = false;
        }
    }

    private void InterpolateToTargetState()
    {
        // Remote players fully interpolate (no physics running)
        float speed = interpolationSpeed;
        
        // Smoothly interpolate position
        transform.position = Vector3.Lerp(transform.position, targetPosition, speed * Time.fixedDeltaTime);
        
        // Smoothly interpolate rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.fixedDeltaTime);
        
        // Smoothly interpolate velocity
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, speed * Time.fixedDeltaTime);
        
        // Check if we're close enough to the target
        float dist = Vector3.Distance(transform.position, targetPosition);
        if (dist < 0.01f)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            rb.linearVelocity = targetVelocity;
            hasTargetState = false;
        }
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
                       currentInput.anchorToggle || currentInput.fireLeft || currentInput.fireRight;
        
        if (!hasInput) return;

        PlayerInputMessage inputMsg = new PlayerInputMessage(
            playerId,
            currentInput.forward,
            currentInput.backward,
            currentInput.turnLeft,
            currentInput.turnRight,
            currentInput.anchorToggle,
            currentInput.fireLeft,
            currentInput.fireRight,
            Time.time,
            inputSequenceNumber++
        );

        GameManager.Instance.gameClient.Send(inputMsg);
        
        // Reset one-shot inputs
        currentInput.fireLeft = false;
        currentInput.fireRight = false;
    }

    private void ProcessPhysics()
    {
        if (!anchorActive && !anchorChanging)
        {
            ApplyForwardMovement(currentInput.forward, currentInput.backward);
            ApplyRotation(currentInput.turnLeft, currentInput.turnRight);
            ApplyWaterResistance();
            ApplyLateralDamping();
        }
        else
        {
            ApplyAnchorStop();
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

    private void ApplyAnchorStop()
    {
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 drag = -horizontalVel * anchorExtraDrag * Time.fixedDeltaTime;
        rb.AddForce(drag, ForceMode.VelocityChange);
    }

    private void ApplyForwardMovement(bool forward, bool backward)
    {
        float currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float targetSpeed = currentSpeed;

        if (forward)
        {
            targetSpeed = Mathf.Min(currentSpeed + acceleration * Time.fixedDeltaTime, maxSpeed);
        }
        else if (backward)
        {
            targetSpeed = Mathf.Max(currentSpeed - acceleration * Time.fixedDeltaTime, -reverseSpeed);
        }
        else
        {
            targetSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * 0.4f * Time.fixedDeltaTime);
        }

        float delta = targetSpeed - currentSpeed;
        Vector3 force = transform.forward * delta;
        force.y = 0;

        rb.AddForce(force, ForceMode.VelocityChange);
    }

    private void ApplyRotation(bool turnLeft, bool turnRight)
    {
        float input = 0f;

        if (turnRight) input += 1f;
        if (turnLeft) input -= 1f;

        if (input != 0)
        {
            rudderAngle += input * rudderChangeSpeed * Time.fixedDeltaTime;
        }
        else
        {
            rudderAngle = Mathf.MoveTowards(rudderAngle, 0f, rudderReturnSpeed * Time.fixedDeltaTime);
        }

        rudderAngle = Mathf.Clamp(rudderAngle, -rudderMaxAngle, rudderMaxAngle);

        float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
        float turnAmount = rudderAngle * speedFactor * Time.fixedDeltaTime;

        Quaternion deltaRot = Quaternion.Euler(0f, turnAmount, 0f);
        rb.MoveRotation(rb.rotation * deltaRot);
    }

    private void ApplyWaterResistance()
    {
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 drag = -horizontalVel * dragWater * Time.fixedDeltaTime;
        rb.AddForce(drag, ForceMode.VelocityChange);
    }

    private void ApplyLateralDamping()
    {
        Vector3 lateral = Vector3.Dot(rb.linearVelocity, transform.right) * transform.right;
        Vector3 force = -lateral * lateralDrag * Time.fixedDeltaTime;
        rb.AddForce(force, ForceMode.VelocityChange);
    }

    private void HandleCannons()
    {
        if (Input.GetKeyDown(fireLeftKey) && Time.time >= nextFireLeft)
        {
            currentInput.fireLeft = true;
            RequestFireCannon(true);
            nextFireLeft = Time.time + fireCooldown;
        }

        if (Input.GetKeyDown(fireRightKey) && Time.time >= nextFireRight)
        {
            currentInput.fireRight = true;
            RequestFireCannon(false);
            nextFireRight = Time.time + fireCooldown;
        }
    }

    private void RequestFireCannon(bool isLeft)
    {
        if (GameManager.Instance.gameClient == null) return;

        Transform cannon = isLeft ? cannonLeft : cannonRight;
        if (cannon == null) return;

        FireCannonMessage fireMsg = new FireCannonMessage(
            playerId,
            isLeft,
            cannon.position,
            cannon.forward,
            Time.time
        );

        GameManager.Instance.gameClient.Send(fireMsg);
    }

    /// <summary>
    /// Server applies input received from client for physics processing
    /// </summary>
    public void ApplyInputFromServer(PlayerInputMessage input)
    {
        Debug.Log($"[NetworkPlayerController - SERVER] Player {playerId} received input - F:{input.forward} B:{input.backward} TL:{input.turnLeft} TR:{input.turnRight}");
        
        // Update current input state from message
        currentInput.forward = input.forward;
        currentInput.backward = input.backward;
        currentInput.turnLeft = input.turnLeft;
        currentInput.turnRight = input.turnRight;
        currentInput.anchorToggle = input.anchorToggle;
        
        // Handle anchor toggle
        if (input.anchorToggle && !anchorChanging)
        {
            StartCoroutine(ToggleAnchor());
        }
    }

    /// <summary>
    /// Apply state received from server (for remote players or reconciliation)
    /// </summary>
    public void ApplyNetworkState(PlayerStateMessage state)
    {
        if (isLocalPlayer)
        {
            // Client-side reconciliation for local player
            float dist = Vector3.Distance(transform.position, state.position);
            float angleDiff = Quaternion.Angle(transform.rotation, state.rotation);

            // Always apply server state, but intensity varies with deviation
            if (dist > 0.05f || angleDiff > 1.0f) // Very small threshold for continuous correction
            {
                // Use smooth interpolation for local player corrections
                targetPosition = state.position;
                targetRotation = state.rotation;
                targetVelocity = state.velocity;
                hasTargetState = true;
                isLocalCorrection = true;
                
                if (dist > positionThreshold || angleDiff > rotationThreshold)
                {
                    Debug.LogWarning($"[NetworkPlayerController] LOCAL player {playerId} large correction. Deviation: {dist:F2}m, {angleDiff:F1}°");
                }
            }
        }
        else
        {
            // Remote player - use interpolation for smooth movement
            Debug.Log($"[NetworkPlayerController] REMOTE player {playerId} setting target state - Pos:{state.position}");
            
            targetPosition = state.position;
            targetRotation = state.rotation;
            targetVelocity = state.velocity;
            hasTargetState = true;
            isLocalCorrection = false;
        }
        
        anchorActive = state.anchorActive;
    }

    /// <summary>
    /// Get current state for server broadcasting
    /// </summary>
    public PlayerStateMessage GetCurrentState(int lastProcessedInputSeq)
    {
        // Ensure Rigidbody is initialized
        if (rb == null)
            rb = GetComponent<Rigidbody>();
            
        Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
        
        return new PlayerStateMessage(
            playerId,
            transform.position,
            transform.rotation,
            velocity,
            anchorActive,
            Time.time,
            lastProcessedInputSeq
        );
    }
}
