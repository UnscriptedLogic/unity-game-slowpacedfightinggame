using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomGameInstance : UGameInstance
{
    [SerializeField] private AbilityGroupSO allAbilityGroup;

    public AbilityGroupSO AllAbilitise => allAbilityGroup;
}
