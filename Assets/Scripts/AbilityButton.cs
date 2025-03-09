using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AbilityButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private Image iconImg;
    [SerializeField] private TextMeshProUGUI nameTMP;
    [SerializeField] private Button button;
    [SerializeField] private DragAndDropAbility slotGem;

    public Action OnHoverEnter;
    public Action OnHoverExit;
    public Action OnMouseDown;
    public Action OnMouseUp;
    public Action OnDragStart;
    public Action OnDragEnd;

    public void OnPointerDown(PointerEventData eventData) => OnMouseDown?.Invoke();
    public void OnPointerEnter(PointerEventData eventData) => OnHoverEnter?.Invoke();
    public void OnPointerExit(PointerEventData eventData) => OnHoverExit?.Invoke();
    public void OnPointerUp(PointerEventData eventData) => OnMouseUp?.Invoke();

    public void SetButton(AbilitySO abilitySO)
    {
        iconImg.sprite = abilitySO.Icon;
        nameTMP.text = abilitySO.AbilityName;

        slotGem.Initialize();
        slotGem.SetAbility(abilitySO);
    }

    public void OnBeginDrag(PointerEventData eventData) => OnDragStart?.Invoke();

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData) => OnDragEnd?.Invoke();

    public void SetSlot(DragAndDropSlot dragAndDropSlot)
    {
        dragAndDropSlot.SetGem(slotGem);
    }
}
