using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class PelotaBehaviour : MonoBehaviour
{
    [Header("Gravity")]
    public float gravedadNormal = 9.81f;
    public float gravedadAzul = 2.5f;
    public float gravedadNaranja = 22f;

    [Header("Serve Float")]
    [SerializeField] private float heldServeColliderRadius = 0.4f;
    [SerializeField] private float grabDistanceThreshold = 0.03f;

    private static readonly Color COLOR_AZUL = new Color(0.2f, 0.5f, 1f);
    private static readonly Color COLOR_NARANJA = new Color(1f, 0.45f, 0f);

    private Rigidbody _rb;
    private Renderer _renderer;
    private SphereCollider _sphereCollider;
    private Color _baseColor = Color.white;
    private bool _floatingOnServe;
    private bool _colorEffectActive;
    private float _currentGravity;
    private Vector3 _floatTargetPosition;
    private Quaternion _floatTargetRotation = Quaternion.identity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _renderer = GetComponent<Renderer>();
        _sphereCollider = GetComponent<SphereCollider>();

        if (_renderer != null)
        {
            _baseColor = _renderer.material.color;
        }

        if (_sphereCollider != null)
        {
            _sphereCollider.radius = heldServeColliderRadius;
        }

        _rb.mass = 0.027f;
        _rb.linearDamping = 0.08f;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _currentGravity = gravedadNormal;
    }

    private void Update()
    {
        if (_floatingOnServe && WasServeBallTaken())
        {
            ActivarFisicasNormales();
            _floatingOnServe = false;
            return;
        }

        if (!_floatingOnServe && GameManager.instance != null && GameManager.instance.waitingForPlayerServeHit)
        {
            OVRGrabbable legacyGrabbable = GetComponent<OVRGrabbable>();
            bool isGrabbed = legacyGrabbable != null && legacyGrabbable.isGrabbed;
            if (!isGrabbed && BallWatchdog.instance != null && !BallWatchdog.instance.IsMonitoring)
            {
                BallWatchdog.instance.IniciarMonitoreo();
            }
        }
    }

    private void FixedUpdate()
    {
        if (_floatingOnServe)
        {
            _rb.useGravity = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.MovePosition(_floatTargetPosition);
            _rb.MoveRotation(_floatTargetRotation);
            return;
        }

        if (_colorEffectActive)
        {
            _rb.AddForce(Vector3.down * _currentGravity, ForceMode.Acceleration);
        }
    }

    public void IniciarFlotando(Vector3 targetPosition, Quaternion targetRotation)
    {
        _floatingOnServe = true;
        _floatTargetPosition = targetPosition;
        _floatTargetRotation = targetRotation;
        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    public void SetEfectoColor(bool esAzul)
    {
        _colorEffectActive = true;
        _currentGravity = esAzul ? gravedadAzul : gravedadNaranja;
        CambiarColor(esAzul ? COLOR_AZUL : COLOR_NARANJA);
        _rb.useGravity = false;
    }

    public void ResetarEfectoColor()
    {
        if (!_colorEffectActive)
        {
            return;
        }

        _colorEffectActive = false;
        _currentGravity = gravedadNormal;
        _rb.useGravity = !_floatingOnServe;
        CambiarColor(_baseColor);
    }

    private bool WasServeBallTaken()
    {
        OVRGrabbable legacyGrabbable = GetComponent<OVRGrabbable>();
        if (legacyGrabbable != null && legacyGrabbable.isGrabbed)
        {
            return true;
        }

        float distance = Vector3.Distance(transform.position, _floatTargetPosition);
        return distance > grabDistanceThreshold;
    }

    private void ActivarFisicasNormales()
    {
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void CambiarColor(Color color)
    {
        if (_renderer != null)
        {
            _renderer.material.color = color;
        }
    }
}
