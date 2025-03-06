using System;
using TMPro;
using UnityEngine;

public class AbilityView : MonoBehaviour
{
    [SerializeField] private AbilityButton abilityButton;
    [SerializeField] private TextMeshProUGUI descriptionTMP;

    internal void Show(AbilitySO ability)
    {
        abilityButton.SetButton(ability.Icon, ability.AbilityName);
        descriptionTMP.text = ability.Desc;

        gameObject.SetActive(true);
    }
}
