using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Ability Mapping 
[CreateAssetMenu(fileName = "New Ability Mapping", menuName = "ScriptableObjects/Create New Ability Mapping")]
public class AbilityMapSO : ScriptableObject
{
    [Serializable]
    public class Mapping
    {
        public AbilitySO ability;
        public Ability prefab;
    }

    [SerializeField] private List<Mapping> abilityMapping;

    public List<Mapping> AbilityMapping => abilityMapping;

    public Ability GetAbilityPrefab(AbilitySO ability)
    {
        foreach (Mapping mapping in abilityMapping)
        {
            if (mapping.ability == ability)
            {
                return mapping.prefab;
            }
        }

        Debug.LogWarning("Ability prefab not found for AbilitySO: " + ability);
        return null;
    }

    public AbilitySO GetAbilitySO(Ability prefab)
    {
        foreach (Mapping mapping in abilityMapping)
        {
            if (prefab.AbilityName == mapping.ability.AbilityName)
            {
                return mapping.ability;
            }
        }

        Debug.LogWarning("AbilitySO not found for prefab: " + prefab);
        return null;
    }

    internal Ability GetAbilityByID(int abilityID)
    {
        for (int i = 0; i < abilityMapping.Count; i++)
        {
            if (abilityMapping[i].ability.ID == abilityID)
                return abilityMapping[i].prefab;
        }

        return null;
    }
}
