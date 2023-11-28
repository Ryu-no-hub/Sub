using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Order{
    Vector3 destination;
    GameObject targetUnit;
    Order nextOrder;
}

public class MoveSubStandart : MonoBehaviour, ISelectable
{
    private Rigidbody subRb;
    private ParticleSystem bubblesLeft, bubblesRight, trail;
    private GameObject selectionSprite;

    // Movement parameters
    private float power = 5f;
    private float speed;
    private float myDrag = 1f;
    private float thrust;
    private int rotationSpeedCoeff = 1500, steadyRotationCoeff = 20, borderDistance = 10;

    //private float angleUp;

    // Attack parameters
    private float reloadTime = 3f;
    private int maxAmmo = 20, ammo = 20;
    public int attackRange = 40;

    private Vector3 currentPos, forwardDir, moveTargetDir, attackTargetDir, startVelocity, slowedVelocity;
    private Vector3 oldmoveDestination;
    private Quaternion newRotation;
    private float forwardTargetAngle, velocityTargetAngle;
    private float targetDistance;
    private float stopTime, lastShotTime;
    private float dragDecel;
    private float targetRotationY;
    private bool aligned = true;
    private float timer;
    private AudioSource submarineSource;
    public BehaviourState behaviour;
    //private Order;

    public enum BehaviourState { FullThrottle, TurnToDirection, Reverse, ReverseTurn, StillApproach, Idle, Attacking};
    public GameObject torpPrefab, target = null;
    public Vector3 moveDestination = Vector3.zero;
    //public int behaviour = 0;
    //public int behaviour = 0;
    public bool stopped = true, searching = false, fixTarget = false;
    public AudioClip launchTorpedoSound;

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
        //bubblesLeft = transform.Find("BubblesLeft").GetComponent<ParticleSystem>();
        //bubblesRight = transform.Find("BubblesRight").GetComponent<ParticleSystem>();
        trail = transform.Find("Trail").GetComponent<ParticleSystem>();
        print("trail = " + trail);
        forwardDir = transform.forward;
        currentPos = transform.position;
        moveTargetDir = moveDestination - currentPos;
        forwardTargetAngle = Vector3.Angle(forwardDir, moveTargetDir);
        targetDistance = moveTargetDir.magnitude;
        selectionSprite = transform.Find("Selection Sprite").gameObject;
        submarineSource = GetComponent<AudioSource>();
        targetRotationY = transform.localEulerAngles.y;
        behaviour = BehaviourState.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        // Симуляция сопротивления среды
        startVelocity = subRb.velocity;
        slowedVelocity = startVelocity * Mathf.Clamp01(1f - myDrag * Time.deltaTime);
        subRb.velocity = slowedVelocity;
        speed = slowedVelocity.magnitude;

        //print(transform.name + " speed = " + speed);
        //print("targetDistance = " + targetDistance);

