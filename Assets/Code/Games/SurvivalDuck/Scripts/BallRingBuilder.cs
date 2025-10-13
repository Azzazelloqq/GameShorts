using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class BallChainBuilder : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Rigidbody обруча, который крутится Hinge-мотором.")]
    public Rigidbody ring;
    [Tooltip("Куда складывать инстансы шаров.")]
    public Transform ballsParent;
    [Tooltip("Префаб визуала шара БЕЗ компонентов (можно просто Mesh).")]
    public GameObject ballPrefab;

    [Header("Layout (цепочка)")]
    [Min(2)] public int count = 8;          // сколько шаров
    [Min(0.01f)] public float spacing = 1f; // расстояние между соседями (по прямой)
    [Tooltip("Строить заново при старте Play Mode.")]
    public bool rebuildOnPlay = true;

    [Header("Ball Rigidbody")]
    [Min(0.01f)] public float ballMass = 0.75f;
    public bool ballsUseGravity = false;
    public RigidbodyInterpolation ballInterpolation = RigidbodyInterpolation.Interpolate;

    [Header("ConfigurableJoint (Ball -> Ring): держим радиус")]
    [Tooltip("Жёсткость удержания радиуса.")]
    public float linearSpring = 50000f;
    [Tooltip("Гашение колебаний радиуса.")]
    public float linearDamper = 800f;
    [Range(0.001f, 0.1f)] public float projectionDistance = 0.02f;
    [Range(1f, 30f)] public float projectionAngle = 12f;

    [Header("SpringJoint (Ball[i] -> Ball[i-1]): связь соседей")]
    [Tooltip("Жёсткость «пружины» между соседями (1 unit).")]
    public float followSpring = 3000f;
    [Tooltip("Гашение в пружине между соседями.")]
    public float followDamper = 200f;
    [Tooltip("Коридор вокруг 1 unit. 0 = строго 1. 0.01 = ±1%.")]
    [Range(0f, 0.1f)] public float distanceBand = 0f;

    [Header("Collisions")]
    [Tooltip("Игнорировать столкновения шар ↔ шар.")]
    public bool ignoreBallToBallCollisions = true;
    [Tooltip("Игнорировать столкновения шаров с кольцом.")]
    public bool ignoreBallsWithRing = true;

    [Header("Info")]
    [ReadOnly] public float firstRadius; // радиус первого звена (равен spacing)
    [ReadOnly] public float lastRadius;  // радиус последнего звена (count*spacing)

    // --- internal ---
    readonly List<Rigidbody> _balls = new List<Rigidbody>();
    readonly List<Collider> _ballCols = new List<Collider>();

    void OnValidate()
    {
        count = Mathf.Max(2, count);
        spacing = Mathf.Max(0.01f, spacing);
        firstRadius = spacing;
        lastRadius  = count * spacing;
    }

    void Start()
    {
        if (rebuildOnPlay) Rebuild();
    }

    [ContextMenu("Rebuild Now")]
    public void Rebuild()
    {
        if (!ValidateInputs()) return;

        ClearExistingBalls();

        // Базис: строим цепочку в плоскости, перпендикулярной оси вращения кольца (ring.up).
        Vector3 center = ring.worldCenterOfMass;
        Vector3 axis   = ring.transform.up.normalized;

        // Выберем поперечное направление (right) стабильно, даже если axis ≈ forward.
        Vector3 right  = Vector3.Cross(axis, Vector3.forward);
        if (right.sqrMagnitude < 1e-4f) right = Vector3.Cross(axis, Vector3.right);
        right.Normalize();

        // Инстанс шаров: Ball_i на расстоянии r = (i+1)*spacing от центра, вдоль right.
        for (int i = 0; i < count; i++)
        {
            float r = (i + 1) * spacing;
            Vector3 pos = center + right * r;

            GameObject go = Instantiate(ballPrefab, pos, Quaternion.identity, ballsParent);
            go.name = $"Ball_{i:00}";

            // Collider (если отсутствует — ставим SphereCollider примерно по визуалу)
            Collider col = go.GetComponent<Collider>();
            if (col == null)
            {
                float guess = GuessVisualRadius(go, 0.25f);
                var sc = go.AddComponent<SphereCollider>();
                sc.radius = guess;
                col = sc;
            }
            _ballCols.Add(col);

            // Rigidbody
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.mass = ballMass;
            rb.useGravity = ballsUseGravity;
            rb.interpolation = ballInterpolation;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _balls.Add(rb);

            // ConfigurableJoint к RING — удерживаем радиус r
            var cj = go.AddComponent<ConfigurableJoint>();
            cj.connectedBody = ring;
            cj.autoConfigureConnectedAnchor = false;
            cj.anchor = Vector3.zero;
            cj.connectedAnchor = Vector3.zero;

            cj.xMotion = ConfigurableJointMotion.Limited;
            cj.yMotion = ConfigurableJointMotion.Limited;
            cj.zMotion = ConfigurableJointMotion.Limited;

            var limit = new SoftJointLimit { limit = r }; // индивидуальный радиус
            cj.linearLimit = limit;

            var lspring = new SoftJointLimitSpring { spring = linearSpring, damper = linearDamper };
            cj.linearLimitSpring = lspring;

            // Углы свободны — цепочка изгибается естественно
            cj.angularXMotion = ConfigurableJointMotion.Free;
            cj.angularYMotion = ConfigurableJointMotion.Free;
            cj.angularZMotion = ConfigurableJointMotion.Free;

            // Стабилизация
            cj.projectionMode = JointProjectionMode.PositionAndRotation;
            cj.projectionDistance = projectionDistance;
            cj.projectionAngle = projectionAngle;

            cj.enableCollision = false;
            cj.enablePreprocessing = true;
        }

        // Пружины между соседями: Ball[i] -> Ball[i-1] (цепочка, без замыкания)
        for (int i = 1; i < count; i++)
        {
            var sj = _balls[i].gameObject.AddComponent<SpringJoint>();
            sj.connectedBody = _balls[i - 1];
            sj.autoConfigureConnectedAnchor = true;

            if (distanceBand <= 0f)
            {
                sj.minDistance = spacing;
                sj.maxDistance = spacing;
            }
            else
            {
                float b = spacing * distanceBand;
                sj.minDistance = spacing - b;
                sj.maxDistance = spacing + b;
            }

            sj.spring = followSpring;
            sj.damper = followDamper;
            sj.tolerance = 0.02f;
            sj.enableCollision = false;
        }

        // Игнор коллизий по настройкам
        if (ignoreBallToBallCollisions)
        {
            for (int i = 0; i < _ballCols.Count; i++)
                for (int j = i + 1; j < _ballCols.Count; j++)
                    Physics.IgnoreCollision(_ballCols[i], _ballCols[j], true);
        }

        if (ignoreBallsWithRing)
        {
            var ringCols = ring.GetComponentsInChildren<Collider>();
            foreach (var bc in _ballCols)
                foreach (var rc in ringCols)
                    Physics.IgnoreCollision(bc, rc, true);
        }
    }

    [ContextMenu("Clear Balls")]
    public void ClearExistingBalls()
    {
        _balls.Clear();
        _ballCols.Clear();
        if (ballsParent == null) return;

        var toDelete = new List<GameObject>();
        foreach (Transform ch in ballsParent)
            toDelete.Add(ch.gameObject);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            foreach (var go in toDelete) UnityEditor.Undo.DestroyObjectImmediate(go);
        }
        else
