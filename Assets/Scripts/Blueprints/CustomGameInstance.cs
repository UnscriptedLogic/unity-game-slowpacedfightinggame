using System;
using System.Collections.Generic;
using UnityEngine;


public class CustomGameInstance : UGameInstance
{
    [SerializeField] private AbilitySO melee;
    [SerializeField] private AbilitySO ability1;
    [SerializeField] private AbilitySO ability2;

    [SerializeField] private AbilityGroupSO allAbilityGroup;
    [SerializeField] private AbilityGroupSO allMeleeGroup;
    [SerializeField] private AbilityMapSO abilityMapSO;
    [SerializeField] private AbilityMapSO meleeMapSO;

    public AbilitySO Melee => melee;
    public AbilitySO Ability1 => ability1;
    public AbilitySO Ability2 => ability2;

    public AbilityGroupSO AllAbilities => allAbilityGroup;
    public AbilityMapSO AbilityMap => abilityMapSO;
    public AbilityGroupSO AllMelee => allMeleeGroup;
    public AbilityMapSO MeleeMap => meleeMapSO;

    public event Action<AbilitySO, AbilitySO> OnAbilitiesChanged;
    public event Action<AbilitySO> OnMeleeChanged;

    public void SetAbilities(AbilitySO ability1, AbilitySO ability2)
    {
        this.ability1 = ability1;
        this.ability2 = ability2;

        Debug.Log("Abilities Changed");
        OnAbilitiesChanged?.Invoke(ability1, ability2);
    }

    public void SetMelee(AbilitySO melee)
    {
        this.melee = melee;
        Debug.Log("Melee Changed");

        OnMeleeChanged?.Invoke(melee);
    }
}
