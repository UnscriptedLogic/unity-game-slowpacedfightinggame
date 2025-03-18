using System;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using UnscriptedEngine;

public class UIC_AbilityLoadout : UCanvasController
{
    [SerializeField] private AbilityView abilityViewModal;
    [SerializeField] private AbilityButton abilityButtonPrefab;
    [SerializeField] private Transform contentParent;

    [SerializeField] private DragAndDropSlot ability1Slot;
    [SerializeField] private DragAndDropSlot ability2Slot;

    [Header("Overrides")]
    [SerializeField] private AbilityGroupSO overrideAbilityDisplayGroup;
    
    private AbilityGroupSO abilityGroup;
    private CustomGameInstance gameInstance;

    public override void OnWidgetAttached(ILevelObject context)
    {
        base.OnWidgetAttached(context);

        this.ToggleMouse(true);
        this.ToggleInput(false);

        gameInstance = GameMode.GetGameInstance<CustomGameInstance>();

        if (overrideAbilityDisplayGroup != null)
            abilityGroup = overrideAbilityDisplayGroup;
        else
            abilityGroup = UGameModeBase.instance.GetGameInstance<CustomGameInstance>().AllAbilities;

        foreach (AbilitySO ability in abilityGroup.List)
        {
            if (ability.ExcludeFromDisplay) return;

            AbilityButton abilityButton = Instantiate(abilityButtonPrefab, contentParent);
            abilityButton.SetButton(ability);
            abilityButton.OnHoverEnter += () => ShowAbilityView(ability);
            abilityButton.OnHoverExit += () => abilityViewModal.Hide();

            if (ability == gameInstance.Ability1)
            {
                abilityButton.SetSlot(ability1Slot);
            }
            else if (ability == gameInstance.Ability2)
            {
                abilityButton.SetSlot(ability2Slot);
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

        gameInstance.SetAbilities(ability1Slot.AbilitySO, ability2Slot.AbilitySO);

        base.OnWidgetDetached(context);
    }
}