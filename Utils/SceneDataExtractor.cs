using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 场景数据提取器 - 从Unity场景中自动提取场景信息
/// </summary>
public class SceneDataExtractor : MonoBehaviour
{
    [Header("提取设置")]
    public bool autoExtractOnStart = true;
    public float extractionInterval = 5f; // 提取间隔（秒）
    public bool enablePeriodicExtraction = false; // 是否启用周期性提取
    public bool onlyExtractOnChange = true; // 只在场景数据变化时提取
    public LayerMask playerLayer = 1;
    public LayerMask enemyLayer = 8;
    
    [Header("场景分析")]
    public bool analyzeEnvironment = true;
    public bool analyzeEnemies = true;
    public bool analyzePlayerState = true;
    public bool analyzeTimeOfDay = true;
    
    [Header("区域系统集成")]
    public bool useZoneTriggerData = true; // 优先使用ZoneTrigger的结构化数据
    public bool fallbackToTagAnalysis = true; // 如果ZoneTrigger数据不可用，回退到标签分析
    
    [Header("调试")]
    public bool showDebugInfo = true;
    
    // 当前场景数据
    private MusicSceneData currentSceneData;
    
    // 事件
    public static event System.Action<MusicSceneData> OnSceneDataUpdated;
    
    private void Start()
    {
        if (autoExtractOnStart)
        {
            ExtractSceneData();
        }
        
        if (enablePeriodicExtraction && extractionInterval > 0)
        {
            StartCoroutine(PeriodicExtraction());
        }
    }
    
    /// <summary>
    /// 提取当前场景数据
    /// </summary>
    public MusicSceneData ExtractSceneData()
    {
        currentSceneData = new MusicSceneData();
        
        // 基础信息
        currentSceneData.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        currentSceneData.sceneID = $"scene_{Time.time.GetHashCode()}";
        
        if (analyzeEnvironment)
        {
            AnalyzeEnvironment();
        }
        
        if (analyzeEnemies)
        {
            AnalyzeEnemies();
        }
        
        if (analyzePlayerState)
        {
            AnalyzePlayerState();
        }
        
        if (analyzeTimeOfDay)
        {
            AnalyzeTimeOfDay();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"场景数据提取完成: {currentSceneData.ToJson()}");
        }
        
