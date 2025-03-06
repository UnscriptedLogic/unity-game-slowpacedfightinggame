using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Ability Group", menuName = "ScriptableObjects/Create New Ability Group")]
public class AbilityGroupSO : ScriptableObject
{
    [SerializeField] private List<AbilitySO> abilities;

    public List<AbilitySO> List => abilities;

    internal int GetIndexByAbility(AbilitySO ability1)
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i] == ability1)
                return i;
        }

        Debug.LogWarning("Ability not found in AbilityGroupSO");
        return -1;
    }
}
