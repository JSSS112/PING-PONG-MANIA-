using System.Collections;
using UnityEngine;

public class BossAI : MonoBehaviour
{
    [Header("Shot Pace")]
    [SerializeField] private float baseHorizontalSpeed = 4.25f;
    [SerializeField] private float extraSpeedPerBossLifeLost = 0.14f;
    [SerializeField] private float maxHorizontalSpeed = 6.6f;
    [SerializeField] private float minimumFlightTime = 0.55f;
    [SerializeField] private float maximumFlightTime = 0.9f;
    [SerializeField] private float serveFirstBounceMinTime = 0.24f;
    [SerializeField] private float serveFirstBounceMaxTime = 0.36f;

    [Header("Boss Animation")]
    [SerializeField] private float catchBlendDuration = 0.14f;
    [SerializeField] private float serveWindupDuration = 0.55f;
    [SerializeField] private Vector3 holdOffset = new Vector3(0f, 0.3f, -0.14f);
    [SerializeField] private Vector3 releaseOffset = new Vector3(0f, 0.18f, -0.08f);

    [Header("Attacks")]
    [SerializeField] private GameObject prefabBolaSenuelo;
    [SerializeField] private float tiempoVidaSenuelo = 2.1f;
    [Range(0f, 1f)] public float probabilidadEfectoColor = 0.18f;
    [Range(0f, 1f)] [SerializeField] private float probabilidadSenuelo = 0.24f;

    private BallSpawner _spawner;
    private TableBounce _bossHalf;
    private TableBounce _playerHalf;
    private bool _busy;

    private void Start()
    {
        _spawner = FindFirstObjectByType<BallSpawner>();

        TableBounce[] halves = FindObjectsByType<TableBounce>(FindObjectsSortMode.None);
        foreach (TableBounce half in halves)
        {
            if (half.esMitadJefe)
            {
                _bossHalf = half;
            }
            else
            {
                _playerHalf = half;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball") || _busy)
        {
            return;
        }

        if (GameManager.instance == null || !GameManager.instance.roundActive || GameManager.instance.waitingForPlayerServeHit)
        {
            return;
        }

        if (_spawner != null && _spawner.GetPelotaActual() != other.gameObject)
        {
            return;
        }

        if (GameManager.instance != null && !GameManager.instance.CanBossStrikeBall())
        {
            return;
        }

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null || rb.linearVelocity.z <= 0f)
        {
            return;
        }

        StartCoroutine(CatchAndReturnRoutine(other.gameObject, rb));
    }

    public void PrepararSaque(GameObject pelota)
    {
        if (!_busy)
        {
            StartCoroutine(BossServeRoutine(pelota));
        }
    }

    public void SetPelotaActual(GameObject pelota)
    {
        _busy = false;
    }

    private IEnumerator BossServeRoutine(GameObject ball)
    {
        if (ball == null)
        {
            yield break;
        }

        _busy = true;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null)
        {
            _busy = false;
            yield break;
        }

        yield return AnimateBallToHold(ball, rb, serveWindupDuration);
        if (ball == null || rb == null)
        {
            _busy = false;
            yield break;
        }

        Vector3 velocity = BuildServeVelocity(GetReleasePosition(), ChoosePlayerTarget());
        LaunchBall(ball, rb, velocity);
        GameManager.instance?.RegisterBossShotReleased();

        yield return new WaitForSeconds(0.08f);
        _busy = false;
    }

    private IEnumerator CatchAndReturnRoutine(GameObject ball, Rigidbody rb)
    {
        _busy = true;
        ball.GetComponent<PelotaBehaviour>()?.ResetarEfectoColor();

        yield return AnimateBallToHold(ball, rb, catchBlendDuration);
        if (ball == null || rb == null || GameManager.instance == null || !GameManager.instance.roundActive)
        {
            _busy = false;
            yield break;
        }

        Vector3 velocity = BuildShotVelocity(GetReleasePosition(), ChoosePlayerTarget());
        LaunchBall(ball, rb, velocity);
        GameManager.instance.RegisterBossShotReleased();

        if (Random.value <= probabilidadSenuelo && prefabBolaSenuelo != null)
        {
            LaunchDecoy(GetReleasePosition(), velocity);
        }

        if (Random.value <= probabilidadEfectoColor)
        {
            PelotaBehaviour pb = ball.GetComponent<PelotaBehaviour>();
            if (pb != null)
            {
                pb.SetEfectoColor(Random.value > 0.5f);
            }
        }

        yield return new WaitForSeconds(0.08f);
        _busy = false;
    }

    private IEnumerator AnimateBallToHold(GameObject ball, Rigidbody rb, float duration)
    {
        Vector3 start = ball.transform.position;
        Vector3 hold = GetHoldPosition();
        Vector3 peak = Vector3.Lerp(start, hold, 0.5f) + Vector3.up * 0.1f;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Vector3 a = Vector3.Lerp(start, peak, eased);
            Vector3 b = Vector3.Lerp(peak, hold, eased);
            ball.transform.position = Vector3.Lerp(a, b, eased);
            yield return null;
        }

