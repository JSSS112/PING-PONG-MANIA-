using System.Collections;
using UnityEngine;

/// <summary>
/// Boss estatico. Al entrar la pelota en su trigger:
///   1) La congela 0.1 s.
///   2) La lanza con arco apuntando hacia la mesa del jugador,
///      con leve variacion lateral para que no sea totalmente predecible.
/// Asi la devolucion casi siempre cae en el lado del jugador.
/// </summary>
public class BossAI : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Punto al que el boss apunta sus devoluciones. Idealmente el centro de la mitad del jugador en la mesa.")]
    public Transform objetivoJugador;

    [Header("Parametros del saque")]
    [Tooltip("Magnitud de la componente horizontal en m/s. Sube si la pelota no llega; baja si pasa larga.")]
    public float velocidadHorizontal = 3.5f;

    [Tooltip("Componente vertical en m/s. Da el arco para que pase la red.")]
    public float componenteVertical = 4.5f;

    [Tooltip("Variacion lateral aleatoria en m/s (perpendicular a la direccion al jugador, no eje X mundial).")]
    public float variacionLateral = 0.4f;

    [Tooltip("Tiempo congelada antes de salir.")]
    public float congelado = 0.1f;

    [Tooltip("Cooldown antes de poder volver a devolver otra pelota.")]
    public float cooldown = 1f;

    private bool ocupado = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball") || ocupado) return;
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        ocupado = true;
        StartCoroutine(LanzarPelota(rb));
    }

    IEnumerator LanzarPelota(Rigidbody rb)
    {
        // Congelar momentaneamente.
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        yield return new WaitForSeconds(congelado);

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
            direccionHorizontal = hacia.sqrMagnitude > 0.001f ? hacia.normalized : -transform.forward;
        }
        else
        {
            direccionHorizontal = -transform.forward; // fallback si no hay objetivo asignado
        }

        // Lateral perpendicular a la direccion al jugador (no eje X mundial).
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

        yield return new WaitForSeconds(cooldown);
        ocupado = false;
    }
}
