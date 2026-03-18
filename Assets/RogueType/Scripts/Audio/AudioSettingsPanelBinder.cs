using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsPanelBinder : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider mainSlider;
    [SerializeField] private Slider backgroundMusicSlider;
    [SerializeField] private Slider uiSlider;
    [SerializeField] private Slider gameSlider;

    [Header("Auto Bind")]
    [SerializeField] private bool autoFindByName = true;

    private void OnEnable()
    {
        TryAutoAssignSliders();
        SyncFromSettings();
        RegisterListeners();
    }

    private void OnDisable()
    {
        UnregisterListeners();
    }

    private void RegisterListeners()
    {
        if (mainSlider != null)
        {
            mainSlider.onValueChanged.RemoveListener(OnMainChanged);
            mainSlider.onValueChanged.AddListener(OnMainChanged);
        }

        if (backgroundMusicSlider != null)
        {
            backgroundMusicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            backgroundMusicSlider.onValueChanged.AddListener(OnMusicChanged);
        }

        if (uiSlider != null)
        {
            uiSlider.onValueChanged.RemoveListener(OnUiChanged);
            uiSlider.onValueChanged.AddListener(OnUiChanged);
        }

        if (gameSlider != null)
        {
            gameSlider.onValueChanged.RemoveListener(OnGameChanged);
            gameSlider.onValueChanged.AddListener(OnGameChanged);
        }
    }

    private void UnregisterListeners()
    {
        if (mainSlider != null)
            mainSlider.onValueChanged.RemoveListener(OnMainChanged);

        if (backgroundMusicSlider != null)
            backgroundMusicSlider.onValueChanged.RemoveListener(OnMusicChanged);

        if (uiSlider != null)
            uiSlider.onValueChanged.RemoveListener(OnUiChanged);

        if (gameSlider != null)
            gameSlider.onValueChanged.RemoveListener(OnGameChanged);
    }

    private void SyncFromSettings()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
            return;

        if (mainSlider != null)
            mainSlider.SetValueWithoutNotify(audioManager.MasterVolume);

        if (backgroundMusicSlider != null)
            backgroundMusicSlider.SetValueWithoutNotify(audioManager.MusicVolume);

        if (uiSlider != null)
            uiSlider.SetValueWithoutNotify(audioManager.UiVolume);

        if (gameSlider != null)
            gameSlider.SetValueWithoutNotify(audioManager.GameVolume);
    }

    private void TryAutoAssignSliders()
    {
        if (!autoFindByName)
            return;

        Slider[] sliders = GetComponentsInChildren<Slider>(true);
        foreach (Slider slider in sliders)
        {
            if (slider == null)
                continue;

            string sliderName = slider.gameObject.name.ToLowerInvariant();

            if (mainSlider == null && (sliderName.Contains("main") || sliderName.Contains("master")))
            {
                mainSlider = slider;
                continue;
            }

            if (backgroundMusicSlider == null &&
                (sliderName.Contains("background") || sliderName.Contains("music") || sliderName.Contains("bgm")))
            {
                backgroundMusicSlider = slider;
                continue;
            }

            if (uiSlider == null && sliderName.Contains("ui"))
            {
                uiSlider = slider;
                continue;
            }

            if (gameSlider == null && sliderName.Contains("game"))
                gameSlider = slider;
        }
    }

    private void OnMainChanged(float value)
    {
        AudioManager.Instance?.SetMasterVolume(value);
    }

    private void OnMusicChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
    }

    private void OnUiChanged(float value)
    {
        AudioManager.Instance?.SetUiVolume(value);
    }

    private void OnGameChanged(float value)
    {
        AudioManager.Instance?.SetGameVolume(value);
    }
}
