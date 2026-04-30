using UnityEngine;

/// <summary>
/// Raqueta pegada a la mano derecha del jugador.
///
/// Comportamiento:
/// - Sigue rígidamente la posición y rotación del Transform de la mano derecha (RightHandAnchor)
/// - Rigidbody Kinematic — se mueve por script, no por física
/// - Aplica fuerza a la pelota usando la velocidad real del movimiento de la mano
/// - No se puede agarrar ni soltar (el jugador la lleva siempre)
///
/// Setup en Unity:
/// - Crear GameObject con MeshFilter+MeshRenderer (raqueta visual)
/// - Añadir Rigidbody (Is Kinematic = true, Use Gravity = false, Interpolate = Interpolate)
/// - Añadir Collider (BoxCollider o similar) en la superficie golpeadora
/// - Asignar este script
/// - En Inspector arrastrar el RightHandAnchor del Camera Rig al campo "manoDerecha"
/// - Configurar offset si la raqueta no aparece bien orientada en la mano
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
    [Tooltip("Velocidad mínima del impulso aunque la raqueta esté quieta.")]
    public float fuerzaMinima = 1.5f;
    [Tooltip("Multiplicador extra de la velocidad de la raqueta al golpear.")]
    public float multiplicadorGolpe = 1.0f;

    private Rigidbody rb;

    // Posición/rotación previa para calcular velocidad cuando la mano se mueve
    private Vector3 posPrev;
    private Quaternion rotPrev;

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
            // Fallback: buscar por nombre estándar de Meta XR
            GameObject go = GameObject.Find("RightHandAnchor");
            if (go == null) go = GameObject.Find("RightControllerAnchor");
            if (go != null) manoDerecha = go.transform;
        }

        if (manoDerecha == null)
        {
            Debug.LogError("[OASIS][Raqueta] No se encontró RightHandAnchor — asignar manualmente en el Inspector.");
            return;
        }

        // Posición inicial sin teletransportes raros
        Quaternion offRot = Quaternion.Euler(offsetRotacion);
        rb.position = manoDerecha.position + manoDerecha.rotation * offsetPosicion;
        rb.rotation = manoDerecha.rotation * offRot;

        posPrev = rb.position;
        rotPrev = rb.rotation;
    }

    void FixedUpdate()
    {
        if (manoDerecha == null) return;

        Quaternion offRot = Quaternion.Euler(offsetRotacion);
        Vector3    nuevaPos = manoDerecha.position + manoDerecha.rotation * offsetPosicion;
        Quaternion nuevaRot = manoDerecha.rotation * offRot;

        rb.MovePosition(nuevaPos);
        rb.MoveRotation(nuevaRot);

        posPrev = nuevaPos;
        rotPrev = nuevaRot;
    }

    // ════════════════════════════════════════════════════════════════════════
    // GOLPE A LA PELOTA
    // ════════════════════════════════════════════════════════════════════════
    void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.CompareTag("Ball")) return;

        Rigidbody rbPelota = col.rigidbody;
        if (rbPelota == null) return;

        // Velocidad real de la raqueta en el punto de contacto.
        // GetPointVelocity en kinematic devuelve 0, así que la calculamos manualmente.
        Vector3 contacto = col.contacts[0].point;
        Vector3 velRaqueta = (rb.position - posPrev) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);

        // Dirección del golpe = inversa a la normal del contacto (apunta hacia afuera de la raqueta)
        Vector3 normal = col.contacts[0].normal;
        Vector3 direccionGolpe = -normal;

        float speed = Mathf.Max(velRaqueta.magnitude * multiplicadorGolpe, fuerzaMinima);

        // Resultado: empuje en dirección de la cara de la raqueta + arrastre en dirección de movimiento
        Vector3 resultado = direccionGolpe * speed + velRaqueta * multiplicadorGolpe;

        rbPelota.linearVelocity  = resultado;
        rbPelota.angularVelocity = Vector3.zero;

        BallWatchdog.instance?.RegistrarGolpe();

        Debug.Log($"[OASIS][Raqueta] Golpe v={resultado} (velMano={velRaqueta.magnitude:F2})");
    }
}
