using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;

[System.Serializable]
public class SunoGenerateRequest
{
    public string prompt;
    public string model = "V3_5";
    public bool customMode = false;
    public bool instrumental = false;
    public string callBackUrl = "";
}

[System.Serializable]
public class SunoGenerateResponse
{
    public string id;
    public string status;
    public string audio_url;
    public string error;
    public string message;
}

[System.Serializable]
public class SunoStatusResponse
{
    public string id;
    public string status;
    public string audio_url;
    public string error;
    public string message;
}

public class AIGeneratedMusic : MonoBehaviour
{
    // 单例模式
    public static AIGeneratedMusic Instance;
    
    [Header("Suno API 设置")]
    public string apiKey = "your_suno_api_key_here";
    public string baseUrl = "https://api.sunoapi.org/api/v1";
    
    [Header("音乐生成设置")]
    public string defaultModel = "V3_5";
    public bool defaultCustomMode = false;
    public bool defaultInstrumental = false;
    public string callBackUrl = "https://api.example.com/callback";
    
    [Header("缓存设置")]
    public bool enableCaching = true;
    public int maxCacheSize = 10; // 最大缓存数量
    public string cacheFolder = "SunoMusicCache";
    
    [Header("调试")]
    public bool showDebugInfo = true;
    public bool useMockAPI = false; // 使用模拟API进行测试
    
    // 音乐缓存
    private Dictionary<string, AudioClip> musicCache = new Dictionary<string, AudioClip>();
    private Queue<string> cacheQueue = new Queue<string>();
    
    // 当前生成状态
    private bool isGenerating = false;
    private string currentGenerationId = "";
    private MusicSceneData pendingSceneDataForSave = null; // AI生成入库时的场景上下文
    
    // 事件
    public static event Action<AudioClip> OnMusicGenerated;
    public static event Action<string> OnGenerationFailed;
    public static event Action<float> OnGenerationProgress;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 确保缓存文件夹存在
            if (enableCaching)
            {
                CreateCacheFolder();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 生成音乐并播放
    /// </summary>
    /// <param name="prompt">音乐描述提示词</param>
    /// <param name="zoneName">区域名称（用于缓存）</param>
    public void GenerateMusicForZone(string prompt, string zoneName = "")
    {
        if (isGenerating)
        {
            Debug.LogWarning("正在生成音乐中，请稍后再试");
            return;
        }
        
        // 检查缓存
        string cacheKey = GetCacheKey(prompt, zoneName);
        if (enableCaching && musicCache.ContainsKey(cacheKey))
        {
            if (showDebugInfo)
            {
                Debug.Log($"使用缓存的音乐: {cacheKey}");
            }
            
            AudioClip cachedClip = musicCache[cacheKey];
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(cachedClip, 1.0f, zoneName);
            }
            OnMusicGenerated?.Invoke(cachedClip);
            return;
        }
        
        StartCoroutine(GenerateAndPlayMusic(prompt, zoneName));
    }
    
    /// <summary>
    /// 根据场景数据生成音乐（新方法）
    /// </summary>
    /// <param name="sceneData">场景数据</param>
    public void GenerateMusicForScene(MusicSceneData sceneData)
    {
        if (isGenerating)
        {
            Debug.LogWarning("正在生成音乐中，请稍后再试");
            return;
        }
        
        // 首先尝试从预设中选择
        if (MusicPresetManager.Instance != null)
        {
            MusicPreset bestPreset = MusicPresetManager.Instance.SelectBestPreset(sceneData);
            if (bestPreset != null)
            {
                StartCoroutine(LoadAndPlayPreset(bestPreset, sceneData));
                return;
            }
        }
        
        // 如果没有合适的预设，使用AI生成
        string prompt = sceneData.GetMusicStyleDescription();
        pendingSceneDataForSave = sceneData; // 记录上下文，便于保存到库
        StartCoroutine(GenerateAndPlayMusic(prompt, sceneData.sceneName));
    }

    /// <summary>
    /// 强制使用AI生成（绕过预设选择），用于批量生成入库
    /// </summary>
    public void GenerateMusicForSceneForceAI(MusicSceneData sceneData)
    {
        if (isGenerating)
        {
            Debug.LogWarning("正在生成音乐中，请稍后再试");
            return;
        }
        string prompt = sceneData.GetMusicStyleDescription();
        pendingSceneDataForSave = sceneData; // 记录上下文，便于保存到库
        StartCoroutine(GenerateAndPlayMusic(prompt, sceneData.sceneName));
    }
    
