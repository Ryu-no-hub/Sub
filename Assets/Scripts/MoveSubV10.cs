using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSubV10 : MonoBehaviour, ISelectable
{
    private Camera mainCamera;
    private LayerMask layerMask = (1 << 3);

    //public GameObject pillarPrefab;
    private Rigidbody subRb;
    private float force = 4f;
    private float speed;
    private float myDrag = 0.8f;
    private int ammo = 15;
    public int moveMode;
    private float reloadTime = 3f;

    Vector3 forwardDirection;
    Vector3 currentPos;
    Vector3 newDirection;
    Vector3 startVelocity;
    Vector3 newVelocity;
    float forwardTargetAngle;
    float targetDistance;
    Quaternion new_rotation;
    float velocityTargetAngle;
    int rotationSpeedCoeff = 1500;
    float ThrustDistCoeff;
    float ThrustDirectionCoeff;
    float stopTime;
    float dragDeceleration;
    private bool aligned = true;
    private bool atacking = false;   

    public Vector3 targetPosition;
    public Vector3 oldTargetPosition;
    private ParticleSystem bubblesLeft;
    private ParticleSystem bubblesRight;
    private GameObject selectionSprite;
    private float angleUp;
    public GameObject target = null;
    public GameObject torpPrefab;
    

    public int team = 1;
    public int Team
    {
        get { return team; }
    }

    public void Select()
    {
        selectionSprite.SetActive(true);
    }

    public void Deselect()
    {
        selectionSprite.SetActive(false);
    }


    // Start is called before the first frame update
    void Start()
    {
        subRb = GetComponent<Rigidbody>();
        bubblesLeft = transform.Find("BubblesLeft").GetComponent<ParticleSystem>();
        bubblesRight = transform.Find("BubblesRight").GetComponent<ParticleSystem>();
        targetPosition = Vector3.zero;
        forwardDirection = transform.forward;
        currentPos = transform.position;
        newDirection = targetPosition - currentPos;
        forwardTargetAngle = Vector3.Angle(forwardDirection, newDirection);
        targetDistance = newDirection.magnitude;
        selectionSprite = transform.Find("Selection Sprite").gameObject;
        mainCamera = Camera.main;
        moveMode = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // Симуляция сопротивления среды
        startVelocity = subRb.velocity;
        newVelocity = startVelocity * Mathf.Clamp01(1f - myDrag * Time.deltaTime);
        subRb.velocity = newVelocity;

        // Отслеживание угла с вертикалью, чтобы сохранять гАризонтальную ориентацию вне активного движения
        angleUp = Vector3.Angle(Vector3.up, transform.up);

        if (moveMode != 0)
        {
            currentPos = transform.position;
            newDirection = targetPosition - currentPos;
            new_rotation = Quaternion.LookRotation(newDirection - startVelocity);
            forwardDirection = transform.forward;
            forwardTargetAngle = Vector3.Angle(forwardDirection, newDirection);
            targetDistance = newDirection.magnitude;

            Debug.DrawRay(currentPos, forwardDirection * 100, Color.green);

            target = null;
            if (moveMode == 1)
            {
                Move(1);
            }
            else if (moveMode == 2)
            {
                Move(-2);
            }
            else if (moveMode == 3)
            {
                //print("targetDistance = " + targetDistance);
                //print("forwardTargetAngle = " + forwardTargetAngle);
                if (targetDistance < 20 && forwardTargetAngle > 20)
                {
                    Move(-1);
                }
                else
                {
                    moveMode = 1;
                }
            }

            // Проверка когда пора выключать двигатели
            speed = Vector3.Magnitude(subRb.velocity);
            stopTime = targetDistance / speed;
            dragDeceleration = (startVelocity.magnitude - newVelocity.magnitude) / Time.deltaTime;
            velocityTargetAngle = Vector3.Angle(startVelocity, newDirection);

            //print("startVelocity.magnitude = " + startVelocity.magnitude + ", newVelocity.magnitude = " + newVelocity.magnitude);
            //print("stopTime = " + stopTime + ", targetDistance = " + targetDistance  + ", speed = " + speed );
            //print("real stop time = " + speed / dragDeceleration * myDrag);
            //print("dragDeceleration = " + dragDeceleration);

            if (targetDistance < 2 || // погрешность достижения точки
                    ((stopTime < speed / dragDeceleration * myDrag)
                    && (velocityTargetAngle < 5 || velocityTargetAngle > 175)) // направление примерно на точку, чтобы не промахиваться
                    )
            {
                moveMode = 0;
                targetPosition = Vector3.zero;
                bubblesLeft.Stop();
                bubblesRight.Stop();
            }
        }
        else if (target == null && angleUp > 1) // Плавное выравнивание
        {
            float xDegreeDelta = (transform.localEulerAngles.x - 180) * 0.2f;
            float zDegreeDelta = (transform.localEulerAngles.z - 180) * 0.2f;

            transform.localEulerAngles += new Vector3(xDegreeDelta, 0, zDegreeDelta) * Time.deltaTime;
        }
        else if (!aligned && angleUp != 0) // Жёсткое выравнивание и остановка вращения
        {
            transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
            subRb.angularVelocity = Vector3.zero;
            aligned = true;
            print("Hard alignment");
        }
        else if (target != null) //angleUp == 0, Поиск целей
        {
            currentPos = transform.position;
            newDirection = target.transform.position - currentPos;
            if (Vector3.Angle(transform.forward, newDirection) > 5)
            {
                //print(transform.name + " Turning to target " + moveMode);
                //print("Vector3.Angle(transform.forward, newDirection) = " + Vector3.Angle(transform.forward, newDirection));
                new_rotation = Quaternion.LookRotation(newDirection - startVelocity);
                forwardDirection = transform.forward;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, Mathf.Sqrt(rotationSpeedCoeff) * Time.deltaTime);
                Debug.DrawRay(currentPos, (newDirection - startVelocity) * 100, Color.grey);
            }
            else if (!atacking && team == 1)
            {
                atacking = true;
                StartCoroutine(Shoot());
            }
        }

        // Проверка на новую точку назначения
        if (targetPosition != Vector3.zero && oldTargetPosition != targetPosition)
        {
            aligned = false;
            atacking = false;
            forwardDirection = transform.forward;
            currentPos = transform.position;
            newDirection = targetPosition - currentPos;
            forwardTargetAngle = Vector3.Angle(forwardDirection, newDirection);
            targetDistance = newDirection.magnitude;
            if (forwardTargetAngle <= 45 || targetDistance > 20)
            {
                moveMode = 1;
            }
            else if (forwardTargetAngle > 155)
            {
                moveMode = 2;
            }
            else
            {
                moveMode = 3;
            }
            Debug.Log(moveMode);
            Debug.Log("forwardTargetAngle " + forwardTargetAngle);
            Debug.Log("targetDistance " + targetDistance);

            oldTargetPosition = targetPosition;
        }
    }
    void Move(int mode = 1)
    {
        ThrustDistCoeff = 0.5f;
        if (mode == 1)
        {
            ThrustDirectionCoeff = 1;
        }
        else if(mode == -1)
        {// Движение задом, разворачиваясь к точке передом
            ThrustDirectionCoeff = 0.5f;
        }
        else
        {// Движение задом на точку
            ThrustDirectionCoeff = 0.5f;
            new_rotation *= Quaternion.AngleAxis(Vector3.Angle(newDirection, forwardDirection), Vector3.up);
        }

        // Поворот на цель + добавка угла, чтобы погасить проекцию скорости в бок от цели
        transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, Mathf.Sqrt(speed * rotationSpeedCoeff) * Time.deltaTime);
        Debug.DrawRay(currentPos, (newDirection - startVelocity) * 100, Color.red);

        // Тяга вперёд
        subRb.AddForce(force * ThrustDirectionCoeff * Mathf.Clamp01(targetDistance * ThrustDistCoeff) * Mathf.Clamp(mode, -1, 1) * forwardDirection);
        //print("Applying force " + force * ThrustDirectionCoeff * Mathf.Clamp01(targetDistance * ThrustDistCoeff) * Mathf.Clamp(mode, -1, 1));

        // Изменение направления пузырьков
        if (mode == 1 && transform.rotation == bubblesLeft.transform.rotation)
        {
            bubblesLeft.transform.rotation = Quaternion.Inverse(transform.rotation);
            bubblesRight.transform.rotation = Quaternion.Inverse(transform.rotation);
        }
        else if ((mode == -1 || mode == -2) && transform.rotation != bubblesLeft.transform.rotation)
        {
            bubblesLeft.transform.rotation = transform.rotation;
            bubblesRight.transform.rotation = transform.rotation;
        }
        if (!bubblesLeft.isPlaying)
        {
            bubblesLeft.Play();
            bubblesRight.Play();
        }
    }
    private IEnumerator Shoot()
    {
        //if (ammo>0 && target != null && moveMode==0)
        while(atacking)
        {
            GameObject torpedo = Instantiate(torpPrefab, transform.position + 5 * transform.forward, transform.rotation);
            torpedo.GetComponent<MoveTorpV1>().SetTarget(target);
            print("Instantiate TORPEDO with target = " + target.name);
            yield return new WaitForSeconds(reloadTime);
        }
    }
}