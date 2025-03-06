using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragAndDropAbility : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image iconImg;
    [SerializeField] private CanvasGroup canvasGroup;

    private AbilitySO abilitySO;
    private Vector3 originalPos;

    public AbilitySO AbilitySO => abilitySO;


    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        originalPos = transform.localPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        transform.localPosition = originalPos;
    }

    public void SetAbility(AbilitySO abilitySO)
    {
        iconImg.sprite = abilitySO.Icon;
        this.abilitySO = abilitySO;
    }
}
