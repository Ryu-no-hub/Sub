using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSubV7 : MonoBehaviour, ISelectable
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
    //float myTerrainHeight;
    Vector3 horProjection;

    public Vector3 targetPosition;
    public Vector3 oldTargetPosition;
    private ParticleSystem bubblesLeft;
    private ParticleSystem bubblesRight;
    private GameObject selectionSprite;
    private float angleUp;
    //private bool newOrderFlag = false;

    public int team = 1;
    public int Team
    {
        get { return team; }
    }

    public void Select()
    {
        //GetComponent<MeshRenderer>().material.color = Color.red;
        selectionSprite.SetActive(true);
    }

    public void Deselect()
    {
        selectionSprite.SetActive(false);
    }


    // Start is called before the first frame update
    void Start()
    {
        subRb = GetComponent<Rigidbody>();
        bubblesLeft = transform.Find("BubblesLeft").GetComponent<ParticleSystem>();
        bubblesRight = transform.Find("BubblesRight").GetComponent<ParticleSystem>();
        targetPosition = Vector3.zero;
        forwardDirection = transform.forward;
        currentPos = transform.position;
        newDirection = targetPosition - currentPos;
        forwardTargetAngle = Vector3.Angle(forwardDirection, newDirection);
        targetDistance = newDirection.magnitude;
        rotationSpeedCoeff = 1500;
        backTrustCoeff = 0.5f;
        selectionSprite = transform.Find("Selection Sprite").gameObject;
        //GetComponent<MeshRenderer>().material.color = Color.white;
    }

    // Update is called once per frame
    void Update()
    {
        //float turn = Input.GetAxis("Horizontal");
        //subRb.AddTorque(transform.up * 1 * turn);

        startVelocity = subRb.velocity;
        newVelocity = startVelocity * Mathf.Clamp01(1f - myDrag * Time.deltaTime);
        subRb.velocity = newVelocity;
        angleUp = Vector3.Angle(Vector3.up, transform.up);
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
            else if (moveMode == 2)
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
                    //print("Switching to forward thrust");
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
                targetPosition = Vector3.zero;
                bubblesLeft.Stop();
                bubblesRight.Stop();
            }
        }
        else if (angleUp > 200) // Align
        {
            print("Aligning,  angle = " + angleUp);

            horProjection = Vector3.ProjectOnPlane(forwardDirection, Vector3.up);
            //Debug.Log(horProjection);
            Debug.DrawRay(transform.position, horProjection * 1000, Color.cyan);
            Debug.DrawRay(transform.position, Vector3.up, Color.yellow);

            Quaternion planeRotation = Quaternion.LookRotation(horProjection, Vector3.up);
            if (angleUp > 90) { planeRotation = Quaternion.Inverse(planeRotation); }
            //transform.rotation = Quaternion.RotateTowards(transform.rotation, planeRotation, 0.2f * Vector3.Angle(forwardDirection, Vector3.forward) * Time.deltaTime);

            float xDegreeDelta;
            float zDegreeDelta;
            //if (transform.localEulerAngles.x <= 180)
            //    xDegreeDelta = transform.localEulerAngles.x * 0.05f;
            //else
            //    xDegreeDelta = (360 - transform.localEulerAngles.x) * 0.05f;
            xDegreeDelta = (transform.localEulerAngles.x - 360) * 0.05f;

            //if (transform.localEulerAngles.z <= 180)
            //    zDegreeDelta = transform.localEulerAngles.z * 0.05f;
            //else
            //    zDegreeDelta = (360 - transform.localEulerAngles.z) * 0.05f;
            zDegreeDelta = (transform.localEulerAngles.z) * 0.05f;

            Debug.Log(gameObject.name + " Before " + transform.localEulerAngles);
            //transform.localEulerAngles += new Vector3(xDegreeDelta, 0, -zDegreeDelta) * Time.deltaTime;
            Debug.Log("After " + transform.localEulerAngles);
            //Debug.Log(Vector3.ProjectOnPlane(transform.up, Vector3.up));
            //if (Vector3.ProjectOnPlane(transform.up, Vector3.up).x >= 0) { torqueForce = 0.05f; Debug.Log("Neg"); } else { torqueForce = -0.05f; }
            //if (angleUp < 90) { torqueForce = -0.05f; Debug.Log("Neg"); } else { torqueForce = 0.1f; }
            //subRb.AddTorque(horProjection * torqueForce, ForceMode.Force);
            //subRb.AddTorque(transform.right * torqueForce, ForceMode.Force);
        }

        if (targetPosition != Vector3.zero && oldTargetPosition != targetPosition)
        //if (targetPosition != Vector3.zero)
        {
            //Debug.Log("Old Target " + oldTargetPosition);
            //Debug.Log("New Target " + targetPosition);
            //print("New order");
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
            //Debug.Log(moveMode);

            //RaycastHit hitTerrain;
            //if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out hitTerrain, Mathf.Infinity, layerMask))
            //{
            //    myTerrainHeight = hitTerrain.point.y;
            //    targetPosition.y += (transform.position.y - myTerrainHeight);
            //}
            oldTargetPosition = targetPosition;
            //Debug.Log("Old Target = new Target = " + oldTargetPosition);
        }
    }
    void Move(int mode = 1)
    {
        Quaternion myFixedQuaternion = new_rotation;
        //rotationSpeedCoeff = 700;
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
        //Debug.DrawRay(currentPos, Vector3.forward * 100, Color.yellow);
        //Debug.DrawRay(currentPos, forwardDirection* 100, Color.cyan);

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