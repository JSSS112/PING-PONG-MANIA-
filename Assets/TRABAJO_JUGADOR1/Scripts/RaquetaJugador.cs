using UnityEngine;

/// <summary>
/// Raqueta kinematica que sigue la mano derecha. Soporta MANDO y HAND TRACKING.
/// En ambos modos la raqueta hereda la rotacion del transform de la mano y
/// se afina con su offset correspondiente desde el Inspector.
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

    [Tooltip("Offset de posicion en hand tracking respecto al wrist (frame de la raqueta). Z = cuanto se aleja la raqueta de la palma hacia afuera. Subi si la raqueta atraviesa la mano.")]
    public Vector3 offsetPosicionHand = new Vector3(0f, 0f, 0.18f);

    [Tooltip("Offset de rotacion para hand tracking. Default (0,90,0) rota la pala para que salga perpendicular a los dedos como una raqueta real. Si queda mal, probar (0,-90,0) / (90,0,0) / (180,0,0).")]
    public Vector3 offsetRotacionHand = new Vector3(0f, 90f, 0f);

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
            // Agarre natural: la raqueta sigue al wrist tal cual, igual que
            // con el mando. El offset de rotacion permite afinar el angulo
            // del mango respecto a la palma desde el Inspector.
            nuevaRot = mano.rotation * Quaternion.Euler(offsetRotacionHand);
            nuevaPos = mano.position + nuevaRot * offsetPosicionHand;
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
