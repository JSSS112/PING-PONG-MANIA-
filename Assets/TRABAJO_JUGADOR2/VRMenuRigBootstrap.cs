using UnityEngine;

public class VRMenuRigBootstrap : MonoBehaviour
{
    [SerializeField] private bool disableLegacyMainCamera = true;

    private void Awake()
    {
        EnsureOvrRig();
        if (disableLegacyMainCamera)
        {
            DisableLegacyMainCamera();
        }
    }

    private void EnsureOvrRig()
    {
        if (FindFirstObjectByType<OVRCameraRig>() != null)
        {
            return;
        }

        GameObject rigRoot = new GameObject("OVRCameraRig");
        rigRoot.transform.position = Vector3.zero;
        rigRoot.transform.rotation = Quaternion.identity;
        rigRoot.AddComponent<OVRManager>();
        rigRoot.AddComponent<OVRCameraRig>();
    }

    private void DisableLegacyMainCamera()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in cameras)
        {
            if (cam.GetComponentInParent<OVRCameraRig>() != null)
            {
                continue;
            }

            cam.enabled = false;
            AudioListener listener = cam.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = false;
            }
        }
    }
}
