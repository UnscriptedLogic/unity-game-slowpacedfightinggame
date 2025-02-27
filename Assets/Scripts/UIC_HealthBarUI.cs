using DG.Tweening;
using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnscriptedEngine;

public class UIC_HealthBarUI : UCanvasController, ICanvasController
{
    [System.Serializable]
    public class HealthSet
    {
        [SerializeField] private Image sliderImg;
        [SerializeField] private TextMeshProUGUI valueTMP;

        public Image SliderImg => sliderImg;
        public TextMeshProUGUI ValueTMP => valueTMP;
    }

    public enum VisualType
    {
        Self,
        Ally,
        Enemy
    }

    [SerializeField] private float followLerp = 10f;
    private Transform mainCameraTransform;

    [SerializeField] private HealthSet selfSet;
    [SerializeField] private HealthSet allySet;
    [SerializeField] private HealthSet enemySet;

    [SerializeField] private GameObject selfHP;
    [SerializeField] private GameObject allyHP;
    [SerializeField] private GameObject enemyHP;

    private VisualType visualType;

    private Color originalColor
    {
        get
        {
            Color value = new Color();
            switch (visualType)
            {
                case VisualType.Self:

                    //hex to rgb
                    ColorUtility.TryParseHtmlString("#9BFF95", out value);

                    break;
                case VisualType.Ally:
                    ColorUtility.TryParseHtmlString("#9BFF95", out value);

                    break;
                case VisualType.Enemy:
                    ColorUtility.TryParseHtmlString("#FF3F38", out value);

                    break;
                default:
                    ColorUtility.TryParseHtmlString("#FFFFFF", out value);
                    break;
            }

            return value;
        }
    }
    private Vector3 velocity;

    public override void OnWidgetAttached(ILevelObject context)
    {
        base.OnWidgetAttached(context);

        this.context = context;
    }

    public void Initialize(NetworkVariable<float> health, VisualType type = VisualType.Self)
    {
        mainCameraTransform = Camera.main.transform;
        health.OnValueChanged += OnHealthChanged;

        visualType = type;

        selfHP.SetActive(type == VisualType.Self);
        allyHP.SetActive(type == VisualType.Ally);
        enemyHP.SetActive(type == VisualType.Enemy);
    }

    private void OnHealthChanged(float previousValue, float newValue)   
    {
        float diff = newValue - previousValue;
        float fillAmount = newValue / 100f;

        switch (visualType)
        {
            case VisualType.Self:
                selfSet.SliderImg.fillAmount = fillAmount;
                selfSet.ValueTMP.text = $"{newValue} / 100";

                selfSet.SliderImg.transform.DOPunchPosition(Vector3.right * 10f, 0.25f, 20, 1f);

                if (diff < 0)
                {
                    selfSet.SliderImg.color = Color.red;
                    selfSet.ValueTMP.color = Color.red;
                    selfSet.SliderImg.DOColor(originalColor, 0.25f).SetDelay(0.25f);
                    selfSet.ValueTMP.DOColor(Color.white, 0.25f).SetDelay(0.25f);
                }
                break;

            case VisualType.Ally:
                break;

            case VisualType.Enemy:
                enemySet.SliderImg.fillAmount = fillAmount;
                enemySet.ValueTMP.text = newValue.ToString();

                enemySet.SliderImg.transform.DOPunchPosition(Vector3.up * 10f, 0.25f, 20, 1f);

                if (diff < 0)
                {
                    enemySet.SliderImg.color = Color.white;
                    enemySet.ValueTMP.color = Color.white;
                    enemySet.SliderImg.DOColor(originalColor, 0.25f).SetDelay(0.25f);
                    enemySet.ValueTMP.DOColor(originalColor, 0.25f).SetDelay(0.25f);
                }
                break;

            default:
                break;
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