using System;
using UnityEngine;
using UnscriptedEngine;

public class AudioHandler : MonoBehaviour
{
    public enum AudioType
    {
        Abilities,
        Environment,
        UI,
        None
    }

    [Serializable]
    public class Settings
    {
        public float volume = 0.5f;
        public float pitch = 1;
        public float range = 20;
        public Transform parent;
        public AudioType type;
    }

    [Serializable]
    public class VolumeSettings
    {
        public AudioType type = AudioType.None;
        public float volume = 1f;
    }

    [SerializeField] private AudioSource prefab;
    [SerializeField] private float masterVolume;
    [SerializeField] private VolumeSettings[] volumeSettings;

    public static AudioHandler instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        CustomGameInstance gameInstance = UGameModeBase.instance.GetGameInstance<CustomGameInstance>();
        masterVolume = gameInstance.settings.volume.Value;
        gameInstance.settings.volume.OnValueChanged += OnVolumeChanged;
    }

    private void OnVolumeChanged(float obj)
    {
        masterVolume = obj;
    }

    private float GetVolume(AudioType type)
    {
        foreach (VolumeSettings volumeSetting in volumeSettings)
        {
            if (volumeSetting.type == type)
                return volumeSetting.volume;
        }
     
        return 1f;
    }

    public void PlayAudio(AudioClip clip, Vector3 position, Settings settings)
    {
        AudioSource audioSource = ObjectPooler.SpawnObject(prefab, position, Quaternion.identity);
        audioSource.clip = clip;
        audioSource.volume = settings.volume * GetVolume(settings.type) * masterVolume;
        audioSource.pitch = settings.pitch;
        audioSource.maxDistance = settings.range;

        if (settings.parent != null)
            audioSource.transform.SetParent(settings.parent);

        audioSource.Play();
        ObjectPooler.DespawnObject(audioSource.gameObject, clip.length, this);
    }
}
