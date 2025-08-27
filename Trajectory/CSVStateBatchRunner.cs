using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AutoAudioDemo.Utils;

public class CSVStateBatchRunner : MonoBehaviour
{
	[Header("Source")]
	public TextAsset csvFile; // e.g., Assets/AutoAudioDemo/ExperimentData/static_states.csv
	public bool autoRunOnStart = false;

	[Header("Mode")]
	public bool usePresetSelection = true; // true: select from preset library; false: call AI (not recommended for batch)
	public bool actuallyPlayAudio = false; // false: just select & log; true: also play (slower)
	public float crossfadeSeconds = 0.5f;
	public float perRowDelaySeconds = 0.25f; // wait between rows to avoid spam

	[Header("Output")]
	public string outputFileName = "static_states_results.csv"; // saved under persistentDataPath
	public bool showDebugInfo = true;

	[Header("Concurrent Batch (Rate Limit)")]
	public bool useConcurrent = false; // enable concurrent submit+poll
	public int maxSubmissionsPerWindow = 18; // Suno: 20/10s, keep safe margin
	public float windowSeconds = 10f;
	public int maxConcurrentPolls = 20;

	private readonly char[] _sep = new[] {','};

	private void Start()
	{
		if (autoRunOnStart && csvFile != null)
		{
			StartCoroutine(Run());
		}
	}

	[ContextMenu("Run Batch (Select & Log)")] 
	public void RunNow()
	{
		if (csvFile == null)
		{
			Debug.LogWarning("CSV not set.");
			return;
		}
		StartCoroutine(Run());
	}

	[ContextMenu("Generate AI Presets From CSV")] 
	public void GenerateAIPresetsNow()
	{
		if (csvFile == null)
		{
			Debug.LogWarning("CSV not set.");
			return;
		}
		StartCoroutine(GenerateAIPresetsSequential());
	}

	[ContextMenu("Generate AI Presets (Concurrent, Rate-Limited)")]
	public void GenerateAIPresetsConcurrentNow()
	{
		if (csvFile == null)
		{
			Debug.LogWarning("CSV not set.");
			return;
		}
		StartCoroutine(GenerateAIPresetsConcurrent());
	}

	/// <summary>
	/// 专门的静态准确率测试 - 评估预设选择准确性
	/// </summary>
	[ContextMenu("Run Static Accuracy Test")]
	public void RunStaticAccuracyTest()
	{
		if (csvFile == null)
		{
			Debug.LogWarning("CSV not set.");
			return;
		}
		StartCoroutine(RunStaticAccuracyTestCoroutine());
	}