        ball.transform.position = hold;
    }

    private void LaunchBall(GameObject ball, Rigidbody rb, Vector3 velocity)
    {
        ball.transform.position = GetReleasePosition();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.zero;
        BallWatchdog.instance?.RegistrarGolpe();
    }

    private Vector3 BuildShotVelocity(Vector3 origin, Vector3 bounceTarget)
    {
        int bossLivesLost = 0;
        if (GameManager.instance != null)
        {
            bossLivesLost = Mathf.Max(0, GameManager.instance.bossMaxLife - GameManager.instance.bossLife);
        }

        float horizontalSpeed = Mathf.Clamp(
            baseHorizontalSpeed + bossLivesLost * extraSpeedPerBossLifeLost + Random.Range(-0.15f, 0.35f),
            3.75f,
            maxHorizontalSpeed);

        Vector3 flatDelta = new Vector3(bounceTarget.x - origin.x, 0f, bounceTarget.z - origin.z);
        float time = flatDelta.magnitude / horizontalSpeed;
        time = Mathf.Clamp(time + Random.Range(-0.04f, 0.08f), minimumFlightTime, maximumFlightTime);

        Vector3 velocity = flatDelta / Mathf.Max(0.01f, time);
        velocity.y = (bounceTarget.y - origin.y - 0.5f * Physics.gravity.y * time * time) / Mathf.Max(0.01f, time);
        return velocity;
    }

    private Vector3 ChoosePlayerTarget()
    {
        Bounds fallback = new Bounds(new Vector3(0f, 0.66f, -1.45f), new Vector3(0.8f, 0.01f, 0.65f));
        Bounds bounds = _playerHalf != null && _playerHalf.TryGetComponent(out Collider col)
            ? col.bounds
            : fallback;

        float lifePressure = 0f;
        if (GameManager.instance != null)
        {
            float maxLife = Mathf.Max(1f, GameManager.instance.bossMaxLife);
            lifePressure = Mathf.InverseLerp(maxLife, Mathf.Max(3f, maxLife * 0.3f), GameManager.instance.bossLife);
        }

        float lateralBias = Random.value < 0.5f ? -1f : 1f;
        float edgeFactor = Mathf.Lerp(0.12f, 0.33f, lifePressure);
        float centerX = bounds.center.x + lateralBias * bounds.extents.x * Random.Range(edgeFactor, 0.85f);
        float z = Mathf.Lerp(bounds.min.z + 0.08f, bounds.max.z - 0.08f, Random.value);
        return new Vector3(centerX, bounds.center.y + 0.02f, z);
    }

    private Vector3 GetHoldPosition()
    {
        return transform.TransformPoint(holdOffset);
    }

    private Vector3 GetReleasePosition()
    {
        return transform.TransformPoint(releaseOffset);
    }

    private void LaunchDecoy(Vector3 origin, Vector3 realVelocity)
    {
        GameObject decoy = Instantiate(prefabBolaSenuelo, origin, Quaternion.identity);
        Rigidbody decoyRb = decoy.GetComponent<Rigidbody>();
        if (decoyRb != null)
        {
            Vector3 mirrored = new Vector3(-realVelocity.x + (realVelocity.x >= 0f ? -0.65f : 0.65f), realVelocity.y, realVelocity.z);
            decoyRb.linearVelocity = mirrored;
        }

        Destroy(decoy, tiempoVidaSenuelo);
    }

    private Vector3 BuildServeVelocity(Vector3 origin, Vector3 receiverBounceTarget)
    {
        Bounds serverBounds = GetHalfBounds(_bossHalf, new Bounds(new Vector3(0f, 0.66f, 0.7f), new Vector3(0.8f, 0.01f, 0.65f)));
        float tableY = serverBounds.center.y + 0.02f;
        float gravity = Mathf.Abs(Physics.gravity.y);

        for (int i = 0; i < 10; i++)
        {
            float firstBounceTime = Random.Range(serveFirstBounceMinTime, serveFirstBounceMaxTime);
            float initialVy = (tableY - origin.y + 0.5f * gravity * firstBounceTime * firstBounceTime)
                            / Mathf.Max(0.01f, firstBounceTime);
            float postBounceVy = gravity * firstBounceTime - initialVy;
            if (postBounceVy <= 0.15f)
            {
                continue;
            }

            float secondBounceTime = 2f * postBounceVy / gravity;
            float blend = firstBounceTime / Mathf.Max(0.01f, firstBounceTime + secondBounceTime);
            Vector3 firstBounce = new Vector3(
                Mathf.Lerp(origin.x, receiverBounceTarget.x, blend),
                tableY,
                Mathf.Lerp(origin.z, receiverBounceTarget.z, blend));

            if (!IsInsideHalf(serverBounds, firstBounce, 0.06f))
            {
                continue;
            }

            Vector3 horizontalVelocity = new Vector3(
                (firstBounce.x - origin.x) / firstBounceTime,
                0f,
                (firstBounce.z - origin.z) / firstBounceTime);

            return new Vector3(horizontalVelocity.x, initialVy, horizontalVelocity.z);
        }

        return BuildShotVelocity(origin, receiverBounceTarget);
    }

    private static Bounds GetHalfBounds(TableBounce half, Bounds fallback)
    {
        return half != null && half.TryGetComponent(out Collider collider)
            ? collider.bounds
            : fallback;
    }

    private static bool IsInsideHalf(Bounds bounds, Vector3 point, float edgePadding)
    {
        return point.x > bounds.min.x + edgePadding
            && point.x < bounds.max.x - edgePadding
            && point.z > bounds.min.z + edgePadding
            && point.z < bounds.max.z - edgePadding;
    }
}
