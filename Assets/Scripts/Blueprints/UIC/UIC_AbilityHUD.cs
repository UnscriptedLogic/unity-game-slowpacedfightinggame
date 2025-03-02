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
        [SerializeField] private TextMeshProUGUI counterTMP;

        public RectTransform AbilityIcon => abilityIcon;
        public Image CooldownImg => cooldownImg;
        public TextMeshProUGUI CooldownTMP => cooldownTMP;
        public TextMeshProUGUI CounterTMP => counterTMP;

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

    public void Initialize(Ability meleeAbility, Ability ability1, Ability ability2)
    {
        mainCameraTransform = Camera.main.transform;

        meleeSet.SetActive(false);
        ability1Set.SetActive(false);
        ability2Set.SetActive(false);

        meleeSet.CounterTMP.text = meleeAbility.uses.Value.ToString();
        ability1Set.CounterTMP.text = ability1.uses.Value.ToString();
        ability2Set.CounterTMP.text = ability2.uses.Value.ToString();

        meleeAbility.cooldown.OnValueChanged += OnMeleeCooldown;
        meleeAbility.uses.OnValueChanged += OnMeleeUses;

        ability1.cooldown.OnValueChanged += OnAbility1Cooldown;
        ability1.uses.OnValueChanged += OnAbility1Uses;

        ability2.cooldown.OnValueChanged += OnAbility2Cooldown;
        ability2.uses.OnValueChanged += OnAbility2Uses;
    }

    private void OnMeleeCooldown(float previousValue, float newValue)
    {
        meleeSet.SetActive(newValue > 0f);

        meleeSet.CooldownImg.fillAmount = newValue / 1f;
        meleeSet.CooldownTMP.text = newValue.ToString("F1");
    }

    private void OnMeleeUses(int previousValue, int newValue)
    {
        meleeSet.CounterTMP.text = newValue.ToString();
    }

    private void OnAbility1Cooldown(float previousValue, float newValue)
    {
        ability1Set.SetActive(newValue > 0f);
        ability1Set.CooldownImg.fillAmount = newValue / 1f;
        ability1Set.CooldownTMP.text = newValue.ToString("F1");
    }

    private void OnAbility1Uses(int previousValue, int newValue)
    {
        ability1Set.CounterTMP.text = newValue.ToString();
    }

    private void OnAbility2Cooldown(float previousValue, float newValue)
    {
        ability2Set.SetActive(newValue > 0f);
        ability2Set.CooldownImg.fillAmount = newValue / 1f;
        ability2Set.CooldownTMP.text = newValue.ToString("F1");
    }

    private void OnAbility2Uses(int previousValue, int newValue)
    {
        ability2Set.CounterTMP.text = newValue.ToString();
    }

    private void Update()
    {
        if (mainCameraTransform != null)
        {
            transform.forward = mainCameraTransform.forward;
        }
    }
}