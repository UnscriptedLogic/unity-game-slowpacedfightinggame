using InteractionSystem;
using UnityEngine;
using UnscriptedEngine;

public class UIC_InteractDisplayHUD : UCanvasController
{
    [SerializeField] private PopUpUI popUpUIPrefab;

    private Interactable interactable;
    private PopUpUI popUpUI;

    private void Start()
    {
        popUpUI = Instantiate(popUpUIPrefab, transform);
        popUpUI.gameObject.SetActive(false);
    }

    public void SetInteractable(Interactable interactable)
    {
        if (this.interactable == interactable)
        {
            if (interactable == null)
            {
                popUpUI.gameObject.SetActive(false);
            }

            return;
        }

        if (interactable == null)
        {
            this.interactable = null;
            popUpUI.gameObject.SetActive(false);
            return;
        }

        this.interactable = interactable;
        popUpUI.gameObject.SetActive(true);
        popUpUI.Initialize(interactable, interactable.Description);
    }
}