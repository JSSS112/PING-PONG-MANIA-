using UnityEngine;

/// <summary>
/// Raqueta pegada a la mano derecha del jugador.
///
/// Comportamiento:
/// - Sigue rígidamente la posición y rotación del Transform de la mano derecha (RightHandAnchor)
/// - Rigidbody Kinematic — se mueve por script, no por física
/// - Refleja la pelota contra la cara de la raqueta y suma la velocidad del swing
/// - Limita el resultado entre un mínimo y un máximo para que la respuesta sea
///   siempre fluida y jugable, aunque el jugador pegue muy suave o muy fuerte.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RaquetaJugador : MonoBehaviour
{
    [Header("Mano derecha del Camera Rig (RightHandAnchor)")]
    [Tooltip("Arrastrar aquí el Transform de la mano/controlador derecho. Se busca por nombre si se deja vacío.")]
    public Transform manoDerecha;

    [Header("Offsets (ajustar para que la raqueta caiga bien en la mano)")]
    public Vector3    offsetPosicion = Vector3.zero;
    public Vector3    offsetRotacion = Vector3.zero;

    [Header("Golpe a la pelota")]
    [Tooltip("Velocidad mínima de la pelota tras un golpe — garantiza que siempre rebote.")]
    public float fuerzaMinima = 3.5f;
    [Tooltip("Velocidad máxima de la pelota tras un golpe — evita tiros imposibles.")]
    public float velocidadMax = 11f;
    [Tooltip("Cuánta energía conserva la pelota al rebotar contra la raqueta (0..1).")]
    [Range(0f, 1f)] public float coefRebote = 0.75f;
    [Tooltip("Cuánto se suma de la velocidad del swing al golpe.")]
    public float multiplicadorGolpe = 1.0f;

    private Rigidbody rb;

    // Velocidad real del swing — recalculada cada FixedUpdate ANTES de mover
    private Vector3 velRaqueta;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void Start()
    {
        if (manoDerecha == null)
        {
            GameObject go = GameObject.Find("RightHandAnchor");
            if (go == null) go = GameObject.Find("RightControllerAnchor");
            if (go != null) manoDerecha = go.transform;
        }

        if (manoDerecha == null)
        {
            Debug.LogError("[OASIS][Raqueta] No se encontró RightHandAnchor — asignar manualmente en el Inspector.");
            return;
        }

        Quaternion offRot = Quaternion.Euler(offsetRotacion);
        rb.position = manoDerecha.position + manoDerecha.rotation * offsetPosicion;
        rb.rotation = manoDerecha.rotation * offRot;
        velRaqueta  = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (manoDerecha == null) return;

        Quaternion offRot   = Quaternion.Euler(offsetRotacion);
        Vector3    nuevaPos = manoDerecha.position + manoDerecha.rotation * offsetPosicion;
        Quaternion nuevaRot = manoDerecha.rotation * offRot;

        // Velocidad real del swing: diferencia entre posición actual y nueva,
        // calculada ANTES de aplicar MovePosition (sino sería 0).
        float dt   = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        velRaqueta = (nuevaPos - rb.position) / dt;

        rb.MovePosition(nuevaPos);
        rb.MoveRotation(nuevaRot);
    }

    // ════════════════════════════════════════════════════════════════════════
    // GOLPE A LA PELOTA — reflexión física + impulso del swing
    // ════════════════════════════════════════════════════════════════════════
    void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.CompareTag("Ball")) return;

        Rigidbody rbPelota = col.rigidbody;
        if (rbPelota == null) return;

        // Normal del contacto: apunta desde la raqueta hacia la pelota.
        Vector3 normal = col.contacts[0].normal;

        // Reflexión natural: invierte el componente de velocidad perpendicular
        // a la cara de la raqueta y conserva un porcentaje (coefRebote).
        Vector3 velPelota = rbPelota.linearVelocity;
        Vector3 reflejada = Vector3.Reflect(velPelota, normal) * coefRebote;

        // Impulso del swing: solo cuando la raqueta se mueve hacia la pelota.
        float compRaqueta = Mathf.Max(0f, Vector3.Dot(velRaqueta, normal));
        Vector3 impulso   = normal * compRaqueta * multiplicadorGolpe;

        Vector3 vFinal = reflejada + impulso;

        // Garantiza que la pelota se aleje de la raqueta con velocidad mínima
        float compNormal = Vector3.Dot(vFinal, normal);
        if (compNormal < fuerzaMinima)
            vFinal += normal * (fuerzaMinima - compNormal);

        // Tope para que el juego siga jugable
        if (vFinal.magnitude > velocidadMax)
            vFinal = vFinal.normalized * velocidadMax;

        rbPelota.linearVelocity  = vFinal;
        rbPelota.angularVelocity = Vector3.zero;

        if (GameManager.instance != null) GameManager.instance.RegistrarGolpeRaqueta();
        else BallWatchdog.instance?.RegistrarGolpe();

        Debug.Log($"[OASIS][Raqueta] golpe v={vFinal} (|v|={vFinal.magnitude:F2}, swing={velRaqueta.magnitude:F2})");
    }
}
