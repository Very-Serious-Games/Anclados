using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 18f;
    public float acceleration = 30f;
    public float braking = 40f;

    [Header("Rudder")]
    public float rudderMaxAngle = 35f;
    public float rudderChangeSpeed = 60f;
    public float rudderReturnSpeed = 35f;
    public float turnSpeed = 90f;

    [Header("Anchor")]
    public KeyCode anchorKey = KeyCode.F;
    public float anchorDropTime = 1.5f;
    public float anchorLiftTime = 1.5f;

    [Header("Behavior")]
    public bool lockHeight = true;

    private Rigidbody rb;
    private bool anchorActive;
    private bool anchorChanging;
    private float rudderAngle;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (lockHeight)
            rb.constraints |= RigidbodyConstraints.FreezePositionY;
    }

    void Update()
    {
        if (Input.GetKeyDown(anchorKey) && !anchorChanging)
            StartCoroutine(ToggleAnchor());

        HandleRudderInput();
    }

    void FixedUpdate()
    {
        if (anchorActive || anchorChanging)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 6f);
            return;
        }

        HandleMovement();
        HandleRotation();
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
        float v = Input.GetAxisRaw("Vertical");

        Vector3 targetVelocity = transform.forward * v * moveSpeed;

        rb.linearVelocity = Vector3.MoveTowards(
            new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z),
            targetVelocity,
            acceleration * Time.fixedDeltaTime
        );
    }

    private void HandleRudderInput()
    {
        float h = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(h) > 0.01f)
            rudderAngle += h * rudderChangeSpeed * Time.deltaTime;
        else
            rudderAngle = Mathf.MoveTowards(rudderAngle, 0f, rudderReturnSpeed * Time.deltaTime);

        rudderAngle = Mathf.Clamp(rudderAngle, -rudderMaxAngle, rudderMaxAngle);
    }

    private void HandleRotation()
    {
        float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / moveSpeed);
        float rudderNormalized = rudderAngle / rudderMaxAngle;

        float turnAmount = rudderNormalized * turnSpeed * speedFactor * Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount, 0f));
    }
}