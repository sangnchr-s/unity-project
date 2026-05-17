using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Сыпь «отходов» сзади машины во время бурения. Визуально лучше с префабом на Rigidbody:
/// меш заменяется на угловатый кусок, масштаб неравномерный, импульс — против хода (как выгрузка породы).
/// </summary>
public class OreSpawner : MonoBehaviour
{

    [SerializeField] private GameObject orePrefab;
    
    [Header("Пул объектов")]
    [SerializeField] private int prewarmCount = 56;
    [SerializeField] private int prewarmPerFrame = 4;
    [SerializeField] private int maxPoolSize = 420;
    [SerializeField] private float oreLifetime = 5.5f;
    [Tooltip("Сколько лежит на земле после приземления, прежде чем вернуться в пул.")]
    [SerializeField] private float groundedLifetime = 15f;
    [SerializeField] private int maxActiveOres = 280;

    [Header("Модель руды")]
    [Tooltip("Если пусто — будет попытка взять меш с объекта в сцене по имени ниже (например, tas).")]
    [SerializeField] private Mesh oreMeshOverride;
    [SerializeField] private bool autoFindSceneOreMesh = false;
    [SerializeField] private string sceneOreModelName = "tas";
    [SerializeField] private bool forceMaterialOnSpawn = true;
    [SerializeField] private Material oreMaterialOverride;

    [Header("Тайминг")]
    [SerializeField] private float delay = 0.08f;
    [Tooltip("Случайная добавка к задержке, сек (0 = без разброса).")]
    [SerializeField] private float delayJitter = 0.02f;
    
    [Header("Поток руды")]
    [Tooltip("Сколько кусков руды спавнить за один тик.")]
    [SerializeField] private int burstMin = 4;
    [SerializeField] private int burstMax = 7;
    
    [Header("Условия спавна")]
    [Tooltip("Если включено — руда спавнится только в состоянии бурения (E + движение вперёд).")]
    [SerializeField] private bool spawnOnlyWhenDrilling = false;

    [Header("Пыль при бурении")]
    [Tooltip("Система частиц пыли/крошки породы у буровой головы. Не создаётся кодом — назначьте в инспекторе или оставьте пустым.")]
    [SerializeField] private ParticleSystem drillingDustParticles;
    [Tooltip("Если включено — при остановке бурения пыль очищается полностью.")]
    [SerializeField] private bool clearDustOnStop = false;
    [Tooltip("Материал с шейдером Built-in (например Particles/Standard Unlit). Подставляется вместо URP/HDRP-материалов, иначе они будут фиолетовыми в этом проекте.")]
    [SerializeField] private Material drillingDustBuiltInMaterialOverride;

