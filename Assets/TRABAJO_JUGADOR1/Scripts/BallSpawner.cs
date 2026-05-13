using UnityEngine;
using System.Collections;

/// <summary>
/// BallSpawner reducido — compatibilidad con GameManager y BallWatchdog.
///
/// Flujo nuevo (simple):
/// - Saque del JEFE: instancia una pelota en posicionSaqueJefe y la deja caer.
///   El collider trigger del jefe la captura via BossAI.OnTriggerEnter y la
///   devuelve hacia el jugador.
/// - Saque del JUGADOR: no hace nada. El SistemaDeServicio escucha el gatillo
///   izquierdo del jugador de forma continua y spawnea la pelota en su mano.
///
/// El GameManager sigue llamando SpawnBall(bool) y DestruirPelotaActual()
/// como antes. GetPelotaActual() se mantiene para el BallWatchdog.
/// </summary>
public class BallSpawner : MonoBehaviour
{
    [Header("Prefab pelota (Rigidbody + Tag Ball + PelotaBehaviour)")]
    public GameObject ballPrefab;

    [Header("Punto donde el jefe deja caer la pelota para sacar")]
    public Transform posicionSaqueJefe;

    private GameObject pelotaActual;

    public void SpawnBall(bool jefeSaca)
    {
        DestruirPelotaActual();
        if (!jefeSaca) return;       // saque del jugador lo maneja SistemaDeServicio
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
        // La pelota cae libre — el trigger del BossAI la captura y la devuelve.
    }
}
