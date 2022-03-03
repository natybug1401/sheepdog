using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class wolf_nav : MonoBehaviour
{
    private CharacterController wolfController;
    private NavMeshAgent wolfNavAgent;

    private Transform currentTarget;
    public Transform centreTarget;
    public Transform houseTarget;
    public Transform smallHillTarget;
    public Transform barnTarget;
    public Transform largeHillTarget;
    private List<Transform> targetList;


    private int state = 0;
    private float wolfEatRadius = 1.5f;
    private GameObject currentSheep;

    public static float wolfNewTargetRadius = 15;
    public static float wolfChangeTargetRadius = 5;
    private static float minCollisionSpeed = 1;

    private float restTimer;
    private static float hurtTimerInitial = 10;
    private float hurtTimer = hurtTimerInitial;

    //Layer 7 contains only sheep
    private int sheepMask = 1 << 7;

    // Start is called before the first frame update
    void Start()
    {
        wolfController = GetComponent<CharacterController>();
        wolfNavAgent = GetComponent<NavMeshAgent>();
        restTimer = Random.Range(2f, 7f);

        targetList = new List<Transform> { centreTarget, houseTarget, smallHillTarget, barnTarget, largeHillTarget };
        currentTarget = centreTarget;
    }

    // Update is called once per frame
    void Update()
    {
        //switch case used to implement FSM behaviour by setting state for next loop
        switch (state)
        {
            //seek
            //wolf will patrol between checkpoints, keeping an eye out for sheep
            case 0:
                wolfNavAgent.speed = 4;
                //if the wolf has reached it's target...
                if (wolfNavAgent.remainingDistance < 1)
                {
                    //randomly pick a new target from the list to patrol to
                    int index = Random.Range(0, (targetList.Count));
                    currentTarget = targetList[index];
                }
                //check for and process any nearby sheep
                locateSheep(wolfNewTargetRadius);
                eatSheep();
                //move towards the current patrol checkpoint
                wolfNavAgent.destination = currentTarget.position;
                break;

            //hunt
            //wolf will chase the target sheep until it leaves range, another sheep comes very close, or the sheep is eaten
            case 1:
                wolfNavAgent.speed = 2;
                //move towards the targetted sheep, as long as it is still within range (plus a margin), if not, got back to seeking
                locateSheep(wolfChangeTargetRadius);
                eatSheep();
                if (((currentSheep.transform.position - transform.position).magnitude) > wolfNewTargetRadius + 2)
                {
                    state = 0;
                }
                wolfNavAgent.destination = currentSheep.transform.position;
                break;
            //rest
            //the wolf will rest after eating or being hurt
            case 2:
                //sit still for 3-6 seconds
                wolfNavAgent.destination = transform.position;
                if (restTimer > 0)
                {
                    restTimer -= Time.deltaTime;
                }
                //then reset timer and seek
                else
                {
                    restTimer = Random.Range(3, 6);
                    state = 0;
                }
                break;

            //hurt
            //the wolf will be unable to move after being hit by a rock or log
            case 3:
                //if hit by a log or boulder, sit still for 10 seconds
                wolfNavAgent.destination = transform.position;
                if (hurtTimer > 0)
                {
                    hurtTimer -= Time.deltaTime;
                }
                //then reset the timer and rest
                else
                {
                    hurtTimer = hurtTimerInitial;
                    state = 2;
                }
                break;

            default:
                //reset state to seek, should never be needed
                state = 0;
                break;
        }
    }
    //the wolf will eat any sheep within it's eating range, then rest
    void eatSheep()
    {
        //detect nearby sheep (layer mask for sheep layer(7) only), using transform.forward to offset the sphere forwards slightly
        Collider[] sphere = Physics.OverlapSphere(transform.position + 2.5f * transform.forward, wolfEatRadius, sheepMask);

        //change the state to rest
        if (sphere.Length > 0)
        {
            state = 2;
        }

        //Iterate through and destroy each sheep in eating range
        List<GameObject> neighbours = new List<GameObject>();
        foreach (Collider c in sphere)
        {
            Destroy(c.gameObject);
        }
    }

    //occurs if the wolf is seeking or hunting, will detect nearby sheep
    //search radius depends on if the wolf is seeking or hunting
    void locateSheep(float searchRadius)
    {
        //detect sheep within wolfTargetRadius
        Collider[] sphere = Physics.OverlapSphere(transform.position, searchRadius, sheepMask);
        //iterate through the sheep if any are found within range to find the closest
        if (sphere.Length > 0)
        {
            //set the initial closest to the max + 1
            float closestDistance = (searchRadius * searchRadius) + 1;
            //set the first sheep in the array as an initial target
            currentSheep = sphere[0].gameObject;

            //Iterate through and find the closest sheep
            foreach (Collider c in sphere)
            {
                //find the closest sheep, the square is used to avoid the root operation, improving performance
                if ((c.gameObject.transform.position - transform.position).sqrMagnitude < closestDistance)
                {
                    currentSheep = c.gameObject;
                }
            }
            //start hunting
            state = 1;
        }
    }

    //if the wolf collides with a rigid body
    void OnCollisionEnter(Collision collision)
    {
        //if the collision with the boulder or log was fast enough...
        if (collision.relativeVelocity.magnitude > minCollisionSpeed)
        {
            //if the wolf was hit with a boulder...
            if (collision.collider.gameObject.layer == 9)
            {
                //TODO award bonus points
            }
            //destroy the boulder/log and put the wolf into the hurt state of its FSM
            Destroy(collision.collider.gameObject);
            state = 3;
        }
    }
}
