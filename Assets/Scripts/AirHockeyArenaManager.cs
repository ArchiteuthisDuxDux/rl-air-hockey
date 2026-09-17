using System.Collections;
using UnityEngine;

public class AirHockeyArenaManager : MonoBehaviour
{
    private enum ScoringSide
    {
        Blue,
        Red
    }

    [Header("Agents")]
    [SerializeField] private AirHockeyAgent blueAgent;
    [SerializeField] private AirHockeyAgent redAgent;

    [Header("Gate Triggers")]
    [SerializeField] private GateTrigger blueGate;
    [SerializeField] private GateTrigger redGate;

    [Header("Rigidbodies")]
    [SerializeField] private Rigidbody blueBody;
    [SerializeField] private Rigidbody redBody;
    [SerializeField] private Rigidbody puckBody;

    [Header("Spawn Points")]
    [SerializeField] private Transform blueSpawnPoint;
    [SerializeField] private Transform redSpawnPoint;
    [SerializeField] private Transform puckSpawnPoint;

    [Header("Puck Tracker")]
    [SerializeField] private PuckTracker puckTracker;

    [Header("Reward Settings")]
    [SerializeField] private float goalReward = 1f;
    [SerializeField] public float impulseRewardScale = 0.01f;
    [SerializeField] private float homeDistancePenaltyScale = 0.0005f;
    [SerializeField] private float halfFieldPenaltyScale = 0.001f;
    [SerializeField] private bool useHalfFieldPenalty = false;

    [Header("Puck Start")]
    [SerializeField] private float puckMinSpeed = 3f;
    [SerializeField] private float puckMaxSpeed = 7f;
    [SerializeField] private float puckMaxAngleDeviation = 30f;

    [Header("Round Settings")]
    [SerializeField] private bool resetOnStart = true;
    [SerializeField] private float positionRewardInterval = 0.05f;

    [Header("Timer Settings")]
    [Tooltip("Максимальная длительность эпизода в секундах")]
    [SerializeField] private float episodeDuration = 30f;
    [Tooltip("Штраф обоим агентам, если время вышло и никто не забил")]
    [SerializeField] private float timeOutPenalty = -0.1f;

    [Header("Puck Stillness Penalty")]
    [Tooltip("Скорость шайбы ниже этого значения считается 'покоем'")]
    [SerializeField] private float stillThreshold = 0.1f;
    [Tooltip("Штраф за секунду простоя (отрицательное число)")]
    [SerializeField] private float stillPenaltyPerSecond = -0.05f;
    [Tooltip("Задержка в секундах перед началом штрафа (чтобы не наказывать за мгновенные остановки)")]
    [SerializeField] private float stillDelay = 0.5f;

    [Header("Debug Info")]
    [SerializeField] private int blueScore;
    [SerializeField] private int redScore;
    [SerializeField] private float currentRoundTime;

    private bool roundActive;
    private Coroutine positionRewardRoutine;

    private AirHockeyAgent lastTouchAgent;

    private float blueSideStillTimer;
    private float redSideStillTimer;

    public bool IsRoundActive => roundActive;


    public int BlueScore => blueScore;
    public int RedScore => redScore;
    public float CurrentRoundTime => currentRoundTime;



    private void Start()
    {
        positionRewardRoutine = StartCoroutine(PositionRewardLoop());

        if (resetOnStart)
            ResetMatch();
        
        blueScore = 0;
        redScore = 0;
    }

    private void FixedUpdate()
    {
        if (!roundActive)
            return;

        currentRoundTime -= Time.fixedDeltaTime;

        if (currentRoundTime <= 0f)
        {
            HandleMatchTimeOut();
        }
    }

    private IEnumerator PositionRewardLoop()
    {
        var wait = new WaitForSeconds(positionRewardInterval);

        while (true)
        {
            yield return wait;

            if (!roundActive)
                continue;

            ApplyPositionRewards();
            ApplyPuckStillnessPenalty();
        }
    }

    private void HandleMatchTimeOut()
    {
        roundActive = false;

        blueAgent?.AddReward(timeOutPenalty);
        redAgent?.AddReward(timeOutPenalty);

        blueAgent?.EndEpisode();
        redAgent?.EndEpisode();

        ResetMatch();
    }

    public void OnGateTriggered(GateTrigger gate)
    {
        if (!roundActive) return;

        roundActive = false;
        puckTracker?.PlayGoalSound();

        AirHockeyAgent scoringAgent = null;
        AirHockeyAgent concedingAgent = null;

        if (gate == blueGate)
        {
            concedingAgent = blueAgent;
            scoringAgent = redAgent;
            redScore++;
        }
        else if (gate == redGate)
        {
            concedingAgent = redAgent;
            scoringAgent = blueAgent;
            blueScore++;
        }
        else
        {
            Debug.LogWarning("Unknown gate trigger.");
            roundActive = true;
            return;
        }

        if (lastTouchAgent == concedingAgent)
        {
            float autoGoalPenalty = -goalReward * 2f;
            concedingAgent?.AddReward(autoGoalPenalty);
            scoringAgent?.AddReward(goalReward * 0.5f);
        }
        else
        {
            concedingAgent?.AddReward(-goalReward);
            scoringAgent?.AddReward(+goalReward);
        }

        lastTouchAgent = null;

        blueAgent?.EndEpisode();
        redAgent?.EndEpisode();
        ResetMatch();
    }

    public void ResetMatch()
    {
        roundActive = true;
        currentRoundTime = episodeDuration;

        blueSideStillTimer = 0f;
        redSideStillTimer = 0f;

        ResetBody(blueBody, blueSpawnPoint);
        ResetBody(redBody, redSpawnPoint);
        ResetBody(puckBody, puckSpawnPoint);

        Physics.SyncTransforms();

        RandomizePuckVelocity();
        puckTracker?.ResetTracker();
        lastTouchAgent = null;
    }

