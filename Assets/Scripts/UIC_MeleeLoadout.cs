using UnityEngine;
using UnscriptedEngine;

public class UIC_MeleeLoadout : UCanvasController
{
    [SerializeField] private AbilityView abilityViewModal;
    [SerializeField] private AbilityButton abilityButtonPrefab;
    [SerializeField] private Transform contentParent;

    [SerializeField] private DragAndDropSlot meleeSlot;

    [Header("Overrides")]
    [SerializeField] private AbilityGroupSO overrideMeleeDisplayGroup;

    private AbilityGroupSO meleeGroup;
    private CustomGameInstance gameInstance;

    public override void OnWidgetAttached(ILevelObject context)
    {
        base.OnWidgetAttached(context);

        this.ToggleMouse(true);
        this.ToggleInput(false);

        gameInstance = GameMode.GetGameInstance<CustomGameInstance>();

        if (overrideMeleeDisplayGroup != null)
            meleeGroup = overrideMeleeDisplayGroup;
        else
            meleeGroup = UGameModeBase.instance.GetGameInstance<CustomGameInstance>().AllMelee;

        foreach (AbilitySO melee in meleeGroup.List)
        {
            AbilityButton abilityButton = Instantiate(abilityButtonPrefab, contentParent);
            abilityButton.SetButton(melee);
            abilityButton.OnHoverEnter += () => ShowAbilityView(melee);
            abilityButton.OnHoverExit += () => abilityViewModal.Hide();

            if (melee == gameInstance.Melee)
            {
                abilityButton.SetSlot(meleeSlot);
            }
        }
    }

    private void ShowAbilityView(AbilitySO ability)
    {
        abilityViewModal.Show(ability);
    }

    public override void OnWidgetDetached(ILevelObject context)
    {
        this.ToggleInput(true);
        this.ToggleMouse(false);

        gameInstance.SetMelee(meleeSlot.AbilitySO);

        base.OnWidgetDetached(context);
    }
}