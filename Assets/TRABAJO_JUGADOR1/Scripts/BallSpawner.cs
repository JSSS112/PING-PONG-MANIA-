using UnityEngine;
using System.Collections;

public class BallSpawner : MonoBehaviour
{
    [Header("Prefab pelota (Rigidbody + Tag Ball + OVR Grabbable + PelotaBehaviour)")]
    public GameObject ballPrefab;

    [Header("Puntos de saque")]
    public Transform posicionSaqueJefe;
    public Transform posicionSaqueJugador;

    [Header("BossAI")]
    public BossAI bossAI;

    private GameObject pelotaActual;

    // ════════════════════════════════════════════════════════════════════════
    public void SpawnBall(bool jefeSaca) => StartCoroutine(DoSpawn(jefeSaca));

    public void DestruirPelotaActual()
    {
        if (pelotaActual != null) { Destroy(pelotaActual); pelotaActual = null; }
    }

    public GameObject GetPelotaActual() => pelotaActual;

    // ════════════════════════════════════════════════════════════════════════
    IEnumerator DoSpawn(bool jefeSaca)
    {
        DestruirPelotaActual();
        yield return new WaitForEndOfFrame();

        if (ballPrefab == null) { Debug.LogError("[Spawner] ballPrefab no asignado!"); yield break; }

        Vector3 pos = jefeSaca
            ? (posicionSaqueJefe     != null ? posicionSaqueJefe.position     : new Vector3(0f, -0.30f, -0.25f))
            : (posicionSaqueJugador  != null ? posicionSaqueJugador.position  : new Vector3(0f, -0.30f, -2.70f));

        pelotaActual = Instantiate(ballPrefab, pos, Quaternion.identity);

        Rigidbody rb = pelotaActual.GetComponent<Rigidbody>();
        if (rb == null) { Debug.LogError("[Spawner] Sin Rigidbody en prefab!"); yield break; }

        // Arrancar congelada 1 frame para evitar bugs de física
        rb.isKinematic     = true;
        rb.useGravity      = false;
        rb.linearVelocity        = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        yield return new WaitForEndOfFrame();
        if (pelotaActual == null) yield break;

        if (bossAI != null) bossAI.SetPelotaActual(pelotaActual);

        rb.isKinematic = false;

        if (jefeSaca)
        {
            // Jefe saca: habilitar físicas y dejar que el boss la lance
            rb.useGravity = true;
            bossAI?.PrepararSaque(pelotaActual);
        }
        else
        {
            // Jugador saca: pelota flota quieta hasta que la agarre
            // PelotaBehaviour maneja todo desde aquí
            PelotaBehaviour pb = pelotaActual.GetComponent<PelotaBehaviour>();
            if (pb != null)
            {
                pb.IniciarFlotando();
            }
            else
            {
                // Fallback si no tiene el script: flota 9 segundos igual que antes
                rb.useGravity = false;
                StartCoroutine(ActivarGravedadTardio(rb, 9f));
            }
        }
    }

    IEnumerator ActivarGravedadTardio(Rigidbody rb, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null) rb.useGravity = true;
    }
}