using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioUI : MonoBehaviour
{
    [Header("UI组件")]
    public Text currentMusicText;
    public Text currentZoneText;
    public Slider volumeSlider;
    public Button muteButton;
    public Text muteButtonText;
    
    [Header("设置")]
    public bool showUI = true;
    public float updateInterval = 0.5f; // UI更新间隔
    
    private bool isMuted = false;
    private float lastVolume = 0.5f;
    
    void Start()
    {
        // 初始化音量滑块
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioManager.Instance != null ? AudioManager.Instance.defaultVolume : 0.5f;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        
        // 初始化静音按钮
        if (muteButton != null)
        {
            muteButton.onClick.AddListener(ToggleMute);
            UpdateMuteButtonText();
        }
        
        // 开始UI更新协程
        if (showUI)
        {
            StartCoroutine(UpdateUI());
        }
    }
    
    private IEnumerator UpdateUI()
    {
        while (showUI)
        {
            if (AudioManager.Instance != null)
            {
                // 更新当前音乐信息
                if (currentMusicText != null)
                {
                    AudioClip currentMusic = AudioManager.Instance.GetCurrentMusic();
                    string musicName = currentMusic != null ? currentMusic.name : "无音乐播放";
                    currentMusicText.text = $"当前音乐: {musicName}";
                }
                
                // 更新当前区域信息
                if (currentZoneText != null)
                {
                    string zoneName = AudioManager.Instance.GetCurrentZoneName();
                    currentZoneText.text = $"当前区域: {zoneName}";
                }
            }
            
            yield return new WaitForSeconds(updateInterval);
        }
    }
    
    private void OnVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(value);
        }
    }
    
    private void ToggleMute()
    {
        if (AudioManager.Instance != null)
        {
            if (isMuted)
            {
                // 取消静音
                AudioManager.Instance.SetVolume(lastVolume);
                volumeSlider.value = lastVolume;
                isMuted = false;
            }
            else
            {
                // 静音
                lastVolume = volumeSlider.value;
                AudioManager.Instance.SetVolume(0);
                volumeSlider.value = 0;
                isMuted = true;
            }
            
            UpdateMuteButtonText();
        }
    }
    
    private void UpdateMuteButtonText()
    {
        if (muteButtonText != null)
        {
            muteButtonText.text = isMuted ? "取消静音" : "静音";
        }
    }
    
    // 公共方法：显示/隐藏UI
    public void SetUIVisible(bool visible)
    {
        showUI = visible;
        if (visible)
        {
            StartCoroutine(UpdateUI());
        }
    }
} 