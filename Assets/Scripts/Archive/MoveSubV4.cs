using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSubV4 : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask layerMask;

    public GameObject pillarPrefab;
    private Rigidbody subRb;
    private float force = 2f;
    private float speed;
    private float myDrag = 1f;
    //private Vector3 gForceVector = new Vector3(0, -9.81f, 0);
    //private Vector3 lastgps;

    private Vector3 targetPosition;
    private ParticleSystem bubblesLeft;
    private ParticleSystem bubblesRight;

    // Start is called before the first frame update
    void Start()
    {
        targetPosition = transform.position;
        //lastgps = transform.position;
        subRb = GetComponent<Rigidbody>();
        bubblesLeft = GameObject.Find("BubblesLeft").GetComponent<ParticleSystem>();
        bubblesRight = GameObject.Find("BubblesRight").GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        speed = Vector3.Magnitude(subRb.velocity);
        Vector3 startVelocity = subRb.velocity;
        Vector3 newVelocity = subRb.velocity ;
        newVelocity = newVelocity * Mathf.Clamp01(1f - myDrag * Time.deltaTime);
        subRb.velocity = newVelocity;
        float targetDistance;

        Vector3 currentPos = transform.position;
        Vector3 forwardDirection = transform.forward;
        //Debug.Log(speed);
        if (currentPos != targetPosition && targetPosition != new Vector3(0, 0, 0))
        {
            Vector3 newDirection = targetPosition - currentPos;
            Quaternion new_rotation = Quaternion.LookRotation(newDirection - startVelocity);
            Debug.DrawRay(currentPos, forwardDirection * 100, Color.green);

            targetDistance = newDirection.magnitude;
            var stopTime = targetDistance / speed;
            var dragDeceleration = (startVelocity.magnitude - newVelocity.magnitude) / Time.deltaTime;
            var forwardTargetAngle = Vector3.Angle(forwardDirection, newDirection);
            var velocityTargetAngle = Vector3.Angle(startVelocity, newDirection);

            //Debug.Log("stopTime = " + stopTime);
            //Debug.Log("Right side = " + (speed / dragDeceleration));
            //Debug.Log("velocityTargetAngle = " + velocityTargetAngle);

            //if((stopTime < speed / dragDeceleration) && (velocityTargetAngle < 1 || velocityTargetAngle > 170)) { print("Engines OFF"); }

            if (targetDistance < 2 || 
                ((stopTime < speed / dragDeceleration) 
                && (velocityTargetAngle < 1 || velocityTargetAngle > 170))
                )
            {
                targetPosition = new Vector3(0, 0, 0);
                bubblesLeft.Stop();
                bubblesRight.Stop();
            }
            else
            {
                Debug.Log("transform.rotation = " + transform.rotation);
                Debug.Log("bubblesLeft.rotation = " + bubblesLeft.transform.rotation);
                Debug.Log("bubblesLeft.inverse = " + Quaternion.Inverse(bubblesLeft.transform.rotation));
                if ((forwardTargetAngle <= 45) || (forwardTargetAngle > 45 && targetDistance > 20))
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, 8 * speed * Time.deltaTime);
                    subRb.AddForce(force * Mathf.Clamp01(targetDistance / 10) * forwardDirection);
                    print("Applying force " + Mathf.Clamp01(targetDistance / 10));
                    Debug.DrawRay(currentPos, (newDirection - startVelocity) * 100, Color.red);
                    if (transform.rotation == bubblesLeft.transform.rotation)
                    {
                        bubblesLeft.transform.rotation = Quaternion.Inverse(transform.rotation);
                        bubblesRight.transform.rotation = Quaternion.Inverse(transform.rotation);
                        //Debug.Log("net equal = " );
                    }
                }
                else
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Inverse(new_rotation), 8 * speed * Time.deltaTime);
                    subRb.AddForce(force / 2 * Mathf.Clamp01(targetDistance / 10) * (-forwardDirection));
                    print("Applying force " + Mathf.Clamp01(targetDistance / 10));
                    Debug.DrawRay(currentPos, (newDirection - startVelocity) * 100, Color.red);
                    if (transform.rotation != bubblesLeft.transform.rotation)
                    {
                        //bubblesLeft.transform.rotation = Quaternion.Inverse(bubblesLeft.transform.rotation);
                        bubblesLeft.transform.rotation = transform.rotation;
                        bubblesRight.transform.rotation = transform.rotation;
                    }
                }
                if (!bubblesLeft.isPlaying)
                {
                    bubblesLeft.Play();
                    bubblesRight.Play();
                }
                //Debug.Log("targetAngle = " + forwardTargetAngle);
                //Debug.Log("targetDistance = " + targetDistance);
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
                Debug.Log("Target Position = " + targetPosition);
            }
        }
    }
}
