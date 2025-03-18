using System;
using UnityEngine;
using UnscriptedEngine;

public class SettingsHUD : UCanvasController, ICanvasController
{
    private CustomGameInstance customGameInstance;

    private UButtonComponent closeButton;

    private USliderComponent volumeSlider;
    private USliderComponent mouseSensSlider;

    [SerializeField] private RectTransform settingsModal;

    private void Start()
    {
        UIEscInputHandler.AddListener(OnEscPressed);
    }

    private void OnEscPressed(out bool swallowEvent)
    {
        if (settingsModal.gameObject.activeInHierarchy)
        {
            CloseModal();
        }
        else
        {
            OpenModal();
        }

        swallowEvent = true;
    }

    private void OpenModal()
    {
        this.ToggleMouse(true);
        this.ToggleInput(false);

        volumeSlider.Slider.value = customGameInstance.settings.volume.Value;
        mouseSensSlider.Slider.value = customGameInstance.settings.mouseSensitivity.Value;

        settingsModal.gameObject.SetActive(true);
    }

    public override void OnWidgetAttached(ILevelObject context)
    {
        base.OnWidgetAttached(context);

        customGameInstance = GameMode.GetGameInstance<CustomGameInstance>();

        closeButton = GetUIComponent<UButtonComponent>("closeSettings");
        volumeSlider = GetUIComponent<USliderComponent>("volume");
        mouseSensSlider = GetUIComponent<USliderComponent>("mouseSens");

        CloseModal();

        closeButton.TMPButton.onClick.AddListener(CloseModal);

        volumeSlider.Slider.value = customGameInstance.settings.volume.Value;
        mouseSensSlider.Slider.value = customGameInstance.settings.mouseSensitivity.Value;

        volumeSlider.Slider.onValueChanged.AddListener(OnVolumeChanged);
        mouseSensSlider.Slider.onValueChanged.AddListener(OnMouseSensChanged);
    }

    private void CloseModal()
    {
        this.ToggleMouse(false);
        this.ToggleInput(true);
        settingsModal.gameObject.SetActive(false);
    }

    private void OnMouseSensChanged(float value)
    {
        customGameInstance.settings.mouseSensitivity.Value = value;
    }

    private void OnVolumeChanged(float value)
    {
        customGameInstance.settings.volume.Value = value;
    }

    public override void OnWidgetDetached(ILevelObject context)
    {
        base.OnWidgetDetached(context);

        closeButton.TMPButton.onClick.RemoveListener(CloseModal);
        volumeSlider.Slider.onValueChanged.RemoveListener(OnVolumeChanged);
        mouseSensSlider.Slider.onValueChanged.RemoveListener(OnMouseSensChanged);
    }
}
