using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityButton : MonoBehaviour
{
    [SerializeField] private Image iconImg;
    [SerializeField] private TextMeshProUGUI nameTMP;
    [SerializeField] private Button button;

    public void SetButton(Sprite icon, string name, Action OnClick = null)
    {
        iconImg.sprite = icon;
        nameTMP.text = name;

        button.onClick.RemoveAllListeners();

        if (OnClick != null)
            button.onClick.AddListener(() => OnClick());
    }
}