	private IEnumerator RunStaticAccuracyTestCoroutine()
	{
		Debug.Log("=== 开始静态准确率测试 ===");
		
		var lines = csvFile.text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		if (lines.Length <= 1)
		{
			Debug.LogWarning("CSV seems empty.");
			yield break;
		}

		// 检查预设管理器状态
		var mgr = MusicPresetManager.Instance;
		if (mgr == null || mgr.musicPresets == null || mgr.musicPresets.Count == 0)
		{
			Debug.LogError("MusicPresetManager/presets not ready.");
			yield break;
		}

		Debug.Log($"预设库状态: 总数量 {mgr.musicPresets.Count}");
		Debug.Log($"相似度阈值: {mgr.similarityThreshold:F2}");

		// 统计变量
		int totalTests = 0;
		int highAccuracyCount = 0;  // 相似度 >= 0.8
		int mediumAccuracyCount = 0; // 相似度 0.6-0.8
		int lowAccuracyCount = 0;    // 相似度 < 0.6
		int noMatchCount = 0;        // 无匹配
		float totalSimilarity = 0f;
		float minSimilarity = 1f;
		float maxSimilarity = 0f;

		// 按环境类型统计
		var environmentStats = new Dictionary<EnvironmentType, (int count, float totalScore)>();
		var actionStats = new Dictionary<ActionType, (int count, float totalScore)>();
		var threatStats = new Dictionary<string, (int count, float totalScore)>();

		// header
		int startIdx = 0;
		if (lines[0].StartsWith("id,")) startIdx = 1;

		var outPath = Path.Combine(Application.persistentDataPath, "static_accuracy_test_results.csv");
		Directory.CreateDirectory(Path.GetDirectoryName(outPath));
		
		using (var sw = new StreamWriter(outPath, false, System.Text.Encoding.UTF8))
		{
			// 添加UTF-8 BOM标记，确保Excel等软件正确识别中文
			sw.Write('\uFEFF');
			sw.WriteLine("id,environment,action,threatLevel,selectedPresetID,selectedPresetName,bestScore,accuracyLevel,calcIntensity,targetNotes,analysis");

			for (int i = startIdx; i < lines.Length; i++)
			{
				var line = lines[i].Trim();
				if (string.IsNullOrEmpty(line)) continue;

				bool ok = TryParseRow(line, out string id, out MusicSceneData scene, out string notes);
				if (!ok)
				{
					if (showDebugInfo) Debug.LogWarning($"Skip malformed row: {line}");
					continue;
				}

				totalTests++;

				// 计算最佳匹配
				MusicPreset best = null;
				float bestScore = -1f;
				foreach (var p in mgr.musicPresets)
				{
					var s = CalcSimilarity(scene, p.sceneData);
					if (s > bestScore)
					{
						bestScore = s;
						best = p;
					}
				}

				// 使用预设管理器的选择逻辑
				var selected = mgr.SelectBestPreset(scene);
				
				// 统计相似度分布
				if (bestScore >= 0.8f) highAccuracyCount++;
				else if (bestScore >= 0.6f) mediumAccuracyCount++;
				else if (bestScore > 0f) lowAccuracyCount++;
				else noMatchCount++;

				totalSimilarity += bestScore;
				minSimilarity = Mathf.Min(minSimilarity, bestScore);
				maxSimilarity = Mathf.Max(maxSimilarity, bestScore);

				// 环境统计
				if (!environmentStats.ContainsKey(scene.environment))
					environmentStats[scene.environment] = (0, 0f);
				var envStat = environmentStats[scene.environment];
				environmentStats[scene.environment] = (envStat.count + 1, envStat.totalScore + bestScore);

				// 动作统计
				if (!actionStats.ContainsKey(scene.currentAction))
					actionStats[scene.currentAction] = (0, 0f);
				var actStat = actionStats[scene.currentAction];
				actionStats[scene.currentAction] = (actStat.count + 1, actStat.totalScore + bestScore);

				// 威胁等级统计（分组）
				string threatGroup = scene.threatLevel < 0.3f ? "Low" : scene.threatLevel < 0.7f ? "Medium" : "High";
				if (!threatStats.ContainsKey(threatGroup))
					threatStats[threatGroup] = (0, 0f);
				var thrStat = threatStats[threatGroup];
				threatStats[threatGroup] = (thrStat.count + 1, thrStat.totalScore + bestScore);

				// 确定准确率等级
				string accuracyLevel = bestScore >= 0.8f ? "High" : bestScore >= 0.6f ? "Medium" : bestScore > 0f ? "Low" : "None";

				// 分析结果
				string analysis = AnalyzeSelectionResult(scene, selected, best, bestScore, notes);

				// 写入结果
				sw.WriteLine($"{id},{scene.environment},{scene.currentAction},{scene.threatLevel:F2},{Val(selected?.presetID)},{Val(selected?.presetName)},{bestScore:F3},{accuracyLevel},{scene.CalculateIntensity():F2},\"{Escape(notes)}\",\"{Escape(analysis)}\"");

				if (showDebugInfo)
				{
					Debug.Log($"[准确率测试] {id}: {scene.environment}-{scene.currentAction} -> 选择:{selected?.presetName ?? "NONE"} 最佳:{best?.presetName ?? "NONE"} 分数:{bestScore:F3} 等级:{accuracyLevel}");
				}

				yield return new WaitForSeconds(perRowDelaySeconds);
			}
		}

		// 输出统计结果
		Debug.Log("=== 静态准确率测试完成 ===");
		Debug.Log($"总测试数: {totalTests}");
		Debug.Log($"高准确率 (≥0.8): {highAccuracyCount} ({highAccuracyCount * 100f / totalTests:F1}%)");
		Debug.Log($"中准确率 (0.6-0.8): {mediumAccuracyCount} ({mediumAccuracyCount * 100f / totalTests:F1}%)");
		Debug.Log($"低准确率 (<0.6): {lowAccuracyCount} ({lowAccuracyCount * 100f / totalTests:F1}%)");
		Debug.Log($"无匹配: {noMatchCount} ({noMatchCount * 100f / totalTests:F1}%)");
		Debug.Log($"平均相似度: {totalSimilarity / totalTests:F3}");
		Debug.Log($"相似度范围: {minSimilarity:F3} - {maxSimilarity:F3}");

		// 环境类型统计
		Debug.Log("=== 按环境类型统计 ===");
		foreach (var kvp in environmentStats)
		{
			float avgScore = kvp.Value.totalScore / kvp.Value.count;
			Debug.Log($"{kvp.Key}: {kvp.Value.count}次, 平均分数: {avgScore:F3}");
		}

		// 动作类型统计
		Debug.Log("=== 按动作类型统计 ===");
		foreach (var kvp in actionStats)
		{
			float avgScore = kvp.Value.totalScore / kvp.Value.count;
			Debug.Log($"{kvp.Key}: {kvp.Value.count}次, 平均分数: {avgScore:F3}");
		}

		// 威胁等级统计
		Debug.Log("=== 按威胁等级统计 ===");
		foreach (var kvp in threatStats)
		{
			float avgScore = kvp.Value.totalScore / kvp.Value.count;
			Debug.Log($"{kvp.Key}: {kvp.Value.count}次, 平均分数: {avgScore:F3}");
		}

		Debug.Log($"详细结果已保存到: {outPath}");
		Debug.Log("=== 测试完成 ===");
	}

