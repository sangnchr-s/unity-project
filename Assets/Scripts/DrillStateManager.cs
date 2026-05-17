using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// это синглтон должен быть
// вообще надо переделать, наверное так чтобы руда выпадала только когда мы в определённом состоянии,
// в которое переходим например по кнопке
// потому что сейчас оно падает всегда когда нужно бурить, даже если нет процесса бурения
public class DrillStateManager : MonoBehaviour
{
    const string DefaultMovementClipPath = "Assets/sound/15780_1460487610.mp3";
    const string DefaultDrillingClipPath = "Assets/sound/burilnaya-ustanovka--glubokiy-gul.mp3";
    private bool _canDrill = false;
    private bool _isDrilling = false;
    public bool canDrill
    {
        private set
        {
            _canDrill = value;
        }

        get
        {

            return drillHeadTurn.turned && movement.moving;

        }
    }

    public bool isDrilling
    {
        private set
        {
            _isDrilling = value;
        }

        get
        {

            return drillHeadTurn.turned && movement.moving;

        }
    }
    public Movement movement;
    public DrillHeadTurn drillHeadTurn;
    public VoxelTerrain vt;
    public static DrillStateManager instance;

    [Header("Sound")]
    [Tooltip("Звук движения бура (едет).")]
    [SerializeField] private AudioClip movementClip;
    [Tooltip("Звук бурения (когда бурит).")]
    [SerializeField] private AudioClip drillingClip;
    [SerializeField] private float movementVolume = 0.75f;
    [SerializeField] private float drillingVolume = 0.9f;
    [SerializeField] private bool spatial3D = true;

    AudioSource _movementSource;
    AudioSource _drillingSource;


    private void Awake()
    {
#if UNITY_EDITOR
        AutoAssignDefaultClipsIfMissing();
#endif
        vt = FindAnyObjectByType<VoxelTerrain>();
        movement = FindAnyObjectByType<Movement>();
        drillHeadTurn = FindAnyObjectByType<DrillHeadTurn>();
        instance = this;
        EnsureAudioSources();
    }

    private void Update()
    {
        UpdateAudioState();
    }

    void EnsureAudioSources()
    {
        _movementSource = CreateLoopSource("DrillMovement_Audio", movementClip, movementVolume);
        _drillingSource = CreateLoopSource("DrillDrilling_Audio", drillingClip, drillingVolume);
    }

    AudioSource CreateLoopSource(string childName, AudioClip clip, float volume)
    {
        var child = transform.Find(childName);
        if (child == null)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        var src = child.GetComponent<AudioSource>();
        if (src == null)
            src = child.gameObject.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.loop = true;
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume);
        src.spatialBlend = spatial3D ? 1f : 0f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.maxDistance = 45f;
        return src;
    }

    void UpdateAudioState()
    {
        if (_movementSource == null || _drillingSource == null)
            EnsureAudioSources();

        bool isMoving = movement != null && movement.moving;
        bool isDrillingNow = drillHeadTurn != null && drillHeadTurn.turned && isMoving;

        SyncLoop(_movementSource, movementClip, movementVolume, isMoving);
        SyncLoop(_drillingSource, drillingClip, drillingVolume, isDrillingNow);
    }

    static void SyncLoop(AudioSource src, AudioClip clip, float volume, bool shouldPlay)
    {
        if (src == null)
            return;
        if (src.clip != clip)
            src.clip = clip;
        src.volume = Mathf.Clamp01(volume);

        if (!shouldPlay || clip == null)
        {
            if (src.isPlaying)
                src.Stop();
            return;
        }

        if (!src.isPlaying)
            src.Play();
    }

    private void OnDisable()
    {
        if (_movementSource != null && _movementSource.isPlaying)
            _movementSource.Stop();
        if (_drillingSource != null && _drillingSource.isPlaying)
            _drillingSource.Stop();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignDefaultClipsIfMissing();
    }

    void AutoAssignDefaultClipsIfMissing()
    {
        if (movementClip == null)
            movementClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultMovementClipPath);
        if (drillingClip == null)
            drillingClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultDrillingClipPath);
    }
#endif
}
