using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player_control : MonoBehaviour
{
    private CharacterController dogController;
    private Vector3 dogVelocity;

    public Transform camera;
    public UI UIScript;

    private static float dogSpeed = 10;
    private static float dogSprintSpeed = 15;
    private static float dogInteractRange = 5;
    private static float dogTotalEnergy = 5;
    private float dogEnergy = dogTotalEnergy;
    private bool sprintEnabled = true;
    private static float dogStrength = 75000;
    private static float dogGravity = -20;
    private static float dogJumpHeight = 10;
    private static int coyoteTime = 30;
    private static float dogRotationSpeed = 5;
    private float cameraPitch = 0;
    private static int digMask = 1 << 3;
    private static int pushMask = 1 << 9;

    // Start is called before the first frame update
    void Start()
    {
        dogController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        rotation();
        translation();
        interact();
        UIScript.energy = dogEnergy;
    }

    //code controlling player and camera rotation
    void rotation()
    {
        //the mouse's vertical movement is read and added to the camera pitch variable, which is limited to +-60 degrees
        cameraPitch = Mathf.Clamp(cameraPitch - Input.GetAxis("Mouse Y") * dogRotationSpeed, -60f, 60f);
        //camera's angles updated with the new cameraPitch
        camera.localEulerAngles = new Vector3(cameraPitch, 0, 0);

        //the mouses horizontal movement is read and directly used to rotate the game object, the camera also rotates as it is a child
        float mouseX = Input.GetAxis("Mouse X") * dogRotationSpeed;
        transform.Rotate(0, mouseX, 0);
    }

    //code controlling player movement and jumping
    void translation()
    {
        //movement
        //the translation inputs are read
        Vector2 inputs = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        //the vector is normalised, to ensure that travelling diagonally isn't faster
        inputs.Normalize();

        //the speed is scaled, depending on if sprint is enabled
        //if sprint is held, energy is remaining, and sprint is enabled...
        if (Input.GetButton("Fire3") && dogEnergy > 0 && sprintEnabled)
        {
            //the higher speed is used and the sprint time decreases
            inputs *= dogSprintSpeed;
            dogEnergy -= 2 * Time.deltaTime;
        }

        //otherwise...
        else
        {
            //the regular speed is used
            inputs *= dogSpeed;

            //energy recovers if it is not full
            if (dogEnergy < dogTotalEnergy)
            {
                dogEnergy += Time.deltaTime;
                sprintEnabled = false;
            }

            //and is re-enabled if it is full
            else
            {
                sprintEnabled = true;
            }
        }

        //the input vector is rotated around the y-axis by the game object's current y angle, to convert it to the local frame of the game object, as the character controller cannot rotate
        dogVelocity = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0) * new Vector3(inputs.x, dogVelocity.y, inputs.y);


        //jumping
        //if the player is on the ground, reset the ledge tolerance, and if moving down, reset vertical velocity to 0
        if (dogController.isGrounded)
        {
            coyoteTime = 30;
            if (dogVelocity.y < 0)
            {
                dogVelocity.y = 0;
            }
        }
        //if player jumps while on the ground (or within ledge tolerance) the velocity vector is updated to give a burst of velocity upwards
        if ((dogController.isGrounded || coyoteTime > 0) && Input.GetButtonDown("Jump"))
        {
            dogVelocity.y = dogJumpHeight;
            coyoteTime = 0;
        }
        //apply gravity
        dogVelocity.y += dogGravity * Time.deltaTime;
        coyoteTime -= 1;


        //move dog according to inputs, Time.deltaTime is used to make movement framerate independant, keeping speed consistent
        dogController.Move(dogVelocity * Time.deltaTime);
    }

    //code to detect and delete tree stumps, and push boulders
    void interact()
    {
        //a bitshift mask is used to only look for collisions on layer 3, which contains the tree stumps, the range is limited by dogInteractRange 
        RaycastHit digRayHit;
        if (Physics.Raycast(camera.position, camera.forward, out digRayHit, dogInteractRange, digMask) && Input.GetButtonDown("Fire1"))
        {
            //destroy the tree stump that was hit by the raycast
            Destroy(digRayHit.transform.gameObject);
        }

        RaycastHit pushRayHit;
        //similar to the prvious code, a mask is used to only look at layer 9, with an additional check to see if the dog has energy
        if (Physics.Raycast(camera.position, camera.forward, out pushRayHit, dogInteractRange, pushMask) && Input.GetButtonDown("Fire2") && dogEnergy >= dogTotalEnergy)
        {
            //apply an impulse to the object's rigid body, along a vector from the player to the boulder
            //the vector is normalised to ensure that standing further away doesn't give a bigger push
            pushRayHit.rigidbody.AddForce((pushRayHit.transform.position - transform.position).normalized * dogStrength, ForceMode.Impulse);
            //use up all the dog's energy
            dogEnergy = 0;
        }
    }
}
