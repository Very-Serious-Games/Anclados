using UnityEngine;

public class BoatFloat : MonoBehaviour
{
    [Header("Balanceo")]
    public float rotationAmount = 2f;       // grados máximos de inclinación
    public float rotationSpeed = 1f;        // velocidad del balanceo

    [Header("Altura de flotación")]
    public float floatAmplitude = 0.1f;     // cuánto sube/baja el barco
    public float floatSpeed = 1f;           // velocidad de la oscilación vertical

    private float initialY;

    void Start()
    {
        initialY = transform.localPosition.y;
    }

    void Update()
    {
        // Movimiento vertical (flotación suave)
        float newY = initialY + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        // Balanceo en los ejes Z (roll) y X (pitch)
        float rotationZ = Mathf.Sin(Time.time * rotationSpeed) * rotationAmount;
        float rotationX = Mathf.Cos(Time.time * rotationSpeed * 0.7f) * rotationAmount;

        // Aplicar transformaciones
        transform.localPosition = new Vector3(
            transform.localPosition.x,
            newY,
            transform.localPosition.z
        );

        transform.localRotation = Quaternion.Euler(rotationX, transform.localRotation.y, rotationZ);
    }
}
