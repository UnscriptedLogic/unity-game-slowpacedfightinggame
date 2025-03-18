using System;
using System.Collections.Generic;
using UnityEngine;

public class CustomGameInstance : UGameInstance
{
    [System.Serializable]
    public class Settings
    {
        public Bindable<float> volume = new Bindable<float>(1f);
        public Bindable<float> mouseSensitivity = new Bindable<float>(2f);
    }

    internal Server server;
    [SerializeField] private AbilitySO melee;
    [SerializeField] private AbilitySO ability1;
    [SerializeField] private AbilitySO ability2;

    [SerializeField] private AbilityGroupSO allAbilityGroup;
    [SerializeField] private AbilityGroupSO allMeleeGroup;
    [SerializeField] private AbilityMapSO abilityMapSO;
    [SerializeField] private AbilityMapSO meleeMapSO;
    private bool startAsClient;

    public AbilitySO Melee => melee;
    public AbilitySO Ability1 => ability1;
    public AbilitySO Ability2 => ability2;

    public AbilityGroupSO AllAbilities => allAbilityGroup;
    public AbilityMapSO AbilityMap => abilityMapSO;
    public AbilityGroupSO AllMelee => allMeleeGroup;
    public AbilityMapSO MeleeMap => meleeMapSO;

    public string IP { get; internal set; }
    public ushort Port { get; internal set; }

    public bool StartAsClient => startAsClient;

    public Settings settings;

    public event Action<AbilitySO, AbilitySO> OnAbilitiesChanged;
    public event Action<AbilitySO> OnMeleeChanged;

    private void Start()
    {
        settings = new Settings();
        settings.volume.Value = 1f;
        settings.mouseSensitivity.Value = 200f;
    }

    public void SetAbilities(AbilitySO ability1, AbilitySO ability2)
    {
        this.ability1 = ability1;
        this.ability2 = ability2;

        OnAbilitiesChanged?.Invoke(ability1, ability2);
    }

    public void SetMelee(AbilitySO melee)
    {
        this.melee = melee;

        OnMeleeChanged?.Invoke(melee);
    }

    internal void ResetToggle()
    {
        startAsClient = false;
    }

    internal void SetStartAsClient()
    {
        startAsClient = true;
    }
}