	/// <summary>
	/// 分析选择结果，提供改进建议
	/// </summary>
	private string AnalyzeSelectionResult(MusicSceneData scene, MusicPreset selected, MusicPreset best, float bestScore, string targetNotes)
	{
		if (bestScore >= 0.8f)
		{
			return "Excellent match - Preset library covers this scenario type well";
		}
		else if (bestScore >= 0.6f)
		{
			return "Good match - Consider adding variations for this scenario type";
		}
		else if (bestScore > 0f)
		{
			return "Marginal match - Suggest adding preset music for this scenario type";
		}
		else
		{
			return "No match - Strongly suggest generating new music for this scenario type";
		}
	}

	private IEnumerator Run()
	{
		var lines = csvFile.text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		if (lines.Length <= 1)
		{
			Debug.LogWarning("CSV seems empty.");
			yield break;
		}

		// header
		int startIdx = 0;
		if (lines[0].StartsWith("id,")) startIdx = 1;

		var outPath = Path.Combine(Application.persistentDataPath, outputFileName);
		Directory.CreateDirectory(Path.GetDirectoryName(outPath));
		using (var sw = new StreamWriter(outPath, false))
		{
			sw.WriteLine("id,selectedPresetID,selectedPresetName,bestPresetID,bestScore,meetsThreshold,usedFallback,calcIntensity,notes");

			for (int i = startIdx; i < lines.Length; i++)
			{
				var line = lines[i].Trim();
				if (string.IsNullOrEmpty(line)) continue;

				bool ok = TryParseRow(line, out string id, out MusicSceneData scene, out string notes);
				if (!ok)
				{
					if (showDebugInfo) Debug.LogWarning($"Skip malformed row: {line}");
					continue;
				}

				if (!usePresetSelection)
				{
					// Selection disabled: log AI intent only (no waiting).
					AIGeneratedMusic.Instance?.GenerateMusicForScene(scene);
					sw.WriteLine($"{id},AI,AI,AI,NA,false,false,{scene.CalculateIntensity():F2},\"{Escape(notes)}\"");
					yield return new WaitForSeconds(perRowDelaySeconds);
					continue;
				}

				var mgr = MusicPresetManager.Instance;
				if (mgr == null || mgr.musicPresets == null || mgr.musicPresets.Count == 0)
				{
					if (showDebugInfo) Debug.LogError("MusicPresetManager/presets not ready.");
					sw.WriteLine($"{id},NONE,NONE,NONE,0,false,true,{scene.CalculateIntensity():F2},\"No presets\"");
					yield return new WaitForSeconds(perRowDelaySeconds);
					continue;
				}

				// Compute best over library using the same weighting idea
				MusicPreset best = null;
				float bestScore = -1f;
				foreach (var p in mgr.musicPresets)
				{
					var s = CalcSimilarity(scene, p.sceneData);
					if (s > bestScore)
					{
						bestScore = s;
						best = p;
					}
				}

				var selected = mgr.SelectBestPreset(scene);
				bool meets = bestScore >= mgr.similarityThreshold;
				bool usedFallback = selected != null && best != null && selected.presetID != best.presetID && !meets;

				if (actuallyPlayAudio && selected != null)
				{
					if (selected.musicClip == null)
					{
						yield return StartCoroutine(mgr.LoadAudioFileOnDemand(selected));
					}
					if (selected.musicClip != null)
					{
						AudioManager.Instance?.PlayMusic(selected.musicClip, crossfadeSeconds, selected.presetName);
					}
				}

				sw.WriteLine($"{id},{Val(selected?.presetID)},{Val(selected?.presetName)},{Val(best?.presetID)},{bestScore:F2},{meets},{usedFallback},{scene.CalculateIntensity():F2},\"{Escape(notes)}\"");

				if (showDebugInfo)
				{
					Debug.Log($"[CSV] {id} -> selected={(selected?.presetName ?? "NONE")} best={(best?.presetName ?? "NONE")} score={bestScore:F2} meets={meets} fallback={usedFallback}");
				}

				yield return new WaitForSeconds(perRowDelaySeconds);
			}
		}

		if (showDebugInfo)
		{
			Debug.Log($"CSV batch finished. Output: {outPath}");
		}
	}

