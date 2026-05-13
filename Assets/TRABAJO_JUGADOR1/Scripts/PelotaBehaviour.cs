using UnityEngine;

/// <summary>
/// Pelota minimalista. Una sola responsabilidad: existir como Rigidbody
/// con gravedad real y un tope de velocidad. NO gestiona efectos de color,
/// NO aplica gravedad manual, NO detecta si esta agarrada.
///
/// El estado (isKinematic / useGravity) lo controlan los scripts duenos:
///   - SistemaDeServicio mientras el jugador la sostiene en el saque.
///   - BossAI durante los 0.1s de congelado antes de devolver.
/// Cualquier otro script SOLO lee la pelota, no escribe sus flags.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class PelotaBehaviour : MonoBehaviour
{
    [Header("Limites de seguridad")]
    [Tooltip("Velocidad maxima permitida en m/s. Si la pelota la supera, se recorta.")]
    public float velocidadMaxima = 12f;

    [Tooltip("Si la pelota cae por debajo de esta Y mundial, se destruye.")]
    public float yMinima = -3f;

    [Tooltip("Si la pelota se queda casi quieta este tiempo (s), se destruye.")]
    public float tiempoQuietaMax = 4f;

    private Rigidbody rb;
    private float tiempoQuieta = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Forzamos configuracion correcta por si el prefab quedo mal seteado.
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void FixedUpdate()
    {
        if (rb.isKinematic) return;

        // Tope de velocidad: evita que la pelota se dispare a 50 m/s
        // tras un golpe con angulo extremo.
        Vector3 v = rb.linearVelocity;
        if (v.magnitude > velocidadMaxima)
        {
            rb.linearVelocity = v.normalized * velocidadMaxima;
        }

        // Watchdog: pelota fuera del mundo o quieta demasiado tiempo.
        if (transform.position.y < yMinima)
        {
            Destroy(gameObject);
            return;
        }
        if (v.magnitude < 0.2f) tiempoQuieta += Time.fixedDeltaTime;
        else                    tiempoQuieta = 0f;
        if (tiempoQuieta > tiempoQuietaMax) Destroy(gameObject);
    }
}
