using System;
using System.Collections.Generic;
using PrisonLife.Core;
using UnityEngine;

namespace PrisonLife.Configs
{
    /// <summary>
    /// SoundType → AudioClip + 볼륨 매핑. ScriptableObject 에셋 1개로 관리.
    /// SoundManager 가 이 registry 를 참조해 type 으로 clip 을 조회.
    /// 신규 entry 의 볼륨 기본값은 0 (struct 직렬화 한계) — 인스펙터에서 1 등으로 직접 세팅 필요.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundClipRegistry", menuName = "PrisonLife/SoundClipRegistry")]
    public class SoundClipRegistry : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public SoundType type;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] private Entry[] entries;

        private Dictionary<SoundType, Entry> entriesByType;

        private void EnsureLookup()
        {
            if (entriesByType != null) return;
            entriesByType = new Dictionary<SoundType, Entry>();
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                entriesByType[entries[i].type] = entries[i];
            }
        }

        public bool TryGet(SoundType _type, out AudioClip _clip, out float _volume)
        {
            EnsureLookup();
            if (!entriesByType.TryGetValue(_type, out Entry entry) || entry.clip == null)
            {
                _clip = null;
                _volume = 0f;
                return false;
            }
            _clip = entry.clip;
            _volume = entry.volume > 0f ? entry.volume : 1f;
            return true;
        }
    }
}
