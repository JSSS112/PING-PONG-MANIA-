using UnityEngine;

/// <summary>
/// Pelota minimalista con dos materiales especiales opcionales.
///
///   - materialAmarillo: aspecto + el punto vale por 2 + sonido magico al golpear + palpitaciones.
///   - materialNaranja:  aspecto + mas liviana, grande y rapida + sonido magico al golpear + palpitaciones.
///
/// Cada audio es opcional — si el AudioClip no esta asignado, simplemente
/// no se reproduce. La pelota crea sus propios AudioSource en runtime.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class PelotaBehaviour : MonoBehaviour
{
    [Header("Tope de seguridad")]
    [Tooltip("Velocidad maxima permitida en m/s.")]
    public float velocidadMaxima = 12f;

    [Header("Material AMARILLO — el punto vale x2")]
    public Material materialAmarillo;
    [Range(0f, 1f)] public float probabilidadAmarillo = 0.2f;
    public int valorPuntoAmarillo = 2;
    [Tooltip("Sonido magico al golpear la pelota amarilla.")]
    public AudioClip sonidoGolpeAmarillo;

    [Header("Material NARANJA — mas liviana, grande y rapida")]
    public Material materialNaranja;
    [Range(0f, 1f)] public float probabilidadNaranja = 0.2f;
    public float escalaNaranja = 1.35f;
    public float masaNaranja = 0.0015f;
    public float velMaxNaranja = 16f;
    public float dragNaranja = 0.01f;
    [Tooltip("Sonido magico al golpear la pelota naranja.")]
    public AudioClip sonidoGolpeNaranja;

    [Header("Tension — palpitaciones de fondo (loop) cuando la pelota es especial")]
    [Tooltip("Audio loopeable. Solo suena si la pelota es amarilla o naranja.")]
    public AudioClip sonidoPalpitaciones;
    [Range(0f, 1f)] public float volumenPalpitaciones = 0.6f;
    [Range(0f, 1f)] public float volumenGolpeEspecial = 1.0f;

    private Rigidbody rb;
    private AudioSource audioGolpe;        // 2D one-shot
    private AudioSource audioPalpita;      // 2D loop
    private AudioClip   clipGolpeEspecial; // se asigna si la pelota es especial

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // AudioSources 2D internos (no spatial) — la pelota es chica y se mueve,
        // queremos que el audio se escuche bien igual.
        audioGolpe = gameObject.AddComponent<AudioSource>();
        audioGolpe.playOnAwake = false;
        audioGolpe.spatialBlend = 0f;
        audioGolpe.volume = volumenGolpeEspecial;

        audioPalpita = gameObject.AddComponent<AudioSource>();
        audioPalpita.playOnAwake = false;
        audioPalpita.spatialBlend = 0f;
        audioPalpita.loop = true;
        audioPalpita.volume = volumenPalpitaciones;

        AplicarMaterialYEfectos();
    }

    void FixedUpdate()
    {
        if (rb.isKinematic) return;

        Vector3 v = rb.linearVelocity;
        if (v.magnitude > velocidadMaxima)
        {
            rb.linearVelocity = v.normalized * velocidadMaxima;
        }
    }

    /// <summary>Lo llama RaquetaJugador cuando golpea la pelota.</summary>
    public void NotificarGolpe()
    {
        if (clipGolpeEspecial == null || audioGolpe == null) return;
        audioGolpe.PlayOneShot(clipGolpeEspecial, volumenGolpeEspecial);
    }

    void AplicarMaterialYEfectos()
    {
        if (GameManager.instance != null) GameManager.instance.puntoVale = 1;

        // 1) Amarillo
        if (materialAmarillo != null && Random.value < probabilidadAmarillo)
        {
            AplicarMaterial(materialAmarillo);
            if (GameManager.instance != null)
                GameManager.instance.puntoVale = valorPuntoAmarillo;
            clipGolpeEspecial = sonidoGolpeAmarillo;
            IniciarPalpitaciones();
            Debug.Log($"[Pelota] AMARILLA — el proximo punto vale {valorPuntoAmarillo}");
            return;
        }

        // 2) Naranja
        if (materialNaranja != null && Random.value < probabilidadNaranja)
        {
            AplicarMaterial(materialNaranja);
            rb.mass          = masaNaranja;
            rb.linearDamping = dragNaranja;
            transform.localScale *= escalaNaranja;
            velocidadMaxima  = velMaxNaranja;
            clipGolpeEspecial = sonidoGolpeNaranja;
            IniciarPalpitaciones();
            Debug.Log("[Pelota] NARANJA — mas liviana, grande y rapida");
        }
    }

    void IniciarPalpitaciones()
    {
        if (sonidoPalpitaciones == null || audioPalpita == null) return;
        audioPalpita.clip = sonidoPalpitaciones;
        audioPalpita.Play();
    }

    void AplicarMaterial(Material m)
    {
        if (m == null) return;
        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
        if (mr != null) mr.material = m;
    }
}
