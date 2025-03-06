using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragAndDropSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image iconImg;

    private AbilitySO abilitySO;

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Dropped");
        if (eventData.pointerDrag == null) return;

        DragAndDropAbility dragAndDrop = eventData.pointerDrag.GetComponent<DragAndDropAbility>();
        Debug.Log(dragAndDrop.AbilitySO);
        if (dragAndDrop != null)
        {
            abilitySO = dragAndDrop.AbilitySO;
            iconImg.sprite = abilitySO.Icon;
        }
    }
}
