using System;
using System.Collections.Generic;
using UnityEngine;


public class CustomGameInstance : UGameInstance
{
    [SerializeField] private AbilitySO ability1;
    [SerializeField] private AbilitySO ability2;

    [SerializeField] private AbilityGroupSO allAbilityGroup;
    [SerializeField] private AbilityMapSO abilityMapSO;

    public AbilitySO Ability1 => ability1;
    public AbilitySO Ability2 => ability2;

    public AbilityGroupSO AllAbilities => allAbilityGroup;
    public AbilityMapSO AbilityMap => abilityMapSO;

    public event Action<AbilitySO, AbilitySO> OnAbilitiesChanged;

    public void SetAbilities(AbilitySO ability1, AbilitySO ability2)
    {
        this.ability1 = ability1;
        this.ability2 = ability2;

        Debug.Log("Abilities Changed");
        OnAbilitiesChanged?.Invoke(ability1, ability2);
    }
}