#endif
        {
            foreach (var go in toDelete) Destroy(go);
        }
    }

    bool ValidateInputs()
    {
        if (ring == null)
        {
            Debug.LogError("[BallChainBuilder] Укажи Rigidbody обруча (ring).");
            return false;
        }
        if (ballsParent == null)
        {
            Debug.LogError("[BallChainBuilder] Укажи Transform-родителя для шаров (ballsParent).");
            return false;
        }
        if (ballPrefab == null)
        {
            Debug.LogError("[BallChainBuilder] Укажи префаб шара (ballPrefab).");
            return false;
        }
        return true;
    }

    float GuessVisualRadius(GameObject go, float fallback)
    {
        float r = fallback;
        var mf = go.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var b = mf.sharedMesh.bounds; // локальные bounds меша
            float maxExtent = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
            // Берём максимальный масштаб (на случай неравномерного)
            Vector3 s = go.transform.lossyScale;
            float maxScale = Mathf.Max(s.x, Mathf.Max(s.y, s.z));
            r = Mathf.Max(0.01f, maxExtent * maxScale);
        }
        return r;
    }
}

/// <summary> Поле «только для чтения» в инспекторе. </summary>
public class ReadOnlyAttribute : PropertyAttribute {}

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        bool prev = GUI.enabled;
        GUI.enabled = false;
        UnityEditor.EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = prev;
    }
}
#endif
