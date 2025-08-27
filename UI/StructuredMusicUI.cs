using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 结构化音乐生成系统UI
/// </summary>
public class StructuredMusicUI : MonoBehaviour
{
    [Header("UI组件")]
    public GameObject mainPanel;
    public Button toggleButton;
    public TextMeshProUGUI toggleButtonText;
    
    [Header("场景数据面板")]
    public GameObject sceneDataPanel;
    public TMP_Dropdown environmentDropdown;
    public TMP_Dropdown actionDropdown;
    public TMP_Dropdown enemyDropdown;
    public TMP_Dropdown timeDropdown;
    public TMP_Dropdown weatherDropdown;
    public TMP_Dropdown levelDropdown;
    public Slider intensitySlider;
    public Slider threatSlider;
    public Slider healthSlider;
    public Toggle stealthToggle;
    public Toggle bossFightToggle;
    
    [Header("预设管理面板")]
    public GameObject presetPanel;
    public Transform presetListContent;
    public GameObject presetItemPrefab;
    public Button generatePresetButton;
    public Button clearPresetsButton;
    public TextMeshProUGUI presetStatsText;
    
    [Header("实时数据面板")]
    public GameObject realtimePanel;
    public TextMeshProUGUI currentSceneText;
    public TextMeshProUGUI currentActionText;
    public TextMeshProUGUI threatLevelText;
    public TextMeshProUGUI intensityText;
    public Button extractDataButton;
    public Button playMusicButton;
    
    [Header("调试面板")]
    public GameObject debugPanel;
    public TextMeshProUGUI debugText;
    public Button refreshButton;
    
    [Header("设置")]
    public bool showDebugInfo = true;
    public float updateInterval = 1f;
    
    private bool isUIVisible = false;
    private MusicSceneData currentSceneData;
    private SceneDataExtractor dataExtractor;
    
    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
        
        // 查找场景数据提取器
        dataExtractor = FindObjectOfType<SceneDataExtractor>();
        if (dataExtractor == null)
        {
            Debug.LogWarning("未找到SceneDataExtractor，将创建默认场景数据");
            currentSceneData = new MusicSceneData();
        }
        else
        {
            currentSceneData = dataExtractor.GetCurrentSceneData();
        }
        
        // 订阅事件
        SceneDataExtractor.OnSceneDataUpdated += OnSceneDataUpdated;
        MusicPresetManager.OnPresetSelected += OnPresetSelected;
        MusicPresetManager.OnPresetGenerated += OnPresetGenerated;
        
        // 开始UI更新
        StartCoroutine(UpdateUI());
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        SceneDataExtractor.OnSceneDataUpdated -= OnSceneDataUpdated;
        MusicPresetManager.OnPresetSelected -= OnPresetSelected;
        MusicPresetManager.OnPresetGenerated -= OnPresetGenerated;
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 初始化下拉菜单
        InitializeDropdowns();
        
        // 设置默认值
        if (currentSceneData != null)
        {
            UpdateUIFromSceneData(currentSceneData);
        }
        
        // 隐藏主面板
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 初始化下拉菜单
    /// </summary>
    private void InitializeDropdowns()
    {
        // 环境下拉菜单
        if (environmentDropdown != null)
        {
            environmentDropdown.ClearOptions();
            environmentDropdown.AddOptions(new List<string> { "草原", "森林", "地牢", "城市", "海洋", "山脉", "沙漠", "雪地" });
        }
        
        // 动作下拉菜单
        if (actionDropdown != null)
        {
            actionDropdown.ClearOptions();
            actionDropdown.AddOptions(new List<string> { "行走", "奔跑", "战斗", "潜行", "飞行", "游泳" });
        }
        
        // 敌人下拉菜单
        if (enemyDropdown != null)
        {
            enemyDropdown.ClearOptions();
            enemyDropdown.AddOptions(new List<string> { "无", "少量", "大量", "Boss", "潜伏" });
        }
        
        // 时间下拉菜单
        if (timeDropdown != null)
        {
            timeDropdown.ClearOptions();
            timeDropdown.AddOptions(new List<string> { "白天", "夜晚", "黎明", "黄昏" });
        }
        
        // 天气下拉菜单
        if (weatherDropdown != null)
        {
            weatherDropdown.ClearOptions();
            weatherDropdown.AddOptions(new List<string> { "晴朗", "雨天", "雪天", "暴风雨", "雾天" });
        }
        
        // 关卡下拉菜单
        if (levelDropdown != null)
        {
            levelDropdown.ClearOptions();
            levelDropdown.AddOptions(new List<string> { "教程", "早期", "中期", "后期", "最终Boss" });
        }
    }
    
