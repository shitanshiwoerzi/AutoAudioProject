using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneSetupHelper : MonoBehaviour
{
    [Header("区域设置")]
    public GameObject zone1Trigger;
    public GameObject zone2Trigger;
    public GameObject zone3Trigger;
    public GameObject zone4Trigger;
    
    [Header("音频文件")]
    public AudioClip zone1Audio;
    public AudioClip zone2Audio;
    public AudioClip zone3Audio;
    public AudioClip zone4Audio;
    
    [Header("区域名称")]
    public string zone1Name = "森林区域";
    public string zone2Name = "城市区域";
    public string zone3Name = "洞穴区域";
    public string zone4Name = "海滩区域";
    
    [Header("触发器设置")]
    public Vector3 zone1Size = new Vector3(10, 5, 10);
    public Vector3 zone2Size = new Vector3(10, 5, 10);
    public Vector3 zone3Size = new Vector3(10, 5, 10);
    public Vector3 zone4Size = new Vector3(10, 5, 10);
    
    [ContextMenu("自动设置所有区域")]
    public void SetupAllZones()
    {
        SetupZone(zone1Trigger, 1, zone1Name, zone1Audio, zone1Size);
        SetupZone(zone2Trigger, 2, zone2Name, zone2Audio, zone2Size);
        SetupZone(zone3Trigger, 3, zone3Name, zone3Audio, zone3Size);
        SetupZone(zone4Trigger, 4, zone4Name, zone4Audio, zone4Size);
        
        Debug.Log("所有区域设置完成！");
    }
    
    private void SetupZone(GameObject zoneObject, int zoneID, string zoneName, AudioClip audioClip, Vector3 size)
    {
        if (zoneObject == null)
        {
            Debug.LogWarning($"区域 {zoneID} 的GameObject未设置！");
            return;
        }
        
        // 添加或获取ZoneTrigger组件
        ZoneTrigger zoneTrigger = zoneObject.GetComponent<ZoneTrigger>();
        if (zoneTrigger == null)
        {
            zoneTrigger = zoneObject.AddComponent<ZoneTrigger>();
        }
        
        // 设置区域属性
        zoneTrigger.zoneID = zoneID;
        zoneTrigger.zoneName = zoneName;
        zoneTrigger.zoneMusic = audioClip;
        
        // 添加或配置触发器
        BoxCollider trigger = zoneObject.GetComponent<BoxCollider>();
        if (trigger == null)
        {
            trigger = zoneObject.AddComponent<BoxCollider>();
        }
        
        trigger.isTrigger = true;
        trigger.size = size;
        trigger.center = Vector3.zero;
        
        // 设置GameObject名称
        zoneObject.name = $"Zone_{zoneID}_{zoneName}";
        
        Debug.Log($"区域 {zoneID} ({zoneName}) 设置完成");
    }
    
    [ContextMenu("创建默认区域GameObject")]
    public void CreateDefaultZoneObjects()
    {
        if (zone1Trigger == null) zone1Trigger = CreateZoneObject("Zone_1", new Vector3(0, 0, 0));
        if (zone2Trigger == null) zone2Trigger = CreateZoneObject("Zone_2", new Vector3(20, 0, 0));
        if (zone3Trigger == null) zone3Trigger = CreateZoneObject("Zone_3", new Vector3(0, 0, 20));
        if (zone4Trigger == null) zone4Trigger = CreateZoneObject("Zone_4", new Vector3(20, 0, 20));
        
        Debug.Log("默认区域GameObject创建完成！");
    }
    
    private GameObject CreateZoneObject(string name, Vector3 position)
    {
        GameObject zoneObject = new GameObject(name);
        zoneObject.transform.position = position;
        zoneObject.transform.SetParent(transform);
        return zoneObject;
    }
    
    [ContextMenu("检查设置")]
    public void CheckSetup()
    {
        Debug.Log("=== 区域设置检查 ===");
        
        CheckZone(zone1Trigger, 1, zone1Name, zone1Audio);
        CheckZone(zone2Trigger, 2, zone2Name, zone2Audio);
        CheckZone(zone3Trigger, 3, zone3Name, zone3Audio);
        CheckZone(zone4Trigger, 4, zone4Name, zone4Audio);
        
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AudioManager实例未找到！请确保场景中有AudioManager对象。");
        }
        else
        {
            Debug.Log("AudioManager实例已找到。");
        }
    }
    
    private void CheckZone(GameObject zoneObject, int zoneID, string zoneName, AudioClip audioClip)
    {
        Debug.Log($"区域 {zoneID}:");
        
        if (zoneObject == null)
        {
            Debug.LogWarning($"  - GameObject: 未设置");
            return;
        }
        
        Debug.Log($"  - GameObject: {zoneObject.name}");
        
        ZoneTrigger trigger = zoneObject.GetComponent<ZoneTrigger>();
        if (trigger == null)
        {
            Debug.LogWarning($"  - ZoneTrigger组件: 缺失");
        }
        else
        {
            Debug.Log($"  - ZoneTrigger组件: 已添加");
            Debug.Log($"  - 区域名称: {trigger.zoneName}");
            Debug.Log($"  - 区域ID: {trigger.zoneID}");
        }
        
        Collider collider = zoneObject.GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogWarning($"  - Collider组件: 缺失");
        }
        else
        {
            Debug.Log($"  - Collider组件: {collider.GetType().Name}");
            Debug.Log($"  - IsTrigger: {collider.isTrigger}");
        }
        
        if (audioClip == null)
        {
            Debug.LogWarning($"  - 音频文件: 未设置");
        }
        else
        {
            Debug.Log($"  - 音频文件: {audioClip.name}");
        }
        
        Debug.Log("  ---");
    }
} 