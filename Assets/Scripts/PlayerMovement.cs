using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float acceleration = 20f;
    public float maxSpeed = 5f;
    public float rotationSpeed = 100f;
    public bool lockHeight = true;
    public float Drag = 1f; 

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (lockHeight)
        {
            rb.constraints |= RigidbodyConstraints.FreezePositionY;
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        bool pressW = Input.GetKey(KeyCode.W);
        bool pressS = Input.GetKey(KeyCode.S);

        float currentForwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        float desiredForwardSpeed = currentForwardSpeed;

        if (pressW)
        {
            desiredForwardSpeed = Mathf.Min(currentForwardSpeed + acceleration * Time.fixedDeltaTime, maxSpeed);
        }
        else if (pressS)
        {
            desiredForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, 0f, acceleration * Time.fixedDeltaTime);
        }

        float deltaSpeed = desiredForwardSpeed - currentForwardSpeed;
        Vector3 velocityChange = transform.forward * deltaSpeed;
        velocityChange.y = 0f;

        rb.AddForce(velocityChange, ForceMode.VelocityChange);

        Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 dragDelta = -currentHorizontal * Drag * Time.fixedDeltaTime;
        rb.AddForce(dragDelta, ForceMode.VelocityChange);
    }

    private void HandleRotation()
    {
        float rotInput = 0f;
        if (Input.GetKey(KeyCode.E)) rotInput += 1f;
        if (Input.GetKey(KeyCode.Q)) rotInput -= 1f;

        if (Mathf.Abs(rotInput) > 0f)
        {
            Quaternion deltaRot = Quaternion.Euler(0f, rotInput * rotationSpeed * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * deltaRot);
        }
    }
}