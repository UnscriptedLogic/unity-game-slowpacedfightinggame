using DG.Tweening;
using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnscriptedEngine;

public class UIC_HealthBarUI : UCanvasController, ICanvasController
{
    [SerializeField] private float followLerp = 10f;
    private Transform mainCameraTransform;
    [SerializeField] private Image healthImg;
    [SerializeField] private TextMeshProUGUI healthTMP;

    private Color originalColor;
    private Vector3 velocity;

    public override void OnWidgetAttached(ILevelObject context)
    {
        base.OnWidgetAttached(context);

        this.context = context;
        originalColor = healthImg.color;
    }

    public void Initialize(NetworkVariable<float> health)
    {
        mainCameraTransform = Camera.main.transform;
        health.OnValueChanged += OnHealthChanged;
    }

    private void OnHealthChanged(float previousValue, float newValue)
    {
        float diff = newValue - previousValue;
        healthImg.fillAmount = newValue / 100f;
        healthTMP.text = $"{newValue} / 100";

        healthImg.transform.DOPunchPosition(Vector3.right * 10f, 0.25f, 20, 1f);

        //flash red if taking damage
        if (diff < 0)
        {
            healthImg.color = Color.red;
            healthTMP.color = Color.red;
            healthImg.DOColor(originalColor, 0.25f).SetDelay(0.25f);
            healthTMP.DOColor(originalColor, 0.25f).SetDelay(0.25f);
        }
    }

    private void FixedUpdate()
    {
        if (mainCameraTransform != null)
        {
            //transform.forward = mainCameraTransform.forward;

            //lerp rotating to face camera
            Vector3 targetDir =mainCameraTransform.forward;
            float step = 10f * Time.fixedDeltaTime;
            Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, step, 0.0f);
            transform.rotation = Quaternion.LookRotation(newDir);
        }
    }
}