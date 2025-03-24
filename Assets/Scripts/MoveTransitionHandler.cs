using DG.Tweening;
using UnityEngine.UI;
using UnityEngine;

public class MoveTransitionHandler : MonoBehaviour
{
    [SerializeField] private float delay;
    [SerializeField] private float closeDelay;

    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease ease = Ease.InOutExpo;

    [SerializeField] private Behaviour[] componentsToEnable;

    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private Vector2 showPos;
    [SerializeField] private Vector2 hidePos;
    [SerializeField] private bool previewHidePos;
    private bool isPreviewingHide;

    private Vector2 finalPosition;
    private RectTransform rectTransform;
    private Tween transitionTween;
    private bool isOutro;

    public Tween TransitionTween => transitionTween;

    private void Awake()
    {
        SendMessageUpwards("AssignTween", this, SendMessageOptions.DontRequireReceiver);
    }

    private void OnEnable()
    {
        TransitionOpen();
    }

    private void TransitionOpen()
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.localPosition = hidePos;
        Transition(showPos, duration, delay, ease);
    }

    public void TransitionClose()
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.localPosition = showPos;
        Transition(hidePos, duration, closeDelay, ease);
    }

    private void Transition(Vector2 finalPos, float duration, float delay, Ease ease)
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

                transitionTween = null;
            });
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;

        if (previewHidePos && !isPreviewingHide)
        {
            rectTransform = GetComponent<RectTransform>();
            showPos = rectTransform.localPosition;
            isPreviewingHide = true;
        }

        if (!previewHidePos && isPreviewingHide)
        {
            rectTransform = GetComponent<RectTransform>();
            rectTransform.localPosition = showPos;
            isPreviewingHide = false;
        }

        if (isPreviewingHide)
        {
            rectTransform.localPosition = hidePos;
        }
    }
}