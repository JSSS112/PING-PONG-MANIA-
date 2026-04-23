using UnityEngine;
using System.Collections;

/// <summary>
/// BOSS = lanzador de pelota. Nada mas.
/// Cuando la pelota lo toca → la manda a velocidad fija hacia el lado del jugador.
/// Usa rb.velocity directo (no AddForce) para maxima precision y predictibilidad.
/// </summary>
public class BossAI : MonoBehaviour
{
    [Header("Velocidad fija del tiro (ajustar en Inspector hasta que llegue bien)")]
    [Tooltip("Velocidad en Z - NEGATIVO para ir hacia el jugador")]
    public float velocidadZ = -2.5f;

    [Tooltip("Velocidad en Y - positivo para que salte por encima de la red")]
    public float velocidadY = 2.2f;

    [Tooltip("Velocidad en X - dejar en 0 para ir siempre al centro")]
    public float velocidadX = 0f;

    // Estado interno
    private bool ocupado = false;

    // ════════════════════════════════════════════════════════════════════════
    // DETECCION: pelota toca al jefe
    // ════════════════════════════════════════════════════════════════════════
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;
        if (ocupado)                   return;
        if (GameManager.instance == null || !GameManager.instance.roundActive) return;

        // Solo reaccionar a la pelota activa del spawner
        BallSpawner spawner = FindFirstObjectByType<BallSpawner>();
        if (spawner != null && spawner.GetPelotaActual() != other.gameObject) return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        ocupado = true;
        StartCoroutine(LanzarPelota(rb));
    }

    // ════════════════════════════════════════════════════════════════════════
    // LANZAR A PUNTO FIJO
    // ════════════════════════════════════════════════════════════════════════
    IEnumerator LanzarPelota(Rigidbody rb)
    {
        if (rb == null) { ocupado = false; yield break; }

        // Congelar la pelota
        rb.isKinematic = true;
        rb.useGravity  = false;

        yield return new WaitForSeconds(0.1f);

        if (rb == null) { ocupado = false; yield break; }

        // Descongelar y asignar velocidad directa (sin AddForce, sin masas)
        rb.isKinematic = false;
        rb.useGravity  = true;
        rb.linearVelocity        = new Vector3(velocidadX, velocidadY, velocidadZ);
        rb.angularVelocity = Vector3.zero;

        Debug.Log($"[Boss] Pelota lanzada con velocidad: {rb.linearVelocity}");
        BallWatchdog.instance?.RegistrarGolpe();

        yield return new WaitForSeconds(1f);
        ocupado = false;
    }

    // ════════════════════════════════════════════════════════════════════════
    // SAQUE DEL JEFE (llamado por BallSpawner)
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

        rb.isKinematic = false;
        rb.useGravity  = true;
        rb.linearVelocity        = new Vector3(velocidadX, velocidadY, velocidadZ);
        rb.angularVelocity = Vector3.zero;

        Debug.Log($"[Boss] Saque lanzado con velocidad: {rb.linearVelocity}");
        BallWatchdog.instance?.RegistrarGolpe();

        yield return new WaitForSeconds(1f);
        ocupado = false;
    }

    // Llamado por BallSpawner al crear nueva pelota
    public void SetPelotaActual(GameObject pelota)
    {
        ocupado = false;
    }
}