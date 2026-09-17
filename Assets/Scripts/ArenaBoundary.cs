using UnityEngine;

public class ArenaBoundary : MonoBehaviour
{
    [SerializeField] private AirHockeyArenaManager arenaManager;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Puck") &&
            !other.CompareTag("Blue Agent") &&
            !other.CompareTag("Red Agent"))
        {
            return;
        }

        arenaManager.AbortEpisode();
    }
}