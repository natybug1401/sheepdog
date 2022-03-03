using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sheep_flocking : MonoBehaviour
{

    private CharacterController sheepController;
    private Vector3 sheepVelocity;
    private float sheepGravity = -20;
    private float sheepSpeed = 7.5f;
    private float sheepRotationSpeed = 3;
    private Quaternion rotation;

    //Layer 6 contains things that the sheep will not avoid, which is the terrain and the wolves
    //Therefore, the inverse is used to ensure that only layer 6 is ignored but the rest are tested
    private int sheepAvoidMask = ~(1 << 6);
    //Layer 7 contains only sheep
    private int sheepMask = 1 << 7;
    //Layer 8 contains only the sheepdog
    private int sheepdogMask = 1 << 8;

    private Vector3 separationVector = Vector3.zero;
    private Vector3 avoidanceVector = Vector3.zero;
    private Vector3 alignmentVector = Vector3.zero;
    private Vector3 cohesionVector = Vector3.zero;
    private Vector3 combinedVector = Vector3.zero;

    public float separationRadius = 2;
    public float avoidanceRadius = 3;
    public float alignmentAndCohesionRadius = 4;

    public float separationStrength = 0.75f;
    public float avoidanceStrength = 1f;
    public float alignmentStrength = 0.05f;
    public float cohesionStrength = 0.02f;


    // Start is called before the first frame update
    void Start()
    {
        sheepController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        moveBoid();
    }

    void moveBoid()
    {
        //calculate vectors for aligning with and approaching the local flock
        (alignmentVector, cohesionVector) = alignmentAndCohesion();
        //calculate vector to avoid nearby boids
        separationVector = separation();
        //calculate vector to avoid the sheepdog
        avoidanceVector = avoidDog();


        //scale
        separationVector *= separationStrength;
        avoidanceVector *= avoidanceStrength;
        alignmentVector *= alignmentStrength;
        //convert cohesion vector to local frame then scale
        cohesionVector -= transform.position;
        cohesionVector *= cohesionStrength;

        //combine new vectors with existing vector
        combinedVector += separationVector + avoidanceVector + alignmentVector + cohesionVector;

        //limit speed
        if (combinedVector.magnitude > sheepSpeed)
        {
            combinedVector.Normalize();
            combinedVector *= sheepSpeed;
        }

        //reset gravity if grounded
        if (sheepController.isGrounded && sheepVelocity.y < 0)
        {
            sheepVelocity.y = 0;
        }
        //apply gravity
        combinedVector.y += sheepGravity * Time.deltaTime;


        //translate sheep according to the combined vector, Time.deltaTime is used to make movement framerate independant, keeping speed consistent
        sheepController.Move(combinedVector * Time.deltaTime);

        //rotate the sheep towards the combined vector, again using Time.deltaTime.
        //Only x and z are used to avoid the sheep looking into the floor due to gravity
        rotation = Quaternion.LookRotation(new Vector3(combinedVector.x, 0, combinedVector.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * sheepRotationSpeed);
    }



    //avoid neighbouring boids (and other objects)
    Vector3 separation()
    {
        //detect nearby sheep and objects
        Collider[] sphere = Physics.OverlapSphere(transform.position, separationRadius, sheepAvoidMask);

        //Iterate through and store the gameObjects for each obstacle, but not the boid itself, in a list
        List<GameObject> obstacles = new List<GameObject>();
        foreach (Collider c in sphere)
        {
            if (c.name != gameObject.name)
            {
                obstacles.Add(c.gameObject);
            }
        }

        Vector3 obstaclesSum = Vector3.zero;
        //return a zero vector if there are no obstacles, to avoid dividing by 0
        if (obstacles.Count == 0)
        {
            return obstaclesSum;
        }

        //loop through list of neighbours
        foreach (GameObject currentObstacle in obstacles)
        {
            //calculate a vector from each obstacle to the boid
            Vector3 obstacleSeparation = (transform.position - currentObstacle.transform.position);
            //divide the vectors by the magnitude squared, to normalise then scale inversely to length
            obstaclesSum += obstacleSeparation / obstacleSeparation.sqrMagnitude;
        }
        //and return the average 
        return obstaclesSum / obstacles.Count;
    }

    //using similar code to separation, but simplified as there is only one possible object, and with different radius and strength variables, avoid the player sheepdog
    Vector3 avoidDog()
    {
        //detect if dog is nearby
        Collider[] sphere = Physics.OverlapSphere(transform.position, separationRadius, sheepdogMask);
        Vector3 dogVector = Vector3.zero;

        //if the dog is present
        if (sphere.Length != 0)
        {
            //calculate a vector from the dog to the sheep
            dogVector = (transform.position - sphere[0].transform.position);
            //divide the vectors by the magnitude squared, to normalise then scale inversely to length
            dogVector += dogVector / dogVector.sqrMagnitude;
        }
        //and return the vector 
        return dogVector;
    }




    //returns vectors pointing towards the average position and heading of the local flock
    (Vector3, Vector3) alignmentAndCohesion()
    {
        //detect nearby sheep (layer mask for sheep layer(7) only)
        Collider[] sphere = Physics.OverlapSphere(transform.position, alignmentAndCohesionRadius, sheepMask);
        //Iterate through and store the gameObjects for each neighbour, but not the boid itself, in a list
        List<GameObject> neighbours = new List<GameObject>();
        foreach (Collider c in sphere)
        {
            if (c.name != gameObject.name)
            {
                neighbours.Add(c.gameObject);
            }
        }

        Vector3 headingSum = Vector3.zero;
        Vector3 positionSum = Vector3.zero;
        //return a zero vector if there are no obstacles, to avoid dividing by 0
        if (neighbours.Count == 0)
        {
            return (headingSum, positionSum);
        }

        //loop through list of neighbours
        foreach (GameObject currentNeighbour in neighbours)
        {
            //and sum their heading vectors (local frame, as is just a directional unit vector)
            headingSum += currentNeighbour.transform.forward;
            //and positions
            positionSum += currentNeighbour.transform.position;
        }
        //and return the average 
        return (headingSum / neighbours.Count, positionSum / neighbours.Count);
    }


    void OnDrawGizmos()
    {
        //red = alignment
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, alignmentVector);
        //green = cohesion
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, cohesionVector);
        //blue = separation
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, separationVector);
        //black = combined
        Gizmos.color = Color.black;
        Gizmos.DrawRay(transform.position, new Vector3(combinedVector.x, 0, combinedVector.z));
    }
}
