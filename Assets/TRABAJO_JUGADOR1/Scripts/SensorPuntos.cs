using UnityEngine;

public class SensorPuntos : MonoBehaviour
{
    public enum Zona
    {
        Jugador,
        Jefe
    }

    public Zona zonaPertenece;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball") || GameManager.instance == null)
        {
            return;
        }

        GameManager.instance.HandleBallPassedZone(zonaPertenece);
    }
}
