using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public Transform target;      // El barco
    public Vector3 offset = new Vector3(0, 10f, -15f);
    public float followSpeed = 3f;
    public float rotateSpeed = 2f;
    public float smoothTime = 0.3f;
    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + target.TransformDirection(offset);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);

        transform.LookAt(target);
    }
}