using System.Collections;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [Header("Ball")]
    public GameObject ballPrefab;

    [Header("Serve Points")]
    public Transform posicionSaqueJefe;
    public Transform posicionSaqueJugador;

    [Header("Boss")]
    public BossAI bossAI;

    [Header("Player Serve Offset")]
    [SerializeField] private Vector3 headRelativeServeOffset = new Vector3(0.22f, -0.12f, 0.42f);

    private GameObject _currentBall;

    public void SpawnBall(bool bossServes)
    {
        StartCoroutine(SpawnRoutine(bossServes));
    }

    public void DestruirPelotaActual()
    {
        if (_currentBall != null)
        {
            Destroy(_currentBall);
            _currentBall = null;
        }
    }

    public GameObject GetPelotaActual()
    {
        return _currentBall;
    }

    private IEnumerator SpawnRoutine(bool bossServes)
    {
        DestruirPelotaActual();
        yield return new WaitForEndOfFrame();

        if (ballPrefab == null)
        {
            Debug.LogError("[Spawner] Missing ball prefab.");
            yield break;
        }

        Vector3 spawnPosition = bossServes ? GetBossServePosition() : GetPlayerServePosition();
        _currentBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);

        Rigidbody rb = _currentBall.GetComponent<Rigidbody>();
        PelotaBehaviour pelota = _currentBall.GetComponent<PelotaBehaviour>();

        if (rb == null || pelota == null)
        {
            Debug.LogError("[Spawner] The ball prefab is missing a Rigidbody or PelotaBehaviour.");
            yield break;
        }

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        bossAI?.SetPelotaActual(_currentBall);

        if (bossServes)
        {
            bossAI?.PrepararSaque(_currentBall);
        }
        else
        {
            pelota.IniciarFlotando(spawnPosition, Quaternion.identity);
        }
    }

    private Vector3 GetBossServePosition()
    {
        if (posicionSaqueJefe != null)
        {
            return posicionSaqueJefe.position;
        }

        return new Vector3(0f, 1.1f, 0.1f);
    }

    private Vector3 GetPlayerServePosition()
    {
        Transform head = Camera.main != null ? Camera.main.transform : null;
        if (head != null)
        {
            Vector3 position = head.position;
            position += head.right * headRelativeServeOffset.x;
            position += Vector3.up * headRelativeServeOffset.y;
            position += head.forward * headRelativeServeOffset.z;
            return position;
        }

        if (posicionSaqueJugador != null)
        {
            return posicionSaqueJugador.position;
        }

        return new Vector3(0f, 1.2f, -1.1f);
    }
}
