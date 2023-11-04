using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSub : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask layerMask;
    private Vector3 targetPosition;
    private int rotate_speed = 200;
    private int speed = 10;

    // Start is called before the first frame update
    void Start()
    {
        targetPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.position != targetPosition)
        {
            Vector3 newDirection = targetPosition - transform.position;
            Quaternion new_rotation = Quaternion.LookRotation(newDirection);
            Debug.DrawRay(transform.position, newDirection*100, Color.red);
            Debug.DrawRay(transform.position, transform.forward*100, Color.green);

            Debug.Log(newDirection);
            if (transform.rotation != new_rotation )
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, rotate_speed * Time.deltaTime);
            }
            else
            {
                if (newDirection.magnitude > new Vector3(0.02f, 0.02f, 0.02f).magnitude)
                {
                    transform.Translate(speed * Time.deltaTime * Vector3.forward);
                }
                else
                {
                    transform.position = targetPosition;
                }
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
                Debug.Log(targetPosition);
            }
        }
    }
}
