using UnityEngine;

/// <summary>
/// Detecta en cual mitad de la mesa rebota la pelota.
/// 
/// SETUP:
/// Crea 2 hijos de la Mesa, cada uno cubriendo su mitad:
///   MitadJefe    → esMitadJefe = true
///   MitadJugador → esMitadJefe = false
///
/// Cada uno necesita:
///   - Box Collider (NO trigger, colision normal)
///   - Mesh Renderer apagado (invisible)
///   - Este script
///
/// Dimensiones sugeridas para cada mitad:
///   Scale X: 1.52  Y: 0.02  Z: 1.37
/// </summary>
public class TableBounce : MonoBehaviour
{
    [Tooltip("true = mitad del jefe (Z positivo). false = mitad del jugador (Z negativo)")]
    public bool esMitadJefe = false;

    void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.CompareTag("Ball")) return;
        if (GameManager.instance == null)       return;
        if (!GameManager.instance.roundActive)  return;

        Debug.Log($"[TableBounce] Pelota reboto en: {(esMitadJefe ? "LADO JEFE" : "LADO JUGADOR")}");
        GameManager.instance.RegistrarRebote(esMitadJefe);
    }
}