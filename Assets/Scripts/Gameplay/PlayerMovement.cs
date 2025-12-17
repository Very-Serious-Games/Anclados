using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
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

    [Header("Cannons")]
    public Transform cannonLeft;
    public Transform cannonRight;
    public GameObject cannonballPrefab;
    public float cannonballSpeed = 40f;
    public float cannonballLifetime = 5f;
    public float fireCooldown = 1.2f;
    public float recoilForce = 200f;

    private float nextFireLeft = 0f;
    private float nextFireRight = 0f;


    [Header("Behavior")]
    public float lateralDrag = 2f;
    public bool lockHeight = true;

    private Rigidbody rb;
    private bool anchorActive = false;
    private bool anchorChanging = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = false;

        if (lockHeight)
            rb.constraints |= RigidbodyConstraints.FreezePositionY;

    }

    void Update()
    {
        if (Input.GetKeyDown(anchorKey) && !anchorChanging)
        {
            StartCoroutine(ToggleAnchor());
        }
        HandleCannons();
    }

    void FixedUpdate()
    {
        if (!anchorActive && !anchorChanging)
        {
            ApplyForwardMovement();
            ApplyRotation();
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

    private void ApplyForwardMovement()
    {
        bool forward = Input.GetKey(KeyCode.W);
        bool backward = Input.GetKey(KeyCode.S);

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

    private void ApplyRotation()
{
    float input = 0f;

    if (Input.GetKey(KeyCode.E)) input += 1f;
    if (Input.GetKey(KeyCode.Q)) input -= 1f;

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
    // Cañón izquierdo (tecla Z)
    if (Input.GetKeyDown(KeyCode.Z) && Time.time >= nextFireLeft)
    {
        FireCannon(cannonLeft, true);
        nextFireLeft = Time.time + fireCooldown;
    }

    // Cañón derecho (tecla X)
    if (Input.GetKeyDown(KeyCode.X) && Time.time >= nextFireRight)
    {
        FireCannon(cannonRight, false);
        nextFireRight = Time.time + fireCooldown;
    }
}


    private void FireCannon(Transform cannon, bool isLeft)
{
    if (cannonballPrefab == null || cannon == null)
        return;

    GameObject ball = Instantiate(cannonballPrefab, cannon.position, cannon.rotation);

    Rigidbody ballRb = ball.GetComponent<Rigidbody>();

    if (ballRb != null)
    {
        ballRb.linearVelocity = cannon.forward * cannonballSpeed;

        Vector3 recoil = -cannon.forward * recoilForce;
        rb.AddForce(recoil, ForceMode.Impulse);
    }

    Destroy(ball, cannonballLifetime);
}

}