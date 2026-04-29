using System.Collections;
using UnityEngine;

public class VRMenuCanvasAnchor : MonoBehaviour
{
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private float distanceFromHead = 1.2f;
    [SerializeField] private float verticalOffset = -0.08f;
    [SerializeField] private float forwardLerpSeconds = 0.2f;

    public void SetCanvas(Canvas canvas)
    {
        targetCanvas = canvas;
    }

    private void Start()
    {
        StartCoroutine(AnchorRoutine());
    }

    private IEnumerator AnchorRoutine()
    {
        yield return null;
        RepositionCanvas();
        yield return new WaitForSeconds(forwardLerpSeconds);
        RepositionCanvas();
    }

    private void RepositionCanvas()
    {
        if (targetCanvas == null)
        {
            return;
        }

        Camera headCamera = ResolveHeadCamera();
        if (headCamera == null)
        {
            return;
        }

        Transform head = headCamera.transform;
        Vector3 anchorPosition = head.position + head.forward * distanceFromHead + Vector3.up * verticalOffset;
        targetCanvas.transform.position = anchorPosition;
        targetCanvas.transform.rotation = Quaternion.LookRotation(head.position - anchorPosition, Vector3.up);
        targetCanvas.worldCamera = headCamera;
    }

    private static Camera ResolveHeadCamera()
    {
        OVRManager manager = FindFirstObjectByType<OVRManager>();
        if (manager != null)
        {
            OVRCameraRig rig = manager.GetComponent<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null)
            {
                Camera cam = rig.centerEyeAnchor.GetComponent<Camera>();
                if (cam != null)
                {
                    return cam;
                }
            }
        }

        return Camera.main;
    }
}
