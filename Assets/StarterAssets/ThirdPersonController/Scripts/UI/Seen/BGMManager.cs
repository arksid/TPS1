using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("기본 설정")]
    public AudioSource source;          // 비워두면 자동으로 추가
    public AudioClip defaultClip;       // 시작하자마자 재생할 음악
    [Range(0f, 1f)] public float defaultVolume = 0.6f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!source) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.volume = defaultVolume;

        if (defaultClip) Play(defaultClip, 0.4f);
    }

    public void Play(AudioClip clip, float fade = 0.5f, bool loop = true)
    {
        StartCoroutine(Co_Play(clip, fade, loop));
    }

    System.Collections.IEnumerator Co_Play(AudioClip clip, float fade, bool loop)
    {
        // 페이드 아웃
        if (source.isPlaying && fade > 0f)
        {
            float v0 = source.volume;
            float t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(v0, 0f, t / fade);
                yield return null;
            }
        }
        source.Stop();

        // 새 클립 재생 + 페이드 인
        source.clip = clip;
        source.loop = loop;
        source.Play();

        if (fade > 0f)
        {
            float t = 0f;
            source.volume = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(0f, defaultVolume, t / fade);
                yield return null;
            }
        }
        else source.volume = defaultVolume;
    }

    public void FadeOut(float fade = 0.5f)
    {
        StartCoroutine(Co_FadeOut(fade));
    }

    System.Collections.IEnumerator Co_FadeOut(float fade)
    {
        float v0 = source.volume;
        float t = 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(v0, 0f, t / fade);
            yield return null;
        }
        source.Stop();
        source.clip = null;
    }
}
