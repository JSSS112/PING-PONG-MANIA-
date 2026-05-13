using UnityEngine;

/// <summary>
/// Pelota minimalista con dos materiales especiales opcionales.
///
///   - materialAmarillo: aspecto + el punto vale por 2.
///   - materialNaranja: aspecto + pelota mas liviana, mas grande y mas rapida.
///
/// Cada uno tiene su propia probabilidad. Se chequean en orden:
/// primero amarillo, despues naranja. Si ninguno sale, la pelota
/// mantiene el material por defecto del prefab. NO toca el PhysicsMaterial.
///
/// El estado (isKinematic / useGravity) lo controlan los scripts duenos:
///   - SistemaDeServicio mientras el jugador la sostiene en el saque.
///   - BossAI durante el congelado antes de devolver / sacar.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class PelotaBehaviour : MonoBehaviour
{
    [Header("Tope de seguridad")]
    [Tooltip("Velocidad maxima permitida en m/s.")]
    public float velocidadMaxima = 12f;

    [Header("Material AMARILLO — el punto vale x2")]
    public Material materialAmarillo;
    [Range(0f, 1f)] public float probabilidadAmarillo = 0.2f;
    [Tooltip("Cuanta vida quita el punto cuando la pelota es amarilla.")]
    public int valorPuntoAmarillo = 2;

    [Header("Material NARANJA — mas liviana, grande y rapida")]
    public Material materialNaranja;
    [Range(0f, 1f)] public float probabilidadNaranja = 0.2f;
    [Tooltip("Multiplicador de escala para la pelota naranja.")]
    public float escalaNaranja = 1.35f;
    [Tooltip("Masa para la pelota naranja (la normal es 0.0027).")]
    public float masaNaranja = 0.0015f;
    [Tooltip("Velocidad maxima para la pelota naranja (la normal es 12).")]
    public float velMaxNaranja = 16f;
    [Tooltip("Drag lineal para la pelota naranja (menor = mantiene mas velocidad). Normal 0.02.")]
    public float dragNaranja = 0.01f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        AplicarMaterialYEfectos();
    }

    void FixedUpdate()
    {
        if (rb.isKinematic) return;

        Vector3 v = rb.linearVelocity;
        if (v.magnitude > velocidadMaxima)
        {
            rb.linearVelocity = v.normalized * velocidadMaxima;
        }
    }

    void AplicarMaterialYEfectos()
    {
        // Reset por defecto: el punto vale 1.
        if (GameManager.instance != null) GameManager.instance.puntoVale = 1;

        // 1) Amarillo
        if (materialAmarillo != null && Random.value < probabilidadAmarillo)
        {
            AplicarMaterial(materialAmarillo);
            if (GameManager.instance != null)
                GameManager.instance.puntoVale = valorPuntoAmarillo;
            Debug.Log($"[Pelota] AMARILLA — el proximo punto vale {valorPuntoAmarillo}");
            return;
        }

        // 2) Naranja
        if (materialNaranja != null && Random.value < probabilidadNaranja)
        {
            AplicarMaterial(materialNaranja);
            rb.mass          = masaNaranja;
            rb.linearDamping = dragNaranja;
            transform.localScale *= escalaNaranja;
            velocidadMaxima  = velMaxNaranja;
            Debug.Log("[Pelota] NARANJA — mas liviana, grande y rapida");
        }
    }

    void AplicarMaterial(Material m)
    {
        if (m == null) return;
        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
        if (mr != null) mr.material = m;
    }
}
