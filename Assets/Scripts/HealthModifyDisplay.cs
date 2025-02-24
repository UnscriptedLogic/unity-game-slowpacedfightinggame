using TMPro;
using UnityEngine;
using UnscriptedEngine;

public class HealthModifyDisplay : MonoBehaviour
{
    [SerializeField] private Color damageColor;
    [SerializeField] private Color healColor;
    [SerializeField] private TextMeshProUGUI counterTMP;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float upwardForce = 4f;

    public void Initialize(float value, bool isDamage = true)
    {
        counterTMP.text = value.ToString();
        counterTMP.color = isDamage ? damageColor : healColor;

        rb.velocity = new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f)) * upwardForce;

        Destroy(gameObject, 1.5f);
    }

    private void Update()
    {
        transform.forward = Camera.main.transform.forward;
    }
}