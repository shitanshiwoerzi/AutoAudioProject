using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SunoAPIConfigUI : MonoBehaviour
{
    [Header("UI组件")]
    public InputField apiKeyInput;
    public InputField baseUrlInput;
    public Dropdown modelDropdown;
    public Toggle customModeToggle;
    public Toggle instrumentalToggle;
    public InputField callbackUrlInput;
    public Button testConnectionButton;
    public Button saveConfigButton;
    public Text statusText;
    
    [Header("设置")]
    public bool showUI = true;
    
    private AIGeneratedMusic aiMusicGenerator;
    
    // 可用的模型选项
    private string[] availableModels = {
        "V3_5",
        "V3",
        "V2",
        "V1"
    };
    
    void Start()
    {
        aiMusicGenerator = FindObjectOfType<AIGeneratedMusic>();
        InitializeUI();
        LoadCurrentConfig();
    }
    
    private void InitializeUI()
    {
        // 初始化模型下拉菜单
        if (modelDropdown != null)
        {
            modelDropdown.ClearOptions();
            List<string> options = new List<string>(availableModels);
            modelDropdown.AddOptions(options);
        }
        
        // 设置按钮事件
        if (testConnectionButton != null)
        {
            testConnectionButton.onClick.AddListener(TestAPIConnection);
        }
        
        if (saveConfigButton != null)
        {
            saveConfigButton.onClick.AddListener(SaveConfiguration);
        }
        
        // 设置默认值
        if (baseUrlInput != null)
        {
            baseUrlInput.text = "https://api.sunoapi.org/api/v1";
        }
        
        UpdateStatus("配置界面已加载");
    }
    
    private void LoadCurrentConfig()
    {
        if (aiMusicGenerator == null) return;
        
        // 加载当前配置
        if (apiKeyInput != null)
        {
            apiKeyInput.text = aiMusicGenerator.apiKey;
        }
        
        if (baseUrlInput != null)
        {
            baseUrlInput.text = aiMusicGenerator.baseUrl;
        }
        
        if (modelDropdown != null)
        {
            int modelIndex = System.Array.IndexOf(availableModels, aiMusicGenerator.defaultModel);
            modelDropdown.value = modelIndex >= 0 ? modelIndex : 0;
        }
        
        if (customModeToggle != null)
        {
            customModeToggle.isOn = aiMusicGenerator.defaultCustomMode;
        }
        
        if (instrumentalToggle != null)
        {
            instrumentalToggle.isOn = aiMusicGenerator.defaultInstrumental;
        }
        
        if (callbackUrlInput != null)
        {
            callbackUrlInput.text = aiMusicGenerator.callBackUrl;
        }
    }
    
    public void TestAPIConnection()
    {
        if (aiMusicGenerator == null)
        {
            UpdateStatus("错误: 未找到AIGeneratedMusic组件", Color.red);
            return;
        }
        
        UpdateStatus("正在测试API连接...", Color.yellow);
        aiMusicGenerator.TestAPIConnection();
        
        // 延迟更新状态
        StartCoroutine(UpdateStatusAfterDelay("API连接测试完成", Color.green, 3f));
    }
    
    public void SaveConfiguration()
    {
        if (aiMusicGenerator == null)
        {
            UpdateStatus("错误: 未找到AIGeneratedMusic组件", Color.red);
            return;
        }
        
        // 保存配置
        if (apiKeyInput != null)
        {
            aiMusicGenerator.apiKey = apiKeyInput.text;
        }
        
        if (baseUrlInput != null)
        {
            aiMusicGenerator.baseUrl = baseUrlInput.text;
        }
        
        if (modelDropdown != null)
        {
            aiMusicGenerator.defaultModel = availableModels[modelDropdown.value];
        }
        
        if (customModeToggle != null)
        {
            aiMusicGenerator.defaultCustomMode = customModeToggle.isOn;
        }
        
        if (instrumentalToggle != null)
        {
            aiMusicGenerator.defaultInstrumental = instrumentalToggle.isOn;
        }
        
        if (callbackUrlInput != null)
        {
            aiMusicGenerator.callBackUrl = callbackUrlInput.text;
        }
        
        UpdateStatus("配置已保存", Color.green);
        
        // 显示当前配置
        Debug.Log("=== Suno API 配置已更新 ===");
        Debug.Log($"API密钥: {(string.IsNullOrEmpty(aiMusicGenerator.apiKey) ? "未设置" : "已设置")}");
        Debug.Log($"基础URL: {aiMusicGenerator.baseUrl}");
        Debug.Log($"模型: {aiMusicGenerator.defaultModel}");
        Debug.Log($"自定义模式: {aiMusicGenerator.defaultCustomMode}");
        Debug.Log($"纯音乐: {aiMusicGenerator.defaultInstrumental}");
        Debug.Log($"回调URL: {aiMusicGenerator.callBackUrl}");
    }
    
    public void GenerateTestMusic()
    {
        if (aiMusicGenerator == null)
        {
            UpdateStatus("错误: 未找到AIGeneratedMusic组件", Color.red);
            return;
        }
        
        UpdateStatus("正在生成测试音乐...", Color.yellow);
        aiMusicGenerator.GenerateMusicForZone("Test ambient music for Unity game", "测试区域");
    }
    
    public void ClearCache()
    {
        if (aiMusicGenerator == null)
        {
            UpdateStatus("错误: 未找到AIGeneratedMusic组件", Color.red);
            return;
        }
        
        aiMusicGenerator.ClearCache();
        UpdateStatus("缓存已清除", Color.green);
    }
    
    private void UpdateStatus(string message, Color color = default)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color == default ? Color.white : color;
        }
        
        Debug.Log($"Suno API Config: {message}");
    }
    
    private IEnumerator UpdateStatusAfterDelay(string message, Color color, float delay)
    {
        yield return new WaitForSeconds(delay);
        UpdateStatus(message, color);
    }
    
    void OnGUI()
    {
        if (!showUI) return;
        
        GUILayout.BeginArea(new Rect(10, 450, 350, 300));
        GUILayout.Label("Suno API 配置面板");
        
        if (GUILayout.Button("测试API连接"))
        {
            TestAPIConnection();
        }
        
        if (GUILayout.Button("保存配置"))
        {
            SaveConfiguration();
        }
        
        if (GUILayout.Button("生成测试音乐"))
        {
            GenerateTestMusic();
        }
        
        if (GUILayout.Button("清除缓存"))
        {
            ClearCache();
        }
        
        GUILayout.Space(10);
        
        // 显示当前配置信息
        if (aiMusicGenerator != null)
        {
            GUILayout.Label("当前配置:");
            GUILayout.Label($"模型: {aiMusicGenerator.defaultModel}");
            GUILayout.Label($"自定义模式: {aiMusicGenerator.defaultCustomMode}");
            GUILayout.Label($"纯音乐: {aiMusicGenerator.defaultInstrumental}");
            GUILayout.Label($"生成状态: {(aiMusicGenerator.IsGenerating() ? "生成中" : "就绪")}");
            GUILayout.Label($"缓存信息: {aiMusicGenerator.GetCacheInfo()}");
        }
        
        GUILayout.EndArea();
    }
    
    // 公共方法：显示/隐藏UI
    public void SetUIVisible(bool visible)
    {
        showUI = visible;
    }
    
    // 公共方法：获取当前配置
    public string GetCurrentConfig()
    {
        if (aiMusicGenerator == null) return "AIGeneratedMusic组件未找到";
        
        return $"模型: {aiMusicGenerator.defaultModel}, " +
               $"自定义模式: {aiMusicGenerator.defaultCustomMode}, " +
               $"纯音乐: {aiMusicGenerator.defaultInstrumental}, " +
               $"回调URL: {aiMusicGenerator.callBackUrl}";
    }
} 