    public void ApplyImpulseReward(AirHockeyAgent agent, Vector3 puckVelocity)
    {
        if (agent == null) return;

        Vector3 localPuckVelocity = agent.transform.InverseTransformDirection(puckVelocity);

        float forwardSpeed = localPuckVelocity.x;

        if (forwardSpeed > 0f)
        {
            agent.AddReward(forwardSpeed * impulseRewardScale);
        }
        else if (forwardSpeed < 0f)
        {
            agent.AddReward(forwardSpeed * 0.5f * impulseRewardScale);
        }

        //Debug.Log($"{agent.name} | Puck Local Velocity X: {forwardSpeed} | Reward: {forwardSpeed * impulseRewardScale}");
    }


    public void SetLastTouch(AirHockeyAgent agent)
    {
        if (agent != null)
            lastTouchAgent = agent;
    }

    private void ApplyPositionRewards()
    {
        ApplyHomePenalty(blueAgent, blueBody, blueSpawnPoint);
        ApplyHomePenalty(redAgent, redBody, redSpawnPoint);

        if (useHalfFieldPenalty)
        {
            ApplyHalfFieldPenalty(blueAgent, blueBody, blueSpawnPoint, redSpawnPoint);
            ApplyHalfFieldPenalty(redAgent, redBody, redSpawnPoint, blueSpawnPoint);
        }
    }

    private void ApplyPuckStillnessPenalty()
    {
        if (puckBody == null || blueAgent == null || redAgent == null)
            return;

        float speed = puckBody.linearVelocity.magnitude;
        float interval = positionRewardInterval;

        int puckSide = GetPuckSide();

        if (speed < stillThreshold)
        {
            if (puckSide == 0)
            {
                blueSideStillTimer += interval;
                redSideStillTimer = 0f;

                if (blueSideStillTimer >= stillDelay)
                {
                    float penalty = stillPenaltyPerSecond * interval;
                    blueAgent.AddReward(penalty);
                }
            }
            else if (puckSide == 1)
            {
                redSideStillTimer += interval;
                blueSideStillTimer = 0f;

                if (redSideStillTimer >= stillDelay)
                {
                    float penalty = stillPenaltyPerSecond * interval;
                    redAgent.AddReward(penalty);
                }
            }
        }
        else
        {
            blueSideStillTimer = 0f;
            redSideStillTimer = 0f;
        }
    }

    private int GetPuckSide()
    {
        if (blueSpawnPoint == null || redSpawnPoint == null || puckBody == null)
            return -1;

        Vector3 lane = redSpawnPoint.position - blueSpawnPoint.position;
        float laneLength = lane.magnitude;
        if (laneLength <= Mathf.Epsilon)
            return -1;

        Vector3 laneDir = lane / laneLength;
        float progress = Vector3.Dot(puckBody.position - blueSpawnPoint.position, laneDir);
        float half = laneLength * 0.5f;

        if (progress <= half)
            return 0;
        else
            return 1;
    }

    private void ApplyHomePenalty(AirHockeyAgent agent, Rigidbody body, Transform homePoint)
    {
        if (agent == null || body == null || homePoint == null)
            return;

        float distanceSq = (body.position - homePoint.position).sqrMagnitude;
        agent.AddReward(-distanceSq * homeDistancePenaltyScale);
    }

    private void ApplyHalfFieldPenalty(
        AirHockeyAgent agent,
        Rigidbody body,
        Transform ownSidePoint,
        Transform enemySidePoint)
    {
        if (agent == null || body == null || ownSidePoint == null || enemySidePoint == null)
            return;

        float penalty = ComputeHalfFieldPenalty(
            body.position,
            ownSidePoint.position,
            enemySidePoint.position
        );

        if (penalty > 0f)
            agent.AddReward(-penalty);
    }

    private float ComputeHalfFieldPenalty(Vector3 agentPosition, Vector3 ownSidePosition, Vector3 enemySidePosition)
    {
        Vector3 lane = enemySidePosition - ownSidePosition;
        float laneLength = lane.magnitude;

        if (laneLength <= Mathf.Epsilon)
            return 0f;

        Vector3 laneDir = lane / laneLength;
        float progressFromOwnSide = Vector3.Dot(agentPosition - ownSidePosition, laneDir);
        float halfField = laneLength * 0.5f;

        if (progressFromOwnSide <= halfField)
            return 0f;

        float excess = progressFromOwnSide - halfField;
        return excess * excess * halfFieldPenaltyScale;
    }

    private void RandomizePuckVelocity()
    {
        if (puckBody == null) return;
        float speed = Random.Range(puckMinSpeed, puckMaxSpeed);
        float angleDeg = Random.Range(-puckMaxAngleDeviation, puckMaxAngleDeviation);
        float angleRad = angleDeg * Mathf.Deg2Rad;

        float direction = Random.value < 0.5f ? -1f : 1f;
        float x = direction * speed * Mathf.Cos(angleRad);
        float z = speed * Mathf.Sin(angleRad);

        puckBody.linearVelocity = new Vector3(x, 0f, z);
        puckBody.angularVelocity = Vector3.zero;
    }

    public void AbortEpisode()
    {
        if (!roundActive)
            return;

        roundActive = false;

        blueAgent?.EndEpisode();
        redAgent?.EndEpisode();

        ResetMatch();
    }
    private static void ResetBody(Rigidbody body, Transform point)
    {
        if (body == null || point == null)
            return;

        body.position = point.position;
        body.rotation = point.rotation;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }
}