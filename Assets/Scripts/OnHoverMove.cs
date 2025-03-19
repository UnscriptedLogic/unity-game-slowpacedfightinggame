using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class OnHoverMove : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private Ease ease = Ease.OutBack;

    [Header("Simulation Settings")]
    [SerializeField] private Vector3 offset;
    [SerializeField] private Space simPosSpace = Space.World;
    [SerializeField] private Space simRotSpace = Space.Self;

    private Vector3 startLocation;
    private Vector3 endLocation;

    private RectTransform rectTransform;

    [Header("Debug")]
    [SerializeField] private bool preview;
    private bool isPreviewing;

    private bool IsUI(ref RectTransform rect)
    {
        if (rect != null) return true;

        rect = GetComponent<RectTransform>();
        return rect != null;
    }

    private void Start()
    {
        if (IsUI(ref rectTransform))
        {
            startLocation = GetRootPos(rectTransform, simPosSpace);
        }
    }

    private Vector3 GetRootPos(RectTransform rect, Space space)
    {
        switch (space)
        {
            case Space.World:
                return rect.position;
            case Space.Self:
                return rect.localPosition;
            default:
                return rect.position;
        }
    }

    private Quaternion GetRootRot(RectTransform rect, Space space)
    {
        switch (space)
        {
            case Space.World:
                return Quaternion.identity;
            case Space.Self:
                return rect.rotation;
            default:
                return rect.rotation;
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (preview && !isPreviewing)
        {
            if (IsUI(ref rectTransform))
            {
                startLocation = GetRootPos(rectTransform, simPosSpace);
                isPreviewing = true;
            }
        }

        if (!preview && isPreviewing)
        {
            if (IsUI(ref rectTransform))
            {
                rectTransform.position = startLocation;
                isPreviewing = false;
            }
        }

        if (isPreviewing)
        {
            if (IsUI(ref rectTransform))
            {
                rectTransform.position = startLocation + (GetRootRot(rectTransform, simRotSpace) * offset);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsUI(ref rectTransform))
        {
            rectTransform.DOMove(startLocation + (GetRootRot(rectTransform, simRotSpace) * offset), duration).SetEase(ease);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsUI(ref rectTransform))
        {
            rectTransform.DOMove(startLocation, duration).SetEase(ease);
        }
    }
}
