using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDropSlot : MonoBehaviour, IDropHandler
{
    private AbilitySO abilitySO;
    private DragAndDropAbility currentDraggedAbility;

    public AbilitySO AbilitySO => abilitySO;

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
    }
}
