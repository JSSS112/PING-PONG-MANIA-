using UnityEngine;

/// <summary>
/// Script de la pelota — versión MVP simple y predecible.
///
/// Responsabilidades:
///   1. Configurar el Rigidbody con valores consistentes al spawnear (masa,
///      drag, gravedad, modo de colisión continuo). De este modo, sin importar
///      qué tenga el prefab, la pelota siempre arranca con la misma física.
///   2. Aplicar un PhysicsMaterial runtime a todos los colliders no-trigger
///      para garantizar un rebote consistente (bounciness fijo, combine =
///      Average — el rebote nunca gana energía).
///   3. Limitar la velocidad máxima en FixedUpdate para que la pelota nunca
///      "salga disparada como si nada".
///
/// No toca isKinematic / useGravity en runtime (eso lo hacen BallSpawner,
/// SistemaDeServicio y BossAI durante saques y lanzamientos). Una vez que la
/// pelota está libre, esta clase no interfiere — solo vigila el cap de velocidad.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PelotaBehaviour : MonoBehaviour
{
    [Header("Físicas")]
    [Tooltip("Velocidad máxima absoluta — nunca debe ir más rápido que esto.")]
    public float velocidadMaxima = 12f;

    [Tooltip("Masa de la pelota en kg. 0.05 da un golpe ágil pero no flotante.")]
    public float masa = 0.05f;

    [Tooltip("Resistencia al aire lineal. Bajo = vuelo limpio.")]
    public float dragLineal = 0.02f;

    [Tooltip("Resistencia al aire angular. Bajo = puede girar libre.")]
    public float dragAngular = 0.05f;

    [Header("Material de física (runtime)")]
    [Tooltip("0..1. 0.82 da un rebote vivo pero sin ganar energía nunca.")]
    [Range(0f, 1f)] public float bounciness = 0.82f;

    [Tooltip("Fricción al deslizar. Bajo = no se traba contra la mesa.")]
    [Range(0f, 1f)] public float friccion = 0.1f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Configuración del Rigidbody — siempre la misma, sin importar el prefab
        rb.mass                   = masa;
        rb.linearDamping          = dragLineal;
        rb.angularDamping         = dragAngular;
        rb.useGravity             = true;
        rb.isKinematic            = false;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // PhysicsMaterial runtime aplicado a todos los colliders sólidos
        PhysicsMaterial mat = new PhysicsMaterial("PelotaRuntime")
        {
            dynamicFriction = friccion,
            staticFriction  = friccion,
            bounciness      = bounciness,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine   = PhysicsMaterialCombine.Average
        };

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (col == null || col.isTrigger) continue;
            col.sharedMaterial = mat;
        }
    }

    void FixedUpdate()
    {
        // Cap de velocidad — único guardarraíl en runtime. Si algo (raqueta,
        // boss, colisión rara) llegó a empujar la pelota más allá del máximo,
        // la clampeamos sin tocar nada más.
        if (rb == null || rb.isKinematic) return;

        Vector3 v = rb.linearVelocity;
        if (v.magnitude > velocidadMaxima)
            rb.linearVelocity = v.normalized * velocidadMaxima;
    }

    // ════════════════════════════════════════════════════════════════════════
    // API legada — stubs no-op por compatibilidad con UnityEvents en escenas.
    // Si alguien los llama, simplemente no pasa nada.
    // ════════════════════════════════════════════════════════════════════════
    public void IniciarFlotando() { }
    public void SetEfectoColor(bool esAzul) { }
    public void ResetarEfectoColor() { }
}
