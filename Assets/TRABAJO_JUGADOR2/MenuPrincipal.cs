using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    public void IrAJugar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("JuegoPartida");
    }

    public void Salir()
    {
        Application.Quit();
    }
}