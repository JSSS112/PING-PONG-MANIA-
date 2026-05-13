using UnityEngine;

/// <summary>
/// Pelota minimalista. Una sola responsabilidad: existir como Rigidbody
/// con configuracion garantizada y un tope de velocidad para que la
/// fisica nunca se vuelva irreal.
///
/// El estado (isKinematic / useGravity) lo controlan los scripts duenos:
///   - SistemaDeServicio mientras el jugador la sostiene en el saque.
///   - BossAI durante los 0.1s de congelado antes de devolver / sacar.
/// El BallWatchdog se encarga de declarar la pelota perdida y darle el
/// punto al lado correcto.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class PelotaBehaviour : MonoBehaviour
{
    [Header("Tope de seguridad")]
    [Tooltip("Velocidad maxima permitida en m/s. Si la pelota la supera, se recorta.")]
    public float velocidadMaxima = 12f;

    private Rigidbody rb;

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

        // Unico guardarrail: tope de velocidad. Sin esto, un golpe con
        // angulo extremo puede catapultar la pelota a 50 m/s.
        Vector3 v = rb.linearVelocity;
        if (v.magnitude > velocidadMaxima)
        {
            rb.linearVelocity = v.normalized * velocidadMaxima;
        }
    }
}
