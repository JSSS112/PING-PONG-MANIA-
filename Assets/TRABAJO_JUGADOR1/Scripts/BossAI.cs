using System.Collections;
using UnityEngine;

/// <summary>
/// Boss estatico. Dos comportamientos:
///   - DEVOLUCION: la pelota entra a su trigger -> la congela 0.1s -> la
///     lanza con arco hacia la mesa del jugador.
///   - SAQUE: BallSpawner llama a SaqueDeJefe(rb) cuando le toca al jefe
///     servir; usa la misma logica que la devolucion pero con un delay
///     mas largo para que el jugador vea venir el saque.
/// </summary>
public class BossAI : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Punto al que el boss apunta sus devoluciones. Idealmente el centro de la mitad del jugador en la mesa.")]
    public Transform objetivoJugador;

    [Header("Parametros del tiro")]
    [Tooltip("Magnitud de la componente horizontal en m/s. Sube si la pelota no llega; baja si pasa larga.")]
    public float velocidadHorizontal = 3.5f;

    [Tooltip("Componente vertical en m/s. Da el arco para que pase la red.")]
    public float componenteVertical = 4.5f;

    [Tooltip("Variacion lateral aleatoria en m/s (perpendicular a la direccion al jugador).")]
    public float variacionLateral = 0.4f;

    [Tooltip("Tiempo congelada antes de devolver (en juego).")]
    public float congelado = 0.1f;

    [Tooltip("Tiempo de preparacion antes del SAQUE inicial.")]
    public float delaySaque = 1.2f;

    [Tooltip("Cooldown antes de poder volver a devolver otra pelota.")]
    public float cooldown = 1f;

    private bool ocupado = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball") || ocupado) return;
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        ocupado = true;
        StartCoroutine(LanzarPelota(rb, congelado));
    }

    /// <summary>
    /// Saque inicial del jefe. Lo invoca BallSpawner cuando la ronda
    /// asigna el saque al boss. La pelota debe llegar congelada o se
    /// congela aca mismo.
    /// </summary>
    public void SaqueDeJefe(Rigidbody rb)
    {
        if (rb == null) return;
        ocupado = true;
        StartCoroutine(LanzarPelota(rb, delaySaque));
    }

    IEnumerator LanzarPelota(Rigidbody rb, float espera)
    {
        // Congelar momentaneamente.
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        yield return new WaitForSeconds(espera);

        // Si la pelota fue destruida durante el congelado, abortar.
        if (rb == null)
        {
            ocupado = false;
            yield break;
        }

        // Calcular direccion horizontal hacia el jugador (ignorando Y).
        Vector3 direccionHorizontal;
        if (objetivoJugador != null)
        {
            Vector3 hacia = objetivoJugador.position - rb.position;
            hacia.y = 0f;
            direccionHorizontal = hacia.sqrMagnitude > 0.001f ? hacia.normalized : Vector3.back;
        }
        else
        {
            // Fallback: -Z mundial es la mitad del jugador en la mayoria de mesas.
            direccionHorizontal = Vector3.back;
        }

        // Lateral perpendicular a la direccion al jugador.
        Vector3 lateral = Vector3.Cross(Vector3.up, direccionHorizontal);
        float varLat = Random.Range(-variacionLateral, variacionLateral);

        Vector3 v = direccionHorizontal * velocidadHorizontal
                  + lateral * varLat
                  + Vector3.up * componenteVertical;

        // Devolver al mundo fisico.
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = v;
        rb.angularVelocity = Vector3.zero;

        // Avisar al GameManager para resetear el contador de doble-rebote
        // y al watchdog para resetear su timer de inactividad.
        if (GameManager.instance != null) GameManager.instance.RegistrarGolpeRaqueta();
        else BallWatchdog.instance?.RegistrarGolpe();

        yield return new WaitForSeconds(cooldown);
        ocupado = false;
    }
}