    /// <summary>
    /// 加载并播放预设音乐
    /// </summary>
    private IEnumerator LoadAndPlayPreset(MusicPreset preset, MusicSceneData sceneData)
    {
        // 如果音频还没有加载，先加载
        if (preset.musicClip == null)
        {
            yield return StartCoroutine(MusicPresetManager.Instance.LoadAudioFileOnDemand(preset));
        }
        
        // 检查加载是否成功
        if (preset.musicClip != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"使用预设音乐: {preset.presetName}");
            }
            
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(preset.musicClip, 1.0f, sceneData.sceneName);
            }
            OnMusicGenerated?.Invoke(preset.musicClip);
        }
        else
        {
            Debug.LogError($"无法加载预设音乐: {preset.presetName}");
            // 回退到AI生成
            string prompt = sceneData.GetMusicStyleDescription();
            StartCoroutine(GenerateAndPlayMusic(prompt, sceneData.sceneName));
        }
    }
    
    /// <summary>
    /// 为特定区域生成音乐
    /// </summary>
    /// <param name="zoneID">区域ID</param>
    /// <param name="zoneName">区域名称</param>
    public void GenerateMusicForZone(int zoneID, string zoneName)
    {
        string prompt = GetZonePrompt(zoneID, zoneName);
        GenerateMusicForZone(prompt, zoneName);
    }
    
    /// <summary>
    /// 根据区域ID获取音乐提示词
    /// </summary>
    private string GetZonePrompt(int zoneID, string zoneName)
    {
        switch (zoneID)
        {
            case 1:
                return $"Peaceful forest ambient music with gentle nature sounds, birds chirping, soft wind, calming and relaxing atmosphere for {zoneName}";
            case 2:
                return $"Urban city background music with modern electronic beats, traffic sounds, people talking, energetic and dynamic for {zoneName}";
            case 3:
                return $"Dark cave atmospheric music with deep echoes, dripping water, mysterious and suspenseful ambient sounds for {zoneName}";
            case 4:
                return $"Ocean beach music with waves crashing, seagulls, gentle breeze, tropical and peaceful atmosphere for {zoneName}";
            default:
                return $"Background music for {zoneName}, ambient and atmospheric";
        }
    }
    
    /// <summary>
    /// 生成并播放音乐的主要协程
    /// </summary>
    private IEnumerator GenerateAndPlayMusic(string prompt, string zoneName)
    {
        isGenerating = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"开始生成音乐: {prompt}");
        }
        
        // 检查API密钥
        if (string.IsNullOrEmpty(apiKey) || apiKey == "your_suno_api_key_here")
        {
            Debug.LogError("请设置有效的Suno API密钥！");
            OnGenerationFailed?.Invoke("API密钥未设置");
            isGenerating = false;
            yield break;
        }
        
        // 使用模拟API进行测试
        if (useMockAPI)
        {
            yield return StartCoroutine(MockAPIGeneration(prompt, zoneName));
            isGenerating = false;
            yield break;
        }
        
        // 1. 发送生成请求
        SunoGenerateRequest requestData = new SunoGenerateRequest
        {
            prompt = prompt,
            model = defaultModel,
            customMode = defaultCustomMode,
            instrumental = defaultInstrumental,
            callBackUrl = callBackUrl
        };
        
        string jsonData = JsonUtility.ToJson(requestData);
        
        if (showDebugInfo)
        {
            Debug.Log($"发送请求到: {baseUrl}/generate");
            Debug.Log($"请求数据: {jsonData}");
        }
        
        using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/generate", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"生成请求失败: {request.error}");
                Debug.LogError($"响应代码: {request.responseCode}");
                Debug.LogError($"响应内容: {request.downloadHandler.text}");
                OnGenerationFailed?.Invoke($"请求失败: {request.error}");
                isGenerating = false;
                yield break;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"API响应: {request.downloadHandler.text}");
            }
            
            SunoGenerateResponse response = JsonUtility.FromJson<SunoGenerateResponse>(request.downloadHandler.text);
            
            if (!string.IsNullOrEmpty(response.error))
            {
                Debug.LogError($"API错误: {response.error}");
                OnGenerationFailed?.Invoke(response.error);
                isGenerating = false;
                yield break;
            }
            
            if (string.IsNullOrEmpty(response.id))
            {
                Debug.LogError("API返回的ID为空");
                OnGenerationFailed?.Invoke("生成ID为空");
                isGenerating = false;
                yield break;
            }
            
            currentGenerationId = response.id;
            
            if (showDebugInfo)
            {
                Debug.Log($"音乐生成已提交，ID: {currentGenerationId}");
            }
        }
        
        // 2. 轮询检查生成状态
        yield return StartCoroutine(PollGenerationStatus());
        
        isGenerating = false;
    }
    
    /// <summary>
    /// 模拟API生成（用于测试）
    /// </summary>
    private IEnumerator MockAPIGeneration(string prompt, string zoneName)
    {
        Debug.Log("使用模拟API进行测试...");
        
        // 模拟生成延迟
        yield return new WaitForSeconds(3f);
        
        // 模拟成功生成
        if (showDebugInfo)
        {
            Debug.Log($"模拟音乐生成成功: {prompt}");
        }
        
        // 创建一个简单的音频片段（静音）
        AudioClip mockClip = AudioClip.Create("MockMusic", 44100 * 10, 1, 44100, false);
        
        // 缓存音乐
        string cacheKey = GetCacheKey(prompt, zoneName);
        if (enableCaching)
        {
            CacheMusic(cacheKey, mockClip);
        }
        
        // 播放音乐
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(mockClip, 1.0f, zoneName);
        }
        
        OnMusicGenerated?.Invoke(mockClip);
    }
    
    /// <summary>
    /// 轮询检查音乐生成状态
    /// </summary>
    private IEnumerator PollGenerationStatus()
    {
        float pollInterval = 2.0f; // 每2秒检查一次
        float maxWaitTime = 300.0f; // 最大等待5分钟
        float elapsedTime = 0f;
        
        while (elapsedTime < maxWaitTime)
        {
            yield return new WaitForSeconds(pollInterval);
            elapsedTime += pollInterval;
            
            OnGenerationProgress?.Invoke(elapsedTime / maxWaitTime);
            
            string statusUrl = $"{baseUrl}/status/{currentGenerationId}";
            
            if (showDebugInfo)
            {
                Debug.Log($"检查状态: {statusUrl}");
            }
            
            using (UnityWebRequest request = UnityWebRequest.Get(statusUrl))
            {
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                
                yield return request.SendWebRequest();
                
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"状态检查失败: {request.error}");
                    Debug.LogError($"状态检查响应代码: {request.responseCode}");
                    Debug.LogError($"状态检查响应内容: {request.downloadHandler.text}");
                    
                    // 如果是404错误，可能是API端点不正确
                    if (request.responseCode == 404)
                    {
                        Debug.LogError("404错误：API端点可能不正确，请检查Suno API文档");
                        OnGenerationFailed?.Invoke("API端点404错误，请检查API配置");
                        yield break;
                    }
                    
                    continue;
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"状态响应: {request.downloadHandler.text}");
                }
                
                SunoStatusResponse statusResponse = JsonUtility.FromJson<SunoStatusResponse>(request.downloadHandler.text);
                
                if (statusResponse.status == "complete")
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"音乐生成完成，下载中...");
                    }
                    
                    // 3. 下载生成的音乐
                    yield return StartCoroutine(DownloadAndPlayMusic(statusResponse.audio_url));
                    yield break;
                }
                else if (statusResponse.status == "failed")
                {
                    Debug.LogError($"音乐生成失败: {statusResponse.error}");
                    OnGenerationFailed?.Invoke(statusResponse.error);
                    yield break;
                }
                else if (statusResponse.status == "processing")
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"音乐生成中... ({elapsedTime:F1}s)");
                    }
                }
            }
        }
        
        Debug.LogError("音乐生成超时");
        OnGenerationFailed?.Invoke("生成超时");
    }
    
    /// <summary>
    /// 下载并播放音乐
    /// </summary>
    private IEnumerator DownloadAndPlayMusic(string audioUrl)
    {
        if (string.IsNullOrEmpty(audioUrl))
        {
            Debug.LogError("音频URL为空");
            OnGenerationFailed?.Invoke("音频URL为空");
            yield break;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"下载音频: {audioUrl}");
        }
        
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.MPEG))
        {
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"音乐下载失败: {request.error}");
                OnGenerationFailed?.Invoke(request.error);
                yield break;
            }
            
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
            
            if (audioClip != null)
            {
                // 缓存音乐
                string cacheKey = GetCacheKey(currentGenerationId, "");
                if (enableCaching)
                {
                    CacheMusic(cacheKey, audioClip);
                }
                
                // 播放音乐
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayMusic(audioClip, 1.0f, "");
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"音乐播放成功: {audioClip.name}");
                }
                
                // 可选：自动保存到预设库
                if (MusicPresetManager.Instance != null && MusicPresetManager.Instance.autoSaveAIGeneratedToLibrary)
                {
                    try
                    {
                        var sceneForSave = pendingSceneDataForSave;
                        if (sceneForSave == null)
                        {
                            // 兜底占位
                            sceneForSave = new MusicSceneData
                            {
                                sceneName = "AI_Generated",
                                environment = EnvironmentType.Grasslands,
                                currentAction = ActionType.Walking,
                                enemyPresence = EnemyPresence.None,
                                gameLevel = GameLevel.Tutorial,
                                timeOfDay = TimeOfDay.Day
                            };
                        }
                        // 文件名使用 presetID 作为基础，便于去重管理
                        string baseName = $"{sceneForSave.environment}_{sceneForSave.currentAction}_{sceneForSave.enemyPresence}_{sceneForSave.gameLevel}_{sceneForSave.timeOfDay}_{Mathf.RoundToInt(sceneForSave.threatLevel * 10)}";
                        MusicPresetManager.Instance.CreatePresetAndSaveAudio(sceneForSave, audioClip, baseName);
                        pendingSceneDataForSave = null; // 重置上下文
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"自动保存到预设库失败: {e.Message}");
                    }
                }

                OnMusicGenerated?.Invoke(audioClip);
            }
            else
            {
                Debug.LogError("下载的音乐片段为空");
                OnGenerationFailed?.Invoke("音乐片段为空");
            }
        }
    }
    
    /// <summary>
    /// 获取缓存键
    /// </summary>
    private string GetCacheKey(string prompt, string zoneName)
    {
        return $"{zoneName}_{prompt.GetHashCode()}";
    }
    
    /// <summary>
    /// 缓存音乐
    /// </summary>
    private void CacheMusic(string key, AudioClip clip)
    {
        if (musicCache.Count >= maxCacheSize)
        {
            string oldestKey = cacheQueue.Dequeue();
            musicCache.Remove(oldestKey);
        }
        
        musicCache[key] = clip;
        cacheQueue.Enqueue(key);
        
        if (showDebugInfo)
        {
            Debug.Log($"音乐已缓存: {key} (缓存数量: {musicCache.Count})");
        }
    }
    
    /// <summary>
    /// 创建缓存文件夹
    /// </summary>
    private void CreateCacheFolder()
    {
        string cachePath = Path.Combine(Application.persistentDataPath, cacheFolder);
        if (!Directory.Exists(cachePath))
        {
            Directory.CreateDirectory(cachePath);
        }
    }
    
    /// <summary>
    /// 清除缓存
    /// </summary>
    public void ClearCache()
    {
        musicCache.Clear();
        cacheQueue.Clear();
        
        if (showDebugInfo)
        {
            Debug.Log("音乐缓存已清除");
        }
    }
    
    /// <summary>
    /// 获取缓存信息
    /// </summary>
    public string GetCacheInfo()
    {
        return $"缓存数量: {musicCache.Count}/{maxCacheSize}";
    }
    
    /// <summary>
    /// 检查是否正在生成
    /// </summary>
    public bool IsGenerating()
    {
        return isGenerating;
    }
    
    /// <summary>
    /// 停止当前生成
    /// </summary>
    public void StopGeneration()
    {
        if (isGenerating)
        {
            isGenerating = false;
            currentGenerationId = "";
            if (showDebugInfo)
            {
                Debug.Log("音乐生成已停止");
            }
        }
    }
    
    /// <summary>
    /// 测试API连接
    /// </summary>
    [ContextMenu("测试API连接")]
    public void TestAPIConnection()
    {
        StartCoroutine(TestAPI());
    }
    
    private IEnumerator TestAPI()
    {
        Debug.Log("测试Suno API连接...");
        
        using (UnityWebRequest request = UnityWebRequest.Get($"{baseUrl}/models"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("API连接成功！");
                Debug.Log($"可用模型: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"API连接失败: {request.error}");
                Debug.LogError($"响应代码: {request.responseCode}");
                Debug.LogError($"响应内容: {request.downloadHandler.text}");
            }
        }
    }
    
    /// <summary>
    /// 设置生成参数
    /// </summary>
    public void SetGenerationParameters(string model, bool customMode, bool instrumental, string callbackUrl = "")
    {
        defaultModel = model;
        defaultCustomMode = customMode;
        defaultInstrumental = instrumental;
        callBackUrl = callbackUrl;
        
        if (showDebugInfo)
        {
            Debug.Log($"生成参数已更新: Model={model}, CustomMode={customMode}, Instrumental={instrumental}");
        }
    }

	/// <summary>
	/// 提交生成请求（仅提交，返回job id），不占用全局isGenerating
	/// </summary>
	public IEnumerator SubmitGenerateRequest(string prompt, string label, Action<string> onId, Action<string> onError)
	{
		if (string.IsNullOrEmpty(apiKey) || apiKey == "your_suno_api_key_here")
		{
			onError?.Invoke("API密钥未设置");
			yield break;
		}
		if (useMockAPI)
		{
			// 模拟返回一个随机ID
			yield return null;
			onId?.Invoke(Guid.NewGuid().ToString());
			yield break;
		}
		SunoGenerateRequest requestData = new SunoGenerateRequest
		{
			prompt = prompt,
			model = defaultModel,
			customMode = defaultCustomMode,
			instrumental = defaultInstrumental,
			callBackUrl = callBackUrl
		};
		string jsonData = JsonUtility.ToJson(requestData);
		using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/generate", "POST"))
		{
			byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
			request.uploadHandler = new UploadHandlerRaw(bodyRaw);
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
			yield return request.SendWebRequest();
			if (request.result != UnityWebRequest.Result.Success)
			{
				onError?.Invoke($"提交失败: {request.error}");
				yield break;
			}
			var resp = JsonUtility.FromJson<SunoGenerateResponse>(request.downloadHandler.text);
			if (!string.IsNullOrEmpty(resp.error) || string.IsNullOrEmpty(resp.id))
			{
				onError?.Invoke(string.IsNullOrEmpty(resp.error) ? "生成ID为空" : resp.error);
				yield break;
			}
			onId?.Invoke(resp.id);
		}
	}

	/// <summary>
	/// 轮询job状态并下载音频，完成后通过回调返回AudioClip（不播放）
	/// </summary>
	public IEnumerator PollAndFetchClip(string jobId, Action<AudioClip, string> onClip, Action<string, string> onError)
	{
		if (useMockAPI)
		{
			yield return new WaitForSeconds(2f);
			var mock = AudioClip.Create("MockMusic", 44100 * 5, 1, 44100, false);
			onClip?.Invoke(mock, jobId);
			yield break;
		}
		float pollInterval = 2f;
		float maxWait = 300f;
		float elapsed = 0f;
		while (elapsed < maxWait)
		{
			yield return new WaitForSeconds(pollInterval);
			elapsed += pollInterval;
			using (UnityWebRequest request = UnityWebRequest.Get($"{baseUrl}/status/{jobId}"))
			{
				request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
				yield return request.SendWebRequest();
				if (request.result != UnityWebRequest.Result.Success)
				{
					continue; // 暂时忽略，下一轮再试
				}
				var status = JsonUtility.FromJson<SunoStatusResponse>(request.downloadHandler.text);
				if (status.status == "complete")
				{
					// 下载
					if (string.IsNullOrEmpty(status.audio_url))
					{
						onError?.Invoke(jobId, "音频URL为空");
						yield break;
					}
					using (UnityWebRequest dl = UnityWebRequestMultimedia.GetAudioClip(status.audio_url, AudioType.MPEG))
					{
						yield return dl.SendWebRequest();
						if (dl.result != UnityWebRequest.Result.Success)
						{
							onError?.Invoke(jobId, dl.error);
							yield break;
						}
						var clip = DownloadHandlerAudioClip.GetContent(dl);
						onClip?.Invoke(clip, jobId);
					}
					yield break;
				}
				else if (status.status == "failed")
				{
					onError?.Invoke(jobId, status.error);
					yield break;
				}
			}
		}
		onError?.Invoke(jobId, "超时");
	}
}
