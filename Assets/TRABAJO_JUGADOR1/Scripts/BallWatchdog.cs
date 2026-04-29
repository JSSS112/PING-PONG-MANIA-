using System.Collections;
using UnityEngine;

public class BallWatchdog : MonoBehaviour
{
    public static BallWatchdog instance;

    [Header("World Limits")]
    public float yMinimo = -1.25f;
    public float xLimite = 3.2f;
    public float zLimiteMas = 2.4f;
    public float zLimiteMenos = -5.8f;

    [Header("Idle Timeout")]
    public float tiempoLimite = 4.5f;
    public float umbralVel = 0.08f;

    [Header("References")]
    public BallSpawner ballSpawner;

    public bool IsMonitoring => _monitoring;

    private float _idleTimer;
    private bool _monitoring;
    private Coroutine _monitorRoutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegistrarGolpe()
    {
        _idleTimer = 0f;
    }

    public void IniciarMonitoreo()
    {
        _idleTimer = 0f;
        _monitoring = true;

        if (_monitorRoutine != null)
        {
            StopCoroutine(_monitorRoutine);
        }

        _monitorRoutine = StartCoroutine(MonitorRoutine());
    }

    public void DetenerMonitoreo()
    {
        _monitoring = false;

        if (_monitorRoutine != null)
        {
            StopCoroutine(_monitorRoutine);
            _monitorRoutine = null;
        }
    }

    private IEnumerator MonitorRoutine()
    {
        while (_monitoring)
        {
            yield return new WaitForSeconds(0.2f);

            if (GameManager.instance == null || !GameManager.instance.roundActive)
            {
                continue;
            }

            GameObject ball = ballSpawner != null ? ballSpawner.GetPelotaActual() : null;
            if (ball == null)
            {
                continue;
            }

            Vector3 pos = ball.transform.position;
            bool outsideBounds =
                pos.y < yMinimo ||
                Mathf.Abs(pos.x) > xLimite ||
                pos.z > zLimiteMas ||
                pos.z < zLimiteMenos;

            if (outsideBounds)
            {
                _monitoring = false;
                GameManager.instance.ReportBallLost(pos);
                yield break;
            }

            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb == null)
            {
                continue;
            }

            if (rb.linearVelocity.magnitude < umbralVel)
            {
                _idleTimer += 0.2f;
                if (_idleTimer >= tiempoLimite)
                {
                    _monitoring = false;
                    GameManager.instance.ReportBallLost(pos);
                    yield break;
                }
            }
            else
            {
                _idleTimer = 0f;
            }
        }
    }
}
