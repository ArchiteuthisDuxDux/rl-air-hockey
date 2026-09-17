using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class AirHockeyAgent : Agent
{
    [Header("Debug")]
    [SerializeField] private bool debugTransforms;
    [SerializeField] private bool debugObservations;

    [Header("References")]
    [SerializeField] private Rigidbody agentBody;

    [SerializeField] private Rigidbody puckBody;
    [SerializeField] private Rigidbody opponentBody;

    [SerializeField] private Transform puck;
    [SerializeField] private Transform opponent;

    [SerializeField] private Transform ownGate;
    [SerializeField] private Transform enemyGate;

    [Header("Movement")]
    [SerializeField] private float moveForce = 20f;
    [SerializeField] private float maxSpeed = 8f;

    private float isTouchingPuck;

    public override void OnEpisodeBegin()
    {
        isTouchingPuck = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        AddRelativePosition(sensor, puck);
        AddRelativeVelocity(sensor, puckBody);

        AddRelativePosition(sensor, opponent);
        AddRelativeVelocity(sensor, opponentBody);

        AddRelativePosition(sensor, ownGate);
        AddRelativePosition(sensor, enemyGate);

        AddSelfVelocity(sensor);

        sensor.AddObservation(isTouchingPuck);

        if (debugObservations && Time.frameCount % 30 == 0)
        {
            Vector3 puckPos =
                transform.InverseTransformPoint(puck.position);

            Vector3 puckVel =
                transform.InverseTransformDirection(
                    puckBody.linearVelocity - agentBody.linearVelocity);

            Vector3 enemyPos =
                transform.InverseTransformPoint(opponent.position);

            Vector3 ownGatePos =
                transform.InverseTransformPoint(ownGate.position);

            Vector3 enemyGatePos =
                transform.InverseTransformPoint(enemyGate.position);

            Vector3 selfVel =
                transform.InverseTransformDirection(agentBody.linearVelocity);

            /*
            Debug.Log(
                $"[{name}]\n" +
                $"Puck Pos : {puckPos}\n" +
                $"Puck Vel : {puckVel}\n" +
                $"Enemy Pos: {enemyPos}\n" +
                $"Own Gate : {ownGatePos}\n" +
                $"EnemyGate: {enemyGatePos}\n" +
                $"Self Vel : {selfVel}"
            );
            */
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float x = Mathf.Clamp(
            actions.ContinuousActions[0],
            -1f,
            1f
        );

        float z = Mathf.Clamp(
            actions.ContinuousActions[1],
            -1f,
            1f
        );

        Vector3 force = new Vector3(
            x,
            0f,
            z
        );

        agentBody.AddRelativeForce(
            force * moveForce,
            ForceMode.Force
        );

        LimitSpeed();

        /*
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log(
                $"{name}  action=({x:F3}, {z:F3})");
        }
        */
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuous =
            actionsOut.ContinuousActions;

        float horizontal = 0f;

        if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed)
            horizontal = 1f;

        if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed)
            horizontal = -1f;

        float vertical = 0f;

        if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed)
            vertical = 1f;

        if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed)
            vertical = -1f;

        continuous[0] = horizontal;
        continuous[1] = vertical;
    }

    private void Start()
    {   
        //debugTransforms = true;
        //debugObservations = true;
    
        if (!debugTransforms)
            return;

        Debug.Log(
            $"{name}\n" +
            $"Right   : {transform.right}\n" +
            $"Forward : {transform.forward}\n" +
            $"Up      : {transform.up}"
        );
    }

    private void AddRelativePosition(
        VectorSensor sensor,
        Transform target)
    {
        Vector3 localPosition =
            transform.InverseTransformPoint(
                target.position
            );


        sensor.AddObservation(localPosition.x);
        sensor.AddObservation(localPosition.z);
    }

    private void AddRelativeVelocity(
        VectorSensor sensor,
        Rigidbody targetBody)
    {
        Vector3 relativeVelocity =
            targetBody.linearVelocity -
            agentBody.linearVelocity;


        Vector3 localVelocity =
            transform.InverseTransformDirection(
                relativeVelocity
            );


        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);
    }

    private void AddSelfVelocity(
        VectorSensor sensor)
    {
        Vector3 localVelocity =
            transform.InverseTransformDirection(
                agentBody.linearVelocity
            );


        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);
    }

    private void LimitSpeed()
    {
        Vector3 velocity =
            agentBody.linearVelocity;


        if (velocity.magnitude > maxSpeed)
        {
            agentBody.linearVelocity =
                velocity.normalized * maxSpeed;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.rigidbody == puckBody)
        {
            isTouchingPuck = 1f;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.rigidbody == puckBody)
        {
            isTouchingPuck = 0f;
        }
    }
}