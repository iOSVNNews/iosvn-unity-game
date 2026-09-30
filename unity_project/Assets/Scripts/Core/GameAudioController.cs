using System.Collections;
using UnityEngine;

namespace IOSVN.TuTien.Core
{
    public sealed class GameAudioController : MonoBehaviour
    {
        public static GameAudioController Instance { get; private set; }
        private const float MusicVolume = 0.34f;
        private const float CrossFadeSeconds = 2.5f;
        private AudioSource musicA;
        private AudioSource musicB;
        private AudioSource effects;
        private AudioSource activeMusic;
        private AudioClip mortalTheme;
        private AudioClip immortalTheme;
        private AudioClip skillEffect;
        private Coroutine transition;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            musicA = gameObject.AddComponent<AudioSource>();
            musicB = gameObject.AddComponent<AudioSource>();
            effects = gameObject.AddComponent<AudioSource>();
            musicA.loop = musicB.loop = true;
            musicA.playOnAwake = musicB.playOnAwake = effects.playOnAwake = false;
            musicA.volume = musicB.volume = 0;
            effects.volume = 0.8f;
            mortalTheme = Resources.Load<AudioClip>("Audio/Music/PhamGioiTheme");
            immortalTheme = Resources.Load<AudioClip>("Audio/Music/TienGioiTheme");
            skillEffect = Resources.Load<AudioClip>("Audio/Effects/SkillEffect");
        }

        public void SetRealm(bool isImmortal)
        {
            var requested = isImmortal ? immortalTheme : mortalTheme;
            if (requested == null || (activeMusic != null && activeMusic.clip == requested && activeMusic.isPlaying)) return;
            if (transition != null) StopCoroutine(transition);
            var incoming = activeMusic == musicA ? musicB : musicA;
            incoming.clip = requested;
            incoming.time = 0;
            incoming.volume = 0;
            incoming.Play();
            transition = StartCoroutine(CrossFade(activeMusic, incoming));
        }

        public void PlaySkillEffect()
        {
            if (skillEffect != null) effects.PlayOneShot(skillEffect);
        }

        private IEnumerator CrossFade(AudioSource outgoing, AudioSource incoming)
        {
            if (outgoing == null)
            {
                incoming.volume = MusicVolume;
                activeMusic = incoming;
                transition = null;
                yield break;
            }
            var elapsed = 0f;
            var startVolume = outgoing.volume;
            while (elapsed < CrossFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / CrossFadeSeconds);
                outgoing.volume = Mathf.Lerp(startVolume, 0, t);
                incoming.volume = Mathf.Lerp(0, MusicVolume, t);
                yield return null;
            }
            outgoing.Stop();
            outgoing.clip = null;
            outgoing.volume = 0;
            incoming.volume = MusicVolume;
            activeMusic = incoming;
            transition = null;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
