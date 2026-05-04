using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    void Awake()
    {
        // Asegurar que existe un VRLaserPointer en la escena para poder
        // interactuar con los botones del menú con el control derecho.
        if (FindFirstObjectByType<VRLaserPointer>() == null)
        {
            GameObject go = new GameObject("LaserPointer");
            go.AddComponent<VRLaserPointer>();
        }
    }

    public void IrAJugar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("JuegoPartida");
    }

    public void JugarDeNuevo()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("JuegoPartida");
    }

    public void Salir()
    {
        Application.Quit();
    }
}
