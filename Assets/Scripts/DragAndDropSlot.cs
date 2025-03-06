using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragAndDropSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image iconImg;

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
        iconImg.sprite = abilitySO.Icon;
        currentDraggedAbility = dragAndDrop;

        dragAndDrop.transform.SetParent(transform);
    }
}
