using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MultiTrajectoryReplayer : MonoBehaviour
{
	[Header("Trajectories")]
	public List<TextAsset> trajectories = new List<TextAsset>();
	public bool playOnStart = false;
	public bool randomOrder = false;
	public int loopCount = 1; // 0 = infinite
	public float pauseBetween = 1.0f; // seconds

	[Header("Playback Options")]
	public bool usePresetSelection = true;
	public float timeScale = 1f;
	public float crossfadeSeconds = 1.0f;
	public bool stopCurrentOnStart = true;
	public bool skipIfSameClip = true;
	public bool enableCsvLog = true;
	public string csvFileNamePrefix = "trajectory_batch_"; // each file per trajectory

	[Header("Debug")] 
	public bool showDebugInfo = true;

	[Header("Summary Output")]
	public bool writeSummaryCsv = true;
	public string summaryCsvFileName = "trajectory_batch_summary.csv";

	private Coroutine _routine;

	private void Start()
	{
		if (playOnStart)
		{
			StartBatch();
		}
	}

	[ContextMenu("Start Batch")]
	public void StartBatch()
	{
		if (_routine != null) StopCoroutine(_routine);
		_routine = StartCoroutine(RunBatch());
	}

	[ContextMenu("Stop Batch")]
	public void StopBatch()
	{
		if (_routine != null) StopCoroutine(_routine);
		_routine = null;
	}

	private IEnumerator RunBatch()
	{
		if (trajectories == null || trajectories.Count == 0)
		{
			Debug.LogWarning("[MultiTrajectory] No trajectories set.");
			yield break;
		}

		var order = new List<TextAsset>(trajectories);
		int cycles = 0;
		while (loopCount == 0 || cycles < loopCount)
		{
			if (randomOrder)
			{
				for (int i = 0; i < order.Count; i++)
				{
					int j = UnityEngine.Random.Range(i, order.Count);
					(order[i], order[j]) = (order[j], order[i]);
				}
			}

			foreach (var ta in order)
			{
				if (ta == null) continue;
				var go = new GameObject($"TrajectoryReplayer_{ta.name}");
				var replay = go.AddComponent<TrajectoryReplayer>();
				replay.trajectoryJson = ta;
				replay.usePresetSelection = usePresetSelection;
				replay.timeScale = timeScale;
				replay.crossfadeSeconds = crossfadeSeconds;
				replay.stopCurrentOnStart = stopCurrentOnStart;
				replay.skipIfSameClip = skipIfSameClip;
				replay.enableCsvLog = enableCsvLog;
				replay.csvFileName = $"{csvFileNamePrefix}{ta.name}.csv";
				replay.StartPlayback();

				bool done = false;
				replay.OnPlaybackCompleted += () => { done = true; };
				while (!done)
				{
					yield return null;
				}
				Destroy(go);

				if (writeSummaryCsv)
				{
					TryAppendSummary(ta, replay.csvFileName);
				}

				if (pauseBetween > 0f)
				{
					yield return new WaitForSeconds(pauseBetween);
				}
			}

			cycles++;
		}
		if (showDebugInfo) Debug.Log("[MultiTrajectory] Batch finished.");
	}

	private void TryAppendSummary(TextAsset ta, string perTrajCsvName)
	{
		try
		{
			var perPath = Path.Combine(Application.persistentDataPath, perTrajCsvName);
			if (!File.Exists(perPath))
			{
				Debug.LogWarning($"[MultiTrajectory] Per-trajectory CSV not found: {perPath}");
				return;
			}
			var kpis = ParseKpis(perPath);
			if (kpis == null || kpis.Count == 0)
			{
				Debug.LogWarning($"[MultiTrajectory] KPI section not found in {perPath}");
				return;
			}
			string title = ta != null ? GetTitleFromJson(ta.text) : "";
			if (string.IsNullOrEmpty(title)) title = ta != null ? ta.name : perTrajCsvName;
			var sumPath = Path.Combine(Application.persistentDataPath, summaryCsvFileName);
			bool needHeader = !File.Exists(sumPath);
			using (var sw = new StreamWriter(sumPath, true, System.Text.Encoding.UTF8))
			{
				if (needHeader)
				{
					sw.Write('\uFEFF');
					sw.WriteLine("title,file,avgBestScore,belowThresholdShare,switchesPerMinute,dwellMedianSec,dwellShortShare,avgIntensityJump,aiShare,bounceCount");
				}
				sw.WriteLine($"\"{title}\",{perTrajCsvName},{Val(kpis, "avgBestScore")},{Val(kpis, "belowThresholdShare")},{Val(kpis, "switchesPerMinute")},{Val(kpis, "dwellMedianSec")},{Val(kpis, "dwellShortShare")},{Val(kpis, "avgIntensityJump")},{Val(kpis, "aiShare")},{Val(kpis, "bounceCount")}");
			}
			if (showDebugInfo) Debug.Log($"[MultiTrajectory] Summary appended: {sumPath}");
		}
		catch (Exception e)
		{
			Debug.LogError($"[MultiTrajectory] Append summary failed: {e.Message}");
		}
	}

	private Dictionary<string, string> ParseKpis(string path)
	{
		var lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);
		int idx = -1;
		for (int i = lines.Length - 1; i >= 0; i--)
		{
			if (lines[i].Trim().Equals("metric,value,notes", StringComparison.OrdinalIgnoreCase))
			{
				idx = i; break;
			}
		}
		if (idx < 0) return null;
		var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		for (int i = idx + 1; i < lines.Length; i++)
		{
			var line = lines[i].Trim(); if (string.IsNullOrEmpty(line)) continue;
			var parts = line.Split(',');
			if (parts.Length < 2) continue;
			var key = parts[0].Trim();
			var val = parts[1].Trim();
			if (!dict.ContainsKey(key)) dict[key] = val;
		}
		return dict;
	}

	private string GetTitleFromJson(string json)
	{
		try
		{
			var t = JsonUtility.FromJson<Trajectory>(json);
			return t != null ? t.title : string.Empty;
		}
		catch { return string.Empty; }
	}

	private static string Val(Dictionary<string, string> map, string key)
	{
		if (map != null && map.TryGetValue(key, out var v)) return v;
		return "";
	}
}


