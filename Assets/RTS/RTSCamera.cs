using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Simple RTS-style camera controller and input handler for demonstration purposes.
/// </summary>
public class RTSCamera : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float rotateSensitivity = 200f;

    private void Update()
    {
        // Movement
        Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Vector3 dir = Quaternion.Euler(0, transform.localEulerAngles.y, 0) * new Vector3(movement.x, 0, movement.y);
        transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);

        // Rotation
        if (Input.GetMouseButton(1))
        {
            float xRotation = Input.GetAxis("Mouse X");
            //float yRotation = Input.GetAxis("Mouse Y");

            // Rotate around y axis
            transform.Rotate(0, xRotation * rotateSensitivity * Time.deltaTime, 0, Space.World);
            //transform.Rotate(0, 0, -yRotation * rotateSensitivity * Time.deltaTime, Space.Self);
        }

        Vector3 pos = transform.position;
        pos.y -= Input.mouseScrollDelta.y;
        transform.position = pos; 
    }
}
