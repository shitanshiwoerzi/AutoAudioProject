using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("BGM")]
    public AudioClip zone1Music;
    public AudioClip zone2Music;
    public AudioClip zone3Music;
    public AudioClip zone4Music;

    [Header("音频设置")]
    public AudioSource musicSource;
    public float defaultVolume = 0.5f;
    public bool loopMusic = true;

    [Header("调试")]
    public bool showDebugInfo = true;

    // 当前播放的音乐信息
    private AudioClip currentMusic;
    private string currentZoneName = "无";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景不销毁
            
            // 初始化音频源
            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
                if (musicSource == null)
                {
                    musicSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            musicSource.volume = defaultVolume;
            musicSource.loop = loopMusic;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMusic(AudioClip clip, float fadeTime = 1.0f, string zoneName = "")
    {
        if (clip == null)
        {
            Debug.LogWarning("try to play empty music！");
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying) 
        {
            if (showDebugInfo)
            {
                Debug.Log($"Music {clip.name} is playing now，skip duplicated play");
            }
            return; // 避免重复播放
        }

        currentMusic = clip;
        currentZoneName = zoneName;
        
        if (showDebugInfo)
        {
            Debug.Log($"Begin playing music: {clip.name} (区域: {zoneName})");
        }

        StartCoroutine(CrossFadeMusic(clip, fadeTime));
    }

    // fade in & fade out
    private IEnumerator CrossFadeMusic(AudioClip newClip, float fadeTime)
    {
        if (musicSource.isPlaying)
        {
            // 淡出当前音乐
            float startVolume = musicSource.volume;
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
                yield return null;
            }
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();

        // 淡入新音乐
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0, defaultVolume, t / fadeTime);
            yield return null;
        }
        
        musicSource.volume = defaultVolume;
    }

    // 获取当前播放的音乐信息
    public AudioClip GetCurrentMusic()
    {
        return currentMusic;
    }

    public string GetCurrentZoneName()
    {
        return currentZoneName;
    }

    // 停止音乐
    public void StopMusic(float fadeTime = 1.0f)
    {
        StartCoroutine(FadeOutMusic(fadeTime));
    }

    private IEnumerator FadeOutMusic(float fadeTime)
    {
        float startVolume = musicSource.volume;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
            yield return null;
        }
        
        musicSource.Stop();
        currentMusic = null;
        currentZoneName = "无";
    }

    // 设置音量
    public void SetVolume(float volume)
    {
        defaultVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = defaultVolume;
        }
    }
}
