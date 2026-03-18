using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    private const string AudioLibraryResourcePath = "AudioLibrary";
    private const string MasterVolumeKey = "Audio.Master";
    private const string MusicVolumeKey = "Audio.Music";
    private const string UiVolumeKey = "Audio.UI";
    private const string GameVolumeKey = "Audio.Game";

    public static AudioManager Instance { get; private set; }

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float UiVolume => uiVolume;
    public float GameVolume => gameVolume;

    private AudioLibrary library;
    private AudioSource musicSource;
    private AudioSource sfxSource;

    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float uiVolume = 1f;
    private float gameVolume = 1f;

    private float nextButtonScanTime;
    private bool missingLibraryLogged;
    private const bool EnableDebugLogs = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static AudioManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        GameObject audioManagerObject = new GameObject("AudioManager");
        Instance = audioManagerObject.AddComponent<AudioManager>();
        DontDestroyOnLoad(audioManagerObject);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSources();
        LoadLibrary();
        LoadSettings();
        ApplyVolumes();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextButtonScanTime)
            return;

        nextButtonScanTime = Time.unscaledTime + 1f;
        EnsureSceneButtonsHaveSoundHooks();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        ApplyVolumes();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        ApplyVolumes();
    }

    public void SetUiVolume(float value)
    {
        uiVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(UiVolumeKey, uiVolume);
    }

    public void SetGameVolume(float value)
    {
        gameVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(GameVolumeKey, gameVolume);
    }

    public void PlayBackgroundMusic(AudioClip clip = null)
    {
        AudioClip targetClip = clip != null ? clip : GetLibraryClip(l => l.backgroundMusic);
        if (targetClip == null)
        {
            LogDebug("PlayBackgroundMusic skipped: no backgroundMusic clip assigned.");
            return;
        }

        if (musicSource.clip == targetClip && musicSource.isPlaying)
        {
            LogDebug($"PlayBackgroundMusic skipped: '{targetClip.name}' is already playing.");
            return;
        }

        musicSource.clip = targetClip;
        musicSource.loop = true;
        musicSource.Play();
        LogDebug($"PlayBackgroundMusic started: '{targetClip.name}', volume={musicSource.volume:F2}.");
    }

    public void PlayButtonClick()
    {
        PlaySfx(GetLibraryClip(l => l.buttonClick), GetUiSfxVolume());
    }

    public void PlayPlayerProjectile()
    {
        PlaySfx(GetLibraryClip(l => l.playerProjectile), GetGameSfxVolume());
    }

    public void PlayWallDamaged()
    {
        PlaySfx(GetLibraryClip(l => l.wallDamaged), GetGameSfxVolume());
    }

    public void PlayAllyArrowFire()
    {
        PlaySfx(GetLibraryClip(l => l.allyArrowFire), GetGameSfxVolume());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        LogDebug($"Scene loaded: '{scene.name}'. Scanning buttons and starting BGM.");
        EnsureSceneButtonsHaveSoundHooks();
        PlayBackgroundMusic();
    }

    private void InitializeSources()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }

    private void LoadLibrary()
    {
        library = Resources.Load<AudioLibrary>(AudioLibraryResourcePath);
        if (library != null)
        {
            string clipName = library.backgroundMusic != null ? library.backgroundMusic.name : "<none>";
            LogDebug($"Loaded AudioLibrary from Resources/{AudioLibraryResourcePath}. BGM={clipName}.");
        }

        if (library == null && !missingLibraryLogged)
        {
            missingLibraryLogged = true;
            Debug.LogWarning(
                "AudioManager could not find Resources/AudioLibrary. " +
                "Create an AudioLibrary asset there and assign the five clips."
            );
        }
    }

    private void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        uiVolume = PlayerPrefs.GetFloat(UiVolumeKey, 1f);
        gameVolume = PlayerPrefs.GetFloat(GameVolumeKey, 1f);
        LogDebug(
            $"Loaded audio settings: master={masterVolume:F2}, music={musicVolume:F2}, ui={uiVolume:F2}, game={gameVolume:F2}."
        );
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = GetMusicVolume();

        if (sfxSource != null)
            sfxSource.volume = masterVolume;

        LogDebug(
            $"Applied volumes: musicSource={musicSource?.volume:F2}, sfxBase={sfxSource?.volume:F2}."
        );
    }

    private void EnsureSceneButtonsHaveSoundHooks()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            if (button.GetComponent<UIButtonClickSound>() == null)
                button.gameObject.AddComponent<UIButtonClickSound>();
        }
    }

    private void PlaySfx(AudioClip clip, float categoryVolume)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, categoryVolume);
    }

    private float GetMusicVolume()
    {
        return masterVolume * musicVolume;
    }

    private float GetUiSfxVolume()
    {
        return uiVolume;
    }

    private float GetGameSfxVolume()
    {
        return gameVolume;
    }

    private AudioClip GetLibraryClip(System.Func<AudioLibrary, AudioClip> selector)
    {
        if (library == null)
            LoadLibrary();

        return library != null ? selector(library) : null;
    }

    private void LogDebug(string message)
    {
        if (!EnableDebugLogs)
            return;

        Debug.Log($"[AudioManager] {message}", this);
    }
}
