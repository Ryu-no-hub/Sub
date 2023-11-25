using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]

/// <summary>
/// Simple RTS-style camera controller and input handler for demonstration purposes.
/// </summary>
public class RTSCamera : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float rotateSensitivity = 200f;


    public float ScreenEdgeBorderThickness = 5.0f; // distance from screen edge. Used for mouse movement

    [Header("Movement Speeds")]
    [Space]
    public float minPanSpeed;
    public float maxPanSpeed;
    public float secToMaxSpeed; //seconds taken to reach max speed;
    public float zoomSpeed;

    [Header("Movement Limits")]
    [Space]
    public bool enableMovementLimits;
    public Vector2 heightLimit;
    public Vector2 lenghtLimit;
    public Vector2 widthLimit;
    public Vector2 zoomRotationLimit;

    private float panSpeed;
    private Vector3 initialPos;
    private Vector3 panMovement;
    private Vector3 pos;
    private Quaternion rot;
    private bool rotationActive = false;
    private Vector3 lastMousePosition;
    private Quaternion initialRot;
    private float panIncrease = 0.0f;

    [Header("Rotation")]
    [Space]
    public float rotateSpeed;

    void Start()
    {
        initialPos = transform.position;
        initialRot = transform.rotation;
    }

    private void Update()
    {
        //// Movement
        //Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        //Vector3 dir = Quaternion.Euler(0, transform.localEulerAngles.y, 0) * new Vector3(movement.x, 0, movement.y);
        //transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);

        //// Rotation
        //if (Input.GetMouseButton(1))
        //{
        //    float xRotation = Input.GetAxis("Mouse X");
        //    //float yRotation = Input.GetAxis("Mouse Y");

        //    // Rotate around y axis
        //    transform.Rotate(0, xRotation * rotateSensitivity * Time.deltaTime, 0, Space.World);
        //    //transform.Rotate(0, 0, -yRotation * rotateSensitivity * Time.deltaTime, Space.Self);
        //}

        panMovement = Vector3.zero;

        if (!rotationActive)
        {
            if (Input.mousePosition.y >= Screen.height - ScreenEdgeBorderThickness)
            {
                panMovement += Vector3.left * panSpeed * Time.deltaTime;
            }
            if (Input.mousePosition.y <= ScreenEdgeBorderThickness)
            {
                panMovement += Vector3.right * panSpeed * Time.deltaTime;
            }
            if (Input.mousePosition.x <= ScreenEdgeBorderThickness)
            {
                panMovement -= Vector3.forward * panSpeed * Time.deltaTime;
            }
            if (Input.mousePosition.x >= Screen.width - ScreenEdgeBorderThickness)
            {
                panMovement += Vector3.forward * panSpeed * Time.deltaTime;
            }

            transform.Translate(panMovement, Space.World);


            //increase pan speed
            if (Input.mousePosition.y >= Screen.height - ScreenEdgeBorderThickness
                || Input.mousePosition.y <= ScreenEdgeBorderThickness
                || Input.mousePosition.x <= ScreenEdgeBorderThickness
                || Input.mousePosition.x >= Screen.width - ScreenEdgeBorderThickness
                )
            {
                panIncrease += Time.deltaTime / secToMaxSpeed;
                panSpeed = Mathf.Lerp(minPanSpeed, maxPanSpeed, panIncrease);
            }
            else
            {
                panIncrease = 0;
                panSpeed = minPanSpeed;
            }
        }

        #region Zoom

        Vector3 pos = transform.position;
        pos.y -= Input.mouseScrollDelta.y * zoomSpeed;
        transform.position = pos;

        //Zoom rotation
        float angleDiffX = 3 * Input.mouseScrollDelta.y;
        Vector3 angleDiff = new Vector3(angleDiffX, 0,0);
        transform.localEulerAngles -= angleDiff;

        #endregion

        #region mouse rotation

        // Rotation
        if (Input.GetKey(KeyCode.Space))
        {
            rotationActive = true;
            Vector3 mouseDelta;
            if (lastMousePosition.x >= 0 &&
                lastMousePosition.y >= 0 &&
                lastMousePosition.x <= Screen.width &&
                lastMousePosition.y <= Screen.height)
                mouseDelta = Input.mousePosition - lastMousePosition;
            else
            {
                mouseDelta = Vector3.zero;
            }
            var rotation = Vector3.up * Time.deltaTime * rotateSpeed * mouseDelta.x;
            rotation += Vector3.left * Time.deltaTime * rotateSpeed * mouseDelta.y;

            transform.Rotate(rotation, Space.World);

            // Make sure z rotation stays locked
            rotation = transform.rotation.eulerAngles;
            rotation.z = 0;
            transform.rotation = Quaternion.Euler(rotation);
        }

        if (Input.GetMouseButtonUp(0))
        {
            rotationActive = false;
            transform.rotation = Quaternion.Slerp(transform.rotation, initialRot, 0.5f * Time.time);
        }

        lastMousePosition = Input.mousePosition;

        #endregion


        #region boundaries

        if (enableMovementLimits == true)
        {
            //movement limits
            pos = transform.position;
            pos.y = Mathf.Clamp(pos.y, heightLimit.x, heightLimit.y);
            pos.z = Mathf.Clamp(pos.z, lenghtLimit.x, lenghtLimit.y);
            pos.x = Mathf.Clamp(pos.x, widthLimit.x, widthLimit.y);
            transform.position = pos;

            //angleDiffX = Mathf.Clamp(transform.localEulerAngles.x + angleDiffX, zoomRotationLimit.x, zoomRotationLimit.y);

            //transform.localEulerAngles.x = Mathf.Clamp(transform.localEulerAngles.x, zoomRotationLimit.x, zoomRotationLimit.y);
        }

        #endregion
    }
}
