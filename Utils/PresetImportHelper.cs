using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// 预设导入助手 - 帮助手动导入音频文件到预设系统
/// </summary>
public class PresetImportHelper : MonoBehaviour
{
	[Header("导入设置")]
	public string importFolder = "MusicImports"; // 导入文件夹名称
	public bool autoImportOnStart = false;
	
	[Header("调试")]
	public bool showDebugInfo = true;
	
	private void Start()
	{
		if (autoImportOnStart)
		{
			StartCoroutine(ImportFromFolder());
		}
	}
	
	/// <summary>
	/// 从导入文件夹导入音频文件
	/// </summary>
	[ContextMenu("从导入文件夹导入")]
	public void ImportFromFolderButton()
	{
		Debug.Log("=== 开始导入过程 ===");
		StartCoroutine(ImportFromFolder());
	}
	
	private IEnumerator ImportFromFolder()
	{
		Debug.Log("步骤1: 检查导入文件夹...");
		string importPath = Path.Combine(Application.persistentDataPath, importFolder);
		
		if (!Directory.Exists(importPath))
		{
			Debug.LogWarning($"导入文件夹不存在: {importPath}");
			Debug.Log($"请创建文件夹: {importPath}");
			Debug.Log("并将你的音频文件放入该文件夹");
			yield break;
		}
		
		Debug.Log($"步骤2: 扫描音频文件...");
		// 收集所有音频文件
		var audioFilesList = new List<string>();
		audioFilesList.AddRange(Directory.GetFiles(importPath, "*.mp3"));
		audioFilesList.AddRange(Directory.GetFiles(importPath, "*.wav"));
		audioFilesList.AddRange(Directory.GetFiles(importPath, "*.ogg"));
		string[] audioFiles = audioFilesList.ToArray();
		
		if (audioFiles.Length == 0)
		{
			Debug.LogWarning("导入文件夹中没有找到音频文件");
			yield break;
		}
		
		Debug.Log($"找到 {audioFiles.Length} 个音频文件");
		
		// 确保预设管理器存在
		Debug.Log("步骤3: 检查预设管理器...");
		if (MusicPresetManager.Instance == null)
		{
			Debug.LogError("MusicPresetManager 实例不存在！");
			Debug.LogError("请确保场景中有MusicPresetManager GameObject");
			yield break;
		}
		
		Debug.Log("步骤4: 开始导入文件...");
		// 逐个导入文件
		for (int i = 0; i < audioFiles.Length; i++)
		{
			string audioFile = audioFiles[i];
			Debug.Log($"正在处理第 {i + 1}/{audioFiles.Length} 个文件: {Path.GetFileName(audioFile)}");
			yield return StartCoroutine(ImportAudioFile(audioFile));
		}
		
		Debug.Log("=== 导入完成！===");
		Debug.Log($"总共处理了 {audioFiles.Length} 个文件");
		
		// 显示导入结果
		if (MusicPresetManager.Instance != null)
		{
			Debug.Log($"当前预设总数: {MusicPresetManager.Instance.musicPresets.Count}");
		}
	}
	
