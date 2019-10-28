using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;

public class RollerAgent : Agent
{
    Rigidbody rBody;
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
    }

    public Transform Target;
    public Transform OtherAgent;
    public ArenaController arena;
    public float lastShootTime;
    public float lastRestartTime;
    public override void AgentReset()
    {
        // Reset the last shoot time
        lastShootTime = Time.time;

        // Reset angular velocity, and randomize real velocity
        this.rBody.angularVelocity = Vector3.zero;
        this.rBody.velocity = new Vector3(Mathf.Lerp(-3, 3, Random.value),
                                          0f,
                                          Mathf.Lerp(-3, 3, Random.value));
        
        // Move the target to a new spot
        Target.localPosition = new Vector3(Mathf.Lerp(-8, 5, Random.value),
                                           1f,
                                           Mathf.Lerp(-5, 7, Random.value));
    }

    public override void CollectObservations()
    {
        // Rewards
        float distanceToTarget      = Vector3.Distance(this      .transform.localPosition, Target.position);
        float otherDistanceToTarget = Vector3.Distance(OtherAgent.transform.localPosition, Target.position);

        // Target and Agent positions
        AddVectorObs(Target.position);
        AddVectorObs(this.transform.localPosition);
        AddVectorObs(distanceToTarget);
        AddVectorObs(OtherAgent.transform.localPosition);
        AddVectorObs(otherDistanceToTarget);

        // Agent velocity
        AddVectorObs(rBody.velocity.x);
        AddVectorObs(rBody.velocity.z);
    }


    public float speed = 10;
    public float boltSpeed = 10;
    public float boltEnergy = .1f;
    public GameObject bolt;
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        // Get outputs and use to control direction
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = vectorAction[0];
        controlSignal.z = vectorAction[1];

        // Get output and if it's over .5, we'll shoot
        float shootWillingness = vectorAction[2];

        // This is the direction to shoot in (only relevant if previous is over .5)
        Vector3 shootDir = Vector3.zero;
        shootDir.x = vectorAction[3];
        shootDir.z = vectorAction[4];

        if (shootWillingness > .5 && Time.time - lastShootTime > 1)
        {
            lastShootTime = Time.time;
            this.rBody.angularVelocity = this.rBody.angularVelocity * (1 - boltEnergy);
            this.rBody.velocity = this.rBody.velocity * (1 - boltEnergy);
            var b = Instantiate(bolt, transform.position, Quaternion.identity);
            b.GetComponent<BoltController>().arena  = arena;
            b.GetComponent<BoltController>().target = OtherAgent.gameObject;
            b.GetComponent<Rigidbody>().velocity    = shootDir * boltSpeed;
        }

        var hittingBolts = from cont in arena.bolts
                           where cont.target == gameObject
                           where Vector3.Distance(cont.transform.position, this.transform.position) < 1.42f
                           select cont;

        if (hittingBolts.Count() != 0)
        {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = hittingBolts.First().GetComponent<Rigidbody>().velocity.normalized * boltSpeed / 2;
            foreach (var bolt in hittingBolts)
            {
                Destroy(bolt.gameObject);
            }
        }
        else
        {
            rBody.AddForce(controlSignal * speed);
        }


        // Reward
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);

        // Hit the target!
        if (distanceToTarget < 1.42f)
        {
            lastRestartTime = Time.time;
            SetReward(1);
            Done();
        }

        // Taking too long, just restart
        if (Time.time - lastRestartTime > 500)
        {
            lastRestartTime = Time.time;
            Done();
        }

    }
}