        if (behaviour != BehaviourState.Idle)
        {
            currentPos = transform.position;
            moveTargetDir = moveDestination - currentPos;
            targetDistance = moveTargetDir.magnitude;
            forwardDir = transform.forward;
            Debug.DrawRay(currentPos, forwardDir * 10, Color.green);

            Move();

            // По необходимости измененить режим движения 
            forwardTargetAngle = Vector3.Angle(forwardDir, moveTargetDir);
            switch (behaviour)
            {
                case BehaviourState.TurnToDirection:
                    if (forwardTargetAngle < 20)
                    {
                        behaviour = BehaviourState.FullThrottle;
                        print(behaviour);
                    }
                    break;
                case BehaviourState.ReverseTurn:
                    if (forwardTargetAngle < 20)
                    {
                        behaviour = BehaviourState.FullThrottle;
                        print(behaviour);
                    }
                    break;
            }

            // Проверка когда пора выключать двигатели
            stopTime = targetDistance / speed;
            dragDecel = (startVelocity.magnitude - slowedVelocity.magnitude) / Time.deltaTime;
            velocityTargetAngle = Vector3.Angle(slowedVelocity, moveTargetDir);

            //print("startVelocity.magnitude = " + startVelocity.magnitude + ", slowedVelocity.magnitude = " + slowedVelocity.magnitude);
            //print("stopTime = " + stopTime + ", targetDistance = " + targetDistance  + ", speed = " + speed );
            //print("real stop time = " + speed / dragDecel * myDrag + ", velocityTargetAngle = " + velocityTargetAngle);
            //print("dragDecel = " + dragDecel);
            print("targetDistance = " + targetDistance + ", moveDestination = " + moveDestination + ", target = " + target);

            if (target == null)
            {
                if (
                targetDistance < 2 // погрешность достижения точки, либо она слишком близко
                || ((stopTime < speed / dragDecel * myDrag) && (velocityTargetAngle < 5 || velocityTargetAngle > 175)) // направление примерно на точку, чтобы не промахиваться по времени остановки
                    )
                {
                    Stop();
                    print(behaviour);
                }
            }
            else
            {
                if(targetDistance < attackRange)
                {
                    Stop();
                    //behaviour = BehaviourState.StillApproach;
                    print(behaviour);
                }
                else
                {

                }
            }
        }
        else if (target == null && !aligned)
        {
            //print(transform.name + " Aligning, angles = " + transform.localEulerAngles);
            TurnToAnglesXY(0, targetRotationY, 1, true);
        }
        else if (target != null && team == 1 && ammo > 0)
        {
            currentPos = transform.position;
            attackTargetDir = target.transform.position - transform.position;
            float angleTarget = Vector3.Angle(transform.forward, attackTargetDir);

            if (angleTarget > 5) { aligned = false; }  // Включить поворот на цель

            if (!aligned) // Поворот на цель
            {
                print(transform.name + " Turning to target, angle = " + angleTarget + ", attackTargetDir = " + attackTargetDir + ", target = " + target.name);
                //newRotation = Quaternion.LookRotation(moveTargetDir - slowedVelocity); // Компенсация боковой скорости

                newRotation = Quaternion.LookRotation(attackTargetDir, Vector3.up);
                float targetAngleX = newRotation.eulerAngles.x;
                float targetAngleY = newRotation.eulerAngles.y;
                //print(transform.name + " newRotation.eulerAngles.y = " + newRotation.eulerAngles.y);

                aligned = TurnToAnglesXY(targetAngleX, targetAngleY, 1, false);
            }
            else if (Vector3.Distance(currentPos, target.transform.position) < attackRange) // На дистанции выстрела
            {
                if (timer - lastShotTime > reloadTime - 0.1f || lastShotTime == 0) // Не идёт перезарядка
                {
                    //stopped = false;
                    lastShotTime = timer;
                    print("SHOOT! Time = " + timer);
                    submarineSource.PlayOneShot(launchTorpedoSound, 0.1f);
                    //Invoke("Shoot", 0.1f);
                    StartCoroutine(Shoot(target));
                }
                //else if(team==1) print("timer - lastShotTime = " + (timer - lastShotTime));
            }
            else if(behaviour != BehaviourState.StillApproach) // Цель уплыла - подплыть на расстояние выстрела
            {
                moveDestination = target.transform.position - (attackRange - 2) * attackTargetDir.normalized;
                print("Setting move destination to approach target for " + transform.name + " target = " + target);
            }
        }
        else if (transform.eulerAngles.x != 0 || transform.eulerAngles.z != 0)
        {
            aligned = false;
        }        
    }
    void Move()
    {
        //print("behaviour = " + behaviour);
        newRotation = Quaternion.LookRotation(moveTargetDir - slowedVelocity);
        switch (behaviour)
        {
            case BehaviourState.TurnToDirection:
                thrust = 0.4f;
                if (trail.transform.rotation == transform.rotation)
                {
                    trail.transform.rotation = Quaternion.Inverse(transform.rotation);
                    print("Trail forward");
                }
                break;
            case BehaviourState.FullThrottle:
                thrust = 1;
                break;
            case BehaviourState.ReverseTurn: // Движение задом, разворачиваясь к точке передом
                thrust = -0.5f;
                break;
            case BehaviourState.Reverse: // Движение задом на точку
                newRotation *= Quaternion.AngleAxis(Vector3.Angle(moveTargetDir, forwardDir), Vector3.up);
                if (trail.transform.rotation != transform.rotation)
                {
                    trail.transform.rotation = transform.rotation;
                    print("Trail reverse");

                }
                goto case BehaviourState.ReverseTurn;
        }

        if (!trail.isPlaying)
            trail.Play();

        // Тяга вперёд
        subRb.AddForce(power * thrust * Mathf.Clamp01(targetDistance / borderDistance) * forwardDir);
        //print("Applying power " + power * thrust * Mathf.Clamp01(targetDistance * thrustDistCoeff) * Mathf.Clamp(mode, -1, 1));

        // Поворот на цель + добавка угла, чтобы погасить проекцию скорости в бок от цели
        transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation, Mathf.Sqrt(speed * rotationSpeedCoeff) * Time.deltaTime);
        Debug.DrawRay(currentPos, (moveTargetDir - slowedVelocity) * 100, Color.red);
    }

    private bool TurnToAnglesXY(float targetAngleX, float targetAngleY, int threshold, bool hardAlignment)
    {
        float currentAngleX = transform.localEulerAngles.x, currentAngleY = transform.localEulerAngles.y, currentAngleZ = transform.localEulerAngles.z;

        targetAngleX = targetAngleX < 180 ? targetAngleX : targetAngleX - 360; // 350 --> -10,  10 --> 10
        targetAngleY = targetAngleY < 180 ? targetAngleY : targetAngleY - 360;

        currentAngleX = currentAngleX < 180 ? currentAngleX : currentAngleX - 360;  // 5 --> 5, 355 --> -5
        currentAngleY = currentAngleY < 180 ? currentAngleY : currentAngleY - 360;
        currentAngleZ = currentAngleZ < 180 ? currentAngleZ : currentAngleZ - 360;

        float angleDiffX = targetAngleX - currentAngleX; 
        float angleDiffY = targetAngleY - currentAngleY;


        if (currentAngleZ > 1 || Mathf.Abs(angleDiffX) + Mathf.Abs(angleDiffY) > threshold) // Плавное выравнивание
        {
            Quaternion targetRotation = Quaternion.Euler(targetAngleX, targetAngleY, -transform.localEulerAngles.z);

            //print(transform.name + " Current angles = " + transform.localEulerAngles);
            //print(transform.name + " Turning to " + targetRotation.eulerAngles);

            float turnSpeed = steadyRotationCoeff;
            if (speed > 1)
            {
                turnSpeed *= Mathf.Sqrt(speed); // Компонента скорости поворота, зависящая от скорости
                //print(transform.name + " Speed modified turnSpeed = " + turnSpeed);
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            Debug.DrawRay(currentPos, transform.up * 10, Color.green);
            Debug.DrawRay(currentPos, Vector3.up * 10, Color.red);
            return false;
        }
        else if (hardAlignment)// Жёсткое выравнивание и остановка вращения
        {
            print(gameObject.name + " Hard alignment");

            transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
            aligned = true;
            subRb.angularVelocity = Vector3.zero;
            return true;
        }
        else // Достаточно близко
        {
            print(gameObject.name + " Alignment threshold reached, angleDiffX = " + angleDiffX + ", angleDiffY = " + angleDiffY);

            aligned = true;
            subRb.angularVelocity = Vector3.zero;
            return true;
        }
    }

    public void Stop()
    {
        print("ENGINES STOP, target = " + target);

        behaviour = BehaviourState.Idle;
        Invoke("Reset", stopTime);
        moveDestination = Vector3.zero;
        subRb.angularVelocity = Vector3.zero;
        trail.Stop();
    }

    private void Reset()
    {
        print("STOPPED");

        stopped = true;
        searching = false;
        subRb.angularVelocity = Vector3.zero;
    }

    //private void Shoot()
    private IEnumerator Shoot(GameObject target)
    {
        yield return new WaitForSeconds(0.1f);

        ammo--;
        GameObject torpedo = Instantiate(torpPrefab, transform.position + transform.forward, transform.rotation);
        torpedo.name = torpPrefab.name + " " + (maxAmmo - ammo);
        torpedo.GetComponent<MoveTorpV1>().SetTarget(target);
        print(gameObject.name + " Created TORPEDO with target = " + target.name);

        torpedo.GetComponent<MoveTorpV1>().team = team;
        Physics.IgnoreCollision(gameObject.GetComponent<BoxCollider>(), torpedo.transform.Find("model").gameObject.GetComponent<BoxCollider>());
        forwardDir = transform.forward;
        torpedo.GetComponent<Rigidbody>().AddForce(subRb.velocity + 10 * forwardDir.normalized, ForceMode.Impulse);

        //Debug.DrawRay(transform.position, subRb.velocity + 10 * forwardDir.normalized, Color.blue, 2);
        //print("subRb.velocity = " + subRb.velocity);
        //print("Torpedo Direction = " + (transform.position + subRb.velocity + 6 * forwardDir.normalized));
        //print("forwardDir = " + forwardDir.normalized);
        //print("10xforwardDir = " + 10 * forwardDir.normalized);

    }

    public void SetMoveDestination(Vector3 moveDestination, bool withTartget)
    {
        this.moveDestination = moveDestination;
        if (!withTartget)
        {
            target = null;
            fixTarget = false;
        }
        aligned = false;
        forwardDir = transform.forward;
        currentPos = transform.position;
        moveTargetDir = moveDestination - currentPos;
        targetDistance = moveTargetDir.magnitude;
        forwardTargetAngle = Vector3.Angle(forwardDir, moveTargetDir);

        if (forwardTargetAngle <= 45 || targetDistance > 20)
        {
            behaviour = BehaviourState.TurnToDirection;
            targetRotationY = Quaternion.LookRotation(moveTargetDir, Vector3.up).eulerAngles.y;
        }
        else if (forwardTargetAngle > 155)
        {
            behaviour = BehaviourState.Reverse; // Движение задом на точку
            targetRotationY = transform.eulerAngles.y;
        }
        else
        {
            behaviour = BehaviourState.ReverseTurn; // Движение задом, разворачиваясь к точке передом
            targetRotationY = Quaternion.LookRotation(moveTargetDir, Vector3.up).eulerAngles.y;
        }
        Debug.Log(behaviour + ", moveDestination " + moveDestination + ", targetDistance " + targetDistance + ", forwardTargetAngle " + forwardTargetAngle);

        //oldmoveDestination = moveDestination;
    }

    public void SetAttackTarget(GameObject target, bool fixTarget)
    {
        this.target = target;
        if (fixTarget == true)
            this.fixTarget = fixTarget;

        //behaviour = BehaviourState.Attacking;
        if(Vector3.Distance(transform.position, target.transform.position) > attackRange)
            SetMoveDestination(target.transform.position, true);
        //Debug.Log("Set attack target, position = " + targetPosition);
    }
}