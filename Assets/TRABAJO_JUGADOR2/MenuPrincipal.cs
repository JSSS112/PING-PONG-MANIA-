using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] private Canvas targetCanvas;

    private void Awake()
    {
        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        EnsureComponent<VRMenuRigBootstrap>();

        VRMenuCanvasAnchor anchor = EnsureComponent<VRMenuCanvasAnchor>();
        anchor.SetCanvas(targetCanvas);

        VRMenuLaserInteractor laserInteractor = EnsureComponent<VRMenuLaserInteractor>();
        laserInteractor.SetCanvas(targetCanvas);
    }

    public void IrAJugar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("JuegoPartida");
    }

    public void Salir()
    {
        Application.Quit();
    }

    private T EnsureComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }
}
