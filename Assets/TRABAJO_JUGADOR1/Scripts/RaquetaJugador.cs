using UnityEngine;

/// <summary>
/// Raqueta kinematica que sigue la mano derecha. Soporta MANDO y HAND TRACKING.
///
/// En modo MANDO la raqueta hereda la rotacion del RightHandAnchor (mas un offset).
/// En modo HAND la rotacion se DERIVA de los ejes del wrist para que el agarre
/// salga natural sin importar la orientacion absoluta del anchor:
///   - El mango queda a lo largo de los dedos (wrist.forward).
///   - El blade mira hacia afuera de la palma (wrist.up).
/// El offsetRotacionHand sirve para tunear pequenas diferencias.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RaquetaJugador : MonoBehaviour
{
    [Header("Mano derecha — MANDO")]
    public Transform manoDerecha;
    public Vector3 offsetPosicion = Vector3.zero;
    public Vector3 offsetRotacion = Vector3.zero;

    [Header("Mano derecha — HAND TRACKING (opcional)")]
    [Tooltip("Transform a seguir en hand tracking. Puede ser el mismo RightHandAnchor.")]
    public Transform manoDerechaHand;

    [Tooltip("Offset de posicion respecto al wrist en hand tracking.")]
    public Vector3 offsetPosicionHand = new Vector3(0f, -0.02f, 0.08f);

    [Tooltip("Offset de rotacion FINO para hand tracking. La rotacion base se calcula a partir de los ejes del wrist; este offset es solo para ajustes pequenos.")]
    public Vector3 offsetRotacionHand = Vector3.zero;

    [Tooltip("OVRHand de la mano derecha. Si esta trackeando, se usa el modo hand tracking.")]
    public OVRHand handDerecha;

    [Header("Golpe")]
    public float velocidadMinimaSalida = 4f;
    public float velocidadMaxima = 10f;
    [Range(0.5f, 1f)] public float coeficienteRebote = 0.9f;
    [Range(0f, 1f)] public float aporteSwing = 0.35f;

    private Rigidbody rb;
    private Vector3 velRaqueta = Vector3.zero;
    private Vector3 posAnterior;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        posAnterior = rb.position;
    }

    void FixedUpdate()
    {
        bool usandoHands = handDerecha != null && handDerecha.IsTracked && manoDerechaHand != null;
        Transform mano = usandoHands ? manoDerechaHand : manoDerecha;
        if (mano == null) return;

        Vector3 nuevaPos;
        Quaternion nuevaRot;

        if (usandoHands)
        {
            // POSICION: la mano (con su offset).
            nuevaPos = mano.position + mano.rotation * offsetPosicionHand;

            // ROTACION FORZADA — la raqueta apunta SIEMPRE lejos del jugador.
            // Forward (blade)  = direccion horizontal desde la cabeza hasta la mano.
            // Up   (top blade) = up del wrist (asi el twist de la muñeca tilta la pala).
            Camera cam = Camera.main;
            Vector3 forwardWorld;
            if (cam != null)
            {
                forwardWorld = mano.position - cam.transform.position;
                forwardWorld.y = 0f;
                if (forwardWorld.sqrMagnitude < 0.0001f) forwardWorld = Vector3.forward;
                forwardWorld.Normalize();
            }
            else
            {
                forwardWorld = Vector3.forward;
            }

            // El up del wrist puede no ser perpendicular a forward; LookRotation
            // lo orto-normaliza internamente, no hace falta hacerlo a mano.
            Quaternion baseRot = Quaternion.LookRotation(forwardWorld, mano.up);
            nuevaRot = baseRot * Quaternion.Euler(offsetRotacionHand);
        }
        else
        {
            nuevaPos = mano.position + mano.rotation * offsetPosicion;
            nuevaRot = mano.rotation * Quaternion.Euler(offsetRotacion);
        }

        float dt = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        velRaqueta = (nuevaPos - posAnterior) / dt;
        posAnterior = nuevaPos;

        rb.MovePosition(nuevaPos);
        rb.MoveRotation(nuevaRot);
    }

    void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.CompareTag("Ball")) return;

        Rigidbody rbPelota = col.rigidbody;
        if (rbPelota == null) return;
        if (rbPelota.isKinematic) return;

        Vector3 normal = col.contacts[0].normal;
        Vector3 velPelota = rbPelota.linearVelocity;

        Vector3 reflejada = Vector3.Reflect(velPelota, normal) * coeficienteRebote;

        float compRaqueta = Mathf.Max(0f, Vector3.Dot(velRaqueta, normal));
        Vector3 impulso = normal * compRaqueta * aporteSwing;

        Vector3 vFinal = reflejada + impulso;

        float compNormal = Vector3.Dot(vFinal, normal);
        if (compNormal < velocidadMinimaSalida)
            vFinal += normal * (velocidadMinimaSalida - compNormal);

        if (vFinal.magnitude > velocidadMaxima)
            vFinal = vFinal.normalized * velocidadMaxima;

        // Liberar la pelota flotante del saque-con-manos si estaba congelada.
        rbPelota.constraints = RigidbodyConstraints.None;
        rbPelota.useGravity = true;
        rbPelota.linearVelocity = vFinal;

        // Sonido especial si la pelota lo tiene asignado.
        PelotaBehaviour pelota = col.gameObject.GetComponent<PelotaBehaviour>();
        if (pelota != null) pelota.NotificarGolpe();

        if (GameManager.instance != null) GameManager.instance.RegistrarGolpe(false);
        else BallWatchdog.instance?.RegistrarGolpe();
    }
}