	/// <summary>
	/// 导入单个音频文件（带重复检查）
	/// </summary>
	private IEnumerator ImportAudioFile(string filePath)
	{
		string fileName = Path.GetFileNameWithoutExtension(filePath);
		string extension = Path.GetExtension(filePath);
		string justName = Path.GetFileName(filePath);
		
		Debug.Log($"  - 开始导入文件: {fileName}");
		
		// 创建预设数据
		Debug.Log($"  - 创建场景数据...");
		MusicSceneData sceneData = CreateSceneDataFromFileName(fileName);
		MusicPreset preset = new MusicPreset
		{
			presetID = $"import_{fileName}",
			presetName = fileName,
			sceneData = sceneData,
			audioFileName = justName,
			intensity = sceneData.CalculateIntensity(),
			description = sceneData.GetMusicStyleDescription(),
			isGenerated = true,
			generationDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
		};
		
		// 去重检查：按 audioFileName 或 presetID
		if (MusicPresetManager.Instance != null)
		{
			bool exists = MusicPresetManager.Instance.musicPresets.Any(p =>
				(!string.IsNullOrEmpty(p.audioFileName) && p.audioFileName == justName) ||
				p.presetID == preset.presetID);
			if (exists)
			{
				Debug.LogWarning($"  - 跳过: 已存在相同音频或ID ({justName})");
				yield return null;
				yield break;
			}
		}
		
		Debug.Log($"  - 预设数据创建完成: {preset.presetName}");
		
		// 复制文件到预设文件夹
		Debug.Log($"  - 复制音频文件...");
		string presetAudioFolder = Path.Combine(Application.persistentDataPath, "MusicPresets", "audio");
		if (!Directory.Exists(presetAudioFolder))
		{
			Directory.CreateDirectory(presetAudioFolder);
			Debug.Log($"  - 创建音频文件夹: {presetAudioFolder}");
		}
		
		string targetPath = Path.Combine(presetAudioFolder, justName);
		
		try
		{
			File.Copy(filePath, targetPath, true);
			Debug.Log($"  - 文件复制成功: {justName}");
			
			// 添加到预设管理器
			Debug.Log($"  - 添加到预设管理器...");
			if (MusicPresetManager.Instance != null)
			{
				MusicPresetManager.Instance.musicPresets.Add(preset);
				Debug.Log($"  - 预设已添加到列表，当前总数: {MusicPresetManager.Instance.musicPresets.Count}");
				
				MusicPresetManager.Instance.SavePresets();
				Debug.Log($"  - 预设已保存到文件");
				
				Debug.Log($"✅ 成功导入: {fileName}");
			}
			else
			{
				Debug.LogError($"  ❌ MusicPresetManager实例为空！");
			}
		}
		catch (System.Exception e)
		{
			Debug.LogError($"❌ 导入文件失败 {fileName}: {e.Message}");
			Debug.LogError($"   文件路径: {filePath}");
			Debug.LogError($"   目标路径: {targetPath}");
		}
		
		yield return null;
	}
	
	/// <summary>
	/// 一键去重（按 audioFileName 优先，其次 presetID），并保存
	/// </summary>
	[ContextMenu("一键去重并保存")]
	public void DeduplicateAndSave()
	{
		var mgr = MusicPresetManager.Instance;
		if (mgr == null || mgr.musicPresets == null)
		{
			Debug.LogError("MusicPresetManager 不可用，无法去重");
			return;
		}
		int before = mgr.musicPresets.Count;
		var newList = new List<MusicPreset>();
		var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
		int removed = 0;
		foreach (var p in mgr.musicPresets)
		{
			string key = !string.IsNullOrEmpty(p.audioFileName) ? $"A:{p.audioFileName}" : $"P:{p.presetID}";
			if (seen.Contains(key)) { removed++; continue; }
			seen.Add(key);
			newList.Add(p);
		}
		mgr.musicPresets = newList;
		mgr.SavePresets();
		Debug.Log($"去重完成：原有 {before} 条，移除 {removed} 条，现有 {mgr.musicPresets.Count} 条。");
	}
	
