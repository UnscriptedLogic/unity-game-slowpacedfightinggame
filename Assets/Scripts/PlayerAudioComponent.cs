using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioComponent : PlayerBaseComponent
{
    [SerializeField] private AudioSource audioSource;

    public void PlayAudio(AudioClip clip, float volume = 1f)
    {
        audioSource.PlayOneShot(clip, volume);
    }
}