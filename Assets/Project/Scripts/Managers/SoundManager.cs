using PrisonLife.Configs;
using PrisonLife.Core;
using UnityEngine;

namespace PrisonLife.Managers
{
    /// <summary>
    /// 사운드 단일 진입점. SoundClipRegistry 로 SoundType → AudioClip 해석.
    /// SFX 는 sfxSource 의 PlayOneShot (2D), BGM 은 bgmSource 의 loop. 시네마 timeScale=0 영향 X
    /// (AudioSource 는 Time.timeScale 와 무관하게 기본 동작).
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        [Header("Registry")]
        [SerializeField] private SoundClipRegistry registry;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;

        public void PlayOneShot(SoundType _type)
        {
            if (registry == null || sfxSource == null) return;
            if (!registry.TryGet(_type, out AudioClip clip, out float volume)) return;
            sfxSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// 3D positional one-shot. PlayClipAtPoint 로 임시 AudioSource 생성 (자동 정리).
        /// </summary>
        public void PlayOneShotAt(SoundType _type, Vector3 _worldPosition)
        {
            if (registry == null) return;
            if (!registry.TryGet(_type, out AudioClip clip, out float volume)) return;
            AudioSource.PlayClipAtPoint(clip, _worldPosition, volume);
        }

        public void PlayBgm(SoundType _type)
        {
            if (registry == null || bgmSource == null) return;
            if (!registry.TryGet(_type, out AudioClip clip, out float volume)) return;

            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            bgmSource.clip = clip;
            bgmSource.volume = volume;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        public void StopBgm()
        {
            if (bgmSource != null) bgmSource.Stop();
        }
    }
}
