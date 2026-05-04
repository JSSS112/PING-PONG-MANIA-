using UnityEngine;
using System.Collections;

/// <summary>
/// BOSS IA — versión MVP simplificada
/// - Estático (solo decoración, no se mueve)
/// - Lanza la pelota SIEMPRE con la misma velocidad fija + leve variación en X
/// - Sin probabilidades, sin efectos de color, sin bola señuelo
/// - Cuando la pelota llega a su zona → congela 0.1s → lanza con vector fijo
/// - El saque del jefe usa exactamente el mismo vector
/// </summary>
public class BossAI : MonoBehaviour
{
    [Header("Velocidad fija del tiro")]
    [Tooltip("Velocidad en Z hacia el jugador — negativo. Más bajo = más suave.")]
    public float velocidadZ = -2.5f;

    [Tooltip("Velocidad en Y para el arco — positivo. Más alto = arco más alto, pasa la red.")]
    public float velocidadY = 5.0f;

    [Tooltip("Variación máxima en X (aleatoria, +/-) — para que no caiga siempre exacto en el mismo punto")]
    public float variacionX = 0.25f;

    [Header("Saque")]
    [Tooltip("Segundos de espera antes de que el jefe lance el saque")]
    public float delaySaque = 1.5f;

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

        ocupado = true;
        StartCoroutine(LanzarPelota(rb));
    }

    // ════════════════════════════════════════════════════════════════════════
    // LANZAR PELOTA — vector fijo
    // ════════════════════════════════════════════════════════════════════════
    IEnumerator LanzarPelota(Rigidbody rb)
    {
        if (rb == null) { ocupado = false; yield break; }

        rb.isKinematic = true;
        rb.useGravity  = false;

        yield return new WaitForSeconds(0.1f);

        if (rb == null) { ocupado = false; yield break; }

        Vector3 v = CalcularVelocidad();

        rb.isKinematic     = false;
        rb.useGravity      = true;
        rb.linearVelocity  = v;
        rb.angularVelocity = Vector3.zero;

        Debug.Log($"[OASIS][Boss] Pelota lanzada v={v}");
        if (GameManager.instance != null) GameManager.instance.RegistrarGolpeRaqueta();
        else BallWatchdog.instance?.RegistrarGolpe();

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

        yield return new WaitForSeconds(delaySaque);

        if (pelota == null) { ocupado = false; yield break; }

        Rigidbody rb = pelota.GetComponent<Rigidbody>();
        if (rb == null) { ocupado = false; yield break; }

        Vector3 v = CalcularVelocidad();

        rb.isKinematic     = false;
        rb.useGravity      = true;
        rb.linearVelocity  = v;
        rb.angularVelocity = Vector3.zero;

        Debug.Log($"[OASIS][Boss] Saque lanzado v={v}");
        if (GameManager.instance != null) GameManager.instance.RegistrarGolpeRaqueta();
        else BallWatchdog.instance?.RegistrarGolpe();

        yield return new WaitForSeconds(1f);
        ocupado = false;
    }

    // ════════════════════════════════════════════════════════════════════════
    Vector3 CalcularVelocidad()
    {
        float vx = Random.Range(-variacionX, variacionX);
        return new Vector3(vx, velocidadY, velocidadZ);
    }

    // Llamado por BallSpawner cuando crea nueva pelota
    public void SetPelotaActual(GameObject pelota)
    {
        ocupado = false;
    }
}
