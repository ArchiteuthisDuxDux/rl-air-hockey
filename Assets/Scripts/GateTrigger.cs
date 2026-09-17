using UnityEngine;

public class GateTrigger : MonoBehaviour
{
    [SerializeField] private AirHockeyArenaManager arenaManager;

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Trigger entered by: " + other.name);

        if (!other.CompareTag("Puck"))
            return;

        arenaManager.OnGateTriggered(this);
    }
}