using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [SerializeField]
    private Camera mainCamera;
    [SerializeField]
    private bool userControls; // if true - user controls the robot, otherwise - drives by itself
    [SerializeField]
    private float wheelSpeedUser = 10; // wheel speed multiplier - user controls
    [SerializeField]
    private float wheelSpeedAuto = 10; // wheel speed multiplier - autonomous
    [SerializeField] 
    private float cameraDistanceToTarget = 2;
    [SerializeField]
    private float rayLength = 1;
    [SerializeField]
    private float rayReach = 4;
    [SerializeField]
    private int rayCount = 13;

    // robot body
    [SerializeField]
    private GameObject robot;
    // wheels
    [SerializeField]
    private Transform leftWheel;
    [SerializeField]
    private Transform rightWheel;
    [SerializeField]
    private WheelCollider leftWheelCollider;
    [SerializeField]
    private WheelCollider rightWheelCollider;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }
    
    // Update is called once per frame
    void FixedUpdate()
    {
        float forceLeftWheel = 0, forceRightWheel = 0;
        if (userControls)
        {
            // receive user inputs
            float inputVert = Input.GetAxis("Vertical");
            float inputHoriz = Input.GetAxis("Horizontal");

            forceLeftWheel = -inputVert * wheelSpeedUser;
            forceRightWheel = -inputVert * wheelSpeedUser;

            if (inputHoriz > 0)
                forceLeftWheel -= inputHoriz * wheelSpeedUser;
            if (inputHoriz < 0)
                forceRightWheel += inputHoriz * wheelSpeedUser;

        }
        else
        {
            // shoot rays
            float raySpread = rayReach / (float)rayCount;
            float minDistLeft = float.PositiveInfinity, minDistRight = float.PositiveInfinity;
            for (float d = -rayReach/2f; d <= rayReach/2f; d += raySpread)
            {
                Vector3 vecSide = robot.transform.right * d;
                Vector3 vecStart = robot.transform.position + robot.transform.up * 0.05f; // lift rays up a bit
                Vector3 vecRay = -robot.transform.forward * rayLength + vecSide;
                
                Debug.DrawRay(vecStart, vecRay, Color.red);
                bool hit = Physics.Raycast(vecStart, vecRay, out RaycastHit rayHit);

                if(hit && rayHit.collider.gameObject.tag != "floor" && rayHit.distance < rayLength)
                {
                    if (d > 0 && minDistLeft > rayHit.distance)
                        minDistLeft = rayHit.distance;
                    else if (d < 0 && minDistRight > rayHit.distance)
                        minDistRight = rayHit.distance;
                }
            }

            // drive straight
            forceLeftWheel = -wheelSpeedAuto;
            forceRightWheel = -wheelSpeedAuto;

            // decide which wheel to speed up
            if (minDistLeft <= rayLength || minDistRight <= rayLength)
            {
                if (minDistLeft < minDistRight)
                {
                    forceLeftWheel -= (rayLength - minDistLeft) * wheelSpeedAuto * 3;
                    //forceRightWheel += (rayLength - minDistLeft);// * wheelSpeedAuto * 1;
                    forceRightWheel = 0;
                }
                else
                {
                    forceRightWheel -= (rayLength - minDistRight) * wheelSpeedAuto * 3;
                    //forceLeftWheel += (rayLength - minDistRight);// * wheelSpeedAuto * 1;
                    forceLeftWheel = 0;
                }
            }

            // stop and turn until free to go
            float stopDist = 0.9f;
            if (minDistLeft < stopDist && minDistRight < stopDist)
            {
                Debug.Log($"MinLeft: {minDistLeft} --- MinRight: {minDistRight}");

                forceLeftWheel = wheelSpeedAuto;
                forceRightWheel = -wheelSpeedAuto;
            }
        }

        // make movement
        leftWheelCollider.motorTorque = forceLeftWheel;
        rightWheelCollider.motorTorque = forceRightWheel;
        //ApplyPositionToWheel(leftWheelCollider, leftWheel);
        //ApplyPositionToWheel(rightWheelCollider, rightWheel);
    }

    public void ApplyPositionToWheel(WheelCollider wheelCollider, Transform wheel)
    {
        Vector3 position;
        Quaternion rotation;
        wheelCollider.GetWorldPose(out position, out rotation);

        wheel.transform.position = position;
        wheel.transform.rotation = rotation;
    }
    
    private Vector3 previousPosition;
    
    void Update()
    {
        Vector3 newPosition = mainCamera.ScreenToViewportPoint(Input.mousePosition);
        Vector3 direction = previousPosition - newPosition;
            
        float rotationAroundYAxis = -direction.x * 180; // camera moves horizontally
        float rotationAroundXAxis = direction.y * 180; // camera moves vertically
            
        mainCamera.transform.position = robot.transform.position;
            
        mainCamera.transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);
        mainCamera.transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World); // <— This is what makes it work!
            
        mainCamera.transform.Translate(new Vector3(0, 0, -cameraDistanceToTarget));
            
        previousPosition = newPosition;
    }

}
