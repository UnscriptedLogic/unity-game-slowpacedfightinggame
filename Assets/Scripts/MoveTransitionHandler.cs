using DG.Tweening;
using UnityEngine.UI;
using UnityEngine;

public class MoveTransitionHandler : MonoBehaviour
{
    [SerializeField] private float delay;
    [SerializeField] private float closeDelay;

    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease ease = Ease.InOutExpo;
    [SerializeField] private Vector2 hidePosition;

    [SerializeField] private Behaviour[] componentsToEnable;

    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Debug Settings")]
    [SerializeField] private bool rememberHidePosition;

    private Vector2 finalPosition;
    private RectTransform rectTransform;
    private Tween transitionTween;
    private bool isOutro;

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();

        if (!isOutro)
        {
            finalPosition = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition = hidePosition;

            Transition(finalPosition, duration, delay, ease);
        }
        else
        {
            for (int i = 0; i < componentsToEnable.Length; i++)
            {
                componentsToEnable[i].enabled = true;
            }
        }
    }

    private void Transition(Vector2 finalPos, float duration, float delay, Ease ease, GameObject root = null)
    {
        if (rectTransform == null) return;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
        }

        transitionTween = rectTransform.DOAnchorPos(finalPos, duration).SetDelay(delay).SetEase(ease).OnComplete(
            () =>
            {
                if (canvasGroup != null)
                {
                    canvasGroup.interactable = true;
                }
            });

        if (isOutro)
        {
            if (root)
            {
                Destroy(root, duration + closeDelay);
            }
            else
            {
                Destroy(gameObject, duration + closeDelay);
            }

        }
    }

    private void OnDisable()
    {
        if (isOutro) return;

        GameObject canvasParent = new GameObject();
        Canvas canvas = canvasParent.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler canvasScaler = canvasParent.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 1f;

        GameObject duplicate = Instantiate(gameObject, canvasParent.transform);
        MoveTransitionHandler moveTransitionHandler = duplicate.GetComponent<MoveTransitionHandler>();
        duplicate.GetComponent<RectTransform>().anchoredPosition = finalPosition;
        moveTransitionHandler.isOutro = true;
        moveTransitionHandler.enabled = true;
        moveTransitionHandler.Transition(hidePosition, duration, closeDelay, ease, canvasParent);
    }

    private void OnDestroy()
    {
        if (transitionTween != null)
        {
            transitionTween.Kill();
        }
    }

    private void OnValidate()
    {
        if (rememberHidePosition)
        {
            rememberHidePosition = false;

            rectTransform = GetComponent<RectTransform>();
            hidePosition = rectTransform.anchoredPosition;
        }
    }
}