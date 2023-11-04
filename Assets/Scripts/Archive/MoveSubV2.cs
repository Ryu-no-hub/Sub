using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSubV2 : MonoBehaviour
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

    // Start is called before the first frame update
    void Start()
    {
        targetPosition = transform.position;
        //lastgps = transform.position;
        subRb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        speed = Vector3.Magnitude(subRb.velocity);
        Vector3 startVelocity = subRb.velocity;
        Vector3 newVelocity = subRb.velocity ;
        newVelocity = newVelocity * Mathf.Clamp01(1f - myDrag * Time.deltaTime);
        subRb.velocity = newVelocity;

        Vector3 currentPos = transform.position;
        Vector3 forwardDirection = transform.forward;
        //Debug.Log(speed);
        if (currentPos != targetPosition && targetPosition != new Vector3(0, 0, 0))
        {
            Vector3 newDirection = targetPosition - currentPos;
            Quaternion new_rotation = Quaternion.LookRotation(newDirection);
            Debug.DrawRay(currentPos, newDirection*100, Color.red);
            Debug.DrawRay(currentPos, forwardDirection * 100, Color.green);

            Debug.Log("newDirection.magnitude/speed = " + newDirection.magnitude/speed);
            //Debug.Log("Right side = " + speed / ((newVelocity.magnitude - startVelocity.magnitude) / Time.deltaTime));
            //Debug.Log("Right side = " + speed / ((startVelocity.magnitude - newVelocity.magnitude) / Time.deltaTime));
            Debug.Log("Right side = " + (startVelocity.magnitude - newVelocity.magnitude));

            if (newDirection.magnitude > 2f && (speed == 0 || newDirection.magnitude/speed > speed/((startVelocity.magnitude - newVelocity.magnitude)/Time.deltaTime)))
            {
                //transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, 100 * forwardDirection.magnitude * Time.deltaTime);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, 10 * speed * Time.deltaTime);
                subRb.AddForce(forwardDirection * force);
                //print("Applying force " + forwardDirection.magnitude);
            }
            else
            {
                //transform.position = targetPosition;
                targetPosition = new Vector3(0, 0, 0);
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
