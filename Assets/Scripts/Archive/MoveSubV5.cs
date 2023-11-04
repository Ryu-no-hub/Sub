using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSubV5 : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask layerMask;

    public GameObject pillarPrefab;
    private Rigidbody subRb;
    private float force = 2f;
    private float speed;
    private float myDrag = 1f;
    int moveMode = 0;
    //private Vector3 gForceVector = new Vector3(0, -9.81f, 0);

    Vector3 forwardDirection;
    Vector3 currentPos;
    Vector3 newDirection;
    Vector3 startVelocity;
    Vector3 newVelocity;
    float forwardTargetAngle;
    float targetDistance;
    Quaternion new_rotation;
    float velocityTargetAngle;
    int rotationSpeedCoeff;
    int decreaseThrustNearCoeff;
    float backTrustCoeff;
    float stopTime;
    float dragDeceleration;

    private Vector3 targetPosition;
    private ParticleSystem bubblesLeft;
    private ParticleSystem bubblesRight;

    // Start is called before the first frame update
    void Start()
    {
        subRb = GetComponent<Rigidbody>();
        bubblesLeft = GameObject.Find("BubblesLeft").GetComponent<ParticleSystem>();
        bubblesRight = GameObject.Find("BubblesRight").GetComponent<ParticleSystem>();
        targetPosition = transform.position;
        forwardDirection = transform.forward;
        currentPos = transform.position;
        newDirection = targetPosition - currentPos;
        forwardTargetAngle = Vector3.Angle(forwardDirection, newDirection);
        targetDistance = newDirection.magnitude;
        backTrustCoeff = 0.5f;
    }

    // Update is called once per frame
    void Update()
    {
        startVelocity = subRb.velocity;
        newVelocity = startVelocity * Mathf.Clamp01(1f - myDrag * Time.deltaTime);
        subRb.velocity = newVelocity;
        if (moveMode != 0)
        {
            speed = Vector3.Magnitude(subRb.velocity);
            //Debug.Log(speed);

            new_rotation = Quaternion.LookRotation(newDirection - startVelocity);
            forwardDirection = transform.forward;
            currentPos = transform.position;
            newDirection = targetPosition - currentPos;
            forwardTargetAngle = Vector3.Angle(forwardDirection, newDirection);
            targetDistance = newDirection.magnitude;

            Debug.DrawRay(currentPos, forwardDirection * 100, Color.green);
            //Debug.DrawRay(currentPos, Quaternion.AngleAxis(-45, Vector3.up) * forwardDirection * 20, Color.yellow);
            //Debug.DrawRay(currentPos, Quaternion.AngleAxis(45, Vector3.up) * forwardDirection * 20, Color.yellow);
            //Debug.DrawRay(currentPos, forwardDirection * 100, Color.green);
            //Debug.DrawRay(currentPos, forwardDirection * 100, Color.green);

            if (moveMode == 1)
            {
                Move(1);
            }
            else if(moveMode == 2)
            {
                Move(-2);
            }
            else
            {
                //print("targetDistance = " + targetDistance);
                //print("forwardTargetAngle = " + forwardTargetAngle);
                if (targetDistance < 15 && forwardTargetAngle > 15f)
                {
                    Move(-1);
                }
                else
                {
                    print("SWITCH to forward = ");
                    moveMode = 1;
                }
            }
            stopTime = targetDistance / speed;
            dragDeceleration = (startVelocity.magnitude - newVelocity.magnitude) / Time.deltaTime;
            velocityTargetAngle = Vector3.Angle(startVelocity, newDirection);

            if (targetDistance < 2 ||
                    ((stopTime < speed / dragDeceleration)
                    && (velocityTargetAngle < 10 || velocityTargetAngle > 170))
                    )
            {
                moveMode = 0;
                targetPosition = new Vector3(0, 0, 0);
                bubblesLeft.Stop();
                bubblesRight.Stop();
            }
        }
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, layerMask))
            {
                targetPosition.x = raycastHit.point.x;
                targetPosition.y = transform.position.y;
                targetPosition.z = raycastHit.point.z;
                Destroy(GameObject.Find("TargetPillar(Clone)"));
                Instantiate(pillarPrefab, raycastHit.point, pillarPrefab.transform.rotation);

                currentPos = transform.position;
                forwardDirection = transform.forward;
                newDirection = targetPosition - currentPos;
                forwardTargetAngle = Vector3.Angle(forwardDirection, newDirection);
                //Debug.Log("forwardTargetAngle = " + forwardTargetAngle);
                targetDistance = newDirection.magnitude;
                //Debug.Log("targetDistance = " + targetDistance);
                if (currentPos != targetPosition && targetPosition != new Vector3(0, 0, 0))
                {

                    print("targetDistance = " + targetDistance);
                    print("forwardTargetAngle = " + forwardTargetAngle);
                    if (forwardTargetAngle <= 45 || (forwardTargetAngle < 155 && targetDistance > 15) || (forwardTargetAngle >= 155 && targetDistance > 15))
                    {
                        moveMode = 1;
                    }
                    else if (forwardTargetAngle > 155)
                    {
                        moveMode = 2;
                    }
                    else
                    {
                        moveMode = 3;
                    }
                    Debug.Log(moveMode);
                }
            }
        }
    }
    void Move(int mode = 1)
    {
        Quaternion myFixedQuaternion = new_rotation;
        rotationSpeedCoeff = 700;
        decreaseThrustNearCoeff = 20;
        if (mode == 1)
        {
            backTrustCoeff = 1;
        }
        else if(mode == -1)
        {
            backTrustCoeff = 0.5f;
        }
        else
        {
            backTrustCoeff = 0.5f;
            //new_rotation = Quaternion.FromToRotation(newDirection - startVelocity, forwardDirection) ;
            new_rotation *= Quaternion.AngleAxis(Vector3.Angle(newDirection, forwardDirection), transform.up);
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, Mathf.Sqrt(speed * rotationSpeedCoeff) * Time.deltaTime);
        Debug.DrawRay(currentPos, (newDirection - startVelocity) * 100, Color.red);

        subRb.AddForce(force * backTrustCoeff * Mathf.Clamp01(targetDistance / decreaseThrustNearCoeff) * Mathf.Clamp(mode, -1, 1) * forwardDirection);
        //print("Applying force " + force * backTrustCoeff * Mathf.Clamp01(targetDistance / decreaseThrustNearCoeff));

        if (mode == 1 && transform.rotation == bubblesLeft.transform.rotation)
        {
            bubblesLeft.transform.rotation = Quaternion.Inverse(transform.rotation);
            bubblesRight.transform.rotation = Quaternion.Inverse(transform.rotation);
        }
        else if ((mode == -1 || mode == -2) && transform.rotation != bubblesLeft.transform.rotation)
        {
            bubblesLeft.transform.rotation = transform.rotation;
            bubblesRight.transform.rotation = transform.rotation;
        }
        if (!bubblesLeft.isPlaying)
        {
            bubblesLeft.Play();
            bubblesRight.Play();
        }
    }
}