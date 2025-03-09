using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Animation Container", menuName = "ScriptableObjects/Create New Animation Container")]
public class AnimationsSO : ScriptableObject
{
    [SerializeField] private List<AnimationClip> allAnimations;

    public List<AnimationClip> AllAnimations => allAnimations;

    internal AnimationClip GetAnimationClip(string clipName)
    {
        return allAnimations.Find(clip => clip.name == clipName);
    }
}
