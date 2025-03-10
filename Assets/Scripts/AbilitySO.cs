using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "New Ability Details", menuName = "ScriptableObjects/Create New Ability Details")]
public class AbilitySO : ScriptableObject
{
    [SerializeField] private int abilityId;
    [SerializeField] private Sprite icon;
    [SerializeField] private string abilityName;
    [SerializeField, TextArea(5, 10)] private string desc;
    [SerializeField] private VideoClip demoVideo;

    public Sprite Icon => icon;
    public int ID => abilityId;
    public string AbilityName => abilityName;
    public string Desc => desc;
    public VideoClip DemoVideo => demoVideo;
}
