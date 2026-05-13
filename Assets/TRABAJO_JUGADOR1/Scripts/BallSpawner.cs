using UnityEngine;
using System.Collections;

/// <summary>
/// BallSpawner — gestor de pelota actual.
///
/// Flujo:
/// - Saque del JEFE: instancia la pelota en posicionSaqueJefe (kinematic),
///   y le dice explicitamente al BossAI que la saque. El BossAI hace el
///   delay y la lanza hacia el jugador. No depende de que la pelota caiga
///   en el trigger del jefe.
/// - Saque del JUGADOR: no spawnea nada. SistemaDeServicio escucha el
///   gatillo izquierdo del jugador y crea la pelota en su mano.
/// </summary>
public class BallSpawner : MonoBehaviour
{
    [Header("Prefab pelota (Rigidbody + Tag Ball + PelotaBehaviour)")]
    public GameObject ballPrefab;

    [Header("Punto donde el jefe saca")]
    public Transform posicionSaqueJefe;

    [Header("Referencia al jefe (para que ejecute el saque)")]
    public BossAI bossAI;

    private GameObject pelotaActual;

    public void SpawnBall(bool jefeSaca)
    {
        DestruirPelotaActual();
        if (!jefeSaca) return;        // el jugador saca con su gatillo
        StartCoroutine(SpawnSaqueJefe());
    }

    public void DestruirPelotaActual()
    {
        if (pelotaActual != null)
        {
            Destroy(pelotaActual);
            pelotaActual = null;
        }
    }

    public GameObject GetPelotaActual() => pelotaActual;
    public GameObject GetBallPrefab() => ballPrefab;

    IEnumerator SpawnSaqueJefe()
    {
        yield return new WaitForEndOfFrame();
        if (ballPrefab == null)
        {
            Debug.LogError("[BallSpawner] ballPrefab no asignado!");
            yield break;
        }

        Vector3 pos = posicionSaqueJefe != null
            ? posicionSaqueJefe.position
            : new Vector3(0f, 0.4f, 1.5f);

        pelotaActual = Instantiate(ballPrefab, pos, Quaternion.identity);

        Rigidbody rb = pelotaActual.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("[BallSpawner] La pelota no tiene Rigidbody!");
            yield break;
        }

        // Congelar para que no caiga antes del saque.
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Buscar el BossAI si no esta asignado y delegar el saque.
        if (bossAI == null) bossAI = FindFirstObjectByType<BossAI>();
        if (bossAI != null)
        {
            bossAI.SaqueDeJefe(rb);
        }
        else
        {
            Debug.LogError("[BallSpawner] No hay BossAI en la escena — el jefe no puede sacar.");
        }
    }
}
