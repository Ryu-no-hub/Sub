using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPTMovesub : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask layerMask;

    public float speed = 5f; // The speed at which the submarine should move
    public float rotationSpeed = 2f; // The speed at which the submarine should rotate
    public float floatHeight = 2f; // The height at which the submarine should float
    public float floatStrength = 0.25f; // The strength of the buoyancy force
    public float pitchStrength = 10f; // The strength of the pitch movement
    public float rollStrength = 10f; // The strength of the roll movement

    private Vector3 targetPosition;

    public GameObject pillarPrefab;
    void Start()
    {
        targetPosition = transform.position;
    }
    // Update is called once per frame
    void Update()
    {
        // Calculate the direction towards the target point
        Vector3 direction = targetPosition - transform.position;

        // Normalize the direction vector
        direction.Normalize();

        // Move the submarine towards the target point
        transform.position += direction * speed * Time.deltaTime;

        // Rotate the submarine to face the target point
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Apply buoyancy force to simulate floating
        Vector3 buoyancyForce = Vector3.up * (floatHeight - transform.position.y) * floatStrength;
        GetComponent<Rigidbody>().AddForce(buoyancyForce, ForceMode.Force);

        // Apply pitch and roll movements based on input
        float pitch = Input.GetAxis("Vertical") * pitchStrength * Time.deltaTime;
        float roll = Input.GetAxis("Horizontal") * rollStrength * Time.deltaTime;
        Debug.Log(pitch + " " + roll);
        transform.Rotate(pitch, 0f, -roll);


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