	/// <summary>
	/// 从文件名创建场景数据
	/// </summary>
	private MusicSceneData CreateSceneDataFromFileName(string fileName)
	{
		MusicSceneData data = new MusicSceneData
		{
			sceneName = fileName,
			sceneID = $"import_{fileName}",
			environment = EnvironmentType.Grasslands,
			currentAction = ActionType.Walking,
			enemyPresence = EnemyPresence.None,
			gameLevel = GameLevel.Tutorial,
			timeOfDay = TimeOfDay.Day,
			weather = WeatherType.Clear,
			temperature = 20f,
			actionIntensity = 0.5f,
			isStealth = false,
			enemyCount = 0,
			threatLevel = 0f,
			playerHealth = 1f,
			isBossFight = false,
			preferredStyle = MusicStyle.Ambient,
			tempo = 1f,
			volume = 1f
		};
		
		// 根据文件名关键词调整场景数据
		string lowerFileName = fileName.ToLower();
		
		// 环境类型
		if (lowerFileName.Contains("forest") || lowerFileName.Contains("森林"))
			data.environment = EnvironmentType.Forest;
		else if (lowerFileName.Contains("dungeon") || lowerFileName.Contains("地牢"))
			data.environment = EnvironmentType.DarkDungeon;
		else if (lowerFileName.Contains("urban") || lowerFileName.Contains("城市"))
			data.environment = EnvironmentType.Urban;
		else if (lowerFileName.Contains("ocean") || lowerFileName.Contains("海洋"))
			data.environment = EnvironmentType.Ocean;
		else if (lowerFileName.Contains("mountain") || lowerFileName.Contains("山脉"))
			data.environment = EnvironmentType.Mountain;
		else if (lowerFileName.Contains("desert") || lowerFileName.Contains("沙漠"))
			data.environment = EnvironmentType.Desert;
		else if (lowerFileName.Contains("snow") || lowerFileName.Contains("雪"))
			data.environment = EnvironmentType.Snow;
		
		// 动作类型
		if (lowerFileName.Contains("combat") || lowerFileName.Contains("战斗"))
			data.currentAction = ActionType.Combat;
		else if (lowerFileName.Contains("stealth") || lowerFileName.Contains("潜行"))
			data.currentAction = ActionType.Stealth;
		else if (lowerFileName.Contains("run") || lowerFileName.Contains("奔跑"))
			data.currentAction = ActionType.Running;
		else if (lowerFileName.Contains("fly") || lowerFileName.Contains("飞行"))
			data.currentAction = ActionType.Flying;
		else if (lowerFileName.Contains("swim") || lowerFileName.Contains("游泳"))
			data.currentAction = ActionType.Swimming;
		
		// 敌人存在
		if (lowerFileName.Contains("boss") || lowerFileName.Contains("boss"))
			data.enemyPresence = EnemyPresence.Boss;
		else if (lowerFileName.Contains("many") || lowerFileName.Contains("大量"))
			data.enemyPresence = EnemyPresence.Many;
		else if (lowerFileName.Contains("few") || lowerFileName.Contains("少量"))
			data.enemyPresence = EnemyPresence.Few;
		else if (lowerFileName.Contains("lurk") || lowerFileName.Contains("潜伏"))
			data.enemyPresence = EnemyPresence.Lurking;
		
		// 时间
		if (lowerFileName.Contains("night") || lowerFileName.Contains("夜晚"))
			data.timeOfDay = TimeOfDay.Night;
		else if (lowerFileName.Contains("dawn") || lowerFileName.Contains("黎明"))
			data.timeOfDay = TimeOfDay.Dawn;
		else if (lowerFileName.Contains("dusk") || lowerFileName.Contains("黄昏"))
			data.timeOfDay = TimeOfDay.Dusk;
		
		// 强度调整
		if (lowerFileName.Contains("intense") || lowerFileName.Contains("激烈"))
			data.actionIntensity = 0.9f;
		else if (lowerFileName.Contains("calm") || lowerFileName.Contains("平静"))
			data.actionIntensity = 0.2f;
		
		// 威胁等级
		if (lowerFileName.Contains("danger") || lowerFileName.Contains("危险"))
			data.threatLevel = 0.8f;
		else if (lowerFileName.Contains("safe") || lowerFileName.Contains("安全"))
			data.threatLevel = 0.1f;
		
		return data;
	}
	
	[ContextMenu("显示导入文件夹路径")]
	public void ShowImportFolderPath()
	{
		string importPath = Path.Combine(Application.persistentDataPath, importFolder);
		Debug.Log($"导入文件夹路径: {importPath}");
		
		if (!Directory.Exists(importPath))
		{
			Directory.CreateDirectory(importPath);
			Debug.Log($"已创建导入文件夹: {importPath}");
		}
	}
	
	[ContextMenu("显示预设文件夹路径")]
	public void ShowPresetFolderPath()
	{
		string presetPath = Path.Combine(Application.persistentDataPath, "MusicPresets");
		Debug.Log($"预设文件夹路径: {presetPath}");
		
		string audioPath = Path.Combine(presetPath, "audio");
		Debug.Log($"音频文件夹路径: {audioPath}");
	}
	
