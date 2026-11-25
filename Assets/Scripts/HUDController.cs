using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// HUD layout:
///  - Level Display (box with the player's level)
///  - Health Points Display (red bar)
///  - Stamina Points Display (blue bar)
///  - Level Bar (green bar: current points / points needed to level up)
///  - Settings Button (top right) -> Settings Panel
///    (player speed, volume, mute, close, restart, quit)
///
/// Attach to an empty "HUD" GameObject and drag the references into the Inspector.
/// Requires GameSettings.cs (holds the shared player speed).
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Level Display")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private int startingLevel = 1;

    [Header("Health Points (Red)")]
    [SerializeField] private Slider hpBar;
    [SerializeField] private int maxHP = 100;

    [Header("Stamina Points (Blue)")]
    [SerializeField] private Slider staminaBar;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenPerSecond = 15f;
    [SerializeField] private float staminaRegenDelay = 1f;

    [Header("Level Bar (Green)")]
    [SerializeField] private Slider levelBar;
    [SerializeField] private int pointsToLevelUp = 100;
    [SerializeField] private float levelUpScaling = 1.25f;

    [Header("Settings Button (Top Right)")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;

    [Header("Player Speed")]
    [SerializeField] private Slider speedSlider;
    [SerializeField] private TMP_Text speedValueText;   // optional
    [SerializeField] private float minSpeed = 2f;
    [SerializeField] private float maxSpeed = 12f;

    // Volume slider covers this many decibels (0 dB = full volume).
    // Every equal slider step changes loudness by the same amount.
    // Try -60 for a wider range, -30 for a tighter one.
    private const float MinDb = -40f;

    private int level;
    private int currentHP;
    private float currentStamina;
    private int currentPoints;
    private float lastStaminaUseTime;
    private bool settingsOpen;

    private void Awake()
    {
        level = startingLevel;
        currentHP = maxHP;
        currentStamina = maxStamina;

        if (settingsButton) settingsButton.onClick.AddListener(ToggleSettings);
        if (closeButton) closeButton.onClick.AddListener(CloseSettings);
        if (restartButton) restartButton.onClick.AddListener(RestartLevel);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);
        if (volumeSlider) volumeSlider.onValueChanged.AddListener(SetVolume);
        if (muteToggle) muteToggle.onValueChanged.AddListener(SetMute);

        if (hpBar) { hpBar.minValue = 0; hpBar.maxValue = maxHP; }
        if (staminaBar) { staminaBar.minValue = 0; staminaBar.maxValue = maxStamina; }
        if (levelBar) { levelBar.minValue = 0; levelBar.maxValue = pointsToLevelUp; }
        if (volumeSlider)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(VolumeToSlider(AudioListener.volume));
        }
        if (speedSlider)
        {
            speedSlider.minValue = minSpeed;
            speedSlider.maxValue = maxSpeed;
            speedSlider.SetValueWithoutNotify(GameSettings.MoveSpeed);
            speedSlider.onValueChanged.AddListener(SetSpeed);
        }
        UpdateSpeedText();

        Time.timeScale = 1f;
        if (settingsPanel) settingsPanel.SetActive(false);

        RefreshAll();
    }

    private void OnDestroy()
    {
        if (settingsButton) settingsButton.onClick.RemoveListener(ToggleSettings);
        if (closeButton) closeButton.onClick.RemoveListener(CloseSettings);
        if (restartButton) restartButton.onClick.RemoveListener(RestartLevel);
        if (quitButton) quitButton.onClick.RemoveListener(QuitGame);
        if (volumeSlider) volumeSlider.onValueChanged.RemoveListener(SetVolume);
        if (muteToggle) muteToggle.onValueChanged.RemoveListener(SetMute);
        if (speedSlider) speedSlider.onValueChanged.RemoveListener(SetSpeed);
        Time.timeScale = 1f; // never leave the game frozen
    }

    private void Update()
    {
        // Delete this line if the popup should only open/close with UI buttons
        if (SettingsKeyPressed()) ToggleSettings();

        // Stamina regenerates after a short delay since it was last used
        if (!settingsOpen &&
            currentStamina < maxStamina &&
            Time.time - lastStaminaUseTime >= staminaRegenDelay)
        {
            currentStamina = Mathf.Min(maxStamina,
                currentStamina + staminaRegenPerSecond * Time.deltaTime);
            UpdateStaminaBar();
        }
    }

    // ------------------------------------------------------------------
    // Input (works with the old Input class or the new Input System)
    // ------------------------------------------------------------------
    private bool SettingsKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    // ------------------------------------------------------------------
    // Level Display
    // ------------------------------------------------------------------
    private void UpdateLevelText()
    {
        if (levelText) levelText.text = level.ToString();
    }

    // ------------------------------------------------------------------
    // Health Points Display (red)
    // ------------------------------------------------------------------
    public void TakeDamage(int amount) => SetHP(currentHP - amount);
    public void Heal(int amount) => SetHP(currentHP + amount);

    private void SetHP(int value)
    {
        currentHP = Mathf.Clamp(value, 0, maxHP);
        UpdateHPBar();
        if (currentHP <= 0) Debug.Log("Player died");
    }

    private void UpdateHPBar()
    {
        if (hpBar) hpBar.value = currentHP;
    }

    // ------------------------------------------------------------------
    // Stamina Points Display (blue)
    // ------------------------------------------------------------------
    /// <summary>Returns false if there isn't enough stamina.</summary>
    public bool UseStamina(float amount)
    {
        if (currentStamina < amount) return false;
        currentStamina -= amount;
        lastStaminaUseTime = Time.time;
        UpdateStaminaBar();
        return true;
    }

    public void RestoreStamina(float amount)
    {
        currentStamina = Mathf.Clamp(currentStamina + amount, 0f, maxStamina);
        UpdateStaminaBar();
    }

    private void UpdateStaminaBar()
    {
        if (staminaBar) staminaBar.value = currentStamina;
    }

    // ------------------------------------------------------------------
    // Level Bar (green): current points / points needed to level up
    // ------------------------------------------------------------------
    public void AddPoints(int amount)
    {
        currentPoints += amount;

        // Carry leftover points over when leveling up (handles multiple levels)
        while (currentPoints >= pointsToLevelUp)
        {
            currentPoints -= pointsToLevelUp;
            LevelUp();
        }
        UpdateLevelBar();
    }

    private void LevelUp()
    {
        level++;
        pointsToLevelUp = Mathf.CeilToInt(pointsToLevelUp * levelUpScaling);
        if (levelBar) levelBar.maxValue = pointsToLevelUp;

        // Reward: refill HP and stamina
        SetHP(maxHP);
        RestoreStamina(maxStamina);
        UpdateLevelText();
    }

    private void UpdateLevelBar()
    {
        if (levelBar) levelBar.value = currentPoints;
    }

    // ------------------------------------------------------------------
    // Settings Button (top right) + Settings Panel
    // ------------------------------------------------------------------
    public void ToggleSettings()
    {
        if (settingsOpen) CloseSettings();
        else OpenSettings();
    }

    private void OpenSettings()
    {
        settingsOpen = true;
        Time.timeScale = 0f;
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        settingsOpen = false;
        Time.timeScale = 1f;
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    // --- Setting 1: player speed ---
    private void SetSpeed(float value)
    {
        GameSettings.MoveSpeed = value;
        UpdateSpeedText();
    }

    private void UpdateSpeedText()
    {
        if (speedValueText) speedValueText.text = $"Speed: {GameSettings.MoveSpeed:0.0}";
    }

    // --- Setting 2: volume (decibel scale so every step feels the same) ---
    private static float SliderToVolume(float s)
    {
        if (s <= 0.0001f) return 0f;                     // bottom of slider = silent
        float db = Mathf.Lerp(MinDb, 0f, s);
        return Mathf.Pow(10f, db / 20f);                 // dB -> linear amplitude
    }

    private static float VolumeToSlider(float v)
    {
        if (v <= 0.0001f) return 0f;
        float db = 20f * Mathf.Log10(v);
        return Mathf.Clamp01((db - MinDb) / -MinDb);     // linear amplitude -> slider
    }

    private void SetVolume(float value)
    {
        AudioListener.volume = SliderToVolume(value);
        if (muteToggle && value > 0f && muteToggle.isOn)
            muteToggle.SetIsOnWithoutNotify(false);
    }

    // --- Setting 3: mute ---
    private void SetMute(bool muted)
    {
        AudioListener.volume = muted ? 0f : (volumeSlider ? SliderToVolume(volumeSlider.value) : 1f);
    }

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------
    private void RefreshAll()
    {
        UpdateLevelText();
        UpdateHPBar();
        UpdateStaminaBar();
        UpdateLevelBar();
    }
}