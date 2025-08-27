using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Linq;

[System.Serializable]
public class MusicPreset
{
    public string presetID;
    public string presetName;
    public MusicSceneData sceneData;
    public AudioClip musicClip;
    public string audioFileName; // 音频文件名（用于外部文件加载）
    public float intensity;
    public string description;
    public bool isGenerated = false;
    public string generationDate;
}

public class MusicPresetManager : MonoBehaviour
{
    [Header("Preset Manager")]
    public List<MusicPreset> musicPresets = new List<MusicPreset>();
    public string presetFolder = "MusicPresets";
    
    [Header("Preset Configuration")]
    public bool autoGeneratePresets = true;
    public int maxPresetsPerCategory = 3;
    public bool autoSaveAIGeneratedToLibrary = true;
    
    [Header("Selection Strategy")]
    public SelectionStrategy selectionStrategy = SelectionStrategy.BestMatch;
    public float similarityThreshold = 0.8f;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // 单例模式
    public static MusicPresetManager Instance;
    
    // 事件
    public static event System.Action<MusicPreset> OnPresetSelected;
    public static event System.Action<MusicPreset> OnPresetGenerated;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPresets();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 根据场景数据选择最合适的音乐预设
    /// </summary>
    public MusicPreset SelectBestPreset(MusicSceneData sceneData)
    {
        if (musicPresets.Count == 0)
        {
            Debug.LogWarning("没有可用的音乐预设！");
            return null;
        }
        
        MusicPreset bestPreset = null;
        float bestScore = 0f;
        
        foreach (var preset in musicPresets)
        {
            // 检查是否有音频文件（即使还没有加载到内存）
            if (preset.musicClip == null && string.IsNullOrEmpty(preset.audioFileName)) continue;
            
            float score = CalculateSimilarityScore(sceneData, preset.sceneData);
            
            if (showDebugInfo)
            {
                Debug.Log($"预设 {preset.presetName} 相似度: {score:F2}");
            }
            
            if (score > bestScore)
            {
                bestScore = score;
                bestPreset = preset;
            }
        }
        
        if (bestPreset != null && bestScore >= similarityThreshold)
        {
            if (showDebugInfo)
            {
                Debug.Log($"选择预设: {bestPreset.presetName} (相似度: {bestScore:F2})");
            }
            
            OnPresetSelected?.Invoke(bestPreset);
            return bestPreset;
        }
        
        // 如果没有找到合适的预设，返回强度最接近的
        if (bestPreset == null)
        {
            bestPreset = GetClosestIntensityPreset(sceneData.CalculateIntensity());
        }
        
        return bestPreset;
    }
    
    /// <summary>
    /// 计算两个场景数据的相似度
    /// </summary>
    private float CalculateSimilarityScore(MusicSceneData data1, MusicSceneData data2)
    {
        float score = 0f;
        float totalWeight = 0f;
        
        // 环境相似度 (权重: 0.3)
        float envWeight = 0.3f;
        float envScore = data1.environment == data2.environment ? 1f : 0f;
        score += envScore * envWeight;
        totalWeight += envWeight;
        
        // 动作相似度 (权重: 0.25)
        float actionWeight = 0.25f;
        float actionScore = data1.currentAction == data2.currentAction ? 1f : 0f;
        score += actionScore * actionWeight;
        totalWeight += actionWeight;
        
        // 威胁等级相似度 (权重: 0.2)
        float threatWeight = 0.2f;
        float threatScore = 1f - Mathf.Abs(data1.threatLevel - data2.threatLevel);
        score += threatScore * threatWeight;
        totalWeight += threatWeight;
        
        // 强度相似度 (权重: 0.15)
        float intensityWeight = 0.15f;
        float intensity1 = data1.CalculateIntensity();
        float intensity2 = data2.CalculateIntensity();
        float intensityScore = 1f - Mathf.Abs(intensity1 - intensity2);
        score += intensityScore * intensityWeight;
        totalWeight += intensityWeight;
        
        // 时间相似度 (权重: 0.1)
        float timeWeight = 0.1f;
        float timeScore = data1.timeOfDay == data2.timeOfDay ? 1f : 0f;
        score += timeScore * timeWeight;
        totalWeight += timeWeight;
        
        return score / totalWeight;
    }
    
