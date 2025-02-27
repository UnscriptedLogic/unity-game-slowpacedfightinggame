using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilitySO : ScriptableObject
{
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea(3, 5)] private string desc;

    public Sprite Icon => icon;
    public string Desc => desc;
}
