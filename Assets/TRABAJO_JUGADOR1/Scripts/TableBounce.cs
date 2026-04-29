using UnityEngine;

public class TableBounce : MonoBehaviour
{
    [Tooltip("true = boss side, false = player side")]
    public bool esMitadJefe;

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball") || GameManager.instance == null)
        {
            return;
        }

        GameManager.instance.RegisterRebote(esMitadJefe);
    }
}