	private IEnumerator GenerateAIPresetsSequential()
	{
		if (AIGeneratedMusic.Instance == null)
		{
			Debug.LogError("AIGeneratedMusic not found.");
			yield break;
		}

		var lines = csvFile.text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		if (lines.Length <= 1)
		{
			Debug.LogWarning("CSV seems empty.");
			yield break;
		}
		int startIdx = 0;
		if (lines[0].StartsWith("id,")) startIdx = 1;

		for (int i = startIdx; i < lines.Length; i++)
		{
			var line = lines[i].Trim();
			if (string.IsNullOrEmpty(line)) continue;

			bool ok = TryParseRow(line, out string id, out MusicSceneData scene, out string notes);
			if (!ok)
			{
				if (showDebugInfo) Debug.LogWarning($"Skip malformed row: {line}");
				continue;
			}

			bool done = false; AudioClip result = null; string error = null;
			Action<AudioClip> onGen = clip => { result = clip; done = true; };
			Action<string> onFail = err => { error = err; done = true; };

			AIGeneratedMusic.OnMusicGenerated += onGen;
			AIGeneratedMusic.OnGenerationFailed += onFail;

			AIGeneratedMusic.Instance.GenerateMusicForSceneForceAI(scene);

			// wait until this round finishes
			float timeout = 90f; float elapsed = 0f;
			while (!done && elapsed < timeout)
			{
				elapsed += Time.deltaTime;
				yield return null;
			}

			AIGeneratedMusic.OnMusicGenerated -= onGen;
			AIGeneratedMusic.OnGenerationFailed -= onFail;

			if (!done)
			{
				Debug.LogWarning($"{id}: generation timeout");
				continue;
			}

			if (result != null)
			{
				if (MusicPresetManager.Instance != null)
				{
					var p = MusicPresetManager.Instance.CreatePreset(scene, result);
					if (showDebugInfo) Debug.Log($"{id}: preset created -> {p.presetName}");
				}
				else
				{
					Debug.LogWarning("MusicPresetManager not found, cannot save preset.");
				}
			}
			else
			{
				Debug.LogWarning($"{id}: generation failed -> {error}");
			}

			yield return new WaitForSeconds(perRowDelaySeconds);
		}

		Debug.Log("AI preset batch finished.");
	}

	private IEnumerator GenerateAIPresetsConcurrent()
	{
		if (AIGeneratedMusic.Instance == null)
		{
			Debug.LogError("AIGeneratedMusic not found.");
			yield break;
		}
		var lines = csvFile.text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		if (lines.Length <= 1)
		{
			Debug.LogWarning("CSV seems empty.");
			yield break;
		}
		int startIdx = 0; if (lines[0].StartsWith("id,")) startIdx = 1;

		// queues
		Queue<(string id, MusicSceneData scene)> submitQ = new Queue<(string, MusicSceneData)>();
		for (int i = startIdx; i < lines.Length; i++)
		{
			var line = lines[i].Trim(); if (string.IsNullOrEmpty(line)) continue;
			if (TryParseRow(line, out string id, out MusicSceneData s, out string _)) submitQ.Enqueue((id, s));
		}
		var inFlight = new Dictionary<string, (string id, MusicSceneData scene)>();
		var completed = 0; var total = submitQ.Count;

		float windowStart = Time.time; int sentInWindow = 0;

		while (completed < total)
		{
			// window reset
			if (Time.time - windowStart >= windowSeconds)
			{
				windowStart = Time.time; sentInWindow = 0;
			}

			// submit respecting rate limit
			while (submitQ.Count > 0 && sentInWindow < maxSubmissionsPerWindow)
			{
				var item = submitQ.Dequeue();
				string prompt = item.scene.GetMusicStyleDescription();
				string jobId = null; string errMsg = null;
				bool done = false;
				yield return StartCoroutine(AIGeneratedMusic.Instance.SubmitGenerateRequest(prompt, item.id, 
					id => { jobId = id; done = true; }, 
					err => { errMsg = err; done = true; }));
				if (!done || string.IsNullOrEmpty(jobId))
				{
					Debug.LogWarning($"Submit failed for {item.id}: {errMsg}");
					continue;
				}
				inFlight[jobId] = item;
				sentInWindow++;
				if (sentInWindow >= maxSubmissionsPerWindow) break;
			}

			// poll a limited number of jobs in flight
			int polled = 0;
			var jobIds = new List<string>(inFlight.Keys);
			foreach (var jid in jobIds)
			{
				if (polled >= maxConcurrentPolls) break;
				polled++;
				bool done = false; AudioClip clip = null; string err = null; string jidCopy = jid;
				yield return StartCoroutine(AIGeneratedMusic.Instance.PollAndFetchClip(jidCopy, 
					(c, id) => { clip = c; done = true; }, 
					(id, e) => { err = e; done = true; }));
				if (done)
				{
					var item = inFlight[jidCopy]; inFlight.Remove(jidCopy);
					if (clip != null)
					{
						if (MusicPresetManager.Instance != null)
						{
							MusicPresetManager.Instance.CreatePreset(item.scene, clip);
						}
						completed++;
					}
					else
					{
						Debug.LogWarning($"Job {jidCopy} failed: {err}");
						completed++;
					}
				}
			}

			// small idle wait
			yield return new WaitForSeconds(0.2f);
		}

		Debug.Log($"Concurrent AI batch finished. total={total}");
	}

