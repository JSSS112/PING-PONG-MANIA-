using UnityEngine;
using System.Collections;

/// <summary>
/// BallSpawner — gestiona la pelota actual del juego.
///
/// MVP nuevo:
/// - Saque del JEFE: spawnea pelota en posicionSaqueJefe y se la pasa al BossAI.
/// - Saque del JUGADOR: NO spawnea nada. Avisa a SistemaDeServicio para que
///   espere el trigger izquierdo y cree la pelota en la mano del jugador.
///
/// SistemaDeServicio llama a RegistrarPelotaJugador() cuando la pelota es
/// creada en la mano izquierda, así el GameManager / BossAI / Watchdog la
/// reconocen como "pelota actual".
/// </summary>
public class BallSpawner : MonoBehaviour
{
    [Header("Prefab pelota (Rigidbody + Tag Ball + PelotaBehaviour)")]
    public GameObject ballPrefab;

    [Header("Punto de saque del jefe")]
    public Transform posicionSaqueJefe;

    [Header("Referencias")]
    public BossAI bossAI;
    public SistemaDeServicio sistemaDeServicio;

    private GameObject pelotaActual;

    // ════════════════════════════════════════════════════════════════════════
    public void SpawnBall(bool jefeSaca)
    {
        if (jefeSaca) StartCoroutine(SpawnSaqueJefe());
        else          IniciarSaqueJugador();
    }

    public void DestruirPelotaActual()
    {
        if (pelotaActual != null) { Destroy(pelotaActual); pelotaActual = null; }

        // Cancelar cualquier saque pendiente del jugador
        if (sistemaDeServicio != null) sistemaDeServicio.CancelarSaque();
    }

    public GameObject GetPelotaActual() => pelotaActual;

    /// <summary>Llamado por SistemaDeServicio cuando crea/suelta la pelota en la mano izquierda.</summary>
    public void RegistrarPelotaJugador(GameObject pelota)
    {
        pelotaActual = pelota;
        if (bossAI != null) bossAI.SetPelotaActual(pelota);
    }

    public GameObject GetBallPrefab() => ballPrefab;

    // ════════════════════════════════════════════════════════════════════════
    IEnumerator SpawnSaqueJefe()
    {
        DestruirPelotaActual();
        yield return new WaitForEndOfFrame();

        if (ballPrefab == null) { Debug.LogError("[OASIS][Spawner] ballPrefab no asignado!"); yield break; }

        Vector3 pos = (posicionSaqueJefe != null)
            ? posicionSaqueJefe.position
            : new Vector3(0f, -0.30f, -0.25f);

        pelotaActual = Instantiate(ballPrefab, pos, Quaternion.identity);

        Rigidbody rb = pelotaActual.GetComponent<Rigidbody>();
        if (rb == null) { Debug.LogError("[OASIS][Spawner] Sin Rigidbody en prefab!"); yield break; }

        rb.isKinematic     = true;
        rb.useGravity      = false;
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        yield return new WaitForEndOfFrame();
        if (pelotaActual == null) yield break;

        if (bossAI != null) bossAI.SetPelotaActual(pelotaActual);

        rb.isKinematic = false;
        rb.useGravity  = true;

        bossAI?.PrepararSaque(pelotaActual);
    }

    // ════════════════════════════════════════════════════════════════════════
    void IniciarSaqueJugador()
    {
        // Aseguramos que no quede ninguna pelota previa
        if (pelotaActual != null) { Destroy(pelotaActual); pelotaActual = null; }

        // Fallback: si no se asignó en el Inspector, lo buscamos en escena
        if (sistemaDeServicio == null)
            sistemaDeServicio = FindFirstObjectByType<SistemaDeServicio>();

        if (sistemaDeServicio == null)
        {
            Debug.LogError("[OASIS][Spawner] SistemaDeServicio no encontrado — agrega un GameObject con el script o asigna en el Inspector.");
            return;
        }

        sistemaDeServicio.IniciarSaqueJugador(this);
    }
}