	[ContextMenu("创建示例JSON配置")]
	public void CreateExampleJSON()
	{
		string presetPath = Path.Combine(Application.persistentDataPath, "MusicPresets");
		if (!Directory.Exists(presetPath))
		{
			Directory.CreateDirectory(presetPath);
		}
		
		string jsonPath = Path.Combine(presetPath, "music_presets.json");
		
		// 创建示例预设
		var examplePreset = new MusicPreset
		{
			presetID = "example_forest_exploration",
			presetName = "森林探索示例",
			sceneData = new MusicSceneData
			{
				sceneName = "森林探索",
				environment = EnvironmentType.Forest,
				currentAction = ActionType.Walking,
				enemyPresence = EnemyPresence.None,
				gameLevel = GameLevel.Tutorial,
				timeOfDay = TimeOfDay.Day,
				threatLevel = 0.1f,
				actionIntensity = 0.3f
			},
			audioFileName = "forest_exploration.mp3",
			intensity = 0.2f,
			description = "peaceful forest exploration",
			isGenerated = true,
			generationDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
		};
		
		var container = new PresetContainer
		{
			presets = new List<MusicPreset> { examplePreset }
		};
		
		string json = JsonUtility.ToJson(container, true);
		File.WriteAllText(jsonPath, json);
		
		Debug.Log($"已创建示例JSON配置文件: {jsonPath}");
	}
	
	[ContextMenu("检查系统状态")]
	public void CheckSystemStatus()
	{
		Debug.Log("=== 系统状态检查 ===");
		
		// 检查预设管理器
		if (MusicPresetManager.Instance != null)
		{
			Debug.Log("✓ MusicPresetManager 实例存在");
			Debug.Log($"当前预设数量: {MusicPresetManager.Instance.musicPresets.Count}");
		}
		else
		{
			Debug.LogError("✗ MusicPresetManager 实例不存在");
		}
		
		// 检查导入文件夹
		string importPath = Path.Combine(Application.persistentDataPath, importFolder);
		if (Directory.Exists(importPath))
		{
			string[] files = Directory.GetFiles(importPath, "*.*");
			Debug.Log($"✓ 导入文件夹存在，包含 {files.Length} 个文件");
		}
		else
		{
			Debug.LogWarning("✗ 导入文件夹不存在");
		}
		
		// 检查预设文件夹
		string presetPath = Path.Combine(Application.persistentDataPath, "MusicPresets");
		if (Directory.Exists(presetPath))
		{
			Debug.Log("✓ 预设文件夹存在");
		}
		else
		{
			Debug.LogWarning("✗ 预设文件夹不存在");
		}
		
		Debug.Log("=== 检查完成 ===");
	}
	
	[ContextMenu("快速诊断")]
	public void QuickDiagnosis()
	{
		Debug.Log("=== 快速诊断开始 ===");
		
		// 1. 检查组件
		Debug.Log("1. 检查组件状态:");
		if (MusicPresetManager.Instance != null)
		{
			Debug.Log("   ✅ MusicPresetManager 存在");
		}
		else
		{
			Debug.LogError("   ❌ MusicPresetManager 不存在");
		}
		
		if (AIGeneratedMusic.Instance != null)
		{
			Debug.Log("   ✅ AIGeneratedMusic 存在");
		}
		else
		{
			Debug.LogError("   ❌ AIGeneratedMusic 不存在");
		}
		
		// 2. 检查文件夹
		Debug.Log("2. 检查文件夹:");
		string importPath = Path.Combine(Application.persistentDataPath, importFolder);
		if (Directory.Exists(importPath))
		{
			string[] files = Directory.GetFiles(importPath, "*.*");
			Debug.Log($"   ✅ 导入文件夹存在，包含 {files.Length} 个文件");
			foreach (string file in files)
			{
				Debug.Log($"      - {Path.GetFileName(file)}");
			}
		}
		else
		{
			Debug.LogError($"   ❌ 导入文件夹不存在: {importPath}");
		}
		
		string presetPath = Path.Combine(Application.persistentDataPath, "MusicPresets");
		if (Directory.Exists(presetPath))
		{
			Debug.Log("   ✅ 预设文件夹存在");
		}
		else
		{
			Debug.LogWarning("   ⚠️ 预设文件夹不存在（首次使用时会自动创建）");
		}
		
		// 3. 检查预设状态
		Debug.Log("3. 检查预设状态:");
		if (MusicPresetManager.Instance != null)
		{
			Debug.Log($"   当前预设数量: {MusicPresetManager.Instance.musicPresets.Count}");
			if (MusicPresetManager.Instance.musicPresets.Count > 0)
			{
				foreach (var preset in MusicPresetManager.Instance.musicPresets)
				{
					Debug.Log($"     - {preset.presetName} (音频: {preset.audioFileName})");
				}
			}
		}
		
		Debug.Log("=== 快速诊断完成 ===");
	}
} 