using System;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

public class AbilityView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private VideoPlayer videoPlayer;

    internal void Hide()
    {
        videoPlayer.Stop();
        gameObject.SetActive(false);
    }

    internal void Show(AbilitySO ability)
    {
        titleTMP.text = ability.AbilityName;
        descriptionTMP.text = ability.Desc;
        
        videoPlayer.gameObject.SetActive(ability.DemoVideo != null);
        if (ability.DemoVideo != null)
        {
            videoPlayer.clip = ability.DemoVideo;
            videoPlayer.Play();
        }
        else
        {
            videoPlayer.Stop();
        }

        gameObject.SetActive(true);
    }
}
