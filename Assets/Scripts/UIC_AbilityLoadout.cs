using System;
using TMPro;
using UnityEngine;
using UnscriptedEngine;

public class UIC_AbilityLoadout : UCanvasController
{
    [SerializeField] private AbilityView abilityViewModal;
    [SerializeField] private AbilityButton abilityButtonPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private AbilityGroupSO overrideAbilityDisplayGroup;

    private AbilityGroupSO abilityGroup;

    public override void OnWidgetAttached(ILevelObject context)
    {
        base.OnWidgetAttached(context);

        this.ToggleMouse(true);
        this.ToggleInput(false);

        if (overrideAbilityDisplayGroup != null)
            abilityGroup = overrideAbilityDisplayGroup;
        else
            abilityGroup = UGameModeBase.instance.GetGameInstance<CustomGameInstance>().AllAbilitise;

        foreach (AbilitySO ability in abilityGroup.Abilities)
        {
            AbilityButton abilityButton = Instantiate(abilityButtonPrefab, contentParent);
            abilityButton.SetButton(ability.Icon, ability.AbilityName, () => ShowAbilityView(ability));
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

        base.OnWidgetDetached(context);
    }
}