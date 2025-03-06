using System;
using TMPro;
using UnityEngine;

public class AbilityView : MonoBehaviour
{
    [SerializeField] private AbilityButton abilityButton;
    [SerializeField] private TextMeshProUGUI descriptionTMP;

    internal void Hide()
    {
        gameObject.SetActive(false);
    }

    internal void Show(AbilitySO ability)
    {
        descriptionTMP.text = ability.Desc;

        gameObject.SetActive(true);
    }
}
