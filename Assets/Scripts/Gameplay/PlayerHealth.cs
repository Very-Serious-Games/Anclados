using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public float damageFromCannonball = 25f;

    private bool isSinking = false;
    private Rigidbody rb;
    private PlayerMovement movementScript;

    public float sinkSpeed = 0.5f;
    public float tiltSpeed = 20f;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        movementScript = GetComponent<PlayerMovement>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("CannonBall"))
        {
            Debug.Log("IMPACTO: Bala de cañón detectada");
            TakeDamage(damageFromCannonball);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isSinking) return;

        currentHealth -= amount;
        Debug.Log("Barco recibió daño. Vida actual: " + currentHealth);

        if (currentHealth <= 0)
        {
            StartSinking();
        }
    }

    void StartSinking()
    {
        Debug.Log("El barco se está hundiendo...");
        isSinking = true;

        if (movementScript != null)
            movementScript.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.freezeRotation = true;
        }
    }

    void Update()
    {
        if (isSinking)
        {
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                Quaternion.Euler(25f, transform.localEulerAngles.y, 0f),
                Time.deltaTime * 0.5f
            );

            transform.position = Vector3.Lerp(
                transform.position,
                transform.position - new Vector3(0, 5f, 0),
                Time.deltaTime * 0.2f
            );
        }
    }
}