    /// <summary>
    /// 获取强度最接近的预设
    /// </summary>
    private MusicPreset GetClosestIntensityPreset(float targetIntensity)
    {
        MusicPreset closest = null;
        float minDifference = float.MaxValue;
        
        foreach (var preset in musicPresets)
        {
            if (preset.musicClip == null) continue;
            
            float difference = Mathf.Abs(preset.intensity - targetIntensity);
            if (difference < minDifference)
            {
                minDifference = difference;
                closest = preset;
            }
        }
        
        return closest;
    }
    
    /// <summary>
    /// 创建新的音乐预设
    /// </summary>
    public MusicPreset CreatePreset(MusicSceneData sceneData, AudioClip musicClip)
    {
        string presetID = GeneratePresetID(sceneData);
        
        // 检查是否已存在
        var existingPreset = musicPresets.Find(p => p.presetID == presetID);
        if (existingPreset != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"预设已存在: {presetID}");
            }
            return existingPreset;
        }
        
        MusicPreset newPreset = new MusicPreset
        {
            presetID = presetID,
            presetName = GeneratePresetName(sceneData),
            sceneData = sceneData,
            musicClip = musicClip,
            intensity = sceneData.CalculateIntensity(),
            description = sceneData.GetMusicStyleDescription(),
            isGenerated = true,
            generationDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        musicPresets.Add(newPreset);
        SavePresets();
        
        if (showDebugInfo)
        {
            Debug.Log($"创建新预设: {newPreset.presetName}");
        }
        
        OnPresetGenerated?.Invoke(newPreset);
        return newPreset;
    }

    /// <summary>
    /// 创建预设并将音频写入磁盘（WAV），返回预设对象
    /// </summary>
    public MusicPreset CreatePresetAndSaveAudio(MusicSceneData sceneData, AudioClip musicClip, string preferredFileNameNoExt = null)
    {
        if (musicClip == null)
        {
            Debug.LogError("CreatePresetAndSaveAudio: musicClip 为空");
            return null;
        }

        string presetID = GeneratePresetID(sceneData);

        // 已存在则直接返回（不重复写入）
        var existingPreset = musicPresets.Find(p => p.presetID == presetID);
        if (existingPreset != null)
        {
            // 如果已有文件名则直接返回；否则补写文件名
            if (string.IsNullOrEmpty(existingPreset.audioFileName))
            {
                string audioFolder = EnsureAudioFolder();
                string fileNameNoExt = string.IsNullOrEmpty(preferredFileNameNoExt) ? presetID : preferredFileNameNoExt;
                string savedFile = SaveClipAsWav(musicClip, audioFolder, MakeSafeFileName(fileNameNoExt));
                existingPreset.audioFileName = Path.GetFileName(savedFile);
                SavePresets();
            }
            return existingPreset;
        }

        // 新建预设并写文件
        string audioFolder2 = EnsureAudioFolder();
        string fileNameNoExt2 = string.IsNullOrEmpty(preferredFileNameNoExt) ? presetID : preferredFileNameNoExt;
        string saved = SaveClipAsWav(musicClip, audioFolder2, MakeSafeFileName(fileNameNoExt2));

        MusicPreset newPreset = new MusicPreset
        {
            presetID = presetID,
            presetName = GeneratePresetName(sceneData),
            sceneData = sceneData,
            musicClip = musicClip,
            audioFileName = Path.GetFileName(saved),
            intensity = sceneData.CalculateIntensity(),
            description = sceneData.GetMusicStyleDescription(),
            isGenerated = true,
            generationDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        musicPresets.Add(newPreset);
        SavePresets();
        if (showDebugInfo)
        {
            Debug.Log($"创建并保存新预设: {newPreset.presetName}, 文件: {newPreset.audioFileName}");
        }
        OnPresetGenerated?.Invoke(newPreset);
        return newPreset;
    }
    
    /// <summary>
    /// 生成预设ID
    /// </summary>
    private string GeneratePresetID(MusicSceneData sceneData)
    {
        // 使用更多字段来避免重复，但保持可读性
        return $"preset_{sceneData.environment}_{sceneData.currentAction}_{sceneData.enemyPresence}_{sceneData.gameLevel}_{sceneData.timeOfDay}_{Mathf.RoundToInt(sceneData.threatLevel * 10)}";
    }
    
    /// <summary>
    /// 生成预设名称
    /// </summary>
    private string GeneratePresetName(MusicSceneData sceneData)
    {
        return $"{sceneData.environment} - {sceneData.currentAction} - {sceneData.enemyPresence}";
    }
    
    /// <summary>
    /// 批量生成预设
    /// </summary>
    public void GeneratePresetBatch()
    {
        if (!autoGeneratePresets) return;
        
        var commonScenes = GenerateCommonSceneConfigurations();
        
        foreach (var sceneData in commonScenes)
        {
            if (GetPresetCountForCategory(sceneData) < maxPresetsPerCategory)
            {
                // 这里可以调用AI生成音乐
                StartCoroutine(GeneratePresetMusic(sceneData));
            }
        }
    }
    
    /// <summary>
    /// 生成常见的场景配置
    /// </summary>
    private List<MusicSceneData> GenerateCommonSceneConfigurations()
    {
        var configurations = new List<MusicSceneData>();
        
        // 探索场景
        var explorationScene = new MusicSceneData
        {
            sceneName = "和平探索",
            environment = EnvironmentType.Grasslands,
            currentAction = ActionType.Walking,
            enemyPresence = EnemyPresence.None,
            gameLevel = GameLevel.Tutorial,
            timeOfDay = TimeOfDay.Day
        };
        configurations.Add(explorationScene);
        
        // 战斗场景
        var combatScene = new MusicSceneData
        {
            sceneName = "激烈战斗",
            environment = EnvironmentType.DarkDungeon,
            currentAction = ActionType.Combat,
            enemyPresence = EnemyPresence.Many,
            gameLevel = GameLevel.Mid,
            timeOfDay = TimeOfDay.Night,
            threatLevel = 0.8f
        };
        configurations.Add(combatScene);
        
        // Boss战场景
        var bossScene = new MusicSceneData
        {
            sceneName = "Boss战",
            environment = EnvironmentType.DarkDungeon,
            currentAction = ActionType.Combat,
            enemyPresence = EnemyPresence.Boss,
            gameLevel = GameLevel.FinalBoss,
            timeOfDay = TimeOfDay.Night,
            threatLevel = 1f,
            isBossFight = true
        };
        configurations.Add(bossScene);
        
        // 潜行场景
        var stealthScene = new MusicSceneData
        {
            sceneName = "潜行任务",
            environment = EnvironmentType.Urban,
            currentAction = ActionType.Stealth,
            enemyPresence = EnemyPresence.Lurking,
            gameLevel = GameLevel.Mid,
            timeOfDay = TimeOfDay.Night,
            isStealth = true
        };
        configurations.Add(stealthScene);
        
        return configurations;
    }
    
    /// <summary>
    /// 获取特定类别的预设数量
    /// </summary>
    private int GetPresetCountForCategory(MusicSceneData sceneData)
    {
        return musicPresets.Count(p => 
            p.sceneData.environment == sceneData.environment &&
            p.sceneData.currentAction == sceneData.currentAction &&
            p.sceneData.enemyPresence == sceneData.enemyPresence);
    }
    
    /// <summary>
    /// 生成预设音乐（调用AI生成）
    /// </summary>
    private IEnumerator GeneratePresetMusic(MusicSceneData sceneData)
    {
        if (AIGeneratedMusic.Instance == null)
        {
            Debug.LogError("AI音乐生成器未找到！");
            yield break;
        }
        
        // 调用AI生成音乐
        AIGeneratedMusic.Instance.GenerateMusicForScene(sceneData);
        
        // 等待生成完成
        bool generationComplete = false;
        AudioClip generatedClip = null;
        
        AIGeneratedMusic.OnMusicGenerated += (clip) => {
            generatedClip = clip;
            generationComplete = true;
        };
        
        AIGeneratedMusic.OnGenerationFailed += (error) => {
            Debug.LogError($"预设音乐生成失败: {error}");
            generationComplete = true;
        };
        
        // 等待生成完成或超时
        float timeout = 60f;
        float elapsed = 0f;
        
        while (!generationComplete && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (generationComplete && generatedClip != null)
        {
            CreatePreset(sceneData, generatedClip);
        }
    }
    
    /// <summary>
    /// 保存预设到文件
    /// </summary>
    public void SavePresets()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, presetFolder);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        
        string filePath = Path.Combine(folderPath, "music_presets.json");
        string json = JsonUtility.ToJson(new PresetContainer { presets = musicPresets }, true);
        File.WriteAllText(filePath, json);
        
        if (showDebugInfo)
        {
            Debug.Log($"预设已保存到: {filePath}");
        }
    }
    
    /// <summary>
    /// 从文件加载预设
    /// </summary>
    private void LoadPresets()
    {
        string filePath = Path.Combine(Application.persistentDataPath, presetFolder, "music_presets.json");
        
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var container = JsonUtility.FromJson<PresetContainer>(json);
            musicPresets = container.presets;
            
            // 加载音频文件
            LoadAudioFiles();
            
            if (showDebugInfo)
            {
                Debug.Log($"已加载 {musicPresets.Count} 个预设");
            }
        }
    }
    
    /// <summary>
    /// 加载音频文件
    /// </summary>
    private void LoadAudioFiles()
    {
        string audioFolder = Path.Combine(Application.persistentDataPath, presetFolder, "audio");
        
        if (!Directory.Exists(audioFolder))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"音频文件夹不存在: {audioFolder}");
            }
            return;
        }
        
        foreach (var preset in musicPresets)
        {
            if (preset.musicClip == null && !string.IsNullOrEmpty(preset.audioFileName))
            {
                string audioPath = Path.Combine(audioFolder, preset.audioFileName);
                if (File.Exists(audioPath))
                {
                    // 使用简化的加载方法
                    LoadAudioFileSimple(preset, audioPath);
                }
                else if (showDebugInfo)
                {
                    Debug.LogWarning($"音频文件不存在: {audioPath}");
                }
            }
        }
    }
    
    /// <summary>
    /// 简化的音频文件加载方法（不使用UnityWebRequest）
    /// </summary>
    private void LoadAudioFileSimple(MusicPreset preset, string filePath)
    {
        try
        {
            // 这里只是标记文件存在，实际加载会在需要时进行
            if (showDebugInfo)
            {
                Debug.Log($"音频文件已准备: {preset.audioFileName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"处理音频文件失败: {filePath}, 错误: {e.Message}");
        }
    }
    
    /// <summary>
    /// 异步加载音频文件
    /// </summary>
    private IEnumerator LoadAudioFile(MusicPreset preset, string filePath)
    {
        // 检查文件扩展名以确定音频类型
        string extension = Path.GetExtension(filePath).ToLower();
        AudioType audioType = AudioType.MPEG; // 默认类型
        
        if (extension == ".wav")
            audioType = AudioType.WAV;
        else if (extension == ".ogg")
            audioType = AudioType.OGGVORBIS;
        else if (extension == ".mp3")
            audioType = AudioType.MPEG;
        
        string url = "file://" + filePath;
        
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                preset.musicClip = DownloadHandlerAudioClip.GetContent(request);
                if (preset.musicClip != null)
                {
                    preset.musicClip.name = preset.presetName;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"成功加载音频文件: {preset.audioFileName}");
                    }
                }
                else
                {
                    Debug.LogError($"音频文件内容为空: {filePath}");
                }
            }
            else
            {
                Debug.LogError($"加载音频文件失败: {filePath}, 错误: {request.error}");
            }
        }
    }
    
    /// <summary>
    /// 清除所有预设
    /// </summary>
    public void ClearAllPresets()
    {
        musicPresets.Clear();
        SavePresets();
        
        if (showDebugInfo)
        {
            Debug.Log("所有预设已清除");
        }
    }
    
    /// <summary>
    /// 获取预设统计信息
    /// </summary>
    public string GetPresetStats()
    {
        int totalPresets = musicPresets.Count;
        int generatedPresets = musicPresets.Count(p => p.isGenerated);
        int validPresets = musicPresets.Count(p => p.musicClip != null);
        
        return $"总预设: {totalPresets}, 已生成: {generatedPresets}, 有效: {validPresets}";
    }
    
    /// <summary>
    /// 按需加载音频文件
    /// </summary>
    public IEnumerator LoadAudioFileOnDemand(MusicPreset preset)
    {
        if (preset.musicClip != null)
        {
            yield break; // 已经加载过了
        }
        
        if (string.IsNullOrEmpty(preset.audioFileName))
        {
            Debug.LogError("预设没有音频文件名");
            yield break;
        }
        
        // 优先尝试从项目文件夹加载，如果失败则从persistentDataPath加载
        string audioPath = "";
        
        // 首先尝试从项目文件夹加载
        string projectAudioFolder = Path.Combine(Application.dataPath, "AutoAudioDemo", presetFolder, "audio");
        string projectAudioPath = Path.Combine(projectAudioFolder, preset.audioFileName);
        
        if (File.Exists(projectAudioPath))
        {
            audioPath = projectAudioPath;
        }
        else
        {
            // 如果项目文件夹中没有，尝试从persistentDataPath加载
            string persistentAudioFolder = Path.Combine(Application.persistentDataPath, presetFolder, "audio");
            string persistentAudioPath = Path.Combine(persistentAudioFolder, preset.audioFileName);
            
            if (File.Exists(persistentAudioPath))
            {
                audioPath = persistentAudioPath;
            }
            else
            {
                Debug.LogWarning($"音频文件在项目文件夹和persistentDataPath中都不存在: {preset.audioFileName}");
                Debug.LogWarning($"项目路径: {projectAudioPath}");
                Debug.LogWarning($"Persistent路径: {persistentAudioPath}");
                yield break;
            }
        }
        
        if (!File.Exists(audioPath))
        {
            Debug.LogError($"音频文件不存在: {audioPath}");
            yield break;
        }
        
        yield return StartCoroutine(LoadAudioFile(preset, audioPath));
    }

    private string EnsureAudioFolder()
    {
        // 优先使用项目文件夹，如果不存在则创建
        string projectFolderPath = Path.Combine(Application.dataPath, "AutoAudioDemo", presetFolder, "audio");
        if (!Directory.Exists(projectFolderPath))
        {
            try
            {
                Directory.CreateDirectory(projectFolderPath);
                Debug.Log($"创建项目音频文件夹: {projectFolderPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"无法创建项目音频文件夹: {e.Message}");
            }
        }
        
        // 同时确保persistentDataPath中的文件夹也存在
        string persistentFolderPath = Path.Combine(Application.persistentDataPath, presetFolder, "audio");
        if (!Directory.Exists(persistentFolderPath))
        {
            Directory.CreateDirectory(persistentFolderPath);
        }
        
        // 返回项目文件夹路径（优先）
        return projectFolderPath;
    }

    private string MakeSafeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }

    // 将 AudioClip 保存为 WAV 文件，返回完整路径
    private string SaveClipAsWav(AudioClip clip, string folderPath, string fileNameNoExt)
    {
        // 防冲突追加时间戳
        string finalName = $"{fileNameNoExt}_{System.DateTime.Now:yyyyMMdd_HHmmss}.wav";
        string filePath = Path.Combine(folderPath, finalName);

        try
        {
            int samples = clip.samples * clip.channels;
            float[] data = new float[samples];
            clip.GetData(data, 0);

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fs))
            {
                int sampleRate = clip.frequency;
                short channels = (short)clip.channels;
                short bitsPerSample = 16;

                int byteRate = sampleRate * channels * (bitsPerSample / 8);
                short blockAlign = (short)(channels * (bitsPerSample / 8));
                byte[] wavData = FloatArrayToPCM16(data);

                // RIFF header
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + wavData.Length);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                // fmt chunk
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // PCM chunk size
                writer.Write((short)1); // audio format = PCM
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write(bitsPerSample);

                // data chunk
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(wavData.Length);
                writer.Write(wavData);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存WAV失败: {filePath}, 错误: {e.Message}");
        }

        return filePath;
    }

    private byte[] FloatArrayToPCM16(float[] data)
    {
        int len = data.Length;
        byte[] bytes = new byte[len * 2];
        int rescale = 32767; // 16-bit
        for (int i = 0; i < len; i++)
        {
            int sample = Mathf.Clamp((int)(data[i] * rescale), short.MinValue, short.MaxValue);
            short s = (short)sample;
            bytes[i * 2] = (byte)(s & 0xFF);
            bytes[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
        }
        return bytes;
    }
}

[System.Serializable]
public class PresetContainer
{
    public List<MusicPreset> presets;
}

public enum SelectionStrategy
{
    BestMatch,      // 最佳匹配
    ClosestIntensity, // 最接近强度
    Random          // 随机选择
} 