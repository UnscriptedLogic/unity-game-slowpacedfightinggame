using InteractionSystem;
using TMPro;
using UnityEngine;

public class PopUpUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionTMP;

    private Interactable interactable;

    public void Initialize(Interactable interactable, string description)
    {
        this.interactable = interactable;
        descriptionTMP.text = $"'E' to {description}";
    }

    private void Update()
    {
        if (interactable == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Camera.main.WorldToScreenPoint(interactable.transform.position);
    }
}
