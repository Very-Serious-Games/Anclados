using UnityEngine;

public class TreasureIndicator : MonoBehaviour
{
    [Header("Settings")]
    public GameObject treasurePrefab;
    public float heightAbovePlayer = 3f;
    public bool hasTreasure = false;
    
    [Header("Animation")]
    public float rotationSpeed = 90f;
    public float bobbingAmplitude = 0.5f;
    public float bobbingSpeed = 2f;

    private GameObject treasure;
    private float time;

    void Update()
    {
        time += Time.deltaTime;

        if (hasTreasure && treasure == null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * heightAbovePlayer;
            treasure = Instantiate(treasurePrefab, spawnPos, Quaternion.identity);
        }

        if (!hasTreasure && treasure != null)
        {
            Destroy(treasure);
        }

        if (treasure != null)
        {
            treasure.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            float bobbing = Mathf.Sin(time * bobbingSpeed) * bobbingAmplitude;
            Vector3 targetPos = transform.position + Vector3.up * (heightAbovePlayer + bobbing);
            treasure.transform.position = targetPos;
        }
    }
}