    [Header("Точка выхода (локально спавнера; повесь спавнер у «жёлоба» сзади)")]
    [SerializeField] private bool spawnAtOreSpawnerPoint = false;
    [Tooltip("Явная точка спавна. Если не задана, можно автоподхватить объект по имени (например, Cube).")]
    [SerializeField] private Transform spawnPointOverride;
    [SerializeField] private bool autoFindSpawnPointByName = true;
    [SerializeField] private string autoSpawnPointName = "Cube";
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.12f, 0f);
    [SerializeField] private float spawnHeightBoost = 0.04f;
    [SerializeField] private Vector3 randomExtents = new Vector3(0.28f, 0.2f, 0.18f);
    [SerializeField] private Vector3 randomEuler = new Vector3(40f, 180f, 40f);

    [Header("Вид куска (вместо сферы)")]
    [SerializeField] private bool replaceWithRockChipMesh = true;
    [Tooltip("Длина/ширина/толщина осколка в случайных пределах (мир после масштаба префаба).")]
    [SerializeField] private Vector3 chipScaleMin = new Vector3(0.42f, 0.3f, 0.22f);
    [SerializeField] private Vector3 chipScaleMax = new Vector3(0.95f, 0.7f, 0.48f);
    [Header("Размер руды")]
    [SerializeField] private bool useFixedOreScale = false;
    [SerializeField] private Vector3 fixedOreScale = new Vector3(0.35f, 0.35f, 0.35f);
    [Tooltip("Использовать fixedOreScale как целевой РАЗМЕР меша в мире (метры), а не как локальный scale.")]
    [SerializeField] private bool fixedScaleIsMeshWorldSize = true;

    [Header("Физика (Rigidbody на префабе)")]
    [SerializeField] private bool applyKick = true;
    [Tooltip("Отключить столкновения между кусками руды, чтобы они не разлетались при спавне в одной точке.")]
    [SerializeField] private bool ignoreOreToOreCollisions = true;
    [Tooltip("Отключить столкновения руды с коллайдерами буровой/носителя спавнера.")]
    [SerializeField] private bool ignoreOreToOwnerCollisions = true;
    [Tooltip("Если пусто, берётся корневой объект спавнера.")]
    [SerializeField] private Transform collisionOwnerRoot;
    [SerializeField] private float rbDrag = 0.35f;
    [SerializeField] private float rbAngularDrag = 0.35f;
    [Tooltip("Нефизический режим потока: имитирует сход руды по конвейеру и осыпание вниз.")]
    [SerializeField] private bool useSimpleDownwardMotion = true;
    [SerializeField] private float simpleDownwardSpeed = 1.5f;
    [SerializeField] private bool settleOnGroundInSimpleMode = false;
    [Tooltip("Минимальное время полёта перед попыткой 'посадки' на землю.")]
    [SerializeField] private float minAirborneTimeBeforeSettle = 0.22f;
    [SerializeField] private float groundProbeDistance = 0.35f;
    [SerializeField] private float groundOffset = 0.01f;
    [SerializeField] private float conveyorSpeedJitter = 0.5f;
    [SerializeField] private Vector3 conveyorLocalDirection = new Vector3(0f, -0.85f, -1.15f);
    [SerializeField] private float conveyorGravity = 7.2f;
    [SerializeField] private float conveyorAirDrag = 0.55f;
    [SerializeField] private float conveyorSideSpread = 0.2f;
    [SerializeField] private float conveyorUpSpread = 0.1f;
    [SerializeField] private float conveyorSpinMin = 90f;
    [SerializeField] private float conveyorSpinMax = 260f;
    [Tooltip("Ограничивает полёт руды вверх при конфликте коллайдеров/спавне в пересечении.")]
    [SerializeField] private bool stabilizeVerticalMotion = true;
    [SerializeField] private float maxUpwardSpeed = 0.05f;
    [SerializeField] private float downwardAssist = 0.25f;
    [Tooltip("Импульс против направления езды (F), как будто порода выбрасывается сзади.")]
    [SerializeField] private bool kickOppositeToMovement = true;
    [SerializeField] private float rearKickStrength = 1.8f;
    [SerializeField] private float sideKickSpread = 0.45f;
    [SerializeField] private float upKick = 0.35f;
    [Tooltip("Если включено — руда сыпется вниз (как поток), а не выстреливает назад.")]
    [SerializeField] private bool pourDownward = true;
    [SerializeField] private float downKickStrength = 2.2f;
    [SerializeField] private float downKickJitter = 0.4f;
    [SerializeField] private float rearKickWhilePour = 0.45f;
    [SerializeField] private float sideKickWhilePour = 0.18f;
    [Tooltip("Если выключено «против хода» — импульс в локальных осях спавнера.")]
    [SerializeField] private Vector3 localImpulseFallback = new Vector3(0f, -0.15f, -1.4f);

    private Transform spawn;
    private readonly Queue<PooledOre> _pool = new Queue<PooledOre>();
    private readonly List<ActiveOre> _active = new List<ActiveOre>();
    private readonly List<Collider> _oreColliders = new List<Collider>();
    private readonly List<Collider> _ownerColliders = new List<Collider>();
    private readonly HashSet<Collider> _oreColliderSet = new HashSet<Collider>();
    private readonly HashSet<Collider> _ownerColliderSet = new HashSet<Collider>();
    private readonly RaycastHit[] _groundHits = new RaycastHit[32];
    private int _createdCount;
    private Transform _poolRoot;
    private bool _wasDrillingLastFrame;

    /// <summary>Кэш автосозданного материала для Built-in RP (проект без URP).</summary>
    private static Material s_runtimeBuiltInDrillingDust;

    private sealed class PooledOre
    {
        public GameObject go;
        public Rigidbody rb;
        public MeshFilter meshFilter;
        public Collider collider;
        public Vector3 baseScale;
    }

    private struct ActiveOre
    {
        public PooledOre ore;
        public float releaseAt;
        public Vector3 velocity;
        public Vector3 spinAxis;
        public float spinSpeed;
        public bool grounded;
        public float spawnedAt;
    }

    private void Awake()
    {
        spawn = ResolveSpawnAnchor();
        if (!ValidateOrePrefab())
        {
            enabled = false;
            return;
        }
        _poolRoot = new GameObject("OrePoolRuntime").transform;
        _poolRoot.position = Vector3.zero;
        CacheOwnerColliders();
        FixDrillingDustMaterialsForBuiltInPipeline();
    }

    private void Start()
    {
        StartCoroutine(PrewarmPoolGradually());
        StartCoroutine(SpawnCoroutine());
    }

    private void Update()
    {
        ReleaseExpired();

        bool drillingNow = IsDrillingNow();
        UpdateDrillingDust(drillingNow);
        if (spawnOnlyWhenDrilling && drillingNow && !_wasDrillingLastFrame)
            SpawnBurst();

        _wasDrillingLastFrame = drillingNow;
    }

    private void FixedUpdate()
    {
        if (useSimpleDownwardMotion)
        {
            SimulateSimpleDownwardMotion();
            return;
        }

        StabilizeActiveBodies();
    }

    private IEnumerator SpawnCoroutine()
    {
        while (true)
        {
            float jitter = delayJitter > 0f ? Random.Range(-delayJitter, delayJitter) : 0f;
            float wait = delay + jitter;
            if (wait <= 0f)
                yield return null;
            else
                yield return new WaitForSeconds(wait);
            bool canSpawn = !spawnOnlyWhenDrilling || IsDrillingNow();
            if (canSpawn)
                SpawnBurst();
        }
    }

    private bool IsDrillingNow()
    {
        // Если менеджера нет, не блокируем спавн полностью.
        if (DrillStateManager.instance == null)
            return true;

        // Стабильнее ориентироваться на состояние включённой буровой головы,
        // иначе при отпускании движения поток "мигает".
        if (DrillStateManager.instance.drillHeadTurn != null)
            return DrillStateManager.instance.drillHeadTurn.turned;

        return DrillStateManager.instance.isDrilling;
    }

    /// <summary>
    /// Материалы из Unity Particle Pack часто на URP — в Built-in они розовые/фиолетовые.
    /// Подменяем только неподдерживаемые слоты; свои корректные материалы не трогаем.
    /// </summary>
    private void FixDrillingDustMaterialsForBuiltInPipeline()
    {
        if (drillingDustParticles == null)
            return;

        Material replacement = drillingDustBuiltInMaterialOverride;
        if (replacement == null)
        {
            if (s_runtimeBuiltInDrillingDust == null)
                s_runtimeBuiltInDrillingDust = CreateRuntimeBuiltInDrillingDustMaterial();
            replacement = s_runtimeBuiltInDrillingDust;
        }

        if (replacement == null)
            return;

        var renderers = drillingDustParticles.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            ParticleSystemRenderer psr = renderers[r];
            if (psr == null)
                continue;

            Material[] mats = psr.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!NeedsBuiltInParticleMaterialFix(mats[i]))
                    continue;
                mats[i] = replacement;
                changed = true;
            }
            if (changed)
                psr.sharedMaterials = mats;

            if (NeedsBuiltInParticleMaterialFix(psr.trailMaterial))
                psr.trailMaterial = replacement;
        }
    }

    private static bool NeedsBuiltInParticleMaterialFix(Material m)
    {
        if (m == null)
            return true;
        if (m.shader == null)
            return true;
        if (!m.shader.isSupported)
            return true;
        string n = m.shader.name;
        if (n.StartsWith("Universal Render Pipeline", System.StringComparison.Ordinal))
            return true;
        if (n.StartsWith("HDRP/", System.StringComparison.Ordinal))
            return true;
        return false;
    }

    private static Material CreateRuntimeBuiltInDrillingDustMaterial()
    {
        Shader s = Shader.Find("Particles/Standard Unlit");
        if (s == null)
            s = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (s != null)
        {
            var m = new Material(s)
            {
                name = "Runtime_BuiltIn_DrillingDust",
                color = new Color(0.72f, 0.58f, 0.44f, 0.85f)
            };
            Material builtinDefault = Resources.GetBuiltinResource<Material>("Default-Particle.mat");
            if (builtinDefault != null && builtinDefault.mainTexture != null)
            {
                if (m.HasProperty("_MainTex"))
                    m.SetTexture("_MainTex", builtinDefault.mainTexture);
                else if (m.HasProperty("_BaseMap"))
                    m.SetTexture("_BaseMap", builtinDefault.mainTexture);
            }
            return m;
        }

        return Resources.GetBuiltinResource<Material>("Default-Particle.mat");
    }

    private void UpdateDrillingDust(bool drillingNow)
    {
        if (drillingDustParticles == null)
            return;

        if (drillingNow)
        {
            if (!drillingDustParticles.isPlaying)
                drillingDustParticles.Play(withChildren: true);
        }
        else if (_wasDrillingLastFrame)
        {
            ParticleSystemStopBehavior stopBehavior = clearDustOnStop
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting;
            drillingDustParticles.Stop(withChildren: true, stopBehavior);
        }
    }

    private void SpawnBurst()
    {
        int minCount = Mathf.Max(1, burstMin);
        int maxCount = Mathf.Max(minCount, burstMax);
        int count = Random.Range(minCount, maxCount + 1);
        for (int i = 0; i < count; i++)
            SpawnOne();
    }

    private void SpawnOne()
    {
        Vector3 half = randomExtents * 0.5f;
        Vector3 r = new Vector3(
            Random.Range(-half.x, half.x),
            Random.Range(-half.y, half.y),
            Random.Range(-half.z, half.z));
        // Принудительно держим спавн близко к "жёлобу" по высоте,
        // чтобы настройки в сцене не уводили поток слишком высоко.
        float clampedY = Mathf.Clamp(localOffset.y, -0.05f, 0.16f);
        float clampedBoost = Mathf.Clamp(spawnHeightBoost, -0.05f, 0.08f);
        float clampedRandomY = Mathf.Clamp(r.y, -0.03f, 0.03f);
        float widenedX = (localOffset.x + r.x) * 1.35f;
        float widenedZ = (localOffset.z + r.z) * 1.1f;
        Vector3 localSpawn = new Vector3(widenedX, clampedY + clampedRandomY, widenedZ);
        Vector3 pos = spawn.TransformPoint(localSpawn);
        pos += Vector3.up * clampedBoost;
        Quaternion rot = spawn.rotation * Quaternion.Euler(
            Random.Range(-randomEuler.x, randomEuler.x),
            Random.Range(-randomEuler.y, randomEuler.y),
            Random.Range(-randomEuler.z, randomEuler.z));

        PooledOre ore = GetFromPool();
        if (ore == null)
            return;

        GameObject go = ore.go;
        go.transform.SetParent(null, true);
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = ore.baseScale;
        if (ore.rb != null)
        {
            ore.rb.velocity = Vector3.zero;
            ore.rb.angularVelocity = Vector3.zero;
            ore.rb.isKinematic = useSimpleDownwardMotion;
            ore.rb.useGravity = !useSimpleDownwardMotion;
            ore.rb.detectCollisions = !useSimpleDownwardMotion;
        }
        go.SetActive(true);
        ApplyRockChipLook(ore);

        if (!useSimpleDownwardMotion && applyKick && ore.rb != null)
        {
            Rigidbody rb = ore.rb;
            Vector3 impulse;
            if (kickOppositeToMovement && DrillStateManager.instance != null &&
                DrillStateManager.instance.movement != null)
            {
                Transform m = DrillStateManager.instance.movement.transform;
                Vector3 rear = -m.forward;
                Vector3 side = m.right * Random.Range(-1f, 1f) * sideKickSpread;
                Vector3 lift = m.up * (upKick + Random.Range(-0.15f, 0.25f));
                if (pourDownward)
                {
                    float down = downKickStrength + Random.Range(-downKickJitter, downKickJitter);
                    Vector3 downFlow = -m.up * Mathf.Max(0f, down);
                    Vector3 rearFlow = rear * rearKickWhilePour;
                    Vector3 sideFlow = m.right * Random.Range(-1f, 1f) * sideKickWhilePour;
                    impulse = downFlow + rearFlow + sideFlow;
                }
                else
                    impulse = rear * rearKickStrength + side + lift;
            }
            else
                impulse = spawn.TransformVector(localImpulseFallback);

            rb.AddForce(impulse, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);
        }

        ActiveOre entry = new ActiveOre
        {
            ore = ore,
            releaseAt = Time.time + Mathf.Max(0.1f, oreLifetime),
            velocity = Vector3.zero,
            spinAxis = Random.onUnitSphere,
            spinSpeed = Random.Range(conveyorSpinMin, conveyorSpinMax),
            grounded = false,
            spawnedAt = Time.time
        };

        if (useSimpleDownwardMotion)
            entry.velocity = BuildConveyorVelocity();

        _active.Add(entry);
    }

    private void ApplyRockChipLook(PooledOre ore)
    {
        if (useFixedOreScale)
        {
            ore.go.transform.localScale = CalculateFixedScale(ore, fixedOreScale);
            return;
        }

        Vector3 s = new Vector3(
            Random.Range(chipScaleMin.x, chipScaleMax.x),
            Random.Range(chipScaleMin.y, chipScaleMax.y),
            Random.Range(chipScaleMin.z, chipScaleMax.z));
        ore.go.transform.localScale = Vector3.Scale(ore.baseScale, s);
    }

    private Vector3 CalculateFixedScale(PooledOre ore, Vector3 target)
    {
        if (!fixedScaleIsMeshWorldSize)
            return target;

        Vector3 parentLossy = GetSafeLossyScale(ore.go.transform.parent);
        Vector3 meshSize = GetSafeMeshSize(ore.meshFilter);

        // Подгоняем локальный scale так, чтобы итоговый размер меша в мире стал target.
        return new Vector3(
            target.x / (meshSize.x * parentLossy.x),
            target.y / (meshSize.y * parentLossy.y),
            target.z / (meshSize.z * parentLossy.z)
        );
    }

    private static Vector3 GetSafeMeshSize(MeshFilter mf)
    {
        Mesh mesh = mf != null ? mf.sharedMesh : null;
        if (mesh == null)
            return Vector3.one;

        Vector3 s = mesh.bounds.size;
        return new Vector3(
            Mathf.Max(0.0001f, Mathf.Abs(s.x)),
            Mathf.Max(0.0001f, Mathf.Abs(s.y)),
            Mathf.Max(0.0001f, Mathf.Abs(s.z))
        );
    }

    private static Vector3 GetSafeLossyScale(Transform t)
    {
        if (t == null)
            return Vector3.one;

        Vector3 s = t.lossyScale;
        return new Vector3(
            Mathf.Max(0.0001f, Mathf.Abs(s.x)),
            Mathf.Max(0.0001f, Mathf.Abs(s.y)),
            Mathf.Max(0.0001f, Mathf.Abs(s.z))
        );
    }

    private void ReleaseExpired()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (Time.time < _active[i].releaseAt)
                continue;

            ReleaseToPool(i);
        }
    }

    private IEnumerator PrewarmPoolGradually()
    {
        if (orePrefab == null)
        {
            Debug.LogError("[OreSpawner] orePrefab is not assigned.", this);
            yield break;
        }

        int target = Mathf.Max(1, prewarmCount);
        int perFrame = Mathf.Max(1, prewarmPerFrame);
        int spawnedThisFrame = 0;
        for (int i = 0; i < target && CanCreateMore(); i++)
        {
            _pool.Enqueue(CreateOreInstance());
            spawnedThisFrame++;
            if (spawnedThisFrame >= perFrame)
            {
                spawnedThisFrame = 0;
                yield return null;
            }
        }
    }

    private PooledOre GetFromPool()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();

        if (CanCreateMore())
            return CreateOreInstance();

        if (_active.Count == 0)
            return null;

        int reusableIndex = FindReusableActiveIndex();
        if (reusableIndex < 0)
            return null;

        PooledOre reused = _active[reusableIndex].ore;
        ReleaseToPool(reusableIndex);
        _pool.Dequeue(); // этот же объект только что положили в пул
        return reused;
    }

    private int FindReusableActiveIndex()
    {
        // 1) Сначала переиспользуем то, что ещё в полёте (не успело "осесть" на земле).
        int bestFlying = -1;
        float bestFlyingTime = float.MaxValue;
        for (int i = 0; i < _active.Count; i++)
        {
            ActiveOre a = _active[i];
            if (a.grounded)
                continue;
            if (a.releaseAt >= bestFlyingTime)
                continue;
            bestFlyingTime = a.releaseAt;
            bestFlying = i;
        }
        if (bestFlying >= 0)
            return bestFlying;

        // 2) Лежащие на земле можно брать только после истечения их groundedLifetime.
        int bestExpiredGrounded = -1;
        float bestExpiredGroundedTime = float.MaxValue;
        float now = Time.time;
        for (int i = 0; i < _active.Count; i++)
        {
            ActiveOre a = _active[i];
            if (!a.grounded || now < a.releaseAt)
                continue;
            if (a.releaseAt >= bestExpiredGroundedTime)
                continue;
            bestExpiredGroundedTime = a.releaseAt;
            bestExpiredGrounded = i;
        }
        if (bestExpiredGrounded >= 0)
            return bestExpiredGrounded;

        // 3) Чтобы не было пауз "то есть, то нет" при полном пуле:
        // если всё занято лежащими объектами, переиспользуем самый старый.
        int oldestGrounded = -1;
        float oldestGroundedRelease = float.MaxValue;
        for (int i = 0; i < _active.Count; i++)
        {
            ActiveOre a = _active[i];
            if (!a.grounded)
                continue;
            if (a.releaseAt >= oldestGroundedRelease)
                continue;
            oldestGroundedRelease = a.releaseAt;
            oldestGrounded = i;
        }

        return oldestGrounded;
    }

    private PooledOre CreateOreInstance()
    {
        GameObject go = Instantiate(orePrefab);
        if (_poolRoot != null)
            go.transform.SetParent(_poolRoot, true);
        go.SetActive(false);
        _createdCount++;

        ApplyOreMaterial(go);

        MeshFilter mf = go.GetComponent<MeshFilter>();

        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null)
            rb = go.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.drag = Mathf.Max(0f, rbDrag);
        rb.angularDrag = Mathf.Max(0f, rbAngularDrag);
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.interpolation = RigidbodyInterpolation.None;

        Collider col = go.GetComponent<Collider>();
        if (col == null)
        {
            // Лёгкий коллайдер безопаснее для массового спавна, чем convex MeshCollider.
            SphereCollider sphere = go.AddComponent<SphereCollider>();
            sphere.radius = 0.5f;
            col = sphere;
        }
        
        if (col != null)
        {
            if (ignoreOreToOreCollisions)
            {
                for (int i = 0; i < _oreColliders.Count; i++)
                {
                    Collider other = _oreColliders[i];
                    if (other == null)
                        continue;
                    Physics.IgnoreCollision(col, other, true);
                }
            }

            if (ignoreOreToOwnerCollisions)
            {
                for (int i = 0; i < _ownerColliders.Count; i++)
                {
                    Collider ownerCol = _ownerColliders[i];
                    if (ownerCol == null)
                        continue;
                    Physics.IgnoreCollision(col, ownerCol, true);
                }
            }

            _oreColliders.Add(col);
            _oreColliderSet.Add(col);
        }

        return new PooledOre
        {
            go = go,
            rb = rb,
            meshFilter = mf,
            collider = col,
            baseScale = go.transform.localScale
        };
    }

    private bool ValidateOrePrefab()
    {
        if (orePrefab == null)
        {
            Debug.LogError("[OreSpawner] Ore Prefab не назначен. Укажи свою 3D модель руды в поле Ore Prefab.", this);
            return false;
        }

        MeshRenderer[] renderers = orePrefab.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogError("[OreSpawner] В Ore Prefab нет MeshRenderer. Спавн отключён.", this);
            return false;
        }

        if (forceMaterialOnSpawn && oreMaterialOverride == null)
            Debug.LogWarning("[OreSpawner] oreMaterialOverride не назначен. Текстура может не примениться.", this);

        return true;
    }

    private void CacheOwnerColliders()
    {
        _ownerColliders.Clear();
        _ownerColliderSet.Clear();
        Transform root = collisionOwnerRoot != null ? collisionOwnerRoot : transform.root;
        if (root == null)
            return;

        Collider[] cols = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            Collider c = cols[i];
            if (c == null)
                continue;
            _ownerColliders.Add(c);
            _ownerColliderSet.Add(c);
        }
    }

    private bool CanCreateMore()
    {
        return _createdCount < Mathf.Max(1, maxPoolSize);
    }

    private void ReleaseToPool(int activeIndex)
    {
        ActiveOre active = _active[activeIndex];
        _active.RemoveAt(activeIndex);

        if (active.ore.rb != null)
        {
            active.ore.rb.velocity = Vector3.zero;
            active.ore.rb.angularVelocity = Vector3.zero;
            active.ore.rb.Sleep();
        }

        if (_poolRoot != null)
            active.ore.go.transform.SetParent(_poolRoot, true);
        active.ore.go.SetActive(false);
        _pool.Enqueue(active.ore);
    }

    private void SimulateSimpleDownwardMotion()
    {
        if (_active.Count == 0)
            return;

        float dt = Time.fixedDeltaTime;
        for (int i = 0; i < _active.Count; i++)
        {
            ActiveOre active = _active[i];
            GameObject go = active.ore.go;
            if (go == null || !go.activeInHierarchy)
                continue;

            if (!active.grounded)
            {
                active.velocity += Vector3.down * Mathf.Max(0f, conveyorGravity) * dt;
                float damping = Mathf.Clamp01(1f - Mathf.Max(0f, conveyorAirDrag) * dt);
                active.velocity *= damping;

                Vector3 currentPos = go.transform.position;
                Vector3 desiredPos = currentPos + active.velocity * dt;

                bool canSettle = Time.time - active.spawnedAt >= Mathf.Max(0f, minAirborneTimeBeforeSettle);
                if (settleOnGroundInSimpleMode && canSettle)
                {
                    float extraDown = Mathf.Max(0f, -active.velocity.y * dt);
                    if (TryGetGroundY(currentPos, extraDown + 0.2f, out float settleY))
                    {
                        if (desiredPos.y <= settleY)
                        {
                            desiredPos.y = settleY;
                            active.velocity = Vector3.zero;
                            active.grounded = true;
                            active.releaseAt = Mathf.Max(active.releaseAt, Time.time + Mathf.Max(0.1f, groundedLifetime));
                        }
                    }
                }

                go.transform.position = desiredPos;
            }

            if (!active.grounded && active.spinSpeed > 0f)
                go.transform.Rotate(active.spinAxis, active.spinSpeed * dt, Space.Self);

            _active[i] = active;
        }
    }

    private bool TryGetGroundY(Vector3 currentPos, float extraDistance, out float groundY)
    {
        groundY = 0f;
        float probe = Mathf.Max(0.05f, groundProbeDistance);
        float rayDistance = probe * 2f + Mathf.Max(0f, extraDistance);
        int hitCount = Physics.RaycastNonAlloc(
            currentPos + Vector3.up * probe,
            Vector3.down,
            _groundHits,
            rayDistance,
            ~0,
            QueryTriggerInteraction.Ignore);

        if (hitCount <= 0)
            return false;

        float bestDistance = float.MaxValue;
        bool found = false;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _groundHits[i];
            Collider c = hit.collider;
            if (c == null)
                continue;

            if (_ownerColliderSet.Contains(c) || _oreColliderSet.Contains(c))
                continue;

            if (hit.distance >= bestDistance)
                continue;

            bestDistance = hit.distance;
            groundY = hit.point.y + groundOffset;
            found = true;
        }

        return found;
    }

    private void ApplyOreMaterial(GameObject go)
    {
        if (!forceMaterialOnSpawn || oreMaterialOverride == null || go == null)
            return;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
                continue;
            r.sharedMaterial = oreMaterialOverride;
        }
    }

    private Transform ResolveSpawnAnchor()
    {
        if (spawnPointOverride != null)
            return spawnPointOverride;

        if (!autoFindSpawnPointByName || string.IsNullOrWhiteSpace(autoSpawnPointName))
            return transform;

        Transform root = transform.root != null ? transform.root : transform;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        Transform best = null;
        float bestDistSqr = float.MaxValue;

        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null)
                continue;
            if (!string.Equals(t.name, autoSpawnPointName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            float d = (t.position - transform.position).sqrMagnitude;
            if (d >= bestDistSqr)
                continue;

            bestDistSqr = d;
            best = t;
        }

        if (best != null)
            return best;

        // Fallback: берём ближайший объект, чьё имя содержит искомую строку (например "Cube (1)").
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null)
                continue;
            if (t.name.IndexOf(autoSpawnPointName, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            float d = (t.position - transform.position).sqrMagnitude;
            if (d >= bestDistSqr)
                continue;

            bestDistSqr = d;
            best = t;
        }

        if (best != null)
            return best;

        Debug.LogWarning($"[OreSpawner] Не найдена точка '{autoSpawnPointName}', использую Transform спавнера.", this);
        return transform;
    }

    private Vector3 BuildConveyorVelocity()
    {
        Vector3 localDir = conveyorLocalDirection.sqrMagnitude > 0.0001f
            ? conveyorLocalDirection.normalized
            : new Vector3(0f, -0.35f, -1f).normalized;

        Vector3 worldDir = spawn.TransformDirection(localDir);
        Vector3 side = spawn.right * Random.Range(-conveyorSideSpread, conveyorSideSpread);
        Vector3 up = spawn.up * Random.Range(-conveyorUpSpread, conveyorUpSpread);
        Vector3 vDir = (worldDir + side + up).sqrMagnitude > 0.0001f
            ? (worldDir + side + up).normalized
            : Vector3.down;

        float baseSpeed = Mathf.Max(0.1f, simpleDownwardSpeed);
        float speed = Mathf.Max(0.1f, baseSpeed + Random.Range(-conveyorSpeedJitter, conveyorSpeedJitter));
        Vector3 v = vDir * speed;
        v.y = Mathf.Min(v.y, 0.25f);
        return v;
    }

    private void StabilizeActiveBodies()
    {
        if (!stabilizeVerticalMotion || _active.Count == 0)
            return;

        for (int i = 0; i < _active.Count; i++)
        {
            Rigidbody rb = _active[i].ore.rb;
            if (rb == null || !rb.gameObject.activeInHierarchy)
                continue;

            Vector3 v = rb.velocity;
            if (v.y > maxUpwardSpeed)
                v.y = maxUpwardSpeed;

            rb.velocity = v;

            if (downwardAssist > 0f)
                rb.AddForce(Vector3.down * downwardAssist, ForceMode.Acceleration);
        }
    }

    private void OnDisable()
    {
        if (drillingDustParticles == null)
            return;

        drillingDustParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

}
