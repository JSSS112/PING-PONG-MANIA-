using UnityEngine;

/// <summary>
/// Raqueta kinematica que sigue la mano derecha. Al colisionar con la pelota,
/// aplica una reflexion limpia + un aporte LIMITADO del swing. Garantiza
/// que la pelota SIEMPRE se aleja de la raqueta con velocidad util,
/// y nunca supera la velocidad maxima.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RaquetaJugador : MonoBehaviour
{
    [Header("Referencia mano")]
    public Transform manoDerecha;
    public Vector3 offsetPosicion = Vector3.zero;
    public Vector3 offsetRotacion = Vector3.zero;

    [Header("Golpe")]
    [Tooltip("Velocidad minima de salida en la direccion normal. Garantiza que el golpe siempre 'cuente'.")]
    public float velocidadMinimaSalida = 4f;

    [Tooltip("Velocidad maxima absoluta de la pelota tras el golpe.")]
    public float velocidadMaxima = 10f;

    [Tooltip("Coeficiente de restitucion del rebote: 1 = elastico perfecto, 0 = pelota se pega.")]
    [Range(0.5f, 1f)] public float coeficienteRebote = 0.9f;

    [Tooltip("Cuanto del swing de la raqueta se suma al rebote (0-1). Bajo = mas controlable.")]
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
        if (manoDerecha == null) return;

        Vector3 nuevaPos = manoDerecha.position + manoDerecha.rotation * offsetPosicion;
        Quaternion nuevaRot = manoDerecha.rotation * Quaternion.Euler(offsetRotacion);

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

        // Si la pelota esta sostenida (kinematic) por el sistema de saque,
        // no la golpeamos. Evita que el jugador rompa su propio saque.
        if (rbPelota.isKinematic) return;

        Vector3 normal = col.contacts[0].normal;
        Vector3 velPelota = rbPelota.linearVelocity;

        // 1) Reflexion fisica clasica, atenuada por el coeficiente de rebote.
        Vector3 reflejada = Vector3.Reflect(velPelota, normal) * coeficienteRebote;

        // 2) Aporte del swing: solo la componente que va en la direccion normal,
        //    y solo un porcentaje. Asi un swing fuerte aumenta el golpe pero
        //    no dispara la pelota a 50 m/s.
        float compRaqueta = Mathf.Max(0f, Vector3.Dot(velRaqueta, normal));
        Vector3 impulso = normal * compRaqueta * aporteSwing;

        Vector3 vFinal = reflejada + impulso;

        // 3) Garantizar velocidad minima EN LA NORMAL.
        //    Si la pelota viene muy lenta, igual sale con fuerza util.
        float compNormal = Vector3.Dot(vFinal, normal);
        if (compNormal < velocidadMinimaSalida)
        {
            vFinal += normal * (velocidadMinimaSalida - compNormal);
        }

        // 4) Tope absoluto de velocidad.
        if (vFinal.magnitude > velocidadMaxima)
        {
            vFinal = vFinal.normalized * velocidadMaxima;
        }

        rbPelota.linearVelocity = vFinal;
        // NO tocamos angularVelocity: dejamos que el efecto natural permanezca.
    }
}
