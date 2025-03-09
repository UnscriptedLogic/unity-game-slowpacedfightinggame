using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDropSlot : MonoBehaviour, IDropHandler
{
    private AbilitySO abilitySO;
    private DragAndDropAbility currentDraggedAbility;

    public AbilitySO AbilitySO => abilitySO;

    public static event Action OnDropped;

    private void Start()
    {
        OnDropped += OnAbilityDroppedIntoSlot;
    }

    private void OnDestroy()
    {
        OnDropped -= OnAbilityDroppedIntoSlot;
    }

    private void OnAbilityDroppedIntoSlot()
    {
        if (currentDraggedAbility.transform.parent != transform)
        {
            currentDraggedAbility = null;
            abilitySO = null;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        DragAndDropAbility dragAndDrop = eventData.pointerDrag.GetComponent<DragAndDropAbility>();
        if (dragAndDrop == null) return;

        if (currentDraggedAbility != null)
        {
            currentDraggedAbility.SetToHome();
            abilitySO = null;
            currentDraggedAbility = null;
        }

        abilitySO = dragAndDrop.AbilitySO;
        currentDraggedAbility = dragAndDrop;

        dragAndDrop.transform.SetParent(transform);

        OnDropped?.Invoke();
    }

    public void SetGem(DragAndDropAbility dragAndDropAbility)
    {
        abilitySO = dragAndDropAbility.AbilitySO;
        currentDraggedAbility = dragAndDropAbility;

        dragAndDropAbility.transform.SetParent(transform);
        dragAndDropAbility.transform.localPosition = Vector3.zero;
    }
}
