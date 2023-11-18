using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveTorpV1 : MonoBehaviour
{
    //public GameObject pillarPrefab;
    private Rigidbody torpRb;
    private float force = 60;
    private float speed;
    private float myDrag = 4;
    public int moveMode=0;

    Vector3 forwardDirection;
    Vector3 currentPos;
    Vector3 newDirection;
    Vector3 startVelocity;
    Vector3 newVelocity;
    float forwardTargetAngle;
    float targetDistance;
    Quaternion new_rotation;
    int rotationSpeedCoeff = 125;
    float ThrustDirectionCoeff;
    float timer;

    public Vector3 targetPosition;
    private ParticleSystem bubbles;
    public GameObject target = null;

    // Start is called before the first frame update
    void Start()
    {
        torpRb = GetComponent<Rigidbody>();
        //torpRb = transform.Find("model").GetComponent<Rigidbody>();
        bubbles = transform.Find("Bubbles").GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer < 0.6) return;

        // —имул€ци€ сопротивлени€ среды
        startVelocity = torpRb.velocity;
        newVelocity = startVelocity * Mathf.Clamp01(1f - myDrag * Time.deltaTime);
        torpRb.velocity = newVelocity;
        speed = Vector3.Magnitude(torpRb.velocity);

        if (moveMode != 0)
        {
            currentPos = transform.position;
            newDirection = targetPosition - currentPos;
            //new_rotation = Quaternion.LookRotation(newDirection - startVelocity);
            new_rotation = Quaternion.LookRotation(newDirection);
            forwardDirection = transform.forward;
            forwardTargetAngle = Vector3.Angle(forwardDirection, newDirection);
            targetDistance = newDirection.magnitude;
            //print("targetPosition = " + targetPosition);

            Debug.DrawRay(currentPos, forwardDirection * 10, Color.green);
            Move(1);
        }
        else {print("No target!");}
    }
    void Move(int mode = 1)
    {
        ThrustDirectionCoeff = 1;

        // ѕоворот на цель + добавка угла, чтобы погасить проекцию скорости в бок от цели
        transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, Mathf.Sqrt(speed * rotationSpeedCoeff) * Time.deltaTime);
        //transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, speed * rotationSpeedCoeff * Time.deltaTime);
        Debug.DrawLine(currentPos, targetPosition, Color.red);
        //print("currentPos = " + currentPos);

        // “€га вперЄд
        torpRb.AddForce(force * ThrustDirectionCoeff * Mathf.Clamp01(targetDistance) * Mathf.Clamp(mode, -1, 1) * forwardDirection);
        //print("Applying force " + force * ThrustDirectionCoeff * Mathf.Clamp01(targetDistance * ThrustDistCoeff) * Mathf.Clamp(mode, -1, 1));

        if (!bubbles.isPlaying)
        {
            bubbles.Play();
        }
    }
    public void SetTarget(GameObject tgt)
    {
        target = tgt;
        moveMode = 1;

        forwardDirection = transform.forward;
        currentPos = transform.position;
        newDirection = targetPosition - currentPos;
        forwardTargetAngle = Vector3.Angle(forwardDirection, newDirection);
        targetDistance = newDirection.magnitude;
        targetPosition = target.transform.position;

        Debug.Log(moveMode);
        Debug.Log("forwardTargetAngle " + forwardTargetAngle);
        Debug.Log("targetDistance " + targetDistance);
    }

    private void OnCollisionEnter(Collision collision)
    {
        print("Collision! " + collision.gameObject.name);
        torpRb.velocity = Vector3.zero;
        moveMode = 0;
        bubbles.Stop();
        if (!collision.gameObject.CompareTag("Projectile"))
        {
            foreach(Transform child in transform)
            {
                print("Checking child: " + child.name);
                if (child.transform.CompareTag("Model"))
                {
                    Destroy(child.gameObject);
                    print("Destroyed: " + child.name);
                }
            }
            Destroy(gameObject, 1);
        }
    }
}