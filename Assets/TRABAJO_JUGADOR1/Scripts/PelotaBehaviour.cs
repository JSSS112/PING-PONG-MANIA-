using UnityEngine;

/// <summary>
/// Poner este script en el PREFAB de la pelota.
/// Maneja:
///   1. Float hasta que el jugador la agarre (turno del jugador)
///   2. Efecto azul  → gravedad reducida (pelota liviana)
///   3. Efecto naranja → gravedad aumentada (pelota pesada)
///   Los efectos de color duran hasta que el boss vuelva a tocar la pelota.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PelotaBehaviour : MonoBehaviour
{
    // ─── CONFIGURACIÓN ───────────────────────────────────────────────────────
    [Header("Gravedad normal de la pelota")]
    public float gravedadNormal  =  9.81f;

    [Header("Gravedad cuando la pelota es AZUL (liviana)")]
    public float gravedadAzul    =  2.5f;

    [Header("Gravedad cuando la pelota es NARANJA (pesada)")]
    public float gravedadNaranja = 22f;

    // ─── ESTADO INTERNO ──────────────────────────────────────────────────────
    private Rigidbody    rb;
    private Renderer     rend;
    private Color        colorOriginal;

    private bool  flotando          = false;  // true solo cuando es turno del jugador
    private bool  efectoColorActivo = false;
    private float gravedadActual    = 9.81f;

    // ─── COLORES ─────────────────────────────────────────────────────────────
    private static readonly Color COLOR_AZUL    = new Color(0.2f, 0.5f, 1.0f);
    private static readonly Color COLOR_NARANJA = new Color(1.0f, 0.45f, 0.0f);

    // ════════════════════════════════════════════════════════════════════════
    void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        if (rend != null) colorOriginal = rend.material.color;
        gravedadActual = gravedadNormal;
    }

    void FixedUpdate()
    {
        // Gravedad manual cuando hay efecto de color activo
        // (UseGravity está en false durante el efecto)
        if (efectoColorActivo && rb != null)
        {
            rb.AddForce(Vector3.down * gravedadActual, ForceMode.Acceleration);
        }
    }

    void Update()
    {
        // ── Detección de agarre ──────────────────────────────────────────────
        if (!flotando) return;

        bool estaAgarrada = false;

        // Método 1: OVRGrabbable (SDK viejo)
        OVRGrabbable ovr = GetComponent<OVRGrabbable>();
        if (ovr != null && ovr.isGrabbed) estaAgarrada = true;

        // Método 2: fallback — si el RB tiene velocidad alguien la movió
        if (!estaAgarrada && rb != null && rb.linearVelocity.magnitude > 0.3f)
            estaAgarrada = true;

        if (estaAgarrada)
        {
            ActivarFisicasNormales();
            flotando = false;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // API PÚBLICA — llamada desde BallSpawner y BossAI
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Llamar cuando es turno del jugador. La pelota flota quieta.</summary>
    public void IniciarFlotando()
    {
        flotando = true;
        if (rb != null)
        {
            rb.useGravity      = false;
            rb.isKinematic     = false;
            rb.linearVelocity        = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>Activa efecto de color. azul=true → liviana, azul=false → pesada.</summary>
    public void SetEfectoColor(bool esAzul)
    {
        efectoColorActivo = true;

        if (esAzul)
        {
            gravedadActual = gravedadAzul;
            CambiarColor(COLOR_AZUL);
            Debug.Log("[Pelota] Efecto AZUL activado — gravedad reducida");
        }
        else
        {
            gravedadActual = gravedadNaranja;
            CambiarColor(COLOR_NARANJA);
            Debug.Log("[Pelota] Efecto NARANJA activado — gravedad aumentada");
        }

        // Desactivar la gravedad Unity y usar la manual
        if (rb != null) rb.useGravity = false;
    }

    /// <summary>Resetea color y gravedad al estado normal. Llamar cuando el boss toca la pelota.</summary>
    public void ResetarEfectoColor()
    {
        if (!efectoColorActivo) return;

        efectoColorActivo = false;
        gravedadActual    = gravedadNormal;

        if (rb != null) rb.useGravity = true;
        CambiarColor(colorOriginal);
        Debug.Log("[Pelota] Efecto de color reseteado — pelota normal");
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════════
    void ActivarFisicasNormales()
    {
        if (rb == null) return;
        rb.useGravity  = true;
        rb.isKinematic = false;
        Debug.Log("[Pelota] Agarrada por el jugador — físicas activadas");
    }

    void CambiarColor(Color c)
    {
        if (rend != null) rend.material.color = c;
    }
}