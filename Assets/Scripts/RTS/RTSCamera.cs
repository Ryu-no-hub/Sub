using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]

/// <summary>
/// Simple RTS-style camera controller and input handler for demonstration purposes.
/// </summary>
public class RTSCamera : MonoBehaviour
{
    private float ScreenEdgeBorderThickness = 20f; // distance from screen edge. Used for mouse movement

    [Header("Movement Speeds")]
    [Space]
    public bool enableMovement;
    public float minPanSpeed;
    public float maxPanSpeed;
    public float secToMaxSpeed; //seconds taken to reach max speed;

    [Header("Movement Limits")]
    [Space]
    public Vector2 heightLimit;
    public Vector2 lenghtLimit;
    public Vector2 widthLimit;

    private float mouseX;
    private float mouseY;
    private float ScreenHeight;
    private float ScreenWidth;

    private float zoomSpeed = 10;
    private int zoomRotationLimit = -340;
    private float panSpeed;
    private Vector3 panMovement;
    private bool rotationActive = false;
    private Vector3 lastMousePosition;
    private Quaternion initialRot;
    private float panIncrease = 0.0f;

    [Header("Rotation")]
    [Space]
    public float rotateSpeed;

    void Start()
    {
        initialRot = transform.rotation;
    }

    private void Update()
    {
        mouseX = Input.mousePosition.x;
        mouseY = Input.mousePosition.y;
        ScreenHeight = Screen.height;
        ScreenWidth = Screen.width;

        panMovement = Vector3.zero;

        if (!rotationActive && enableMovement)
        {
            if (mouseY >= ScreenHeight - ScreenEdgeBorderThickness)
            {
                panMovement += panSpeed * Time.deltaTime * Vector3.left;
                panMovement += (mouseX - ScreenWidth / 2) / (ScreenWidth / 2) * panSpeed * Time.deltaTime * Vector3.forward;
            }
            else if (mouseY <= ScreenEdgeBorderThickness)
            {
                panMovement += panSpeed * Time.deltaTime * Vector3.right;
                panMovement += (mouseX - ScreenWidth / 2) / (ScreenWidth / 2) * panSpeed * Time.deltaTime * Vector3.forward;
            }
            else if (mouseX <= ScreenEdgeBorderThickness)
            {
                panMovement -= panSpeed * Time.deltaTime * Vector3.forward;
                panMovement += (mouseY - ScreenHeight / 2) / (ScreenWidth / 2) * panSpeed * Time.deltaTime * Vector3.left;
            }
            else if (mouseX >= ScreenWidth - ScreenEdgeBorderThickness)
            {
                panMovement += panSpeed * Time.deltaTime * Vector3.forward;
                panMovement += (mouseY - ScreenHeight / 2) / (ScreenWidth / 2) * panSpeed * Time.deltaTime * Vector3.left;
            }

            transform.Translate(panMovement, Space.World);


            //increase pan speed
            if (   mouseY >= ScreenHeight - ScreenEdgeBorderThickness
                || mouseY <= ScreenEdgeBorderThickness
                || mouseX <= ScreenEdgeBorderThickness
                || mouseX >= ScreenWidth - ScreenEdgeBorderThickness
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


        #endregion

        #region mouse rotation

        // Rotation
        if (Input.GetKey(KeyCode.Space))
        {
            rotationActive = true;
            Vector3 mouseDelta;
            if (lastMousePosition.x >= 0 &&
                lastMousePosition.y >= 0 &&
                lastMousePosition.x <= ScreenWidth &&
                lastMousePosition.y <= ScreenHeight)
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

        if (Input.GetKeyUp(KeyCode.Space))
        {
            rotationActive = false;
            transform.rotation = Quaternion.Slerp(transform.rotation, initialRot, 1);
        }

        lastMousePosition = Input.mousePosition;

        #endregion

        // Movement limits
        pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, heightLimit.x, heightLimit.y);
        pos.z = Mathf.Clamp(pos.z, lenghtLimit.x, lenghtLimit.y);
        pos.x = Mathf.Clamp(pos.x, widthLimit.x, widthLimit.y);
        transform.position = pos;

        // Zooming rotation
        float newAngleX = zoomRotationLimit + 2.390458f * Mathf.Sqrt(pos.y - heightLimit.x);
        transform.localEulerAngles = new Vector3(newAngleX, transform.localEulerAngles.y, transform.localEulerAngles.z);
    }
}
