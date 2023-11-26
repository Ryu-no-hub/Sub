using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSubV13 : MonoBehaviour, ISelectable
{
    private Rigidbody subRb;
    private ParticleSystem bubblesLeft, bubblesRight;
    private GameObject selectionSprite;

    // Movement parameters
    private float force = 4f;
    private float speed;
    private float myDrag = 0.8f;
    private float ThrustDistCoeff = 0.1f, ThrustDirectionCoeff;
    private int rotationSpeedCoeff = 1500; //, steadyRotationCoeff = 2;

    private float angleUp;

    // Attack parameters
    private float reloadTime = 3f;
    private int ammo = 20;

    private Vector3 currentPos, forwardDir, targetDir, startVelocity, slowedVelocity;
    private Vector3 oldmoveDestination;
    private Quaternion new_rotation;
    private float forwardTargetAngle, velocityTargetAngle;
    private float targetDistance;
    private float stopTime, lastShotTime;
    private float dragDecel;
    private bool aligned = true;
    private float timer; 

    public GameObject torpPrefab, target = null;
    public Vector3 moveDestination;
    public int moveMode;
    public bool searching = false;
    public int attackRange = 40;


    public int team = 1;
    public int Team
    {
        get { return team; }
    }

    public bool Alive
    {
        get { return this != null; }
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
        moveDestination = Vector3.zero;
        forwardDir = transform.forward;
        currentPos = transform.position;
        targetDir = moveDestination - currentPos;
        forwardTargetAngle = Vector3.Angle(forwardDir, targetDir);
        targetDistance = targetDir.magnitude;
        selectionSprite = transform.Find("Selection Sprite").gameObject;
        moveMode = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // Симуляция сопротивления среды
        startVelocity = subRb.velocity;
        slowedVelocity = startVelocity * Mathf.Clamp01(1f - myDrag * Time.deltaTime);
        subRb.velocity = slowedVelocity;

        // Отслеживание угла с вертикалью, чтобы сохранять гАризонтальную ориентацию вне активного движения
        angleUp = Vector3.Angle(Vector3.up, transform.up);

        currentPos = transform.position;
        targetDir = moveDestination - currentPos;
        targetDistance = targetDir.magnitude;
        //print("Timer = " + timer);
        //print("counter = " + counter);
        //print("Time.frameCount = " + Time.frameCount);
        //print("targetDistance = " + targetDistance);
        timer += Time.deltaTime;
        //counter++;

        if (moveMode != 0)
        {
            new_rotation = Quaternion.LookRotation(targetDir - slowedVelocity);
            forwardDir = transform.forward;
            forwardTargetAngle = Vector3.Angle(forwardDir, targetDir);
            Debug.DrawRay(currentPos, forwardDir * 100, Color.green);

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
            dragDecel = (startVelocity.magnitude - slowedVelocity.magnitude) / Time.deltaTime;
            velocityTargetAngle = Vector3.Angle(slowedVelocity, targetDir);

            //print("startVelocity.magnitude = " + startVelocity.magnitude + ", slowedVelocity.magnitude = " + slowedVelocity.magnitude);
            //print("stopTime = " + stopTime + ", targetDistance = " + targetDistance  + ", speed = " + speed );
            //print("real stop time = " + speed / dragDecel * myDrag);
            //print("dragDecel = " + dragDecel);

            if (targetDistance < 2 || // погрешность достижения точки
                    ((stopTime < speed / dragDecel * myDrag)
                    && (velocityTargetAngle < 5 || velocityTargetAngle > 175)) // направление примерно на точку, чтобы не промахиваться по времени остановки
                    )
            {
                Stop();
            }

            if(target!=null && targetDistance < 10 && Vector3.Distance(currentPos, target.transform.position) < attackRange)
            {
                Stop();
            }
        }
        else if (target == null && !aligned) 
        {
            AlignToAngle(0, transform.localEulerAngles.y, 1, true);
        }
        else if (target != null && team == 1 && ammo > 0)
        {
            targetDir = target.transform.position - transform.position;
            float angleTarget = Vector3.Angle(transform.forward, targetDir);

            if (angleTarget > 5) // Поворот на цель
            {
                print(transform.name + " Turning to target, angle = " + angleTarget);

                //print("Vector3.Angle(transform.forward, targetDir) = " + Vector3.Angle(transform.forward, targetDir));
                //forwardDir = transform.forward;
                currentPos = transform.position;
                targetDir = target.transform.position - currentPos;
                new_rotation = Quaternion.LookRotation(targetDir - slowedVelocity);
                float targetAngleX = new_rotation.eulerAngles.x;
                float targetAngleY = new_rotation.eulerAngles.y;

                AlignToAngle(targetAngleX, targetAngleY, 1, false);
            }
            else if (Vector3.Distance(currentPos, target.transform.position) < attackRange) // На дистанции выстрела
            {
                if (timer - lastShotTime > reloadTime || lastShotTime == 0) // Не идёт перезарядка
                {
                    ammo--;
                    searching = false;
                    lastShotTime = timer;
                    print("SHOOT! Time = " + timer);
                    Shoot();
                }
                //else if(team==1) print("timer - lastShotTime = " + (timer - lastShotTime));
            }
            else // Цель уплыла - подплыть на расстояние выстрела
            {
                //targetDir = target.transform.position - transform.position;
                moveDestination = target.transform.position - (attackRange - 2) * targetDir.normalized;
            }
        }

        // Проверка на новую точку назначения
        if (moveDestination != Vector3.zero && moveDestination != oldmoveDestination)
        {
            aligned = false;
            forwardDir = transform.forward;
            currentPos = transform.position;
            targetDir = moveDestination - currentPos;
            forwardTargetAngle = Vector3.Angle(forwardDir, targetDir);
            targetDistance = targetDir.magnitude;
            if (forwardTargetAngle <= 45 || targetDistance > 20 || target != null)
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

            oldmoveDestination = moveDestination;
        }
    }
    void Move(int mode = 1)
    {
        if (mode == 1)
        {
            ThrustDirectionCoeff = 1;
        }
        else 
        {// Движение задом, разворачиваясь к точке передом
            ThrustDirectionCoeff = 0.5f;
            if (mode == -2) // Движение задом на точку
                new_rotation *= Quaternion.AngleAxis(Vector3.Angle(targetDir, forwardDir), Vector3.up);
        }

        // Поворот на цель + добавка угла, чтобы погасить проекцию скорости в бок от цели
        transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, Mathf.Sqrt(speed * rotationSpeedCoeff) * Time.deltaTime);
        Debug.DrawRay(currentPos, (targetDir - slowedVelocity) * 100, Color.red);

        // Тяга вперёд
        subRb.AddForce(force * ThrustDirectionCoeff * Mathf.Clamp01(targetDistance * ThrustDistCoeff) * Mathf.Clamp(mode, -1, 1) * forwardDir);
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
    private void AlignToAngle(float targetAngleX, float targetAngleY, int threshold, bool hardAlignment)
    {
        //float turnSpeed = 30;
        float turnSpeed = 1.5f;
        float currentAngleX = transform.localEulerAngles.x, currentAngleY = transform.localEulerAngles.y, currentAngleZ = transform.localEulerAngles.z;
        if (targetAngleX > 180)
            targetAngleX = targetAngleX - 360; // 350 --> 10,  10 --> 10
        if (targetAngleY > 180)
            targetAngleY = targetAngleY - 360;

        if (currentAngleX > 180)
            currentAngleX = currentAngleX - 360; // 5 --> 5, 355 --> -5
        if (currentAngleY > 180)
            currentAngleY = currentAngleY - 360;
        if (currentAngleZ > 180)
            currentAngleZ = currentAngleZ - 360;

        float angleDiffX = targetAngleX - currentAngleX;
        float angleDiffY = targetAngleY - currentAngleY;
        if (Mathf.Abs(angleDiffX) > threshold || Mathf.Abs(angleDiffY) > threshold) // Плавное выравнивание
        {
            print(gameObject.name + " Aligning, angles = " + transform.localEulerAngles);
            print(gameObject.name + " targetAngleX = " + targetAngleX + ", targetAngleY = " + targetAngleY);

            //float xDegreeDelta = Mathf.Sign(angleDiffX) * turnSpeed;
            //float yDegreeDelta = Mathf.Sign(angleDiffY) * turnSpeed;
            //float zDegreeDelta = Mathf.Sign(-currentAngleZ) * turnSpeed;

            float xDegreeDelta = angleDiffX * turnSpeed;
            float yDegreeDelta = angleDiffY * turnSpeed;
            float zDegreeDelta = -currentAngleZ * turnSpeed / 2;
            
            print("Delta angles = " + new Vector3(xDegreeDelta, yDegreeDelta, zDegreeDelta));
            transform.localEulerAngles += new Vector3(xDegreeDelta, yDegreeDelta, zDegreeDelta) * Time.deltaTime;
        }
        else if(hardAlignment)// Жёсткое выравнивание и остановка вращения
        {
            print(gameObject.name + " Hard alignment");

            transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
            aligned = true;
            subRb.angularVelocity = Vector3.zero;
        }
        else // Достаточно близко
        {
            print(gameObject.name + " Alignment threshold reached, angleDiffX = " + angleDiffX + ", angleDiffY = " + angleDiffY);

            aligned = true;
            subRb.angularVelocity = Vector3.zero;
        }
    }

    public void Stop()
    {
        print("ENGINES STOP, target = " + target);
        
        moveMode = 0;
        moveDestination = Vector3.zero;
        subRb.angularVelocity = Vector3.zero;
        searching = false;
        bubblesLeft.Stop();
        bubblesRight.Stop();
    }

    private void Shoot()
    {
        GameObject torpedo = Instantiate(torpPrefab, transform.position + transform.forward, transform.rotation);
        Physics.IgnoreCollision(gameObject.GetComponent<BoxCollider>(), torpedo.transform.Find("model").gameObject.GetComponent<BoxCollider>());
        forwardDir = transform.forward;
        torpedo.GetComponent<Rigidbody>().AddForce(subRb.velocity + 10 * forwardDir.normalized, ForceMode.Impulse);

        //Debug.DrawRay(transform.position, subRb.velocity + 10 * forwardDir.normalized, Color.blue, 2);

        //print("subRb.velocity = " + subRb.velocity);
        //print("Torpedo Direction = " + (transform.position + subRb.velocity + 6 * forwardDir.normalized));
        //print("forwardDir = " + forwardDir.normalized);
        //print("10xforwardDir = " + 10 * forwardDir.normalized);

        torpedo.GetComponent<MoveTorpV1>().SetTarget(target);
        print(gameObject.name + " Instantiate TORPEDO with target = " + target.name);
    }
}