    /// <summary>
    /// 设置事件监听器
    /// </summary>
    private void SetupEventListeners()
    {
        // 切换按钮
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleUI);
        }
        
        // 场景数据面板事件
        if (environmentDropdown != null)
        {
            environmentDropdown.onValueChanged.AddListener((index) => UpdateSceneDataFromUI());
        }
        if (actionDropdown != null)
        {
            actionDropdown.onValueChanged.AddListener((index) => UpdateSceneDataFromUI());
        }
        if (enemyDropdown != null)
        {
            enemyDropdown.onValueChanged.AddListener((index) => UpdateSceneDataFromUI());
        }
        if (timeDropdown != null)
        {
            timeDropdown.onValueChanged.AddListener((index) => UpdateSceneDataFromUI());
        }
        if (weatherDropdown != null)
        {
            weatherDropdown.onValueChanged.AddListener((index) => UpdateSceneDataFromUI());
        }
        if (levelDropdown != null)
        {
            levelDropdown.onValueChanged.AddListener((index) => UpdateSceneDataFromUI());
        }
        if (intensitySlider != null)
        {
            intensitySlider.onValueChanged.AddListener((value) => UpdateSceneDataFromUI());
        }
        if (threatSlider != null)
        {
            threatSlider.onValueChanged.AddListener((value) => UpdateSceneDataFromUI());
        }
        if (healthSlider != null)
        {
            healthSlider.onValueChanged.AddListener((value) => UpdateSceneDataFromUI());
        }
        if (stealthToggle != null)
        {
            stealthToggle.onValueChanged.AddListener((value) => UpdateSceneDataFromUI());
        }
        if (bossFightToggle != null)
        {
            bossFightToggle.onValueChanged.AddListener((value) => UpdateSceneDataFromUI());
        }
        
        // 预设管理事件
        if (generatePresetButton != null)
        {
            generatePresetButton.onClick.AddListener(GeneratePresetBatch);
        }
        if (clearPresetsButton != null)
        {
            clearPresetsButton.onClick.AddListener(ClearAllPresets);
        }
        
        // 实时数据事件
        if (extractDataButton != null)
        {
            extractDataButton.onClick.AddListener(ManualExtractData);
        }
        if (playMusicButton != null)
        {
            playMusicButton.onClick.AddListener(PlayCurrentMusic);
        }
        
        // 调试事件
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshDebugInfo);
        }
    }
    
    /// <summary>
    /// 切换UI显示
    /// </summary>
    public void ToggleUI()
    {
        isUIVisible = !isUIVisible;
        
        if (mainPanel != null)
        {
            mainPanel.SetActive(isUIVisible);
        }
        
        if (toggleButtonText != null)
        {
            toggleButtonText.text = isUIVisible ? "隐藏音乐系统" : "显示音乐系统";
        }
    }
    
    /// <summary>
    /// 从场景数据更新UI
    /// </summary>
    private void UpdateUIFromSceneData(MusicSceneData data)
    {
        if (data == null) return;
        
        // 更新下拉菜单
        if (environmentDropdown != null)
        {
            environmentDropdown.value = (int)data.environment;
        }
        if (actionDropdown != null)
        {
            actionDropdown.value = (int)data.currentAction;
        }
        if (enemyDropdown != null)
        {
            enemyDropdown.value = (int)data.enemyPresence;
        }
        if (timeDropdown != null)
        {
            timeDropdown.value = (int)data.timeOfDay;
        }
        if (weatherDropdown != null)
        {
            weatherDropdown.value = (int)data.weather;
        }
        if (levelDropdown != null)
        {
            levelDropdown.value = (int)data.gameLevel;
        }
        
        // 更新滑块
        if (intensitySlider != null)
        {
            intensitySlider.value = data.actionIntensity;
        }
        if (threatSlider != null)
        {
            threatSlider.value = data.threatLevel;
        }
        if (healthSlider != null)
        {
            healthSlider.value = data.playerHealth;
        }
        
        // 更新开关
        if (stealthToggle != null)
        {
            stealthToggle.isOn = data.isStealth;
        }
        if (bossFightToggle != null)
        {
            bossFightToggle.isOn = data.isBossFight;
        }
    }
    
    /// <summary>
    /// 从UI更新场景数据
    /// </summary>
    private void UpdateSceneDataFromUI()
    {
        if (currentSceneData == null)
        {
            currentSceneData = new MusicSceneData();
        }
        
        // 从下拉菜单获取值
        if (environmentDropdown != null)
        {
            currentSceneData.environment = (EnvironmentType)environmentDropdown.value;
        }
        if (actionDropdown != null)
        {
            currentSceneData.currentAction = (ActionType)actionDropdown.value;
        }
        if (enemyDropdown != null)
        {
            currentSceneData.enemyPresence = (EnemyPresence)enemyDropdown.value;
        }
        if (timeDropdown != null)
        {
            currentSceneData.timeOfDay = (TimeOfDay)timeDropdown.value;
        }
        if (weatherDropdown != null)
        {
            currentSceneData.weather = (WeatherType)weatherDropdown.value;
        }
        if (levelDropdown != null)
        {
            currentSceneData.gameLevel = (GameLevel)levelDropdown.value;
        }
        
        // 从滑块获取值
        if (intensitySlider != null)
        {
            currentSceneData.actionIntensity = intensitySlider.value;
        }
        if (threatSlider != null)
        {
            currentSceneData.threatLevel = threatSlider.value;
        }
        if (healthSlider != null)
        {
            currentSceneData.playerHealth = healthSlider.value;
        }
        
        // 从开关获取值
        if (stealthToggle != null)
        {
            currentSceneData.isStealth = stealthToggle.isOn;
        }
        if (bossFightToggle != null)
        {
            currentSceneData.isBossFight = bossFightToggle.isOn;
        }
        
        // 更新场景数据提取器
        if (dataExtractor != null)
        {
            dataExtractor.SetSceneData(currentSceneData);
        }
    }
    
    /// <summary>
    /// 手动提取数据
    /// </summary>
    public void ManualExtractData()
    {
        if (dataExtractor != null)
        {
            dataExtractor.ManualExtract();
        }
    }
    
    /// <summary>
    /// 播放当前音乐
    /// </summary>
    public void PlayCurrentMusic()
    {
        if (AIGeneratedMusic.Instance != null && currentSceneData != null)
        {
            AIGeneratedMusic.Instance.GenerateMusicForScene(currentSceneData);
        }
    }
    
    /// <summary>
    /// 生成预设批次
    /// </summary>
    public void GeneratePresetBatch()
    {
        if (MusicPresetManager.Instance != null)
        {
            MusicPresetManager.Instance.GeneratePresetBatch();
        }
    }
    
    /// <summary>
    /// 清除所有预设
    /// </summary>
    public void ClearAllPresets()
    {
        if (MusicPresetManager.Instance != null)
        {
            MusicPresetManager.Instance.ClearAllPresets();
        }
    }
    
    /// <summary>
    /// 刷新调试信息
    /// </summary>
    public void RefreshDebugInfo()
    {
        UpdateDebugInfo();
    }
    
    /// <summary>
    /// 更新UI
    /// </summary>
    private IEnumerator UpdateUI()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            
            if (isUIVisible)
            {
                UpdateRealtimeInfo();
                UpdatePresetInfo();
                UpdateDebugInfo();
            }
        }
    }
    
    /// <summary>
    /// 更新实时信息
    /// </summary>
    private void UpdateRealtimeInfo()
    {
        if (currentSceneData == null) return;
        
        if (currentSceneText != null)
        {
            currentSceneText.text = $"场景: {currentSceneData.sceneName}";
        }
        if (currentActionText != null)
        {
            currentActionText.text = $"动作: {currentSceneData.currentAction}";
        }
        if (threatLevelText != null)
        {
            threatLevelText.text = $"威胁: {currentSceneData.threatLevel:F2}";
        }
        if (intensityText != null)
        {
            intensityText.text = $"强度: {currentSceneData.CalculateIntensity():F2}";
        }
    }
    
    /// <summary>
    /// 更新预设信息
    /// </summary>
    private void UpdatePresetInfo()
    {
        if (MusicPresetManager.Instance != null && presetStatsText != null)
        {
            presetStatsText.text = MusicPresetManager.Instance.GetPresetStats();
        }
    }
    
    /// <summary>
    /// 更新调试信息
    /// </summary>
    private void UpdateDebugInfo()
    {
        if (debugText == null) return;
        
        string debugInfo = "";
        
        // 当前场景数据
        if (currentSceneData != null)
        {
            debugInfo += $"当前场景数据:\n{currentSceneData.ToJson()}\n\n";
        }
        
        // 音频管理器状态
        if (AudioManager.Instance != null)
        {
            debugInfo += $"音频管理器: {AudioManager.Instance.GetCurrentZoneName()}\n";
        }
        
        // AI生成器状态
        if (AIGeneratedMusic.Instance != null)
        {
            debugInfo += $"AI生成器: {(AIGeneratedMusic.Instance.IsGenerating() ? "生成中" : "空闲")}\n";
        }
        
        // 预设管理器状态
        if (MusicPresetManager.Instance != null)
        {
            debugInfo += $"预设管理器: {MusicPresetManager.Instance.GetPresetStats()}\n";
        }
        
        debugText.text = debugInfo;
    }
    
    /// <summary>
    /// 场景数据更新事件
    /// </summary>
    private void OnSceneDataUpdated(MusicSceneData data)
    {
        currentSceneData = data;
        UpdateUIFromSceneData(data);
    }
    
    /// <summary>
    /// 预设选择事件
    /// </summary>
    private void OnPresetSelected(MusicPreset preset)
    {
        if (showDebugInfo)
        {
            Debug.Log($"选择了预设: {preset.presetName}");
        }
    }
    
    /// <summary>
    /// 预设生成事件
    /// </summary>
    private void OnPresetGenerated(MusicPreset preset)
    {
        if (showDebugInfo)
        {
            Debug.Log($"生成了新预设: {preset.presetName}");
        }
    }
    
    /// <summary>
    /// 键盘快捷键
    /// </summary>
    private void Update()
    {
        // F1 切换UI
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleUI();
        }
        
        // F2 手动提取数据
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ManualExtractData();
        }
        
        // F3 播放音乐
        if (Input.GetKeyDown(KeyCode.F3))
        {
            PlayCurrentMusic();
        }
    }
} 