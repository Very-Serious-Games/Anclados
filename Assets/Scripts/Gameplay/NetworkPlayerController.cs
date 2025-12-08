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
    public float positionThreshold = 3.0f;
    public float rotationThreshold = 15.0f;

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

    // Input state for server processing
    private struct PlayerInput
    {
        public bool forward, backward, turnLeft, turnRight;
        public bool anchorToggle, fireLeft, fireRight;
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
        currentInput.turnLeft = Input.GetKey(KeyCode.Q);
        currentInput.turnRight = Input.GetKey(KeyCode.E);
        
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

        Debug.Log($"[NetworkPlayerController] Sending input to server - Forward:{currentInput.forward} Backward:{currentInput.backward}");
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
