using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VRMenuLaserInteractor : MonoBehaviour
{
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private float laserWidth = 0.0035f;

    private readonly List<RaycastResult> _results = new List<RaycastResult>();
    private EventSystem _eventSystem;
    private GraphicRaycaster _graphicRaycaster;
    private LineRenderer _lineRenderer;
    private PointerEventData _pointerEventData;
    private GameObject _hoveredObject;
    private GameObject _pressedObject;

    public void SetCanvas(Canvas canvas)
    {
        targetCanvas = canvas;
    }

    private void Awake()
    {
        _eventSystem = FindFirstObjectByType<EventSystem>();
        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        _graphicRaycaster = targetCanvas != null ? targetCanvas.GetComponent<GraphicRaycaster>() : null;
        _pointerEventData = _eventSystem != null ? new PointerEventData(_eventSystem) : null;
        SetupLaserVisual();
    }

    private void Update()
    {
        if (_eventSystem == null || _graphicRaycaster == null || targetCanvas == null || _pointerEventData == null)
        {
            return;
        }

        Camera headCamera = targetCanvas.worldCamera != null ? targetCanvas.worldCamera : Camera.main;
        if (headCamera == null)
        {
            return;
        }

        Transform rayOrigin = ResolveRayOrigin();
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        Vector3 endPoint = ray.origin + ray.direction * maxDistance;
        GameObject currentHit = null;

        Plane canvasPlane = new Plane(targetCanvas.transform.forward, targetCanvas.transform.position);
        if (canvasPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            endPoint = hitPoint;

            _pointerEventData.Reset();
            _pointerEventData.position = headCamera.WorldToScreenPoint(hitPoint);
            _pointerEventData.pointerCurrentRaycast = default;
            _results.Clear();
            _graphicRaycaster.Raycast(_pointerEventData, _results);

            if (_results.Count > 0)
            {
                currentHit = _results[0].gameObject;
                _pointerEventData.pointerCurrentRaycast = _results[0];
            }
        }

        UpdateHover(currentHit);
        UpdateClickState(currentHit);
        UpdateLaser(ray.origin, endPoint);
    }

    private Transform ResolveRayOrigin()
    {
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null)
        {
            if (OVRInput.IsControllerConnected(OVRInput.Controller.RTouch) && rig.rightControllerAnchor != null)
            {
                return rig.rightControllerAnchor;
            }

            if (OVRInput.IsControllerConnected(OVRInput.Controller.LTouch) && rig.leftControllerAnchor != null)
            {
                return rig.leftControllerAnchor;
            }

            if (rig.centerEyeAnchor != null)
            {
                return rig.centerEyeAnchor;
            }
        }

        return Camera.main != null ? Camera.main.transform : transform;
    }

    private void UpdateHover(GameObject currentHit)
    {
        if (_hoveredObject == currentHit)
        {
            return;
        }

        if (_hoveredObject != null)
        {
            ExecuteEvents.ExecuteHierarchy(_hoveredObject, _pointerEventData, ExecuteEvents.pointerExitHandler);
        }

        _hoveredObject = currentHit;

        if (_hoveredObject != null)
        {
            ExecuteEvents.ExecuteHierarchy(_hoveredObject, _pointerEventData, ExecuteEvents.pointerEnterHandler);
        }
    }

    private void UpdateClickState(GameObject currentHit)
    {
        bool triggerDown = OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)
                        || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)
                        || Input.GetMouseButtonDown(0);
        bool triggerUp = OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger)
                      || OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger)
                      || Input.GetMouseButtonUp(0);

        if (triggerDown && currentHit != null)
        {
            _pressedObject = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentHit);
            if (_pressedObject != null)
            {
                ExecuteEvents.ExecuteHierarchy(_pressedObject, _pointerEventData, ExecuteEvents.pointerDownHandler);
            }
        }

        if (triggerUp && _pressedObject != null)
        {
            ExecuteEvents.ExecuteHierarchy(_pressedObject, _pointerEventData, ExecuteEvents.pointerUpHandler);

            GameObject clickTarget = currentHit != null
                ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentHit)
                : null;

            if (clickTarget == _pressedObject)
            {
                ExecuteEvents.ExecuteHierarchy(_pressedObject, _pointerEventData, ExecuteEvents.pointerClickHandler);
            }

            _pressedObject = null;
        }
    }

    private void SetupLaserVisual()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = laserWidth;
        _lineRenderer.endWidth = laserWidth * 0.75f;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = new Color(0.9f, 0.96f, 1f, 0.95f);
        _lineRenderer.endColor = new Color(0.1f, 0.75f, 1f, 0.35f);
    }

    private void UpdateLaser(Vector3 origin, Vector3 endPoint)
    {
        if (_lineRenderer == null)
        {
            return;
        }

        _lineRenderer.SetPosition(0, origin);
        _lineRenderer.SetPosition(1, endPoint);
    }
}
