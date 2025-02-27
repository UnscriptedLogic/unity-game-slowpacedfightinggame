using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnscriptedEngine;

public class UIC_AbilityHUD : UCanvasController, ICanvasController
{
    [System.Serializable]
    public class AbilitySet
    {
        [SerializeField] private RectTransform abilityIcon;
        [SerializeField] private Image cooldownImg;
        [SerializeField] private TextMeshProUGUI cooldownTMP;

        public RectTransform AbilityIcon => abilityIcon;
        public Image CooldownImg => cooldownImg;
        public TextMeshProUGUI CooldownTMP => cooldownTMP;

        public void SetActive(bool value)
        {
            cooldownImg.enabled = value;
            cooldownTMP.enabled = value;
        }
    }

    [SerializeField] private float followLerp = 10f;
    private Transform mainCameraTransform;

    [SerializeField] private AbilitySet meleeSet;
    [SerializeField] private AbilitySet ability1Set;
    [SerializeField] private AbilitySet ability2Set;

    public void Initialize(Ability meleeAbility)
    {
        mainCameraTransform = Camera.main.transform;

        meleeSet.SetActive(false);
        meleeAbility.cooldown.OnValueChanged += OnMeleeCooldown;
    }

    private void OnMeleeCooldown(float previousValue, float newValue)
    {
        meleeSet.SetActive(newValue > 0f);

        meleeSet.CooldownImg.fillAmount = newValue / 1f;
        meleeSet.CooldownTMP.text = newValue.ToString("F1");
    }

    private void FixedUpdate()
    {
        if (mainCameraTransform != null)
        {
            Vector3 targetDir = mainCameraTransform.forward;
            float step = 10f * Time.fixedDeltaTime;
            Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, step, 0.0f);
            transform.rotation = Quaternion.LookRotation(newDir);
        }
    }
}