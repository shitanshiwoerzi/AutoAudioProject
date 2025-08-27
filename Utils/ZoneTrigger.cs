using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    [Header("区域设置")]
    public int zoneID = 1; // 区域ID (1-4)
    public string zoneName = "区域1"; // 区域名称

    [Header("音频设置")]
    public AudioClip zoneMusic; // 该区域的背景音乐（传统方式）
    public float fadeTime = 1.0f; // 淡入淡出时间
    
    [Header("结构化场景数据")]
    public bool useStructuredData = true; // 是否使用结构化数据
    public MusicSceneData sceneData; // 场景数据

    [Header("调试")]
    public bool showDebugInfo = true; // 是否显示调试信息

    private bool playerInZone = false; // 玩家是否在区域内

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !playerInZone)
        {
            playerInZone = true;

            if (useStructuredData && sceneData != null)
            {
                // 使用结构化数据生成音乐
                if (AIGeneratedMusic.Instance != null)
                {
                    AIGeneratedMusic.Instance.GenerateMusicForScene(sceneData);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"玩家进入 {zoneName} (ID: {zoneID})，使用结构化数据生成音乐");
                        Debug.Log($"场景数据: {sceneData.ToJson()}");
                    }
                }
                else
                {
                    Debug.LogWarning("AI音乐生成器未找到！");
                }
            }
            else if (AudioManager.Instance != null && zoneMusic != null)
            {
                // 使用传统方式播放音乐
                AudioManager.Instance.PlayMusic(zoneMusic, fadeTime);

                if (showDebugInfo)
                {
                    Debug.Log($"玩家进入 {zoneName} (ID: {zoneID})，播放音乐: {zoneMusic.name}");
                }
            }
            else
            {
                Debug.LogWarning($"音频管理器或区域音乐未设置！区域: {zoneName}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;

            if (showDebugInfo)
            {
                Debug.Log($"玩家离开 {zoneName} (ID: {zoneID})");
            }
        }
    }

    // 在Scene视图中显示触发器范围
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = GetZoneColor(zoneID);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider boxCol)
            {
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            }
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawWireSphere(sphereCol.center, sphereCol.radius);
            }
        }
    }

    // 根据区域ID返回不同的颜色
    private Color GetZoneColor(int id)
    {
        switch (id)
        {
            case 1: return Color.red;
            case 2: return Color.green;
            case 3: return Color.blue;
            case 4: return Color.yellow;
            default: return Color.white;
        }
    }
}