        OnSceneDataUpdated?.Invoke(currentSceneData);
        return currentSceneData;
    }
    
    /// <summary>
    /// 分析环境
    /// </summary>
    private void AnalyzeEnvironment()
    {
        // 优先尝试从ZoneTrigger获取结构化数据
        if (useZoneTriggerData && TryGetZoneTriggerData())
        {
            if (showDebugInfo)
            {
                Debug.Log("使用ZoneTrigger的结构化数据");
            }
            return;
        }
        
        // 如果ZoneTrigger数据不可用，回退到传统分析
        if (fallbackToTagAnalysis)
        {
            // 通过地形分析环境类型
            Terrain terrain = FindObjectOfType<Terrain>();
            if (terrain != null)
            {
                // 分析地形高度和纹理
                AnalyzeTerrainEnvironment(terrain);
            }
            else
            {
                // 通过GameObject标签分析
                AnalyzeEnvironmentByTags();
            }
            
            // 分析天气效果
            AnalyzeWeather();
        }
    }
    
    /// <summary>
    /// 内部分析环境方法
    /// </summary>
    private void AnalyzeEnvironmentInternal(MusicSceneData data)
    {
        // 优先尝试从ZoneTrigger获取结构化数据
        if (useZoneTriggerData && TryGetZoneTriggerDataInternal(data))
        {
            return;
        }
        
        // 如果ZoneTrigger数据不可用，回退到传统分析
        if (fallbackToTagAnalysis)
        {
            // 通过地形分析环境类型
            Terrain terrain = FindObjectOfType<Terrain>();
            if (terrain != null)
            {
                // 分析地形高度和纹理
                AnalyzeTerrainEnvironmentInternal(terrain, data);
            }
            else
            {
                // 通过GameObject标签分析
                AnalyzeEnvironmentByTagsInternal(data);
            }
            
            // 分析天气效果
            AnalyzeWeatherInternal(data);
        }
    }
    
    /// <summary>
    /// 尝试从ZoneTrigger获取结构化数据
    /// </summary>
    private bool TryGetZoneTriggerData()
    {
        // 查找场景中所有的ZoneTrigger
        ZoneTrigger[] zoneTriggers = FindObjectsOfType<ZoneTrigger>();
        
        if (zoneTriggers.Length == 0)
        {
            if (showDebugInfo)
            {
                Debug.Log("场景中没有找到ZoneTrigger组件");
            }
            return false;
        }
        
        // 查找玩家附近的ZoneTrigger
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            if (showDebugInfo)
            {
                Debug.Log("场景中没有找到Player标签的GameObject");
            }
            return false;
        }
        
        ZoneTrigger closestZone = null;
        float closestDistance = float.MaxValue;
        
        foreach (var zone in zoneTriggers)
        {
            if (zone.sceneData != null && zone.useStructuredData)
            {
                float distance = Vector3.Distance(player.transform.position, zone.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestZone = zone;
                }
            }
        }
        
        if (closestZone != null && closestZone.sceneData != null)
        {
            // 使用ZoneTrigger的数据更新当前场景数据
            UpdateSceneDataFromZoneTrigger(closestZone.sceneData);
            
            if (showDebugInfo)
            {
                Debug.Log($"使用ZoneTrigger数据: {closestZone.zoneName} (距离: {closestDistance:F1})");
            }
            return true;
        }
        
        if (showDebugInfo)
        {
            Debug.Log("没有找到可用的ZoneTrigger结构化数据");
        }
        return false;
    }
    
    /// <summary>
    /// 内部分析方法：尝试从ZoneTrigger获取结构化数据
    /// </summary>
    private bool TryGetZoneTriggerDataInternal(MusicSceneData data)
    {
        // 查找场景中所有的ZoneTrigger
        ZoneTrigger[] zoneTriggers = FindObjectsOfType<ZoneTrigger>();
        
        if (zoneTriggers.Length == 0)
        {
            return false;
        }
        
        // 查找玩家附近的ZoneTrigger
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return false;
        }
        
        ZoneTrigger closestZone = null;
        float closestDistance = float.MaxValue;
        
        foreach (var zone in zoneTriggers)
        {
            if (zone.sceneData != null && zone.useStructuredData)
            {
                float distance = Vector3.Distance(player.transform.position, zone.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestZone = zone;
                }
            }
        }
        
        if (closestZone != null && closestZone.sceneData != null)
        {
            // 复制ZoneTrigger的数据到目标数据对象
            CopySceneData(closestZone.sceneData, data);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 从ZoneTrigger更新场景数据
    /// </summary>
    private void UpdateSceneDataFromZoneTrigger(MusicSceneData zoneData)
    {
        if (zoneData == null) return;
        
        // 复制基础数据
        currentSceneData.environment = zoneData.environment;
        currentSceneData.timeOfDay = zoneData.timeOfDay;
        currentSceneData.weather = zoneData.weather;
        currentSceneData.temperature = zoneData.temperature;
        currentSceneData.currentAction = zoneData.currentAction;
        currentSceneData.actionIntensity = zoneData.actionIntensity;
        currentSceneData.isStealth = zoneData.isStealth;
        currentSceneData.enemyPresence = zoneData.enemyPresence;
        currentSceneData.enemyCount = zoneData.enemyCount;
        currentSceneData.threatLevel = zoneData.threatLevel;
        currentSceneData.gameLevel = zoneData.gameLevel;
        currentSceneData.playerHealth = zoneData.playerHealth;
        currentSceneData.isBossFight = zoneData.isBossFight;
        currentSceneData.preferredStyle = zoneData.preferredStyle;
        currentSceneData.tempo = zoneData.tempo;
        currentSceneData.volume = zoneData.volume;
    }
    
    /// <summary>
    /// 复制场景数据
    /// </summary>
    private void CopySceneData(MusicSceneData source, MusicSceneData target)
    {
        if (source == null || target == null) return;
        
        target.environment = source.environment;
        target.timeOfDay = source.timeOfDay;
        target.weather = source.weather;
        target.temperature = source.temperature;
        target.currentAction = source.currentAction;
        target.actionIntensity = source.actionIntensity;
        target.isStealth = source.isStealth;
        target.enemyPresence = source.enemyPresence;
        target.enemyCount = source.enemyCount;
        target.threatLevel = source.threatLevel;
        target.gameLevel = source.gameLevel;
        target.playerHealth = source.playerHealth;
        target.isBossFight = source.isBossFight;
        target.preferredStyle = source.preferredStyle;
        target.tempo = source.tempo;
        target.volume = source.volume;
    }
    
    /// <summary>
    /// 通过地形分析环境
    /// </summary>
    private void AnalyzeTerrainEnvironment(Terrain terrain)
    {
        // 获取地形数据
        TerrainData terrainData = terrain.terrainData;
        
        // 分析高度
        float avgHeight = 0f;
        float maxHeight = terrainData.size.y;
        
        // 简单的高度分析
        if (maxHeight > 100f)
        {
            currentSceneData.environment = EnvironmentType.Mountain;
        }
        else if (maxHeight > 50f)
        {
            currentSceneData.environment = EnvironmentType.Forest;
        }
        else
        {
            currentSceneData.environment = EnvironmentType.Grasslands;
        }
        
        // 分析纹理（如果有的话）
        if (terrainData.alphamapTextures.Length > 0)
        {
            // 这里可以添加更复杂的纹理分析
        }
    }
    
    /// <summary>
    /// 内部分析地形环境方法
    /// </summary>
    private void AnalyzeTerrainEnvironmentInternal(Terrain terrain, MusicSceneData data)
    {
        // 获取地形数据
        TerrainData terrainData = terrain.terrainData;
        
        // 分析高度
        float avgHeight = 0f;
        float maxHeight = terrainData.size.y;
        
        // 简单的高度分析
        if (maxHeight > 100f)
        {
            data.environment = EnvironmentType.Mountain;
        }
        else if (maxHeight > 50f)
        {
            data.environment = EnvironmentType.Forest;
        }
        else
        {
            data.environment = EnvironmentType.Grasslands;
        }
        
        // 分析纹理（如果有的话）
        if (terrainData.alphamapTextures.Length > 0)
        {
            // 这里可以添加更复杂的纹理分析
        }
    }
    
    /// <summary>
    /// 通过标签分析环境
    /// </summary>
    private void AnalyzeEnvironmentByTags()
    {
        // 查找环境相关的GameObject，使用安全的标签查找
        GameObject[] forestObjects = FindGameObjectsWithTagSafe("Forest");
        GameObject[] dungeonObjects = FindGameObjectsWithTagSafe("Dungeon");
        GameObject[] urbanObjects = FindGameObjectsWithTagSafe("Urban");
        GameObject[] oceanObjects = FindGameObjectsWithTagSafe("Ocean");
        GameObject[] desertObjects = FindGameObjectsWithTagSafe("Desert");
        GameObject[] snowObjects = FindGameObjectsWithTagSafe("Snow");
        
        if (dungeonObjects.Length > 0)
        {
            currentSceneData.environment = EnvironmentType.DarkDungeon;
        }
        else if (urbanObjects.Length > 0)
        {
            currentSceneData.environment = EnvironmentType.Urban;
        }
        else if (oceanObjects.Length > 0)
        {
            currentSceneData.environment = EnvironmentType.Ocean;
        }
        else if (desertObjects.Length > 0)
        {
            currentSceneData.environment = EnvironmentType.Desert;
        }
        else if (snowObjects.Length > 0)
        {
            currentSceneData.environment = EnvironmentType.Snow;
        }
        else if (forestObjects.Length > 0)
        {
            currentSceneData.environment = EnvironmentType.Forest;
        }
        else
        {
            currentSceneData.environment = EnvironmentType.Grasslands;
        }
    }
    
    /// <summary>
    /// 内部分析环境标签方法
    /// </summary>
    private void AnalyzeEnvironmentByTagsInternal(MusicSceneData data)
    {
        // 查找环境相关的GameObject，使用安全的标签查找
        GameObject[] forestObjects = FindGameObjectsWithTagSafe("Forest");
        GameObject[] dungeonObjects = FindGameObjectsWithTagSafe("Dungeon");
        GameObject[] urbanObjects = FindGameObjectsWithTagSafe("Urban");
        GameObject[] oceanObjects = FindGameObjectsWithTagSafe("Ocean");
        GameObject[] desertObjects = FindGameObjectsWithTagSafe("Desert");
        GameObject[] snowObjects = FindGameObjectsWithTagSafe("Snow");
        
        if (dungeonObjects.Length > 0)
        {
            data.environment = EnvironmentType.DarkDungeon;
        }
        else if (urbanObjects.Length > 0)
        {
            data.environment = EnvironmentType.Urban;
        }
        else if (oceanObjects.Length > 0)
        {
            data.environment = EnvironmentType.Ocean;
        }
        else if (desertObjects.Length > 0)
        {
            data.environment = EnvironmentType.Desert;
        }
        else if (snowObjects.Length > 0)
        {
            data.environment = EnvironmentType.Snow;
        }
        else if (forestObjects.Length > 0)
        {
            data.environment = EnvironmentType.Forest;
        }
        else
        {
            data.environment = EnvironmentType.Grasslands;
        }
    }
    
    /// <summary>
    /// 安全地查找带有指定标签的GameObject，如果标签不存在则返回空数组
    /// </summary>
    private GameObject[] FindGameObjectsWithTagSafe(string tag)
    {
        try
        {
            return GameObject.FindGameObjectsWithTag(tag);
        }
        catch (UnityException)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"标签 '{tag}' 未定义，跳过此标签的查找");
            }
            return new GameObject[0];
        }
    }
    
    /// <summary>
    /// 分析天气
    /// </summary>
    private void AnalyzeWeather()
    {
        // 查找天气相关的GameObject，使用安全的标签查找
        GameObject[] rainObjects = FindGameObjectsWithTagSafe("Rain");
        GameObject[] snowObjects = FindGameObjectsWithTagSafe("Snow");
        GameObject[] stormObjects = FindGameObjectsWithTagSafe("Storm");
        GameObject[] fogObjects = FindGameObjectsWithTagSafe("Fog");
        
        if (stormObjects.Length > 0)
        {
            currentSceneData.weather = WeatherType.Storm;
        }
        else if (snowObjects.Length > 0)
        {
            currentSceneData.weather = WeatherType.Snow;
        }
        else if (rainObjects.Length > 0)
        {
            currentSceneData.weather = WeatherType.Rain;
        }
        else if (fogObjects.Length > 0)
        {
            currentSceneData.weather = WeatherType.Fog;
        }
        else
        {
            currentSceneData.weather = WeatherType.Clear;
        }
    }
    
    /// <summary>
    /// 内部分析天气方法
    /// </summary>
    private void AnalyzeWeatherInternal(MusicSceneData data)
    {
        // 查找天气相关的GameObject，使用安全的标签查找
        GameObject[] rainObjects = FindGameObjectsWithTagSafe("Rain");
        GameObject[] snowObjects = FindGameObjectsWithTagSafe("Snow");
        GameObject[] stormObjects = FindGameObjectsWithTagSafe("Storm");
        GameObject[] fogObjects = FindGameObjectsWithTagSafe("Fog");
        
        if (stormObjects.Length > 0)
        {
            data.weather = WeatherType.Storm;
        }
        else if (snowObjects.Length > 0)
        {
            data.weather = WeatherType.Snow;
        }
        else if (rainObjects.Length > 0)
        {
            data.weather = WeatherType.Rain;
        }
        else if (fogObjects.Length > 0)
        {
            data.weather = WeatherType.Fog;
        }
        else
        {
            data.weather = WeatherType.Clear;
        }
    }
    
    /// <summary>
    /// 分析敌人
    /// </summary>
    private void AnalyzeEnemies()
    {
        // 查找敌人，使用安全的标签查找
        GameObject[] enemies = FindGameObjectsWithTagSafe("Enemy");
        GameObject[] bosses = FindGameObjectsWithTagSafe("Boss");
        
        currentSceneData.enemyCount = enemies.Length + bosses.Length;
        
        if (bosses.Length > 0)
        {
            currentSceneData.enemyPresence = EnemyPresence.Boss;
            currentSceneData.isBossFight = true;
            currentSceneData.threatLevel = 1f;
        }
        else if (enemies.Length > 5)
        {
            currentSceneData.enemyPresence = EnemyPresence.Many;
            currentSceneData.threatLevel = 0.8f;
        }
        else if (enemies.Length > 0)
        {
            currentSceneData.enemyPresence = EnemyPresence.Few;
            currentSceneData.threatLevel = 0.4f;
        }
        else
        {
            currentSceneData.enemyPresence = EnemyPresence.None;
            currentSceneData.threatLevel = 0f;
        }
        
        // 分析敌人距离（简单实现）
        if (enemies.Length > 0)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float closestDistance = float.MaxValue;
                foreach (GameObject enemy in enemies)
                {
                    float distance = Vector3.Distance(player.transform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                    }
                }
                
                // 根据距离调整威胁等级
                if (closestDistance < 10f)
                {
                    currentSceneData.threatLevel = Mathf.Min(1f, currentSceneData.threatLevel + 0.3f);
                }
            }
        }
    }
    
    /// <summary>
    /// 内部分析敌人方法
    /// </summary>
    private void AnalyzeEnemiesInternal(MusicSceneData data)
    {
        // 查找敌人，使用安全的标签查找
        GameObject[] enemies = FindGameObjectsWithTagSafe("Enemy");
        GameObject[] bosses = FindGameObjectsWithTagSafe("Boss");
        
        data.enemyCount = enemies.Length + bosses.Length;
        
        if (bosses.Length > 0)
        {
            data.enemyPresence = EnemyPresence.Boss;
            data.isBossFight = true;
            data.threatLevel = 1f;
        }
        else if (enemies.Length > 5)
        {
            data.enemyPresence = EnemyPresence.Many;
            data.threatLevel = 0.8f;
        }
        else if (enemies.Length > 0)
        {
            data.enemyPresence = EnemyPresence.Few;
            data.threatLevel = 0.4f;
        }
        else
        {
            data.enemyPresence = EnemyPresence.None;
            data.threatLevel = 0f;
        }
        
        // 分析敌人距离（简单实现）
        if (enemies.Length > 0)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float closestDistance = float.MaxValue;
                foreach (GameObject enemy in enemies)
                {
                    float distance = Vector3.Distance(player.transform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                    }
                }
                
                // 根据距离调整威胁等级
                if (closestDistance < 10f)
                {
                    data.threatLevel = Mathf.Min(1f, data.threatLevel + 0.3f);
                }
            }
        }
    }
    
    /// <summary>
    /// 分析玩家状态
    /// </summary>
    private void AnalyzePlayerState()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 分析玩家动作
            AnalyzePlayerAction(player);
            
            // 分析玩家血量（如果有Health组件）
            AnalyzePlayerHealth(player);
            
            // 分析玩家是否在潜行
            AnalyzePlayerStealth(player);
        }
    }
    
    /// <summary>
    /// 内部分析玩家状态方法
    /// </summary>
    private void AnalyzePlayerStateInternal(MusicSceneData data)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 分析玩家动作
            AnalyzePlayerActionInternal(player, data);
            
            // 分析玩家血量（如果有Health组件）
            AnalyzePlayerHealthInternal(player, data);
            
            // 分析玩家是否在潜行
            AnalyzePlayerStealthInternal(player, data);
        }
    }
    
    /// <summary>
    /// 分析玩家动作
    /// </summary>
    private void AnalyzePlayerAction(GameObject player)
    {
        // 检查是否有动画器
        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            // 这里可以添加更复杂的动画状态分析
            // 简单实现：通过速度判断
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                float speed = rb.velocity.magnitude;
                if (speed > 5f)
                {
                    currentSceneData.currentAction = ActionType.Running;
                    currentSceneData.actionIntensity = 0.8f;
                }
                else if (speed > 1f)
                {
                    currentSceneData.currentAction = ActionType.Walking;
                    currentSceneData.actionIntensity = 0.4f;
                }
                else
                {
                    currentSceneData.currentAction = ActionType.Walking;
                    currentSceneData.actionIntensity = 0.1f;
                }
            }
        }
        
        // 检查是否在战斗
        if (currentSceneData.threatLevel > 0.5f)
        {
            currentSceneData.currentAction = ActionType.Combat;
            currentSceneData.actionIntensity = Mathf.Max(currentSceneData.actionIntensity, 0.7f);
        }
    }
    
    /// <summary>
    /// 内部分析玩家动作方法
    /// </summary>
    private void AnalyzePlayerActionInternal(GameObject player, MusicSceneData data)
    {
        // 检查是否有动画器
        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            // 这里可以添加更复杂的动画状态分析
            // 简单实现：通过速度判断
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                float speed = rb.velocity.magnitude;
                if (speed > 5f)
                {
                    data.currentAction = ActionType.Running;
                    data.actionIntensity = 0.8f;
                }
                else if (speed > 1f)
                {
                    data.currentAction = ActionType.Walking;
                    data.actionIntensity = 0.4f;
                }
                else
                {
                    data.currentAction = ActionType.Walking;
                    data.actionIntensity = 0.1f;
                }
            }
        }
        
        // 检查是否在战斗
        if (data.threatLevel > 0.5f)
        {
            data.currentAction = ActionType.Combat;
            data.actionIntensity = Mathf.Max(data.actionIntensity, 0.7f);
        }
    }
    
    /// <summary>
    /// 分析玩家血量
    /// </summary>
    private void AnalyzePlayerHealth(GameObject player)
    {
        // 查找Health组件（假设有Health脚本）
        MonoBehaviour[] components = player.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component.GetType().Name.Contains("Health"))
            {
                // 通过反射获取血量信息
                var healthProperty = component.GetType().GetProperty("CurrentHealth");
                var maxHealthProperty = component.GetType().GetProperty("MaxHealth");
                
                if (healthProperty != null && maxHealthProperty != null)
                {
                    float currentHealth = (float)healthProperty.GetValue(component);
                    float maxHealth = (float)maxHealthProperty.GetValue(component);
                    
                    if (maxHealth > 0)
                    {
                        currentSceneData.playerHealth = currentHealth / maxHealth;
                    }
                }
                break;
            }
        }
    }
    
    /// <summary>
    /// 内部分析玩家血量方法
    /// </summary>
    private void AnalyzePlayerHealthInternal(GameObject player, MusicSceneData data)
    {
        // 查找Health组件（假设有Health脚本）
        MonoBehaviour[] components = player.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component.GetType().Name.Contains("Health"))
            {
                // 通过反射获取血量信息
                var healthProperty = component.GetType().GetProperty("CurrentHealth");
                var maxHealthProperty = component.GetType().GetProperty("MaxHealth");
                
                if (healthProperty != null && maxHealthProperty != null)
                {
                    float currentHealth = (float)healthProperty.GetValue(component);
                    float maxHealth = (float)maxHealthProperty.GetValue(component);
                    
                    if (maxHealth > 0)
                    {
                        data.playerHealth = currentHealth / maxHealth;
                    }
                }
                break;
            }
        }
    }
    
    /// <summary>
    /// 分析玩家潜行状态
    /// </summary>
    private void AnalyzePlayerStealth(GameObject player)
    {
        // 查找潜行相关的组件
        MonoBehaviour[] components = player.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component.GetType().Name.Contains("Stealth") || 
                component.GetType().Name.Contains("Sneak"))
            {
                // 检查是否在潜行状态
                var isStealthProperty = component.GetType().GetProperty("IsStealth");
                if (isStealthProperty != null)
                {
                    bool isStealth = (bool)isStealthProperty.GetValue(component);
                    currentSceneData.isStealth = isStealth;
                    if (isStealth)
                    {
                        currentSceneData.currentAction = ActionType.Stealth;
                    }
                }
                break;
            }
        }
    }
    
    /// <summary>
    /// 内部分析玩家潜行状态方法
    /// </summary>
    private void AnalyzePlayerStealthInternal(GameObject player, MusicSceneData data)
    {
        // 查找潜行相关的组件
        MonoBehaviour[] components = player.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component.GetType().Name.Contains("Stealth") || 
                component.GetType().Name.Contains("Sneak"))
            {
                // 检查是否在潜行状态
                var isStealthProperty = component.GetType().GetProperty("IsStealth");
                if (isStealthProperty != null)
                {
                    bool isStealth = (bool)isStealthProperty.GetValue(component);
                    data.isStealth = isStealth;
                    if (isStealth)
                    {
                        data.currentAction = ActionType.Stealth;
                    }
                }
                break;
            }
        }
    }
    
    /// <summary>
    /// 分析时间
    /// </summary>
    private void AnalyzeTimeOfDay()
    {
        // 查找光照系统
        Light[] lights = FindObjectsOfType<Light>();
        if (lights.Length > 0)
        {
            Light mainLight = null;
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    mainLight = light;
                    break;
                }
            }
            
            if (mainLight != null)
            {
                // 通过光照角度判断时间
                float angle = Vector3.Angle(mainLight.transform.forward, Vector3.down);
                
                if (angle < 30f)
                {
                    currentSceneData.timeOfDay = TimeOfDay.Day;
                }
                else if (angle < 60f)
                {
                    currentSceneData.timeOfDay = TimeOfDay.Dawn;
                }
                else if (angle < 120f)
                {
                    currentSceneData.timeOfDay = TimeOfDay.Dusk;
                }
                else
                {
                    currentSceneData.timeOfDay = TimeOfDay.Night;
                }
            }
        }
    }
    
    /// <summary>
    /// 内部分析时间方法
    /// </summary>
    private void AnalyzeTimeOfDayInternal(MusicSceneData data)
    {
        // 查找光照系统
        Light[] lights = FindObjectsOfType<Light>();
        if (lights.Length > 0)
        {
            Light mainLight = null;
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    mainLight = light;
                    break;
                }
            }
            
            if (mainLight != null)
            {
                // 通过光照角度判断时间
                float angle = Vector3.Angle(mainLight.transform.forward, Vector3.down);
                
                if (angle < 30f)
                {
                    data.timeOfDay = TimeOfDay.Day;
                }
                else if (angle < 60f)
                {
                    data.timeOfDay = TimeOfDay.Dawn;
                }
                else if (angle < 120f)
                {
                    data.timeOfDay = TimeOfDay.Dusk;
                }
                else
                {
                    data.timeOfDay = TimeOfDay.Night;
                }
            }
        }
    }
    
    /// <summary>
    /// 周期性提取
    /// </summary>
    private IEnumerator PeriodicExtraction()
    {
        while (true)
        {
            yield return new WaitForSeconds(extractionInterval);
            
            if (onlyExtractOnChange)
            {
                // 只在数据变化时提取
                var newData = ExtractSceneDataInternal();
                if (HasSceneDataChanged(newData))
                {
                    currentSceneData = newData;
                    if (showDebugInfo)
                    {
                        Debug.Log($"场景数据发生变化，更新: {currentSceneData.ToJson()}");
                    }
                    OnSceneDataUpdated?.Invoke(currentSceneData);
                }
            }
            else
            {
                ExtractSceneData();
            }
        }
    }
    
    /// <summary>
    /// 检查场景数据是否发生变化
    /// </summary>
    private bool HasSceneDataChanged(MusicSceneData newData)
    {
        if (currentSceneData == null) return true;
        
        return currentSceneData.environment != newData.environment ||
               currentSceneData.timeOfDay != newData.timeOfDay ||
               currentSceneData.weather != newData.weather ||
               currentSceneData.currentAction != newData.currentAction ||
               currentSceneData.enemyPresence != newData.enemyPresence ||
               currentSceneData.threatLevel != newData.threatLevel ||
               currentSceneData.gameLevel != newData.gameLevel ||
               Mathf.Abs(currentSceneData.actionIntensity - newData.actionIntensity) > 0.1f ||
               Mathf.Abs(currentSceneData.threatLevel - newData.threatLevel) > 0.1f;
    }
    
    /// <summary>
    /// 内部提取方法，不触发事件和日志
    /// </summary>
    private MusicSceneData ExtractSceneDataInternal()
    {
        var data = new MusicSceneData();
        
        // 基础信息
        data.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        data.sceneID = $"scene_{Time.time.GetHashCode()}";
        
        if (analyzeEnvironment)
        {
            AnalyzeEnvironmentInternal(data);
        }
        
        if (analyzeEnemies)
        {
            AnalyzeEnemiesInternal(data);
        }
        
        if (analyzePlayerState)
        {
            AnalyzePlayerStateInternal(data);
        }
        
        if (analyzeTimeOfDay)
        {
            AnalyzeTimeOfDayInternal(data);
        }
        
        return data;
    }
    
    /// <summary>
    /// 手动触发提取
    /// </summary>
    [ContextMenu("手动提取场景数据")]
    public void ManualExtract()
    {
        ExtractSceneData();
    }
    
    /// <summary>
    /// 启用周期性提取
    /// </summary>
    [ContextMenu("启用周期性提取")]
    public void EnablePeriodicExtraction()
    {
        enablePeriodicExtraction = true;
        if (extractionInterval > 0)
        {
            StartCoroutine(PeriodicExtraction());
        }
    }
    
    /// <summary>
    /// 禁用周期性提取
    /// </summary>
    [ContextMenu("禁用周期性提取")]
    public void DisablePeriodicExtraction()
    {
        enablePeriodicExtraction = false;
        StopAllCoroutines();
    }
    
    /// <summary>
    /// 设置提取间隔
    /// </summary>
    public void SetExtractionInterval(float interval)
    {
        extractionInterval = interval;
        if (enablePeriodicExtraction)
        {
            StopAllCoroutines();
            if (interval > 0)
            {
                StartCoroutine(PeriodicExtraction());
            }
        }
    }
    
    /// <summary>
    /// 手动刷新ZoneTrigger数据
    /// </summary>
    [ContextMenu("刷新ZoneTrigger数据")]
    public void RefreshZoneTriggerData()
    {
        if (useZoneTriggerData)
        {
            if (TryGetZoneTriggerData())
            {
                if (showDebugInfo)
                {
                    Debug.Log("ZoneTrigger数据刷新成功");
                }
                OnSceneDataUpdated?.Invoke(currentSceneData);
            }
            else
            {
                Debug.LogWarning("无法刷新ZoneTrigger数据");
            }
        }
        else
        {
            Debug.LogWarning("ZoneTrigger数据集成已禁用");
        }
    }
    
    /// <summary>
    /// 获取当前激活的ZoneTrigger信息
    /// </summary>
    public string GetActiveZoneInfo()
    {
        if (!useZoneTriggerData) return "ZoneTrigger集成已禁用";
        
        ZoneTrigger[] zoneTriggers = FindObjectsOfType<ZoneTrigger>();
        if (zoneTriggers.Length == 0) return "场景中没有ZoneTrigger";
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return "没有找到Player";
        
        ZoneTrigger closestZone = null;
        float closestDistance = float.MaxValue;
        
        foreach (var zone in zoneTriggers)
        {
            if (zone.sceneData != null && zone.useStructuredData)
            {
                float distance = Vector3.Distance(player.transform.position, zone.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestZone = zone;
                }
            }
        }
        
        if (closestZone != null)
        {
            return $"当前区域: {closestZone.zoneName} (距离: {closestDistance:F1})";
        }
        
        return "没有找到可用的ZoneTrigger数据";
    }
    
    /// <summary>
    /// 获取当前场景数据
    /// </summary>
    public MusicSceneData GetCurrentSceneData()
    {
        return currentSceneData;
    }
    
    /// <summary>
    /// 设置场景数据
    /// </summary>
    public void SetSceneData(MusicSceneData data)
    {
        currentSceneData = data;
        OnSceneDataUpdated?.Invoke(currentSceneData);
    }
} 