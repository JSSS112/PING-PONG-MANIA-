using UnityEngine;
using System.Collections;

public class BallSpawner : MonoBehaviour
{
    [Header("Prefab pelota (Rigidbody + Tag Ball + OVR Grabbable)")]
    public GameObject ballPrefab;

    [Header("Puntos de saque - GameObjects vacios en la escena")]
    public Transform posicionSaqueJefe;
    public Transform posicionSaqueJugador;

    [Header("Referencia al BossAI")]
    public BossAI bossAI;

    private GameObject pelotaActual;

    // ════════════════════════════════════════════════════════════════════════
    public void SpawnBall(bool jefeSaca) => StartCoroutine(DoSpawn(jefeSaca));

    public void DestruirPelotaActual()
    {
        if (pelotaActual != null)
        {
            Destroy(pelotaActual);
            pelotaActual = null;
        }
    }

    public GameObject GetPelotaActual() => pelotaActual;

    // ════════════════════════════════════════════════════════════════════════
    IEnumerator DoSpawn(bool jefeSaca)
    {
        DestruirPelotaActual();
        yield return new WaitForEndOfFrame();

        if (ballPrefab == null)
        {
            Debug.LogError("[Spawner] ballPrefab no asignado!");
            yield break;
        }

        // Posicion de spawn segun quien saca
        Vector3 pos;
        if (jefeSaca)
            pos = posicionSaqueJefe    != null ? posicionSaqueJefe.position    : new Vector3(0f, -0.30f, -0.25f);
        else
            pos = posicionSaqueJugador != null ? posicionSaqueJugador.position : new Vector3(0f, -0.30f, -2.70f);

        pelotaActual = Instantiate(ballPrefab, pos, Quaternion.identity);

        Rigidbody rb = pelotaActual.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("[Spawner] El prefab no tiene Rigidbody!");
            yield break;
        }

        // Arrancar congelada 1 frame para evitar bugs
        rb.isKinematic = true;
        rb.useGravity  = false;
        rb.linearVelocity = rb.angularVelocity = Vector3.zero;

        yield return new WaitForEndOfFrame();
        if (pelotaActual == null) yield break;

        // Notificar al boss
        if (bossAI != null) bossAI.SetPelotaActual(pelotaActual);

        rb.isKinematic = false;

        if (jefeSaca)
        {
            rb.useGravity = true;
            bossAI?.PrepararSaque(pelotaActual);
        }
        else
        {
            // Pelota del jugador: flota para que la agarre
            rb.useGravity = false;
            StartCoroutine(ActivarGravedadTardio(rb, 9f));
        }
    }

    IEnumerator ActivarGravedadTardio(Rigidbody rb, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null) rb.useGravity = true;
    }
}