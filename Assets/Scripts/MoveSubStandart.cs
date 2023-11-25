using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSubStandart : MonoBehaviour, ISelectable
{
    private Rigidbody subRb;
    private ParticleSystem bubblesLeft, bubblesRight;
    private GameObject selectionSprite;

    // Movement parameters
    private float force = 4f;
    private float speed;
    private float myDrag = 0.8f;
    private float ThrustDistCoeff = 0.1f, ThrustDirectionCoeff;
    private int rotationSpeedCoeff = 1500, steadyRotationCoeff = 20;

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
    public bool stopped = true, searching = false;
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
        speed = Vector3.Magnitude(subRb.velocity);
        timer += Time.deltaTime;

        //print("Timer = " + timer);
        //print("counter = " + counter);
        //print("Time.frameCount = " + Time.frameCount);
        //print("targetDistance = " + targetDistance);


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

            if (target != null && targetDistance < 10 && Vector3.Distance(currentPos, target.transform.position) < attackRange)
            {
                Stop();
            }
        }
        else if (target == null && !aligned)
        {
            print(transform.name + " Turning WITHOUT target, y = "+ transform.localEulerAngles.y);
            TurnToAnglesXY(0, transform.localEulerAngles.y, 1, true);
        }
        else if (target != null && team == 1 && ammo > 0)
        {
            targetDir = target.transform.position - transform.position;
            float angleTarget = Vector3.Angle(transform.forward, targetDir);

            if (angleTarget > 5) // Поворот на цель
            {
                print(transform.name + " Turning to target, angle = " + angleTarget + ", targetDir = " + targetDir + ", target = " + target.name);
                //new_rotation = Quaternion.LookRotation(targetDir - slowedVelocity);

                new_rotation = Quaternion.LookRotation(targetDir, Vector3.up);
                float targetAngleX = new_rotation.eulerAngles.x;
                float targetAngleY = new_rotation.eulerAngles.y;
                //print(transform.name + " new_rotation.eulerAngles.y = " + new_rotation.eulerAngles.y);

                TurnToAnglesXY(targetAngleX, targetAngleY, 1, false);
            }
            else if (Vector3.Distance(currentPos, target.transform.position) < attackRange) // На дистанции выстрела
            {
                if (timer - lastShotTime > reloadTime || lastShotTime == 0) // Не идёт перезарядка
                {
                    ammo--;
                    //stopped = false;
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

    private void TurnToAnglesXY(float targetAngleX, float targetAngleY, int threshold, bool hardAlignment)
    {
        float turnSpeed = steadyRotationCoeff;
        float currentAngleX = transform.localEulerAngles.x, currentAngleY = transform.localEulerAngles.y, currentAngleZ = transform.localEulerAngles.z;
        if (targetAngleX > 180)
            targetAngleX -= 360; // 350 --> -10,  10 --> 10
        if (targetAngleY > 180)
            targetAngleY -= 360;

        if (currentAngleX > 180)
            currentAngleX -= 360; // 5 --> 5, 355 --> -5
        if (currentAngleY > 180)
            currentAngleY -= 360;
        if (currentAngleZ > 180)           
            currentAngleZ -= 360;

        float angleDiffX = targetAngleX - currentAngleX; 
        float angleDiffY = targetAngleY - currentAngleY;


        if (currentAngleZ > 1 || Mathf.Abs(angleDiffX) + Mathf.Abs(angleDiffY) > threshold) // Плавное выравнивание
        {
            Quaternion targetRotation = Quaternion.Euler(targetAngleX, targetAngleY, -transform.localEulerAngles.z);

            //print(transform.name + " Current angles = " + transform.localEulerAngles);
            //print(transform.name + " Turning to " + targetRotation.eulerAngles);

            if (speed > 1)
            {
                turnSpeed *= Mathf.Sqrt(speed);
                //print(transform.name + " Speed modified turnSpeed = " + turnSpeed);
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            Debug.DrawRay(currentPos, (targetDir - slowedVelocity) * 100, Color.grey);
        }
        else if (hardAlignment)// Жёсткое выравнивание и остановка вращения
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
        Invoke("Reset", stopTime);
        moveDestination = Vector3.zero;
        subRb.angularVelocity = Vector3.zero;
        bubblesLeft.Stop();
        bubblesRight.Stop();
    }

    private void Reset()
    {
        stopped = true;
        searching = false;
        print("STOPPED");
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