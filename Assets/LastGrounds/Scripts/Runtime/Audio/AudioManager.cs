using UnityEngine;

namespace LastGrounds
{
    /// <summary>
    /// Persistent music + sound effects player. Created on demand, survives scene loads,
    /// and only restarts the music when the requested track actually changes
    /// (the original kept the menu song playing across menu rooms the same way).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int SfxVoices = 8;

        static AudioManager instance;
        static bool quitting;

        AudioSource music;
        AudioSource[] voices;
        int nextVoice;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            quitting = false;
        }

        static AudioManager Instance
        {
            get
            {
                if (instance == null && !quitting && Application.isPlaying)
                {
                    GameObject go = new GameObject("[AudioManager]");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<AudioManager>();
                }
                return instance;
            }
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;

            voices = new AudioSource[SfxVoices];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }

            GameSettings.Changed += ApplyVolumes;
            Application.quitting += OnQuitting;
            ApplyVolumes();
        }

        void OnDestroy()
        {
            GameSettings.Changed -= ApplyVolumes;
            Application.quitting -= OnQuitting;
            if (instance == this) instance = null;
        }

        static void OnQuitting()
        {
            quitting = true;
        }

        void ApplyVolumes()
        {
            if (music != null) music.volume = GameSettings.MusicVolume;
        }

        public static void PlayMusic(AudioClip clip)
        {
            AudioManager am = Instance;
            if (am == null || clip == null) return;
            if (am.music.clip == clip && am.music.isPlaying) return;
            am.music.clip = clip;
            am.music.Play();
        }

        public static void StopMusic()
        {
            AudioManager am = Instance;
            if (am != null) am.music.Stop();
        }

        public static void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            AudioManager am = Instance;
            if (am == null || clip == null) return;
            AudioSource voice = am.voices[am.nextVoice];
            am.nextVoice = (am.nextVoice + 1) % am.voices.Length;
            voice.pitch = pitch;
            voice.PlayOneShot(clip, volume * GameSettings.SfxVolume);
        }
    }
}
