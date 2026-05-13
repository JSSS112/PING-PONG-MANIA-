using UnityEngine;

/// <summary>
/// Saque del jugador. Dos modos segun el input activo:
///
///   - MANDO: el jugador aprieta el gatillo izq -> aparece pelota en la mano,
///     la sostiene mientras lo mantiene apretado y al soltar la pelota cae
///     con gravedad.
///
///   - MANOS (hand tracking): al activarse el turno, aparece automaticamente
///     una pelota FLOTANTE en una posicion fija (un poco afuera de la mesa,
///     un poco arriba). El jugador solo tiene que golpearla con la raqueta
///     para arrancar el punto. NO hay que hacer pinch ni nada.
/// </summary>
public class SistemaDeServicio : MonoBehaviour
{
    [Header("Mano izquierda — transform que sigue la pelota (solo modo mando)")]
    [Tooltip("LeftHandAnchor del OVRCameraRig.")]
    public Transform manoIzquierda;

    [Tooltip("Prefab PingPongBall Variant.")]
    public GameObject prefabPelota;

    [Tooltip("Offset de la pelota respecto a la mano (solo modo mando).")]
    public Vector3 offsetPelota = new Vector3(0f, -0.05f, 0.05f);

    [Header("Saque con MANOS — pelota flotante")]
    [Tooltip("Posicion donde aparece la pelota flotante cuando se usa hand tracking. Un poco afuera de la mesa y arriba.")]
    public Transform posicionSaqueManos;

    [Header("Input MANDO")]
    public OVRInput.Button botonSaque = OVRInput.Button.PrimaryIndexTrigger;
    public OVRInput.Controller controlador = OVRInput.Controller.LTouch;

    [Header("Hand tracking (detector)")]
    [Tooltip("OVRHand de la mano izquierda. Si esta trackeando al iniciar el turno, se usa el modo de pelota flotante.")]
    public OVRHand handIzquierda;

    private GameObject pelotaActual;
    private Rigidbody rbPelota;

    private enum Modo { Inactivo, EsperandoMando, SosteniendoConMando, FlotandoConManos }
    private Modo modo = Modo.Inactivo;

    /// <summary>Lo llama GameManager al inicio del turno del jugador.</summary>
    public void HabilitarSaque(bool habilitado)
    {
        // Limpiar cualquier pelota previa de este sistema.
        if (pelotaActual != null)
        {
            Destroy(pelotaActual);
            pelotaActual = null;
            rbPelota = null;
        }

        if (!habilitado)
        {
            modo = Modo.Inactivo;
            return;
        }

        // Decidir modo segun el input disponible.
        bool manosActivas = handIzquierda != null && handIzquierda.IsTracked && posicionSaqueManos != null;
        if (manosActivas)
        {
            SpawnPelotaFlotante();
            modo = Modo.FlotandoConManos;
        }
        else
        {
            modo = Modo.EsperandoMando;
        }
    }

    void Update()
    {
        // En modo manos no escuchamos input — la pelota ya esta lista y
        // el punto arranca cuando la raqueta la golpee.
        if (modo == Modo.FlotandoConManos || modo == Modo.Inactivo) return;
        if (manoIzquierda == null || prefabPelota == null) return;

        // Modo mando: gatillo apretado -> crear pelota en la mano.
        if (modo == Modo.EsperandoMando && OVRInput.GetDown(botonSaque, controlador))
        {
            CrearPelotaEnMano();
            modo = Modo.SosteniendoConMando;
        }

        // Modo mando: gatillo soltado -> soltar pelota (cae).
        if (modo == Modo.SosteniendoConMando && OVRInput.GetUp(botonSaque, controlador))
        {
            SoltarPelota();
            modo = Modo.Inactivo;
        }
    }

    void FixedUpdate()
    {
        // Mientras este sostenida en la mano (solo modo mando), seguirla.
        if (modo != Modo.SosteniendoConMando) return;
        if (pelotaActual == null || rbPelota == null) return;
        if (!rbPelota.isKinematic) return;

        Vector3 destino = manoIzquierda.position + manoIzquierda.rotation * offsetPelota;
        rbPelota.MovePosition(destino);
    }

    // ════════════════════════════════════════════════════════════════════════
    // MODO MANDO — pelota en la mano hasta soltar el gatillo
    // ════════════════════════════════════════════════════════════════════════
    void CrearPelotaEnMano()
    {
        Vector3 pos = manoIzquierda.position + manoIzquierda.rotation * offsetPelota;
        pelotaActual = Instantiate(prefabPelota, pos, Quaternion.identity);
        rbPelota = pelotaActual.GetComponent<Rigidbody>();
        if (rbPelota == null) return;

        rbPelota.isKinematic = true;
        rbPelota.useGravity = false;
        rbPelota.linearVelocity = Vector3.zero;
        rbPelota.angularVelocity = Vector3.zero;
    }

    void SoltarPelota()
    {
        if (rbPelota == null) return;
        rbPelota.isKinematic = false;
        rbPelota.useGravity = true;
        rbPelota.linearVelocity = Vector3.zero;
        rbPelota.angularVelocity = Vector3.zero;

        // Soltamos las referencias internas — la pelota vive su vida y se
        // limpia cuando GameManager llame HabilitarSaque(false) o destruya
        // todas las pelotas al iniciar la siguiente ronda.
        pelotaActual = null;
        rbPelota = null;
    }

    // ════════════════════════════════════════════════════════════════════════
    // MODO MANOS — pelota flotante en posicion fija hasta que la raqueta la golpee
    // ════════════════════════════════════════════════════════════════════════
    void SpawnPelotaFlotante()
    {
        Vector3 pos = posicionSaqueManos.position;
        pelotaActual = Instantiate(prefabPelota, pos, Quaternion.identity);
        rbPelota = pelotaActual.GetComponent<Rigidbody>();
        if (rbPelota == null) return;

        // Dinamica (no kinematic) para que el OnCollisionEnter de la raqueta
        // funcione, pero sin gravedad y con la posicion congelada por
        // constraints. La raqueta liberara las constraints al golpearla.
        rbPelota.isKinematic = false;
        rbPelota.useGravity = false;
        rbPelota.linearVelocity = Vector3.zero;
        rbPelota.angularVelocity = Vector3.zero;
        rbPelota.constraints = RigidbodyConstraints.FreezePosition;
    }
}
