using UnityEngine;

/// <summary>
/// Cubo invisible detras de cada jugador.
/// Box Collider → Is Trigger = ON
/// Mesh Renderer → apagado
///
/// Zona = Jugador → sensor detras del jugador (Z muy negativo)
/// Zona = Jefe    → sensor detras del jefe (Z positivo)
/// </summary>
public class SensorPuntos : MonoBehaviour
{
    public enum Zona { Jugador, Jefe }

    [Tooltip("Jugador = sensor detras del jugador. Jefe = sensor detras del jefe.")]
    public Zona zonaPertenece;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball"))         return;
        if (GameManager.instance == null)      return;
        if (!GameManager.instance.roundActive) return;
        if (GameManager.instance.gameOver)     return;

        if (zonaPertenece == Zona.Jugador)
        {
            // Pelota paso el lado del jugador sin ser devuelta → jefe anota
            GameManager.instance.JefeAnota();
        }
        else
        {
            // Pelota paso el lado del jefe → jugador anota
            GameManager.instance.JugadorAnota();
        }
    }
}