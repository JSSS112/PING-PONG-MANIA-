using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Laser pointer estilo Meta para menús VR.
///
/// - Sale del controlador derecho (RightHandAnchor)
/// - Visualiza un LineRenderer hasta el primer Canvas WorldSpace que toque
/// - Detecta hover con GraphicRaycaster (no necesita Colliders en los botones)
/// - Trigger derecho → invoca el onClick del Button apuntado
/// - Funciona aunque Time.timeScale=0 (panel de victoria/derrota)
///
/// Setup:
/// - Crear un GameObject vacío llamado "LaserPointer" como hijo de RightHandAnchor
/// - Añadir este script
/// - El Canvas del menú debe ser RenderMode = WorldSpace y tener GraphicRaycaster
/// - Si el Canvas no tiene Event Camera asignado, se le asigna automáticamente la del rig
/// </summary>
[DefaultExecutionOrder(50)]
public class VRLaserPointer : MonoBehaviour
{
    [Header("Origen del rayo (RightHandAnchor o el control derecho)")]
    [Tooltip("Si se deja vacío, se busca por nombre 'RightHandAnchor' / 'RightControllerAnchor'.")]
    public Transform rayOrigin;

    [Header("Cámara del rig (CenterEyeAnchor) — necesaria para raycast UI")]
    public Camera eventCamera;

    [Header("Visual")]
    public LineRenderer line;
    public float maxDistance = 8f;
    public float anchoLinea  = 0.005f;
    public Color colorNormal = new Color(0.4f, 0.7f, 1f, 0.9f);
    public Color colorHover  = new Color(0.2f, 1f,  0.4f, 1f);

    [Header("Input")]
    [Tooltip("Trigger del control derecho para confirmar.")]
    public OVRInput.Button botonClick = OVRInput.Button.PrimaryIndexTrigger;
    [Tooltip("Mando que dispara el click. Por defecto el derecho.")]
    public OVRInput.Controller mando = OVRInput.Controller.RTouch;

    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private GameObject objetoApuntado;

    void Awake()
    {
        if (rayOrigin == null)
        {
            GameObject go = GameObject.Find("RightHandAnchor");
            if (go == null) go = GameObject.Find("RightControllerAnchor");
            if (go != null) rayOrigin = go.transform;
        }

        if (eventCamera == null)
        {
            GameObject ce = GameObject.Find("CenterEyeAnchor");
            if (ce != null) eventCamera = ce.GetComponent<Camera>();
            if (eventCamera == null) eventCamera = Camera.main;
        }

        if (line == null)
        {
            line = gameObject.GetComponent<LineRenderer>();
            if (line == null) line = gameObject.AddComponent<LineRenderer>();
        }

        line.useWorldSpace   = true;
        line.positionCount   = 2;
        line.startWidth      = anchoLinea;
        line.endWidth        = anchoLinea;
        line.material        = new Material(Shader.Find("Sprites/Default"));
        line.startColor      = colorNormal;
        line.endColor        = colorNormal;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows  = false;

        if (EventSystem.current == null)
        {
            // Necesario para procesar PointerEventData
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    void Update()
    {
        if (rayOrigin == null || eventCamera == null) { if (line) line.enabled = false; return; }

        Vector3 origen   = rayOrigin.position;
        Vector3 dir      = rayOrigin.forward;
        Vector3 endPoint = origen + dir * maxDistance;

        objetoApuntado = null;
        Vector3 puntoHit = endPoint;
        float distHit = maxDistance;

        // Recorrer todos los Canvas WorldSpace y hacer raycast UI
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            if (c == null || !c.isActiveAndEnabled) continue;
            if (c.renderMode != RenderMode.WorldSpace) continue;

            // Asegurar que el Canvas tenga Event Camera (sino el raycast UI da posiciones raras)
            if (c.worldCamera == null) c.worldCamera = eventCamera;

            // Intersectar el rayo con el plano del canvas
            Plane plano = new Plane(-c.transform.forward, c.transform.position);
            if (!plano.Raycast(new Ray(origen, dir), out float d)) continue;
            if (d > maxDistance || d < 0f) continue;

            Vector3 hitWorld = origen + dir * d;
            Vector2 screenPoint = eventCamera.WorldToScreenPoint(hitWorld);

            GraphicRaycaster gr = c.GetComponent<GraphicRaycaster>();
            if (gr == null) continue;

            PointerEventData ped = new PointerEventData(EventSystem.current) { position = screenPoint };
            hits.Clear();
            gr.Raycast(ped, hits);

            if (hits.Count > 0 && d < distHit)
            {
                objetoApuntado = hits[0].gameObject;
                puntoHit = hitWorld;
                distHit  = d;
            }
        }

        // Actualizar visual
        line.enabled = true;
        line.SetPosition(0, origen);
        line.SetPosition(1, puntoHit);
        Color c2 = (objetoApuntado != null) ? colorHover : colorNormal;
        line.startColor = c2; line.endColor = c2;

        // Click
        if (objetoApuntado != null && OVRInput.GetDown(botonClick, mando))
        {
            Button b = objetoApuntado.GetComponentInParent<Button>();
            if (b != null && b.interactable)
            {
                b.onClick.Invoke();
                Debug.Log($"[OASIS][Laser] Click en {b.name}");
            }
        }
    }
}
