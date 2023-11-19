using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveTorpV1 : MonoBehaviour
{
    //public GameObject pillarPrefab;
    private Rigidbody torpRb;
    private float force = 8;
    private float speed;
    private float myDrag = 0.1f;
    public int moveMode = 0;
    private bool destroyed = false;

    Vector3 forwardDirection;
    Vector3 currentPos;
    Vector3 newDirection;
    Vector3 startVelocity;
    Vector3 newVelocity;
    float forwardTargetAngle;
    float targetDistance;
    Quaternion new_rotation;
    int rotationSpeedCoeff = 125;
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
        if (destroyed) return;
        timer += Time.deltaTime;
        if (timer < 0.5) return;

        // —имул€ци€ сопротивлени€ среды
        startVelocity = torpRb.velocity;
        //newVelocity = startVelocity * Mathf.Clamp01(1f - myDrag * Time.deltaTime);

        newVelocity = startVelocity * Mathf.Clamp01(1f - myDrag * Mathf.Pow(startVelocity.magnitude/10, 2) * Time.deltaTime);
        //print("Torp resistance coeff = " + Mathf.Clamp01(1f - myDrag * Mathf.Pow(startVelocity.magnitude/10, 2) * Time.deltaTime));
        //print("Mathf.Pow(startVelocity.magnitude, 2) = " + Mathf.Pow(startVelocity.magnitude, 2));
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
        else {print(gameObject.name + " No target!");}
    }
    void Move(int mode = 1)
    {
        // ѕоворот на цель + добавка угла, чтобы погасить проекцию скорости в бок от цели
        transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, Mathf.Sqrt(speed * rotationSpeedCoeff) * Time.deltaTime);
        //transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, speed * rotationSpeedCoeff * Time.deltaTime);
        Debug.DrawLine(currentPos, targetPosition, Color.red);
        //print("currentPos = " + currentPos);

        // “€га вперЄд
        torpRb.AddForce(force * forwardDirection, ForceMode.Acceleration);
        //print("Applying force " + force * forwardDirection);

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

    //private void OnCollisionEnter(Collision collision)
    //{
    //    print("Collision! " + collision.gameObject.name);
    //    torpRb.velocity = Vector3.zero;
    //    moveMode = 0;
    //    bubbles.Stop();
    //    if (!collision.gameObject.CompareTag("Projectile"))
    //    {
    //        foreach(Transform child in transform)
    //        {
    //            print("Checking child: " + child.name);
    //            if (child.transform.CompareTag("Model"))
    //            {
    //                Destroy(child.gameObject);
    //                destroyed = true;
    //                print("Destroyed: " + child.name);
    //                break;
    //            }
    //        }
    //        Destroy(gameObject, 2);
    //    }
    //}

    private void OnTriggerEnter(Collider other)
    {
        print("Collision! " + other.gameObject.name);
        torpRb.velocity = Vector3.zero;
        moveMode = 0;
        bubbles.Stop();
        if (!other.gameObject.CompareTag("Projectile"))
        {
            foreach (Transform child in transform)
            {
                print("Checking child: " + child.name);
                if (child.transform.CompareTag("Model"))
                {
                    Destroy(child.gameObject);
                    destroyed = true;
                    print("Destroyed: " + child.name);
                    break;
                }
            }
            Destroy(gameObject, 2);
        }

    }
}