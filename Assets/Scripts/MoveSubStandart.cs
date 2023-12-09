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
    private ParticleSystem trail;
    private GameObject selectionSprite;

    // Movement parameters
    private float power = 5f;
    private float speed;
    public float myDrag = 1f;
    private float thrust;
    private int rotationSpeedCoeff = 1500, steadyYawSpeed, steadyPitchSpeed=10, borderDistance = 10, turnRadius = 10;

    // Attack parameters
    private float reloadTime = 3f;
    private int maxAmmo = 20, ammo = 20, angleStepCount = 0;
    public int attackRange = 40;

    private Vector3 currentPos, forwardDir, moveTargetDir, attackTargetDir, startVelocity, slowedVelocity;
    private Vector3 oldPos = Vector3.zero;
    private Quaternion newRotation;
    private float forwardTargetAngle, forwardTargetAngleStart, velocityTargetAngle;
    private float targetDistance;
    private float stopTime, lastShotTime;
    private float dragDecel;
    private float targetRotationY;
    private bool aligned = true;
    private float timer;
    private AudioSource submarineSource;
    public BehaviourState behaviour;
    private string unitName;
    //private Order;

    public enum BehaviourState { FullThrottle, TurnToDirection, Reverse, ReverseTurn, StillApproach, Idle, Attacking};
    public GameObject torpPrefab, target = null;
    public Vector3 moveDestination, finalDestination;
    public bool stopped = true, searching = false, fixTarget = false;
    public AudioClip launchTorpedoSound, launchFastTorpedoSound;

    private int count = 0;

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
        moveDestination = finalDestination = Vector3.zero;
        moveTargetDir = moveDestination - currentPos;
        forwardTargetAngle = Vector3.Angle(forwardDir, moveTargetDir);
        targetDistance = moveTargetDir.magnitude;
        selectionSprite = transform.Find("Selection Sprite").gameObject;
        submarineSource = GetComponent<AudioSource>();
        targetRotationY = transform.localEulerAngles.y;
        behaviour = BehaviourState.Idle;
        stopped = false;
        unitName = gameObject.name;
        //if (unitName.IndexOf(' ') != -1) unitName = unitName.Substring(0, unitName.IndexOf(' '));
        steadyYawSpeed = unitName == "ST_Terminator 0" ? 0 : 10;
        //print(unitName + ", steadyYawSpeed = " + steadyYawSpeed);
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        //if (gameObject.name == "ST_Terminator")
        //{
        //    print("timer = " + timer);
        //    print("count = " + count++);
        //}
        startVelocity = subRb.velocity;

        // Симуляция сопротивления среды
        if (startVelocity.magnitude > 0.5f)
        {
            //slowedVelocity = startVelocity * Mathf.Clamp01(1f - myDrag * Time.deltaTime);
            //slowedVelocity = startVelocity * (1 - myDrag * Mathf.Clamp(startVelocity.magnitude, 1, 1000) * Time.deltaTime);
            forwardDir = transform.forward;
            float alignSin = Mathf.Sin(Vector3.Angle(forwardDir, startVelocity) * Mathf.PI / 180);
            print("alignSin = " + alignSin + ", angle = " + Vector3.Angle(forwardDir, startVelocity));

            slowedVelocity = startVelocity * (1f - myDrag * (1 + alignSin) * Time.deltaTime);
            //slowedVelocity = Quaternion.RotateTowards(Quaternion.LookRotation(slowedVelocity, transform.up), transform.rotation, alignSin * Time.deltaTime) * slowedVelocity;
            slowedVelocity = Vector3.RotateTowards(slowedVelocity, transform.forward * slowedVelocity.magnitude, alignSin * Time.deltaTime, 0f);
            subRb.velocity = slowedVelocity;

            print("slowedVelocity before turn = " + slowedVelocity);
            //subRb.velocity = slowedVelocity;
            Debug.DrawRay(transform.position, slowedVelocity * 10, Color.white);
            Debug.DrawRay(transform.position, slowedVelocity * 10, Color.yellow);
            print("slowedVelocity after turn = " + slowedVelocity);// + ", rotation = " + Vector3.Angle(slowedVelocity, forwardDir));
            print("rotation = " + Vector3.Angle(slowedVelocity, forwardDir) + ", turn step = " + alignSin * Time.deltaTime);
            speed = slowedVelocity.magnitude;

            //print(transform.name + " speed = " + speed);
            //print("targetDistance = " + targetDistance);

            // Проверка когда пора выключать двигатели
            stopTime = targetDistance / speed;
            dragDecel = (startVelocity.magnitude - slowedVelocity.magnitude) / Time.deltaTime;
            velocityTargetAngle = Vector3.Angle(slowedVelocity, moveTargetDir);

            print("startVelocity = " + startVelocity.magnitude + ", slowedVelocity = " + slowedVelocity.magnitude);
            //print("stopTime = " + stopTime + ", targetDistance = " + targetDistance  + ", speed = " + speed );
            //print("real stop time = " + speed / dragDecel * myDrag + ", velocityTargetAngle = " + velocityTargetAngle);
            print("dragDecel = " + dragDecel);
            //print("stopped = " + stopped + ", moveDestination = " + moveDestination);
        }
        else if(!stopped && moveDestination == Vector3.zero)
        {
            subRb.velocity = subRb.angularVelocity = slowedVelocity = Vector3.zero;
            stopped = true;
            print("Kill velocity");
        }

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
            var speedTargetAngle = Vector3.Angle(slowedVelocity, moveTargetDir);
            switch (behaviour)
            {
                case BehaviourState.TurnToDirection:
                    //if (forwardTargetAngle < 90 / turnRadius * targetDistance)
                    if (speedTargetAngle < forwardTargetAngleStart / (2 + turnRadius / targetDistance))
                    {
                        behaviour = BehaviourState.FullThrottle;
                        print(behaviour);
                        print("Angle turn limit = " + forwardTargetAngleStart / (2.4f + turnRadius / targetDistance));
                    }
                    break;
                case BehaviourState.ReverseTurn:
                    //if (forwardTargetAngle < forwardTargetAngleStart/1.5f)
                    if (forwardTargetAngle < forwardTargetAngleStart/(2 + turnRadius/targetDistance))
                    {
                        behaviour = BehaviourState.TurnToDirection;
                        print(behaviour);
                        print("Angle turn limit = " + forwardTargetAngleStart / (2 + turnRadius / targetDistance));
                    }
                    break;
            }

            if (finalDestination == Vector3.zero && target == null)
            {
                if (
                targetDistance < 2 // погрешность достижения точки, либо она слишком близко
                || ((stopTime < speed / dragDecel * myDrag) && (velocityTargetAngle < 5 || velocityTargetAngle > 175)) // направление примерно на точку, чтобы не промахиваться по времени остановки
                    )
                {
                        Stop();
                        print("111111111111111");
                }
            }
            else if (target != null)
            {
                if (targetDistance < attackRange)
                {
                    Stop();
                    //print(behaviour);
                    print("2222222222222222");
                }
            }
            else if(targetDistance < 2 || (stopTime < speed / dragDecel * myDrag) && (velocityTargetAngle < 5 || velocityTargetAngle > 175))
            {
                //SetMoveDestination(finalDestination, false);
                Stop();
                aligned = true;
                //targetRotationY = transform.eulerAngles.y;
                StartCoroutine(CSetMoveDestination(stopTime, finalDestination, false, targetRotationY));
                finalDestination = Vector3.zero;
                print(behaviour);
                print("333333333333333");
            }

        }
        else if (target == null && !aligned) // Выравнивание без цели
        {
            //print(transform.name + " Aligning, angles = " + transform.localEulerAngles);
            TurnToAnglesXY(0, targetRotationY, 1, true);
        }
        else if (target != null && team == 1 && ammo > 0) // Выравнивание на цель
        {
            currentPos = transform.position;
            attackTargetDir = target.transform.position - transform.position;
            float angleTarget = Vector3.Angle(transform.forward, attackTargetDir);

            if (angleTarget > 5) { aligned = false; }  // Не выровнен

            if (!aligned) // Поворот на цель
            {
                print(transform.name + " Turning to target, angle = " + angleTarget + ", attackTargetDir = " + attackTargetDir + ", target = " + target.name);
                //newRotation = Quaternion.LookRotation(moveTargetDir - slowedVelocity, Vector3.up); // Компенсация боковой скорости

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
                    //Invoke("Shoot", 0.1f);
                    if (steadyYawSpeed == 0)
                    {
                        submarineSource.PlayOneShot(launchTorpedoSound, 0.1f);
                        StartCoroutine(Shoot(target, 0.1f));
                    }
                    else
                    {
                        submarineSource.PlayOneShot(launchFastTorpedoSound, 0.1f);
                        StartCoroutine(Shoot(target, 0.25f));
                    }
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
                angleStepCount = 0;
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
        //subRb.AddForce(power * thrust * Mathf.Clamp01(targetDistance / borderDistance) * forwardDir);
        subRb.AddForce(power * thrust * forwardDir);
        //print("Applying power " + power * thrust * Mathf.Clamp01(targetDistance * thrustDistCoeff) * Mathf.Clamp(mode, -1, 1));

        var distanceStep = (currentPos - oldPos).magnitude;
        //var anglestep = turnRadius * Mathf.Sqrt(speed) * Time.deltaTime;
        var anglestep = turnRadius * Mathf.Sqrt(speed) * Time.deltaTime;

        // Поворот на цель + добавка угла, чтобы погасить проекцию скорости в бок от цели
        transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation, anglestep);
        //transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation, turnRadius * speed * Time.deltaTime);

        //print("speed = " + speed + ", sqrt(speed) = " + Mathf.Sqrt(speed));
        print("step=" + ++angleStepCount + ", angle=" + anglestep + ", targetDistance = " + targetDistance);
        //print("travelled = " + distanceStep + ", angle/travelled = " + anglestep / distanceStep);

        oldPos = currentPos;
        Debug.DrawRay(currentPos, (moveTargetDir - slowedVelocity), Color.red);
    }

    private bool TurnToAnglesXY(float targetAngleX, float targetAngleY, int threshold, bool hardAlignment)
    {
        float currentAngleX = transform.localEulerAngles.x, currentAngleY = transform.localEulerAngles.y, currentAngleZ = transform.localEulerAngles.z;

        targetAngleX = targetAngleX < 180 ? targetAngleX : targetAngleX - 360; // 350 --> -10,  10 --> 10
        targetAngleY = targetAngleY < 180 ? targetAngleY : targetAngleY - 360;

        currentAngleX = currentAngleX < 180 ? currentAngleX : currentAngleX - 360;  // 5 --> 5, 355 --> -5
        currentAngleY = currentAngleY < 180 ? currentAngleY : currentAngleY - 360;
        currentAngleZ = currentAngleZ < 180 ? currentAngleZ : currentAngleZ - 360;

        if (steadyYawSpeed == 0 && stopped)
        {
            aligned = true;
            return true;
        }

        Vector2 angleDiff = new(targetAngleX - currentAngleX, targetAngleY - currentAngleY);
        // Плавное выравнивание
        if (currentAngleZ > 1 || angleDiff.magnitude > threshold) 
        {
            Quaternion targetRotationY = Quaternion.Euler(transform.localEulerAngles.x, targetAngleY, -transform.localEulerAngles.z);

            //print(transform.name + " Current angles = " + transform.localEulerAngles);
            //print(transform.name + " Turning to " + targetRotation.eulerAngles);

            float turnSpeedYaw = steadyYawSpeed;
            if (speed > 1)
            {
                turnSpeedYaw += Mathf.Sqrt(speed); // Компонента скорости поворота, зависящая от скорости
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotationY, turnSpeedYaw * Time.deltaTime);
            //print(transform.name + " turnSpeedYaw = " + turnSpeedYaw);


            Quaternion targetRotationX = Quaternion.Euler(targetAngleX, transform.localEulerAngles.y, 0);

            float turnSpeedPitch = steadyPitchSpeed;
            if (speed > 1)
            {
                turnSpeedPitch += Mathf.Sqrt(speed); // Компонента скорости поворота, зависящая от скорости
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotationX, turnSpeedPitch * Time.deltaTime);
            //print(transform.name + " turnSpeedPitch = " + turnSpeedPitch);

            Debug.DrawRay(currentPos, transform.up * 10, Color.green);
            Debug.DrawRay(currentPos, Vector3.up * 10, Color.red);
            return false;
        }
        else if (hardAlignment) // Жёсткое выравнивание
        {
            print(gameObject.name + " Hard alignment");

            transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
            aligned = true;
            subRb.angularVelocity = Vector3.zero;
            return true;
        }
        else // Достаточно близко
        {
            print(gameObject.name + " Alignment threshold reached, angleDiff = " + angleDiff.magnitude);

            aligned = true;
            subRb.angularVelocity = Vector3.zero;
            return true;
        }
    }

    public void Stop()
    {
        print("ENGINES STOP, target = " + target);

        trail.Stop();
        behaviour = BehaviourState.Idle;
        Invoke("SearchReset", stopTime);
        subRb.angularVelocity = moveDestination = Vector3.zero;
    }

    private void SearchReset()
    {
        print(gameObject.name + " Enable target search");
        searching = false;
    }

    //private void Shoot()
    private IEnumerator Shoot(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);

        ammo--;
        GameObject torpedo = Instantiate(torpPrefab, transform.position + transform.forward, transform.rotation);
        torpedo.name = torpPrefab.name + " " + (maxAmmo - ammo);
        torpedo.GetComponent<MoveTorpV1>().SetTarget(target);
        print(gameObject.name + " Created TORPEDO with target = " + target.name);

        torpedo.GetComponent<MoveTorpV1>().team = team;
        Physics.IgnoreCollision(gameObject.GetComponent<CapsuleCollider>(), torpedo.transform.Find("model").gameObject.GetComponent<BoxCollider>());
        forwardDir = transform.forward;
        torpedo.GetComponent<Rigidbody>().AddForce(subRb.velocity + 10 * forwardDir.normalized, ForceMode.Impulse);

        //Debug.DrawRay(transform.position, subRb.velocity + 10 * forwardDir.normalized, Color.blue, 2);
        //print("subRb.velocity = " + subRb.velocity);
        //print("Torpedo Direction = " + (transform.position + subRb.velocity + 6 * forwardDir.normalized));
        //print("forwardDir = " + forwardDir.normalized);
        //print("10xforwardDir = " + 10 * forwardDir.normalized);

    }

    private IEnumerator CSetMoveDestination(float time, Vector3 moveDestination, bool withTartget, float recievedTargetRotationY = 0, bool moveInGroup = true)
    {
        yield return new WaitForSeconds(time);
        SetMoveDestination(moveDestination, withTartget, recievedTargetRotationY, moveInGroup, setPrimary: false);
    }

    public void SetMoveDestination(Vector3 moveDestination, bool withTartget, float recievedTargetRotationY = 0, bool moveInGroup = true, bool setPrimary = true)
    {
        print("Entered SetMoveDestination, moveDestination = " + moveDestination);
        //if (steadyYawSpeed == 0) 
        if (recievedTargetRotationY != 0 && setPrimary)
        {
            Vector3 vecRad = (moveDestination - currentPos).normalized * turnRadius;
            Vector3 turnCircleCenter, targetRotationVec = Quaternion.Euler(new Vector3(0, recievedTargetRotationY, 0)) * Vector3.forward;
            short sign = (short)Mathf.Sign(Vector3.SignedAngle(moveDestination - currentPos, Quaternion.Euler(new Vector3(0, recievedTargetRotationY, 0)) * Vector3.forward, Vector3.up));
            print("sign = " + sign);
            //turnCircleCenter = moveDestination - Quaternion.AngleAxis(90 - Vector3.SignedAngle(moveDestination - currentPos, Quaternion.Euler(new Vector3(0, recievedTargetRotationY, 0)) * Vector3.forward, Vector3.up), transform.up) * vecRad;
            //if (Vector3.SignedAngle(moveDestination - currentPos, Quaternion.Euler(new Vector3(0, recievedTargetRotationY, 0)) * Vector3.forward, Vector3.up) < 0)
            //    turnCircleCenter = moveDestination - Quaternion.AngleAxis(90, transform.up) * targetRotationVec * turnRadius;
            //else
            //    turnCircleCenter = moveDestination + Quaternion.AngleAxis(90, transform.up) * targetRotationVec * turnRadius;

            turnCircleCenter = moveDestination + Quaternion.AngleAxis(90, transform.up) * targetRotationVec * turnRadius * sign;
            
            Vector3 radiusPoint = turnCircleCenter + vecRad, radiusPointFinal = radiusPoint;
            print("Angle direction to rotation = " + Vector3.SignedAngle(moveDestination - currentPos, Quaternion.Euler(new Vector3(0, recievedTargetRotationY, 0)) * Vector3.forward, Vector3.up));
            for(int y=0; y< 180; y++)
            {
                vecRad = Quaternion.AngleAxis(2, transform.up) * vecRad * sign;
                radiusPoint = turnCircleCenter + vecRad;
                if (Mathf.Abs(Vector3.Angle(turnCircleCenter - radiusPoint, radiusPoint - currentPos) - 90) < 5)
                {
                    Debug.DrawLine(turnCircleCenter, radiusPoint, Color.green, 10);
                    radiusPointFinal = radiusPoint;
                    print("Angle radial scan orthogonal = " + Vector3.Angle(targetRotationVec, turnCircleCenter - radiusPoint));
                }
                else Debug.DrawLine(turnCircleCenter, radiusPoint, Color.yellow, 10);
                //print("ANGLE = " + Mathf.Abs(Vector3.Angle(turnCircleCenter - radiusPoint, radiusPoint - currentPos) - 90) + ", RADIUS = " + vecRad.magnitude);
            }
            this.moveDestination = radiusPointFinal;
            finalDestination = moveDestination;


        }
        else this.moveDestination = moveDestination;


        if (!withTartget)
        {
            target = null;
            fixTarget = false;
        }
        aligned = false;
        stopped = false;
        forwardDir = transform.forward;
        currentPos = transform.position;
        moveTargetDir = moveDestination - currentPos;
        print("moveDestination = " + moveDestination + ", currentPos = " + currentPos);
        targetDistance = moveTargetDir.magnitude;
        forwardTargetAngleStart = Vector3.Angle(forwardDir, moveTargetDir);

        if (forwardTargetAngleStart < 90)
        {
            print("angle limit = " + 90 / turnRadius * targetDistance + ", angle = " + forwardTargetAngleStart);
            if (forwardTargetAngleStart < 90 / turnRadius * targetDistance) // Вперёд
            {
                behaviour = BehaviourState.TurnToDirection;
                targetRotationY = recievedTargetRotationY == 0 ? Quaternion.LookRotation(moveTargetDir, Vector3.up).eulerAngles.y : recievedTargetRotationY;
            }
            else
            {
                behaviour = BehaviourState.ReverseTurn; // Назад, разворачиваясь к точке передом
                targetRotationY = recievedTargetRotationY == 0 ? Quaternion.LookRotation(moveTargetDir, Vector3.up).eulerAngles.y : recievedTargetRotationY;
            }
        }
        else if ((targetDistance < turnRadius && forwardTargetAngleStart < 150) || targetDistance > turnRadius)
        {
            behaviour = BehaviourState.ReverseTurn; // Назад, разворачиваясь к точке передом
            targetRotationY = recievedTargetRotationY == 0 ? Quaternion.LookRotation(moveTargetDir, Vector3.up).eulerAngles.y : recievedTargetRotationY;
        }
        else 
        {
            behaviour = BehaviourState.Reverse; // Задом на точку
            if (recievedTargetRotationY != 0)
                targetRotationY = recievedTargetRotationY;
            else if (moveInGroup)
                targetRotationY = Quaternion.LookRotation(moveTargetDir, Vector3.up).eulerAngles.y;
            else 
                targetRotationY = transform.eulerAngles.y;
        }
        //Debug.Log(behaviour + ", moveDestination " + moveDestination + ", targetDistance " + targetDistance + ", forwardTargetAngleStart " + forwardTargetAngleStart);
        Debug.Log("targetDistance " + targetDistance + ", forwardTargetAngleStart " + forwardTargetAngleStart);

        //oldmoveDestination = moveDestination;
    }

    public void SetAttackTarget(GameObject target, bool fixTarget)
    {
        print("Entered SetAttackTarget, target = " + target.name);
        this.target = target;
        if (fixTarget == true)
            this.fixTarget = fixTarget;

        //behaviour = BehaviourState.Attacking;
        if(Vector3.Distance(transform.position, target.transform.position) > attackRange)
            SetMoveDestination(target.transform.position, true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        stopped = false;
    }
}