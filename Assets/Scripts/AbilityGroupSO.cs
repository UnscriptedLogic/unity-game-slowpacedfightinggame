using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Ability Group", menuName = "ScriptableObjects/Create New Ability Group")]
public class AbilityGroupSO : ScriptableObject
{
    [SerializeField] private List<AbilitySO> abilities;

    public List<AbilitySO> Abilities => abilities;
}
