using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Ability Details", menuName = "ScriptableObjects/Create New Ability Details")]
public class AbilitySO : ScriptableObject
{
    [SerializeField] private Sprite icon;
    [SerializeField] private string abilityName;
    [SerializeField, TextArea(5, 10)] private string desc;

    public Sprite Icon => icon;
    public string AbilityName => abilityName;
    public string Desc => desc;
}
