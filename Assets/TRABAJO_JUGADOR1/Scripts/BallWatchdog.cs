using UnityEngine;
using System.Collections;

/// <summary>
/// Anti-bug: detecta pelota perdida (fuera del mundo o quieta demasiado tiempo)
/// y delega al GameManager para que decida quien anota.
///
/// NO usa coordenadas mundiales para decidir el ganador — esa decision la
/// toma GameManager.PelotaPerdidaPorWatchdog() en base al ultimo bote
/// detectado por TableBounce, que es la unica fuente de verdad confiable.
/// </summary>
public class BallWatchdog : MonoBehaviour
{
    public static BallWatchdog instance;

    [Header("Limites del mundo")]
    public float yMinimo      = -1.5f;
    public float xLimite      =  3.0f;
    public float zLimiteMas   =  3.0f;
    public float zLimiteMenos = -5.5f;

    [Header("Segundos quieta antes de declarar la pelota perdida")]
    public float tiempoLimite = 4f;
    public float umbralVel    = 0.05f;

    [Header("OBLIGATORIO")]
    public BallSpawner ballSpawner;

    private float    timer  = 0f;
    private bool     activo = false;
    private Coroutine cor;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void RegistrarGolpe() => timer = 0f;

    public void IniciarMonitoreo()
    {
        activo = true;
        timer  = 0f;
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

    IEnumerator Monitor()
    {
        // Gracia inicial para que la pelota tenga tiempo de salir del saque.
        yield return new WaitForSeconds(2f);

        while (activo)
        {
            yield return new WaitForSeconds(0.25f);

            if (GameManager.instance == null || !GameManager.instance.roundActive) continue;

            GameObject p = ballSpawner != null ? ballSpawner.GetPelotaActual() : null;
            if (p == null) p = GameObject.FindWithTag("Ball");

            if (p == null)
            {
                Debug.Log("[Watchdog] Pelota no encontrada → punto al lado del ultimo bote.");
                Perdida();
                yield break;
            }

            Vector3 pos = p.transform.position;

            // Fuera de los limites del mundo
            bool fuera = pos.y < yMinimo
                      || Mathf.Abs(pos.x) > xLimite
                      || pos.z > zLimiteMas
                      || pos.z < zLimiteMenos;
            if (fuera)
            {
                Debug.Log($"[Watchdog] Pelota fuera del mundo en {pos} → declarar perdida.");
                Perdida();
                yield break;
            }

            // Pelota casi quieta demasiado tiempo
            Rigidbody rb = p.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                if (rb.linearVelocity.magnitude < umbralVel)
                {
                    timer += 0.25f;
                    if (timer >= tiempoLimite)
                    {
                        Debug.Log("[Watchdog] Pelota quieta demasiado tiempo → declarar perdida.");
                        Perdida();
                        yield break;
                    }
                }
                else timer = 0f;
            }
        }
    }

    void Perdida()
    {
        activo = false;
        GameManager.instance?.PelotaPerdidaPorWatchdog();
    }
}