	private static string Val(string s) => string.IsNullOrEmpty(s) ? "NONE" : s.Replace(',', '_');
	private static string Escape(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("\"", "''");

	private bool TryParseRow(string line, out string id, out MusicSceneData scene, out string notes)
	{
		id = ""; notes = ""; scene = new MusicSceneData();
		var cols = SplitCSV(line);
		if (cols.Length < 14) return false;
		try
		{
			id = cols[0];
			scene.environment = EnumUtils.ParseEnum<EnvironmentType>(cols[1], EnvironmentType.Grasslands);
			scene.timeOfDay = EnumUtils.ParseEnum<TimeOfDay>(cols[2], TimeOfDay.Day);
			scene.weather = EnumUtils.ParseEnum<WeatherType>(cols[3], WeatherType.Clear);
			scene.currentAction = EnumUtils.ParseEnum<ActionType>(cols[4], ActionType.Walking);
			scene.actionIntensity = EnumUtils.Clamp01(cols[5]);
			scene.isStealth = EnumUtils.ParseBool(cols[6]);
			scene.enemyPresence = EnumUtils.ParseEnum<EnemyPresence>(cols[7], EnemyPresence.None);
			scene.enemyCount = Mathf.Max(0, EnumUtils.ParseInt(cols[8]));
			scene.threatLevel = EnumUtils.Clamp01(cols[9]);
			scene.gameLevel = EnumUtils.ParseEnum<GameLevel>(cols[10], GameLevel.Early);
			scene.playerHealth = EnumUtils.Clamp01(cols[11]);
			scene.isBossFight = EnumUtils.ParseBool(cols[12]);
			notes = cols[13];
			return true;
		}
		catch
		{
			return false;
		}
	}

	private string[] SplitCSV(string line)
	{
		// Simple split; our CSV has no embedded commas in fields except notes, which we keep as the last column.
		var arr = new List<string>();
		int idx = 0; int start = 0;
		while (idx < line.Length)
		{
			if (line[idx] == ',')
			{
				arr.Add(line.Substring(start, idx - start));
				start = idx + 1;
			}
			idx++;
		}
		arr.Add(line.Substring(start));
		return arr.ToArray();
	}



	private static float CalcSimilarity(MusicSceneData a, MusicSceneData b)
	{
		// Mirror the weights used in MusicPresetManager
		float score = 0f; float total = 0f;
		// Env 0.30
		float envW = 0.30f; score += (a.environment == b.environment ? 1f : 0f) * envW; total += envW;
		// Action 0.25
		float actW = 0.25f; score += (a.currentAction == b.currentAction ? 1f : 0f) * actW; total += actW;
		// Threat 0.20
		float thrW = 0.20f; score += (1f - Mathf.Abs(a.threatLevel - b.threatLevel)) * thrW; total += thrW;
		// Intensity 0.15
		float intW = 0.15f; score += (1f - Mathf.Abs(a.CalculateIntensity() - b.CalculateIntensity())) * intW; total += intW;
		// Time 0.10
		float timW = 0.10f; score += (a.timeOfDay == b.timeOfDay ? 1f : 0f) * timW; total += timW;
		return total > 0f ? score / total : 0f;
	}
}
