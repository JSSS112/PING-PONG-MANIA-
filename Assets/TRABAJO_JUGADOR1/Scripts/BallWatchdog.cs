using UnityEngine;
using System.Collections;

/// <summary>
/// Anti-bug: detecta pelota perdida o atascada y da el punto al jefe.
/// Ponlo en un GameObject vacio en la escena.
/// OBLIGATORIO: asignar el campo Ball Spawner en el Inspector.
/// </summary>
public class BallWatchdog : MonoBehaviour
{
    public static BallWatchdog instance;

    [Header("Limites del mundo - pelota fuera = punto perdido")]
    public float yMinimo      = -1.5f;
    public float xLimite      =  3.0f;
    public float zLimiteMas   =  1.0f;
    public float zLimiteMenos = -5.5f;

    [Header("Segundos quieta antes de dar punto perdido")]
    public float tiempoLimite = 6f;
    public float umbralVel    = 0.04f;

    [Header("Z de la red — pelota con z > netZ está en lado del JEFE")]
    public float netZ = 1.5f;

    [Header("OBLIGATORIO")]
    public BallSpawner ballSpawner;

    private float    timer  = 0f;
    private bool     activo = false;
    private Coroutine cor;
    private Vector3  ultimaPosPelota;
    private bool     hayUltimaPos = false;

    // ════════════════════════════════════════════════════════════════════════
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // ════════════════════════════════════════════════════════════════════════
    public void RegistrarGolpe()    => timer = 0f;

    public void IniciarMonitoreo()
    {
        activo = true;
        timer  = 0f;
        hayUltimaPos = false;
        if (cor != null) StopCoroutine(cor);
        cor = StartCoroutine(Monitor());
    }

    public void DetenerMonitoreo()
    {
        activo = false;
        if (cor != null)
        {
            StopCoroutine(cor);
            cor = null;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    IEnumerator Monitor()
    {
        // Gracia inicial
        yield return new WaitForSeconds(2f);

        while (activo)
        {
            yield return new WaitForSeconds(0.3f);

            if (GameManager.instance == null || !GameManager.instance.roundActive) continue;

            // Buscar pelota
            GameObject p = ballSpawner != null ? ballSpawner.GetPelotaActual() : null;
            if (p == null) p = GameObject.FindWithTag("Ball");

            if (p == null)
            {
                Debug.Log("[Watchdog] Pelota no encontrada, reiniciando...");
                PelotaPerdida();
                yield break;
            }

            Vector3 pos = p.transform.position;
            ultimaPosPelota = pos;
            hayUltimaPos    = true;

            // Verificar limites
            bool fuera = pos.y < yMinimo
                      || Mathf.Abs(pos.x) > xLimite
                      || pos.z > zLimiteMas
                      || pos.z < zLimiteMenos;

            if (fuera)
            {
                Debug.Log($"[Watchdog] Pelota fuera de limites: {pos}");
                PelotaPerdida();
                yield break;
            }

            // Verificar quietud
            Rigidbody rb = p.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (rb.linearVelocity.magnitude < umbralVel)
                {
                    timer += 0.3f;
                    if (timer >= tiempoLimite)
                    {
                        Debug.Log("[Watchdog] Pelota quieta demasiado tiempo, reiniciando...");
                        PelotaPerdida();
                        yield break;
                    }
                }
                else
                {
                    timer = 0f;
                }
            }
        }
    }

    void PelotaPerdida()
    {
        activo = false;
        if (GameManager.instance == null || !GameManager.instance.roundActive) return;

        // Si la pelota quedó del lado del JEFE (z > netZ), el jefe falló → punto al jugador.
        // En cualquier otro caso (lado jugador o desconocido) → punto al jefe.
        if (hayUltimaPos && ultimaPosPelota.z > netZ)
        {
            Debug.Log($"[Watchdog] Pelota perdida en lado JEFE (z={ultimaPosPelota.z:F2}) → punto al jugador.");
            GameManager.instance.JugadorAnota();
        }
        else
        {
            Debug.Log($"[Watchdog] Pelota perdida en lado JUGADOR → punto al jefe.");
            GameManager.instance.JefeAnota();
        }
    }
}