using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Animation Container", menuName = "ScriptableObjects/Create New Animation Container")]
public class AnimationsSO : ScriptableObject
{
    public enum AnimationType
    {
        Upper,
        Lower
    }

    [Serializable]
    public class AnimationSet
    {
        [SerializeField] private AnimationClip animation;
        [SerializeField] private AnimationType animationType = AnimationType.Upper;

        public AnimationClip Animation => animation;
        public AnimationType AnimationType => animationType;
    }

    [SerializeField] private List<AnimationSet> allAnimations;

    public List<AnimationSet> AllAnimations => allAnimations;

    internal AnimationSet GetAnimationSet(string clipName)
    {
        foreach (AnimationSet animationSet in allAnimations)
        {
            if (animationSet.Animation.name == clipName)
            {
                return animationSet;
            }
        }
        return null;
    }

    internal AnimationClip GetAnimationClip(string clipName)
    {
        foreach (AnimationSet animationSet in allAnimations)
        {
            if (animationSet.Animation.name == clipName)
            {
                return animationSet.Animation;
            }
        }
        return null;
    }

    internal AnimationSet GetAnimationSetFromAbility(Ability ability)
    {
        foreach (AnimationSet animationSet in allAnimations)
        {
            if (animationSet.Animation == ability.AbilityAnimation)
            {
                return animationSet;
            }
        }
        return null;
    }

    internal AnimationSet GetAnimationSet(AnimationClip animClip)
    {
        foreach (AnimationSet animationSet in allAnimations)
        {
            if (animationSet.Animation == animClip)
            {
                return animationSet;
            }
        }

        return null;
    }
}
