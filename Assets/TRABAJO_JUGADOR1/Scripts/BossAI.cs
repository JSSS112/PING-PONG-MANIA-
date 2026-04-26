using UnityEngine;
using System.Collections;

/// <summary>
/// BOSS IA FINAL
/// - Estático (solo decoración, no se mueve)
/// - Velocidad base que AUMENTA con cada vida perdida
/// - 30% de probabilidad: lanza bola roja señuelo en dirección opuesta en X
/// - 20% de probabilidad: aplica efecto azul/naranja a la pelota
/// - Siempre asegura que la pelota llegue bien a la mesa del jugador
/// </summary>
public class BossAI : MonoBehaviour
{
    // ─── VELOCIDAD (aumenta al perder vida) ──────────────────────────────────
    [Header("Velocidad base del tiro")]
    [Tooltip("Velocidad en Z hacia el jugador — negativo. Ej: -2.5")]
    public float velocidadZBase = -2.5f;

    [Tooltip("Velocidad en Y para el arco — positivo. Ej: 2.2")]
    public float velocidadYBase = 2.2f;

    [Tooltip("Cuánto aumenta la velocidad Z por cada vida que pierde el jefe")]
    public float incrementoVelocidadPorVida = 0.18f;

    [Tooltip("Velocidad máxima en Z (para que no se vuelva imposible)")]
    public float velocidadZMaxima = -6.0f;

    // ─── BOLA SEÑUELO (30%) ───────────────────────────────────────────────────
    [Header("Bola señuelo roja (30% de probabilidad)")]
    [Tooltip("Prefab de bola ROJA sin collider — solo visual")]
    public GameObject prefabBolaSenuelo;

    [Tooltip("Tiempo en segundos que vive la bola señuelo antes de desaparecer")]
    public float tiempoVidaSenuelo = 2.5f;

    // ─── EFECTO DE COLOR (20%) ────────────────────────────────────────────────
    [Header("Efecto de color azul/naranja (20% de probabilidad)")]
    [Range(0f, 1f)]
    public float probabilidadEfectoColor = 0.20f;

    // ─── ESTADO INTERNO ──────────────────────────────────────────────────────
    private bool ocupado = false;

    // ════════════════════════════════════════════════════════════════════════
    // DETECCIÓN: pelota toca al jefe
    // ════════════════════════════════════════════════════════════════════════
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;
        if (ocupado)                   return;
        if (GameManager.instance == null || !GameManager.instance.roundActive) return;

        BallSpawner spawner = FindFirstObjectByType<BallSpawner>();
        if (spawner != null && spawner.GetPelotaActual() != other.gameObject) return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Resetear efecto de color de la ronda anterior
        other.GetComponent<PelotaBehaviour>()?.ResetarEfectoColor();

        ocupado = true;
        StartCoroutine(LanzarPelota(rb, other.gameObject));
    }

    // ════════════════════════════════════════════════════════════════════════
    // LANZAR PELOTA — siempre al mismo punto, velocidad progresiva
    // ════════════════════════════════════════════════════════════════════════
    IEnumerator LanzarPelota(Rigidbody rb, GameObject pelotaObj)
    {
        if (rb == null) { ocupado = false; yield break; }

        // Congelar la pelota
        rb.isKinematic = true;
        rb.useGravity  = false;

        yield return new WaitForSeconds(0.1f);

        if (rb == null) { ocupado = false; yield break; }

        // Calcular velocidad con incremento por vida perdida
        Vector3 velocidad = CalcularVelocidad();

        // ── 30%: Lanzar bola señuelo antes de soltar la real ──
        if (Random.value <= 0.30f && prefabBolaSenuelo != null)
            LanzarSenuelo(rb.transform.position, velocidad);

        // ── Descongelar y lanzar la pelota real ──
        rb.isKinematic     = false;
        rb.useGravity      = true;
        rb.linearVelocity        = velocidad;
        rb.angularVelocity = Vector3.zero;

        // ── 20%: Aplicar efecto de color (azul o naranja) ──
        if (Random.value <= probabilidadEfectoColor)
        {
            PelotaBehaviour pb = pelotaObj.GetComponent<PelotaBehaviour>();
            if (pb != null)
            {
                bool esAzul = (Random.value > 0.5f);
                pb.SetEfectoColor(esAzul);
            }
        }

        Debug.Log($"[Boss] Pelota lanzada v={velocidad} | Vida boss:{GameManager.instance?.bossLife}");
        BallWatchdog.instance?.RegistrarGolpe();

        yield return new WaitForSeconds(1f);
        ocupado = false;
    }

    // ════════════════════════════════════════════════════════════════════════
    // SAQUE DEL JEFE
    // ════════════════════════════════════════════════════════════════════════
    public void PrepararSaque(GameObject pelota)
    {
        StartCoroutine(Saque(pelota));
    }

    IEnumerator Saque(GameObject pelota)
    {
        ocupado = true;

        yield return new WaitForSeconds(1.5f);

        if (pelota == null) { ocupado = false; yield break; }

        Rigidbody rb = pelota.GetComponent<Rigidbody>();
        if (rb == null) { ocupado = false; yield break; }

        Vector3 velocidad = CalcularVelocidad();

        rb.isKinematic     = false;
        rb.useGravity      = true;
        rb.linearVelocity        = velocidad;
        rb.angularVelocity = Vector3.zero;

        Debug.Log($"[Boss] Saque lanzado v={velocidad}");
        BallWatchdog.instance?.RegistrarGolpe();

        yield return new WaitForSeconds(1f);
        ocupado = false;
    }

    // ════════════════════════════════════════════════════════════════════════
    // BOLA SEÑUELO
    // ════════════════════════════════════════════════════════════════════════
    void LanzarSenuelo(Vector3 posOrigen, Vector3 velocidadReal)
    {
        GameObject senuelo = Instantiate(prefabBolaSenuelo, posOrigen, Quaternion.identity);
        Rigidbody  srb     = senuelo.GetComponent<Rigidbody>();

        if (srb != null)
        {
            // Dirección opuesta en X, mismo Y y Z
            Vector3 velocidadSenuelo = new Vector3(
                -velocidadReal.x + (velocidadReal.x >= 0 ? -0.8f : 0.8f),
                velocidadReal.y,
                velocidadReal.z
            );
            srb.linearVelocity = velocidadSenuelo;
        }

        // Destruir después de un tiempo
        Destroy(senuelo, tiempoVidaSenuelo);
        Debug.Log("[Boss] Bola señuelo lanzada");
    }

    // ════════════════════════════════════════════════════════════════════════
    // CALCULAR VELOCIDAD PROGRESIVA
    // ════════════════════════════════════════════════════════════════════════
    Vector3 CalcularVelocidad()
    {
        int vidasPerdidas = 11 - (GameManager.instance?.bossLife ?? 11);

        // VelocidadZ se vuelve más negativa (más rápida) con cada vida perdida
        float vz = velocidadZBase - vidasPerdidas * incrementoVelocidadPorVida;
        vz = Mathf.Max(vz, velocidadZMaxima); // no superar el máximo

        // VelocidadY también sube un poco para mantener el arco proporcional
        float vy = velocidadYBase + vidasPerdidas * 0.05f;

        return new Vector3(0f, vy, vz);
    }

    // Llamado por BallSpawner cuando crea nueva pelota
    public void SetPelotaActual(GameObject pelota)
    {
        ocupado = false;
    }
}