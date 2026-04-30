using UnityEngine;

/// <summary>
/// Sistema de saque del jugador (estilo "10 Billion Table Tennis").
///
/// Flujo:
/// 1. GameManager → BallSpawner.SpawnBall(false) → BallSpawner llama a IniciarSaqueJugador()
/// 2. Este script entra en "esperandoTrigger".
/// 3. El jugador presiona el trigger izquierdo → se instancia la pelota en la mano izquierda
///    (isKinematic=true, sigue la mano cada FixedUpdate).
/// 4. Mientras sostiene el trigger, calculamos la velocidad de la mano.
/// 5. Al soltar el trigger → isKinematic=false, useGravity=true, rb.linearVelocity = velocidadMano.
/// 6. La pelota se registra en BallSpawner como "pelota actual" y queda libre para ser golpeada.
///
/// Setup en Unity:
/// - Crear GameObject "SistemaServicio" en la jerarquía
/// - Añadir este script
/// - En el Inspector: arrastrar el LeftHandAnchor del Camera Rig a "manoIzquierda"
/// - Asignar este componente al campo BallSpawner.sistemaDeServicio
/// </summary>
public class SistemaDeServicio : MonoBehaviour
{
    [Header("Mano izquierda del Camera Rig (LeftHandAnchor)")]
    [Tooltip("Si se deja vacío se busca por nombre 'LeftHandAnchor' en escena.")]
    public Transform manoIzquierda;

    [Header("Offset de la pelota respecto a la mano izquierda")]
    public Vector3 offsetPelota = new Vector3(0f, 0.04f, 0.04f);

    [Header("Lanzamiento")]
    [Tooltip("Multiplicador para la velocidad heredada de la mano al soltar.")]
    public float multiplicadorLanzamiento = 1.0f;
    [Tooltip("Velocidad mínima al soltar (por si el jugador no movió la mano).")]
    public float velocidadMinimaSoltar = 1.5f;

    [Header("Controles (OVRInput)")]
    [Tooltip("Si tu proyecto NO usa OVRInput, desactivar esto y conectar IniciarSaque/SoltarPelota desde otro input.")]
    public bool usarOVRInput = true;

    // ─── Estado ──────────────────────────────────────────────────────────────
    private enum Estado { Inactivo, EsperandoTrigger, SosteniendoPelota }
    private Estado estado = Estado.Inactivo;

    private BallSpawner ballSpawner;
    private GameObject pelotaActual;
    private Rigidbody  rbPelota;

    private Vector3 posManoPrev;
    private Vector3 velocidadMano;

    // ════════════════════════════════════════════════════════════════════════
    void Start()
    {
        if (manoIzquierda == null)
        {
            GameObject go = GameObject.Find("LeftHandAnchor");
            if (go == null) go = GameObject.Find("LeftControllerAnchor");
            if (go != null) manoIzquierda = go.transform;
        }

        if (manoIzquierda == null)
            Debug.LogError("[OASIS][Servicio] No se encontró LeftHandAnchor — asignar manualmente.");
    }

    /// <summary>Llamado por BallSpawner cuando le toca al jugador sacar.</summary>
    public void IniciarSaqueJugador(BallSpawner spawner)
    {
        ballSpawner = spawner;
        estado = Estado.EsperandoTrigger;
        // Mientras esperamos al jugador no debe correr el watchdog
        // (no hay pelota y se gatillaría "Pelota no encontrada")
        BallWatchdog.instance?.DetenerMonitoreo();
        Debug.Log("[OASIS][Servicio] Esperando trigger izquierdo del jugador para sacar...");
    }

    /// <summary>Cancela un saque pendiente (ej. final de ronda, reinicio).</summary>
    public void CancelarSaque()
    {
        if (pelotaActual != null) { Destroy(pelotaActual); pelotaActual = null; }
        rbPelota = null;
        estado = Estado.Inactivo;
    }

    // ════════════════════════════════════════════════════════════════════════
    void Update()
    {
        if (!usarOVRInput) return;

        if (estado == Estado.EsperandoTrigger)
        {
            // GameManager arranca el watchdog 2.5s después del SpawnBall — lo silenciamos
            // mientras seguimos esperando el trigger del jugador.
            BallWatchdog.instance?.DetenerMonitoreo();

            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
                CrearPelotaEnMano();
        }
        else if (estado == Estado.SosteniendoPelota)
        {
            // Mientras la pelota está en la mano tampoco queremos watchdog (no se mueve)
            BallWatchdog.instance?.DetenerMonitoreo();

            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
                SoltarPelota();
        }
    }

    void FixedUpdate()
    {
        if (estado != Estado.SosteniendoPelota || rbPelota == null || manoIzquierda == null) return;

        Vector3 nuevaPos = manoIzquierda.position + manoIzquierda.rotation * offsetPelota;

        // Velocidad estimada de la mano para usar al soltar
        float dt = Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        velocidadMano = (nuevaPos - posManoPrev) / dt;

        rbPelota.MovePosition(nuevaPos);
        posManoPrev = nuevaPos;
    }

    // ════════════════════════════════════════════════════════════════════════
    void CrearPelotaEnMano()
    {
        if (ballSpawner == null) { Debug.LogWarning("[OASIS][Servicio] BallSpawner null."); return; }
        if (manoIzquierda == null) return;

        GameObject prefab = ballSpawner.GetBallPrefab();
        if (prefab == null) { Debug.LogError("[OASIS][Servicio] ballPrefab no asignado en BallSpawner."); return; }

        Vector3 pos = manoIzquierda.position + manoIzquierda.rotation * offsetPelota;
        pelotaActual = Instantiate(prefab, pos, Quaternion.identity);
        rbPelota = pelotaActual.GetComponent<Rigidbody>();

        if (rbPelota == null)
        {
            Debug.LogError("[OASIS][Servicio] La pelota no tiene Rigidbody.");
            Destroy(pelotaActual);
            pelotaActual = null;
            return;
        }

        rbPelota.isKinematic     = true;
        rbPelota.useGravity      = false;
        rbPelota.linearVelocity  = Vector3.zero;
        rbPelota.angularVelocity = Vector3.zero;

        // Registrar como pelota actual del juego
        ballSpawner.RegistrarPelotaJugador(pelotaActual);

        posManoPrev   = pos;
        velocidadMano = Vector3.zero;
        estado = Estado.SosteniendoPelota;

        Debug.Log("[OASIS][Servicio] Pelota creada en la mano izquierda — sostener y soltar trigger para lanzar.");
    }

    void SoltarPelota()
    {
        if (rbPelota == null) { estado = Estado.Inactivo; return; }

        rbPelota.isKinematic = false;
        rbPelota.useGravity  = true;

        Vector3 vSalida = velocidadMano * multiplicadorLanzamiento;
        if (vSalida.magnitude < velocidadMinimaSoltar)
            vSalida = Vector3.up * velocidadMinimaSoltar; // suelta tipo "saque vertical" si la mano no se movió

        rbPelota.linearVelocity  = vSalida;
        rbPelota.angularVelocity = Vector3.zero;

        // Reactivar watchdog ahora que la pelota está libre
        BallWatchdog.instance?.IniciarMonitoreo();
        Debug.Log($"[OASIS][Servicio] Pelota soltada v={vSalida}");

        // Soltamos referencias — la pelota sigue siendo "pelotaActual" del BallSpawner
        rbPelota = null;
        pelotaActual = null;
        estado = Estado.Inactivo;
    }
}
