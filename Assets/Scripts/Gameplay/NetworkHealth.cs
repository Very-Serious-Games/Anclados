using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Network-synchronized health component. Server authoritative damage.
/// </summary>
public class NetworkHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    
    [Header("Network")]
    public bool isLocalPlayer = false;
    public int playerId = -1;

    [Header("UI (Optional)")]
    public Slider healthBar;

    private NetworkPlayer networkPlayer;

    void Awake()
    {
        networkPlayer = GetComponent<NetworkPlayer>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        // Subscribe to damage messages
        if (GameManager.Instance.gameClient != null)
        {
            GameManager.Instance.gameClient.OnMessageReceived += HandleMessage;
        }

        if (GameManager.Instance.gameServer != null)
        {
            GameManager.Instance.gameServer.OnMessageReceived += HandleServerMessage;
        }

        UpdateHealthUI();
    }

    void OnDestroy()
    {
        if (GameManager.Instance.gameClient != null)
        {
            GameManager.Instance.gameClient.OnMessageReceived -= HandleMessage;
        }

        if (GameManager.Instance.gameServer != null)
        {
            GameManager.Instance.gameServer.OnMessageReceived -= HandleServerMessage;
        }
    }

    private void HandleMessage(INetworkMessage message)
    {
        if (message is DamageMessage damageMsg)
        {
            if (damageMsg.targetId == playerId)
            {
                ApplyDamage(damageMsg.damage, damageMsg.hitPosition);
            }
        }
    }

    private void HandleServerMessage(Peer peer, INetworkMessage message)
    {
        // Server processes damage requests here
        // For now, damage is applied directly
    }

    /// <summary>
    /// Apply damage locally (called from network message)
    /// </summary>
    public void ApplyDamage(float damage, Vector3 hitPosition)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"[NetworkHealth] Player {playerId} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Request damage to be applied (server validates and broadcasts)
    /// </summary>
    public void RequestDamage(int targetPlayerId, float damage, Vector3 hitPosition)
    {
        if (GameManager.Instance.gameClient == null) return;

        DamageMessage damageMsg = new DamageMessage(
            playerId,
            targetPlayerId,
            damage,
            hitPosition,
            Time.time
        );

        GameManager.Instance.gameClient.Send(damageMsg);
    }

    private void Die()
    {
        Debug.Log($"[NetworkHealth] Player {playerId} died!");

        // TODO: Death animation, respawn logic
        if (isLocalPlayer)
        {
            // Handle local player death
            Debug.Log("[NetworkHealth] Local player died - implement respawn");
        }
    }

    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
    }

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Detect cannonball hits (only on server or local player)
        if (!isLocalPlayer && GameManager.Instance.connectionType != ConnectionType.Host)
            return;

        if (collision.gameObject.CompareTag("CannonBall"))
        {
            // Get cannonball owner ID (would need to be stored on cannonball)
            // For now, just apply damage
            float damage = 25f;
            Vector3 hitPos = collision.contacts[0].point;

            if (GameManager.Instance.connectionType == ConnectionType.Host)
            {
                // Server: Apply damage and broadcast
                ApplyDamage(damage, hitPos);

                DamageMessage damageMsg = new DamageMessage(
                    -1, // Attacker ID (from cannonball)
                    playerId,
                    damage,
                    hitPos,
                    Time.time
                );
                GameManager.Instance.gameServer?.Broadcast(damageMsg);
            }
            else if (isLocalPlayer)
            {
                // Client: Request damage from server
                RequestDamage(playerId, damage, hitPos);
            }
        }
    }
}
