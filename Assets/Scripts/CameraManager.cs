using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Transform target;      // El barco
    // Por defecto la cámara estará centrada sobre el barco (misma X,Z) y más alta
    public Vector3 offset = new Vector3(0, 0, 0);
    public float followSpeed = 3f;
    public float rotateSpeed = 2f;
    public float smoothTime = 0.3f;
    private Vector3 velocity = Vector3.zero;
    public bool isIsometric = true;
    public float isoAngleX = 60f; // ángulo sobre X (inclinación). Valores mayores = vista más desde arriba
    public float isoAngleY = 45f; // ángulo sobre Y (rotación hacia el mundo)
    public float orthoSize = 45f;
    private Camera cam;

    void LateUpdate()
    {
        if (target == null) return;

        // Calcula la posición objetivo en espacio mundial (no rotada por el target)
        // Aseguramos explícitamente que X,Z se centren en el target y la altura se tome desde offset.y
        Vector3 targetPos = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            target.position.z + offset.z
        );
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);

        if (isIsometric)
        {
            // Mantener una rotación fija isométrica (no depende de la rotación del barco)
            transform.rotation = Quaternion.Euler(isoAngleX, isoAngleY, 0f);
            // Asegurar que la cámara esté en modo ortográfico
            if (cam != null && !cam.orthographic)
            {
                cam.orthographic = true;
                cam.orthographicSize = orthoSize;
            }
        }
        else
        {
            // Comportamiento original: mirar al target
            transform.LookAt(target);
            if (cam != null && cam.orthographic)
            {
                cam.orthographic = false;
            }
        }
    }

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        // Si queremos isométrica desde el inicio, aplicar ajustes
        if (isIsometric && cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            transform.rotation = Quaternion.Euler(isoAngleX, isoAngleY, 0f);
        }
    }
}