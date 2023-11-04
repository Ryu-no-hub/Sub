using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSubV3 : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask layerMask;

    public GameObject pillarPrefab;
    private Rigidbody subRb;
    private float force = 3f;
    private float speed;
    private float myDrag = 1f;
    //private Vector3 gForceVector = new Vector3(0, -9.81f, 0);
    //private Vector3 lastgps;

    private Vector3 targetPosition;
    private ParticleSystem bubblesLeft ;
    private ParticleSystem bubblesRight ;

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
            //Quaternion new_rotation = Quaternion.LookRotation(newDirection);
            Quaternion correction = Quaternion.LookRotation(startVelocity - newDirection);
            Debug.DrawRay(currentPos, (newDirection - startVelocity) * 100, Color.red);
            Debug.DrawRay(currentPos, forwardDirection * 100, Color.green);
            //if(Vector3.Angle(targetPosition, currentPos) < 180) { }
            //Debug.Log("Angle = " + Vector3.Angle(forwardDirection, newDirection));

            targetDistance = newDirection.magnitude;
            var stopTime = targetDistance / speed;
            var dragDeceleration = (startVelocity.magnitude - newVelocity.magnitude) / Time.deltaTime;
            //Debug.Log("stopTime = " + stopTime);
            //Debug.Log("Right side = " + (speed / dragDeceleration));

            //if (targetDistance < 1f || ((stopTime < speed/dragDeceleration) && Vector3.Angle(forwardDirection, newDirection) < 1))
            if (targetDistance < 1f || ((stopTime < speed/dragDeceleration) && Vector3.Angle(startVelocity, newDirection) < 1))
            {
                targetPosition = new Vector3(0, 0, 0);
                bubblesLeft.Stop();
                bubblesRight.Stop();
                //Destroy(GameObject.Find("TargetPillar(Clone)"));
            }
            else
            {
                //transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, 100 * forwardDirection.magnitude * Time.deltaTime);
                //transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation * correction, 10 * speed * Time.deltaTime);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, 10 * speed * Time.deltaTime);

                subRb.AddForce(forwardDirection * force * Mathf.Clamp01(targetDistance * 5));
                if (!bubblesLeft.isPlaying)
                {
                    bubblesLeft.Play();
                    bubblesRight.Play();
                }
                //print("Applying force " + forwardDirection.magnitude);
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
