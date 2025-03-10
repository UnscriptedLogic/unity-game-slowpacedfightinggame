using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnscriptedEngine;

[System.Serializable]
public class DamageSettings : INetworkSerializable
{
    public float damage;
    public Vector3 kbDir;
    public float kbForce;
    public float kbDuration;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref damage);
        serializer.SerializeValue(ref kbDir);
        serializer.SerializeValue(ref kbForce);
        serializer.SerializeValue(ref kbDuration);
    }
}

[System.Serializable]
public class HealthSettings : INetworkSerializable
{
    public float maxHealth;
    public float health;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref maxHealth);
        serializer.SerializeValue(ref health);
    }
}

public class PlayerHealthComponent : PlayerBaseComponent
{
    [SerializeField] private Renderer playerRenderer;
    [SerializeField] private Material damageMaterial;
    [SerializeField] private HealthModifyDisplay healthModifyDisplayPrefab;
    [SerializeField] private UIC_HealthBarUI healthBarUIPrefab;
    [SerializeField] private GameObject ragdollPrefab;

    private PlayerStateComponent stateComponent;

    private NetworkVariable<float> maxHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> health = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Rigidbody rb;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        health.OnValueChanged += OnHealthChanged;
        maxHealth.OnValueChanged += OnMaxHealthChanged;

        stateComponent = GetComponentInChildren<PlayerStateComponent>();
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= OnHealthChanged;
        maxHealth.OnValueChanged -= OnMaxHealthChanged;
        base.OnNetworkDespawn();
    }

    private void OnMaxHealthChanged(float previousValue, float newValue){ }

    private void OnHealthChanged(float previousValue, float newValue)
    {
        HealthModifyDisplay healthModifyDisplay = Instantiate(healthModifyDisplayPrefab, transform.position, Quaternion.identity);

        healthModifyDisplay.Initialize(newValue - previousValue, newValue < previousValue);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (IsOwner)
        {
            context.AttachUIWidget(healthBarUIPrefab).Initialize(health, UIC_HealthBarUI.VisualType.Self);
        }
        else
        {
            transform.GetComponent<P_DefaultPlayerPawn>().AttachUIWidget(healthBarUIPrefab).Initialize(health, UIC_HealthBarUI.VisualType.Enemy);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetHealthServerRpc(HealthSettings settings)
    {
        maxHealth.Value = settings.maxHealth;
        health.Value = settings.health;
    }

    public void Server_TakeDamage(DamageSettings damageSettings)
    {
        if (stateComponent.HasStatusEffect(StatusEffect.Type.Invincible)) return;

        health.Value -= damageSettings.damage;

        if (health.Value <= 0)
        {
            health.Value = 0;
            Debug.Log("player died");
            PlayerDiedClientRpc(damageSettings);
            Die();
            return;
        }

        ApplyKnockback(damageSettings);
        TakeDamageClientRpc(damageSettings);
    }

    [ClientRpc]
    private void PlayerDiedClientRpc(DamageSettings damageSettings)
    {
        GameObject ragdoll = Instantiate(ragdollPrefab, transform.position, transform.rotation);
        ragdoll.GetComponentInChildren<Rigidbody>().AddForce(damageSettings.kbDir * (damageSettings.kbForce * 3f));
        Destroy(ragdoll, 10f);

        if (!IsOwner) return;

        UGameModeBase.instance.GetPlayerController().UnPossessPawn();
        Debug.Log("unpossessed");
    }

    private void Die()
    {
        GetComponent<NetworkObject>().Despawn(true);
        PlayerEvents.PlayerDiedEvent(OwnerClientId);
        Debug.Log("Player died: " + OwnerClientId);
    }

    [ClientRpc]
    private void TakeDamageClientRpc(DamageSettings damageSettings)
    {
        StartCoroutine(FlashDamage());

        if (!IsOwner) return;

        ApplyKnockback(damageSettings);
    }

    private void ApplyKnockback(DamageSettings damageSettings)
    {
        rb.AddForce(damageSettings.kbDir * damageSettings.kbForce, ForceMode.Impulse);

        if (damageSettings.kbDuration > 0)
        {
            Invoke(nameof(ResetKnockback), damageSettings.kbDuration);
        }
    }

    private void ResetKnockback()
    {
        rb.velocity = Vector3.zero;
    }

    private IEnumerator FlashDamage()
    {
        Material original = playerRenderer.material;
        playerRenderer.material = damageMaterial;
        yield return new WaitForSeconds(0.1f);
        playerRenderer.material = original;
    }
}