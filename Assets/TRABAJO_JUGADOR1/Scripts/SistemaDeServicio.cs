using UnityEngine;

/// <summary>
/// Saque del jugador, version minima:
///   1) Aprieta el gatillo izquierdo  -> aparece una pelota nueva en la mano.
///   2) Mientras lo mantenga apretado -> la pelota sigue la mano (kinematic).
///   3) Suelta el gatillo             -> la pelota se vuelve dinamica con gravedad y CAE.
///
/// NO se transfiere velocidad de la mano. NO hay multiplicadores. NO hay
/// velocidad minima vertical. La pelota solo cae. Si el jugador quiere
/// que la pelota suba, que la golpee con la raqueta.
/// </summary>
public class SistemaDeServicio : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("LeftHandAnchor del OVRCameraRig.")]
    public Transform manoIzquierda;

    [Tooltip("Prefab PingPongBall Variant.")]
    public GameObject prefabPelota;

    [Tooltip("Offset de la pelota respecto a la mano (un poco abajo y adelante para que no choque con la mano).")]
    public Vector3 offsetPelota = new Vector3(0f, -0.05f, 0.05f);

    [Header("Input VR")]
    public OVRInput.Button botonSaque = OVRInput.Button.PrimaryIndexTrigger;
    public OVRInput.Controller controlador = OVRInput.Controller.LTouch;

    private GameObject pelotaActual;
    private Rigidbody rbPelota;

    void Update()
    {
        if (manoIzquierda == null || prefabPelota == null) return;

        // Gatillo recien apretado -> crear pelota.
        if (OVRInput.GetDown(botonSaque, controlador))
        {
            CrearPelota();
        }

        // Gatillo recien soltado -> soltar pelota.
        if (OVRInput.GetUp(botonSaque, controlador))
        {
            SoltarPelota();
        }
    }

    void FixedUpdate()
    {
        // Mientras este sostenida (kinematic), seguir la mano.
        if (pelotaActual == null || rbPelota == null) return;
        if (!rbPelota.isKinematic) return;

        Vector3 destino = manoIzquierda.position + manoIzquierda.rotation * offsetPelota;
        rbPelota.MovePosition(destino);
    }

    void CrearPelota()
    {
        // Si ya hay una pelota viva (porque quedo una previa o el jugador se mando
        // dos saques seguidos), la destruimos para evitar duplicados.
        if (pelotaActual != null) Destroy(pelotaActual);

        Vector3 pos = manoIzquierda.position + manoIzquierda.rotation * offsetPelota;
        pelotaActual = Instantiate(prefabPelota, pos, Quaternion.identity);
        rbPelota = pelotaActual.GetComponent<Rigidbody>();

        if (rbPelota != null)
        {
            // Estado: sostenida en la mano.
            rbPelota.isKinematic = true;
            rbPelota.useGravity = false;
            rbPelota.linearVelocity = Vector3.zero;
            rbPelota.angularVelocity = Vector3.zero;
        }
    }

    void SoltarPelota()
    {
        if (pelotaActual == null || rbPelota == null) return;

        // Estado: en juego. Solo cae con gravedad.
        rbPelota.isKinematic = false;
        rbPelota.useGravity = true;
        rbPelota.linearVelocity = Vector3.zero;       // <-- CLAVE: SIN velocidad de mano
        rbPelota.angularVelocity = Vector3.zero;

        // Soltamos la referencia. La pelota ya vive su vida.
        pelotaActual = null;
        rbPelota = null;
    }
}
