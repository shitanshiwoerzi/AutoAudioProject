using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMusicTest : MonoBehaviour
{
    [Header("测试设置")]
    public bool autoTestOnStart = false;
    public float testInterval = 10.0f;
    
    [Header("测试提示词")]
    public string[] testPrompts = {
        "Peaceful forest ambient music with gentle nature sounds",
        "Urban city background music with modern electronic beats",
        "Dark cave atmospheric music with deep echoes",
        "Ocean beach music with waves crashing and seagulls"
    };
    
    private AIGeneratedMusic aiMusicGenerator;
    private int currentTestIndex = 0;
    private bool isTesting = false;
    
    void Start()
    {
        aiMusicGenerator = FindObjectOfType<AIGeneratedMusic>();
        
        if (aiMusicGenerator == null)
        {
            Debug.LogError("未找到AIGeneratedMusic组件！");
            return;
        }
        
        if (autoTestOnStart)
        {
            StartCoroutine(AutoTest());
        }
    }
    
    [ContextMenu("开始自动测试")]
    public void StartAutoTest()
    {
        if (!isTesting)
        {
            StartCoroutine(AutoTest());
        }
    }
    
    [ContextMenu("停止自动测试")]
    public void StopAutoTest()
    {
        isTesting = false;
        Debug.Log("自动测试已停止");
    }
    
    private IEnumerator AutoTest()
    {
        isTesting = true;
        Debug.Log("开始AI音乐生成自动测试");
        
        while (isTesting && currentTestIndex < testPrompts.Length)
        {
            string prompt = testPrompts[currentTestIndex];
            Debug.Log($"测试 {currentTestIndex + 1}/{testPrompts.Length}: {prompt}");
            
            // 生成音乐
            aiMusicGenerator.GenerateMusicForZone(prompt, $"测试区域{currentTestIndex + 1}");
            
            // 等待生成完成或超时
            float waitTime = 0f;
            while (aiMusicGenerator.IsGenerating() && waitTime < 60f)
            {
                yield return new WaitForSeconds(1f);
                waitTime += 1f;
            }
            
            if (aiMusicGenerator.IsGenerating())
            {
                Debug.LogWarning($"测试 {currentTestIndex + 1} 超时");
                aiMusicGenerator.StopGeneration();
            }
            
            currentTestIndex++;
            
            if (isTesting && currentTestIndex < testPrompts.Length)
            {
                yield return new WaitForSeconds(testInterval);
            }
        }
        
        Debug.Log("AI音乐生成自动测试完成");
        isTesting = false;
    }
    
    [ContextMenu("测试单个提示词")]
    public void TestSinglePrompt()
    {
        if (aiMusicGenerator == null) return;
        
        string prompt = testPrompts[Random.Range(0, testPrompts.Length)];
        Debug.Log($"测试单个提示词: {prompt}");
        aiMusicGenerator.GenerateMusicForZone(prompt, "单次测试");
    }
    
    [ContextMenu("测试区域音乐生成")]
    public void TestZoneMusicGeneration()
    {
        if (aiMusicGenerator == null) return;
        
        int zoneID = Random.Range(1, 5);
        string zoneName = $"测试区域{zoneID}";
        Debug.Log($"测试区域 {zoneID} 音乐生成: {zoneName}");
        aiMusicGenerator.GenerateMusicForZone(zoneID, zoneName);
    }
    
    [ContextMenu("清除缓存")]
    public void ClearCache()
    {
        if (aiMusicGenerator != null)
        {
            aiMusicGenerator.ClearCache();
            Debug.Log("音乐缓存已清除");
        }
    }
    
    [ContextMenu("显示缓存信息")]
    public void ShowCacheInfo()
    {
        if (aiMusicGenerator != null)
        {
            Debug.Log($"缓存信息: {aiMusicGenerator.GetCacheInfo()}");
        }
    }
    
    void Update()
    {
        // 键盘快捷键测试
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestSinglePrompt();
        }
        
        if (Input.GetKeyDown(KeyCode.Z))
        {
            TestZoneMusicGeneration();
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearCache();
        }
        
        if (Input.GetKeyDown(KeyCode.I))
        {
            ShowCacheInfo();
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTesting)
            {
                StopAutoTest();
            }
            else
            {
                StartAutoTest();
            }
        }
    }
    
    void OnGUI()
    {
        if (aiMusicGenerator == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("AI音乐生成测试控制台");
        
        if (GUILayout.Button("测试单个提示词 (T)"))
        {
            TestSinglePrompt();
        }
        
        if (GUILayout.Button("测试区域音乐 (Z)"))
        {
            TestZoneMusicGeneration();
        }
        
        if (GUILayout.Button("清除缓存 (C)"))
        {
            ClearCache();
        }
        
        if (GUILayout.Button("显示缓存信息 (I)"))
        {
            ShowCacheInfo();
        }
        
        if (GUILayout.Button(isTesting ? "停止自动测试 (Space)" : "开始自动测试 (Space)"))
        {
            if (isTesting)
            {
                StopAutoTest();
            }
            else
            {
                StartAutoTest();
            }
        }
        
        GUILayout.Label($"生成状态: {(aiMusicGenerator.IsGenerating() ? "生成中" : "就绪")}");
        GUILayout.Label($"缓存信息: {aiMusicGenerator.GetCacheInfo()}");
        
        GUILayout.EndArea();
    }
} 