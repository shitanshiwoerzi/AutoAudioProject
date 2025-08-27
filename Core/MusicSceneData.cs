using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MusicSceneData
{
    [Header("基础场景信息")]
    public string sceneName = "默认场景";
    public string sceneID = "scene_001";
    
    [Header("环境属性")]
    public EnvironmentType environment = EnvironmentType.Grasslands;
    public TimeOfDay timeOfDay = TimeOfDay.Day;
    public WeatherType weather = WeatherType.Clear;
    public float temperature = 20f; // 温度影响音乐风格
    
    [Header("动作属性")]
    public ActionType currentAction = ActionType.Walking;
    public float actionIntensity = 0.5f; // 0-1 动作强度
    public bool isStealth = false;
    
    [Header("敌人/威胁")]
    public EnemyPresence enemyPresence = EnemyPresence.None;
    public int enemyCount = 0;
    public float threatLevel = 0f; // 0-1 威胁等级
    
    [Header("游戏状态")]
    public GameLevel gameLevel = GameLevel.Tutorial;
    public float playerHealth = 1f; // 0-1 玩家血量
    public bool isBossFight = false;
    
    [Header("音乐偏好")]
    public MusicStyle preferredStyle = MusicStyle.Ambient;
    public float tempo = 1f; // 0.5-2.0 音乐速度
    public float volume = 1f; // 0-1 音量
    
    // 生成唯一的场景标识符
    public string GetSceneKey()
    {
        return $"{environment}_{currentAction}_{enemyPresence}_{gameLevel}_{timeOfDay}";
    }
    
    // 转换为JSON格式
    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }
    
    // 从JSON创建场景数据
    public static MusicSceneData FromJson(string json)
    {
        return JsonUtility.FromJson<MusicSceneData>(json);
    }
    
    // 计算场景的总体强度（用于音乐选择）
    public float CalculateIntensity()
    {
        float intensity = 0f;
        
        // action intensity (weight:0.3)
        intensity += actionIntensity * 0.3f;
        
        // threatLevel (weight:0.4)
        intensity += threatLevel * 0.4f;
        
        // enemy count (weight:0.2)
        intensity += Mathf.Clamp01(enemyCount / 10f) * 0.2f;
        
        // BossFight (weight:0.3)
        if (isBossFight) intensity += 0.3f;
        
        // Health impact (increased tension when health is low) (weight:0.1)
        intensity += (1f - playerHealth) * 0.1f;
        
        return Mathf.Clamp01(intensity);
    }
    
    // 获取音乐风格描述
    public string GetMusicStyleDescription()
    {
        string description = "";
        
        // 环境描述
        description += $"{GetEnvironmentDescription()} ";
        
        // 动作描述
        description += $"{GetActionDescription()} ";
        
        // 威胁描述
        description += $"{GetThreatDescription()} ";
        
        // 时间描述
        description += $"{GetTimeDescription()} ";
        
        return description.Trim();
    }
    
    private string GetEnvironmentDescription()
    {
        switch (environment)
        {
            case EnvironmentType.Grasslands: return "peaceful grasslands";
            case EnvironmentType.Forest: return "mysterious forest";
            case EnvironmentType.DarkDungeon: return "dark dungeon";
            case EnvironmentType.Urban: return "urban city";
            case EnvironmentType.Ocean: return "ocean beach";
            case EnvironmentType.Mountain: return "mountain peaks";
            case EnvironmentType.Desert: return "hot desert";
            case EnvironmentType.Snow: return "frozen snow";
            default: return "unknown environment";
        }
    }
    
    private string GetActionDescription()
    {
        switch (currentAction)
        {
            case ActionType.Walking: return "exploration";
            case ActionType.Running: return "fast-paced";
            case ActionType.Combat: return "intense combat";
            case ActionType.Stealth: return "stealth";
            case ActionType.Flying: return "aerial";
            case ActionType.Swimming: return "underwater";
            default: return "general";
        }
    }
    
    private string GetThreatDescription()
    {
        if (threatLevel > 0.7f) return "high tension";
        if (threatLevel > 0.3f) return "moderate tension";
        return "relaxed";
    }
    
    private string GetTimeDescription()
    {
        switch (timeOfDay)
        {
            case TimeOfDay.Day: return "daytime";
            case TimeOfDay.Night: return "nighttime";
            case TimeOfDay.Dawn: return "dawn";
            case TimeOfDay.Dusk: return "dusk";
            default: return "";
        }
    }
}

// 枚举定义
public enum EnvironmentType
{
    Grasslands, Forest, DarkDungeon, Urban, Ocean, Mountain, Desert, Snow
}

public enum TimeOfDay
{
    Day, Night, Dawn, Dusk
}

public enum WeatherType
{
    Clear, Rain, Snow, Storm, Fog
}

public enum ActionType
{
    Walking, Running, Combat, Stealth, Flying, Swimming
}

public enum EnemyPresence
{
    None, Few, Many, Boss, Lurking
}

public enum GameLevel
{
    Tutorial, Early, Mid, Late, FinalBoss
}

public enum MusicStyle
{
    Ambient, Action, Tension, Peaceful, Epic, Mysterious
} 