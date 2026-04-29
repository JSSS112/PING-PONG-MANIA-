using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerPaddleController : MonoBehaviour
{
    [Header("Return Tuning")]
    [SerializeField] private float velocityTransfer = 0.9f;
    [SerializeField] private float forwardAssist = 2.4f;
    [SerializeField] private float upwardAssist = 0.85f;
    [SerializeField] private float minimumReturnSpeed = 3.8f;
    [SerializeField] private float maximumReturnSpeed = 8.6f;
    [SerializeField] private float hitCooldown = 0.06f;

    [Header("Collider Tuning")]
    [SerializeField] private string faceChildName = "Cara_raqueta";
    [SerializeField] private string handleChildName = "Mango_mango_mango";

    private Rigidbody _rb;
    private BossAI _boss;
    private Transform _strikeSurface;
    private Vector3 _lastPosition;
    private Vector3 _linearVelocity;
    private float _lastHitAt = -10f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _boss = FindFirstObjectByType<BossAI>();
        _strikeSurface = transform.Find(faceChildName);

        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        RebuildCollidersFromRuntimeSetup();
        _lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        _linearVelocity = (transform.position - _lastPosition) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        _lastPosition = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball") || Time.time - _lastHitAt < hitCooldown)
        {
            return;
        }

        Rigidbody ballRb = collision.rigidbody;
        if (ballRb == null)
        {
            return;
        }

        if (GameManager.instance != null && !GameManager.instance.CanPlayerStrikeBall())
        {
            GameManager.instance.ReportIllegalStrike(GameManager.RallySide.Player, "Golpeaste la pelota sin esperar el bote reglamentario.");
            _lastHitAt = Time.time;
            return;
        }

        Vector3 towardBoss = _boss != null
            ? (_boss.transform.position - collision.GetContact(0).point).normalized
            : Vector3.forward;

        Vector3 surfaceNormal = _strikeSurface != null ? _strikeSurface.forward : transform.forward;
        if (Vector3.Dot(surfaceNormal, towardBoss) < 0f)
        {
            surfaceNormal = -surfaceNormal;
        }

        Vector3 contactNormal = collision.GetContact(0).normal;
        if (Vector3.Dot(contactNormal, towardBoss) > 0f)
        {
            surfaceNormal = Vector3.Slerp(surfaceNormal, contactNormal, 0.2f).normalized;
        }

        if (surfaceNormal.sqrMagnitude < 0.0001f)
        {
            surfaceNormal = towardBoss;
        }

        Vector3 reflected = Vector3.Reflect(ballRb.linearVelocity, surfaceNormal);
        Vector3 candidate = reflected * 0.28f;
        candidate += _linearVelocity * velocityTransfer;
        candidate += Vector3.ProjectOnPlane(towardBoss, Vector3.up).normalized * forwardAssist;
        candidate.y = Mathf.Max(candidate.y + upwardAssist, 1.1f);

        if (candidate.sqrMagnitude < 0.0001f)
        {
            candidate = Vector3.up * 1.1f + Vector3.forward * minimumReturnSpeed;
        }

        ballRb.useGravity = true;
        ballRb.angularVelocity = Vector3.zero;
        ballRb.linearVelocity = Vector3.ClampMagnitude(candidate, maximumReturnSpeed);

        if (ballRb.linearVelocity.magnitude < minimumReturnSpeed)
        {
            ballRb.linearVelocity = ballRb.linearVelocity.normalized * minimumReturnSpeed;
        }

        GameManager.instance?.RegisterPlayerPaddleHit();
        BallWatchdog.instance?.RegistrarGolpe();
        _lastHitAt = Time.time;
    }

    private void RebuildCollidersFromRuntimeSetup()
    {
        Transform face = transform.Find(faceChildName);
        if (face != null)
        {
            CapsuleCollider legacyCollider = face.GetComponent<CapsuleCollider>();
            if (legacyCollider != null)
            {
                legacyCollider.enabled = false;
            }

            MeshFilter faceMesh = face.GetComponent<MeshFilter>();
            MeshCollider meshCollider = face.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = face.gameObject.AddComponent<MeshCollider>();
            }

            if (faceMesh != null)
            {
                meshCollider.sharedMesh = faceMesh.sharedMesh;
            }

            meshCollider.material = legacyCollider != null ? legacyCollider.material : null;
            meshCollider.convex = true;
            meshCollider.enabled = meshCollider.sharedMesh != null;
        }

        Transform handle = transform.Find(handleChildName);
        if (handle != null)
        {
            CapsuleCollider handleCapsule = handle.GetComponent<CapsuleCollider>();
            if (handleCapsule != null)
            {
                handleCapsule.enabled = false;
            }

            BoxCollider handleBox = handle.GetComponent<BoxCollider>();
            if (handleBox == null)
            {
                handleBox = handle.gameObject.AddComponent<BoxCollider>();
            }

            MeshFilter handleMesh = handle.GetComponent<MeshFilter>();
            if (handleMesh != null && handleMesh.sharedMesh != null)
            {
                Bounds meshBounds = handleMesh.sharedMesh.bounds;
                handleBox.center = meshBounds.center;
                handleBox.size = meshBounds.size;
            }
            else if (handleCapsule != null)
            {
                handleBox.center = handleCapsule.center;
                handleBox.size = new Vector3(handleCapsule.radius * 2f, handleCapsule.height, handleCapsule.radius * 2f);
            }

            handleBox.material = handleCapsule != null ? handleCapsule.material : null;
            handleBox.enabled = true;
        }
    }
}
