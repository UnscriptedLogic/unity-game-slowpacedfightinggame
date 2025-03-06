using System;
using TMPro;
using UnityEngine;

public class AbilityView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;

    internal void Hide()
    {
        gameObject.SetActive(false);
    }

    internal void Show(AbilitySO ability)
    {
        titleTMP.text = ability.AbilityName;
        descriptionTMP.text = ability.Desc;

        gameObject.SetActive(true);
    }
}
