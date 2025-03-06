using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragAndDropAbility : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image iconImg;
    [SerializeField] private CanvasGroup canvasGroup;

    private AbilitySO abilitySO;
    private Vector3 originalPos;

    private Transform homeParent;
    private Vector3 homePos;

    public AbilitySO AbilitySO => abilitySO;

    public static event Action<bool> OnDragEvent;

    private void Start()
    {
        homePos = transform.localPosition;
        homeParent = transform.parent;

        OnDragEvent += DragEvent;
    }

    public void SetToHome()
    {
        transform.SetParent(homeParent);
        transform.localPosition = homePos;
    }

    private void DragEvent(bool value)
    {
        canvasGroup.blocksRaycasts = !value;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        originalPos = transform.localPosition;

        OnDragEvent?.Invoke(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        transform.localPosition = Vector3.zero;

        OnDragEvent?.Invoke(false);
    }

    public void SetAbility(AbilitySO abilitySO)
    {
        iconImg.sprite = abilitySO.Icon;
        this.abilitySO = abilitySO;
    }
}
