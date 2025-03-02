using InteractionSystem;
using UnityEngine;

public class PlayerInteractComponent : PlayerBaseComponent
{
    [SerializeField] private float range = 5f;
    [SerializeField] private float thickness = 3f;
    [SerializeField] private UIC_InteractDisplayHUD interactDisplayHUDPrefab;

    private UIC_InteractDisplayHUD interactDisplayHUD;
    private Interactable interactable;
    private Transform cameraTransform;

    public override void Initialize(P_PlayerPawn context)
    {
        base.Initialize(context);

        cameraTransform = Camera.main.transform;
        interactDisplayHUD = context.AttachUIWidget(interactDisplayHUDPrefab);

        initialized = true;
    }

    public override void UpdateTick(out bool swallowTick)
    {
        base.UpdateTick(out swallowTick);

        if (!initialized) return;

        interactable = FindInteractable();
        interactDisplayHUD.SetInteractable(interactable);
    }

    public override void OnInteract(bool pressed, out bool swallowInput)
    {
        base.OnInteract(pressed, out swallowInput);

        if (interactable == null) return;

        if (pressed)
        {
            interactable.OnInteract.Invoke(gameObject);
        }
    }

    private Interactable FindInteractable()
    {
        RaycastHit hit;
        if (Physics.SphereCast(cameraTransform.position, thickness, cameraTransform.forward, out hit, range))
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                return interactable;
            }
        }

        return null;
    }

    public override void DeInitialize(P_PlayerPawn context)
    {
        context.DettachUIWidget(interactDisplayHUD);

        base.DeInitialize(context);
    }
}
