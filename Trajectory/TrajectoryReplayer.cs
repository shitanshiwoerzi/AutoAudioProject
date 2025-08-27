using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AutoAudioDemo.Utils;

[Serializable]
public class Trajectory
{
	public string title;
	public List<TrajectorySegment> segments;
}

[Serializable]
public class TrajectorySegment
{
	public float start;
	public float duration;
	public TrajectoryState state;
}

[Serializable]
public class TrajectoryState
{
	public string environment;
	public string timeOfDay;
	public string weather;
	public string currentAction;
	public float actionIntensity;
	public bool isStealth;
	public string enemyPresence;
	public int enemyCount;
	public float threatLevel;
	public string gameLevel;
	public float playerHealth;
	public bool isBossFight;
}

public class TrajectoryReplayer : MonoBehaviour
{
	[Header("Source")]
	public TextAsset trajectoryJson;
	public bool autoStart = true;
	public float timeScale = 1f; // 1 = realtime; 2 = 2x speed

	[Header("Mode")] 
	public bool usePresetSelection = true; // true: select from preset library; false: call AI generator
	public float crossfadeSeconds = 1.0f;

	[Header("Debug")] 
	public bool showDebugInfo = true;
	
	[Header("CSV Logging")]
	public bool enableCsvLog = false;
	public string csvFileName = "";
	
	[Header("Playback Control")]
	public bool stopCurrentOnStart = true;
	public bool skipIfSameClip = false;

	// 事件
	public System.Action OnPlaybackCompleted;
	
	private Coroutine playRoutine;

	private void Start()
	{
		if (autoStart && trajectoryJson != null)
		{
			StartPlayback();
		}
	}

	[ContextMenu("Start Playback")]
	public void StartPlayback()
	{
		if (playRoutine != null) StopCoroutine(playRoutine);
		playRoutine = StartCoroutine(Play());
	}

	[ContextMenu("Stop Playback")]
	public void StopPlayback()
	{
		if (playRoutine != null) StopCoroutine(playRoutine);
		playRoutine = null;
	}

	private IEnumerator Play()
	{
		if (trajectoryJson == null)
		{
			Debug.LogWarning("Trajectory JSON not set.");
			yield break;
		}

		Trajectory traj = null;
		try
		{
			traj = JsonUtility.FromJson<Trajectory>(trajectoryJson.text);
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to parse trajectory JSON: {e.Message}");
			yield break;
		}

		if (traj == null || traj.segments == null || traj.segments.Count == 0)
		{
			Debug.LogWarning("Empty trajectory.");
			yield break;
		}

		if (showDebugInfo)
		{
			Debug.Log($"[Trajectory] {traj.title} | segments={traj.segments.Count}");
		}

		// Sort by start just in case
		traj.segments.Sort((a, b) => a.start.CompareTo(b.start));

		foreach (var seg in traj.segments)
		{
			var sceneData = Convert(seg.state);

			if (showDebugInfo)
			{
				Debug.Log($"[Segment] start={seg.start}s dur={seg.duration}s state={sceneData.ToJson()}");
			}

			FeedScene(sceneData);

			var wait = Mathf.Max(0f, seg.duration) / Mathf.Max(0.001f, timeScale);
			yield return new WaitForSeconds(wait);
		}

		if (showDebugInfo)
		{
			Debug.Log("[Trajectory] Playback complete.");
		}
		
		// 触发播放完成事件
		OnPlaybackCompleted?.Invoke();
	}

	private void FeedScene(MusicSceneData sceneData)
	{
		if (usePresetSelection)
		{
			if (MusicPresetManager.Instance == null)
			{
				Debug.LogError("MusicPresetManager not found.");
				return;
			}

			var preset = MusicPresetManager.Instance.SelectBestPreset(sceneData);
			if (preset == null)
			{
				Debug.LogWarning("No preset selected for current state.");
				return;
			}

			if (preset.musicClip == null)
			{
				StartCoroutine(EnsureAndPlay(preset));
			}
			else
			{
				PlayClip(preset.musicClip, preset.presetName);
			}
		}
		else
		{
			if (AIGeneratedMusic.Instance == null)
			{
				Debug.LogError("AIGeneratedMusic not found.");
				return;
			}
			AIGeneratedMusic.Instance.GenerateMusicForScene(sceneData);
		}
	}

	private IEnumerator EnsureAndPlay(MusicPreset preset)
	{
		yield return StartCoroutine(MusicPresetManager.Instance.LoadAudioFileOnDemand(preset));
		if (preset.musicClip != null)
		{
			PlayClip(preset.musicClip, preset.presetName);
		}
	}

	private void PlayClip(AudioClip clip, string label)
	{
		if (AudioManager.Instance == null)
		{
			Debug.LogError("AudioManager not found.");
			return;
		}
		AudioManager.Instance.PlayMusic(clip, crossfadeSeconds, label);
	}

	private static MusicSceneData Convert(TrajectoryState s)
	{
		var d = new MusicSceneData();
		// Map strings to enums safely
		d.environment = EnumUtils.ParseEnum<EnvironmentType>(s.environment, EnvironmentType.Grasslands);
		d.timeOfDay = EnumUtils.ParseEnum<TimeOfDay>(s.timeOfDay, TimeOfDay.Day);
		d.weather = EnumUtils.ParseEnum<WeatherType>(s.weather, WeatherType.Clear);
		d.currentAction = EnumUtils.ParseEnum<ActionType>(s.currentAction, ActionType.Walking);
		d.enemyPresence = EnumUtils.ParseEnum<EnemyPresence>(s.enemyPresence, EnemyPresence.None);
		d.gameLevel = EnumUtils.ParseEnum<GameLevel>(s.gameLevel, GameLevel.Early);

		d.actionIntensity = Mathf.Clamp01(s.actionIntensity);
		d.isStealth = s.isStealth;
		d.enemyCount = Mathf.Max(0, s.enemyCount);
		d.threatLevel = Mathf.Clamp01(s.threatLevel);
		d.playerHealth = Mathf.Clamp01(s.playerHealth);
		d.isBossFight = s.isBossFight;
		return d;
	}

}
