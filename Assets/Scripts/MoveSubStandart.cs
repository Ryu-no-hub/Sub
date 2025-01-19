using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MoveSubStandart: MonoBehaviour, ISelectable
{
    private Rigidbody subRb;
    private ParticleSystem trail;
    private GameObject selectionSprite;

    // Movement parameters
    private float power = 5f;
    private float speed;
    public float myDrag = 1f;
    private float thrust;
    private int rotationSpeedCoeff = 1500, steadyYawSpeed, steadyPitchSpeed = 10, borderDistance = 10, turnRadius = 10;

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
    private bool canChangeState = true;
    private float timer;
    private AudioSource submarineSource;
    public BehaviourState behaviour;
    private string unitName;
    //private Order;

    public enum BehaviourState { FullThrottle, TurnToDirection, Reverse, ReverseTurn, Idle, StillApproach, Attacking };
    public GameObject torpPrefab, target = null;
    public Vector3 moveDestination, intermediateDestination, finalDestination;
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
        moveDestination = finalDestination = intermediateDestination = Vector3.zero;
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
        startVelocity = subRb.linearVelocity;

        // Симуляция сопротивления среды
        if (startVelocity.magnitude > 0.5f)
        {
            //slowedVelocity = startVelocity * Mathf.Clamp01(1f - myDrag * Time.deltaTime);
            //slowedVelocity = startVelocity * (1 - myDrag * Mathf.Clamp(startVelocity.magnitude, 1, 1000) * Time.deltaTime);
            forwardDir = transform.forward;
            float alignSin = Mathf.Sin(Vector3.Angle(forwardDir, startVelocity) * Mathf.PI / 180);
            //print("alignSin = " + alignSin + ", angle = " + Vector3.Angle(forwardDir, startVelocity));

            slowedVelocity = startVelocity * (1f - myDrag * (1 + alignSin) * Time.deltaTime);
            //slowedVelocity = Quaternion.RotateTowards(Quaternion.LookRotation(slowedVelocity, transform.up), transform.rotation, alignSin * Time.deltaTime) * slowedVelocity;
            slowedVelocity = Vector3.RotateTowards(slowedVelocity, transform.forward * slowedVelocity.magnitude, alignSin * Time.deltaTime, 0f);
            subRb.linearVelocity = slowedVelocity;

            //print("slowedVelocity before turn = " + slowedVelocity);
            //subRb.linearVelocity = slowedVelocity;
            Debug.DrawRay(transform.position, slowedVelocity * 10, Color.white);
            Debug.DrawRay(transform.position, slowedVelocity * 10, Color.yellow);
            //print("slowedVelocity after turn = " + slowedVelocity);// + ", rotation = " + Vector3.Angle(slowedVelocity, forwardDir));
            //print("rotation = " + Vector3.Angle(slowedVelocity, forwardDir) + ", turn step = " + alignSin * Time.deltaTime);
            speed = slowedVelocity.magnitude;

            //print(transform.name + " speed = " + speed);
            //print("targetDistance = " + targetDistance);

            // Проверка когда пора выключать двигатели
            stopTime = targetDistance / speed;
            dragDecel = (startVelocity.magnitude - slowedVelocity.magnitude) / Time.deltaTime;
            velocityTargetAngle = Vector3.Angle(slowedVelocity, moveTargetDir);

            //print("startVelocity = " + startVelocity.magnitude + ", slowedVelocity = " + slowedVelocity.magnitude);
            //print("stopTime = " + stopTime + ", targetDistance = " + targetDistance  + ", speed = " + speed );
            //print("real stop time = " + speed / dragDecel * myDrag + ", velocityTargetAngle = " + velocityTargetAngle);
            //print("dragDecel = " + dragDecel);
            //print("stopped = " + stopped + ", moveDestination = " + moveDestination);
        }
        else if (!stopped && moveDestination == Vector3.zero)
        {
            subRb.linearVelocity = subRb.angularVelocity = slowedVelocity = Vector3.zero;
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

            if (!canChangeState)
                goto Stopcheck;
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
                        print("Angle speedTargetAngle = " + speedTargetAngle);
                    }
                    break;
                case BehaviourState.ReverseTurn:
                    //if (forwardTargetAngle < forwardTargetAngleStart/1.5f)
                    if (forwardTargetAngle < forwardTargetAngleStart / (2 + turnRadius / targetDistance))
                    {
                        behaviour = BehaviourState.TurnToDirection;
                        print(behaviour);
                        print("Angle turn limit = " + forwardTargetAngleStart / (2 + turnRadius / targetDistance));
                        print("Angle forwardTargetAngle = " + forwardTargetAngle);
                    }
                    break;
            }

        // Проверка когда пора выключать двигатели
        Stopcheck:
            #region Stop check
            if (intermediateDestination != Vector3.zero)
            {
                if (target != null && targetDistance < attackRange)
                {
                    Stop();
                    print("Intermediate exists, target in range!");
                }
                else if (targetDistance < 2 || (stopTime < speed / dragDecel * myDrag) && (velocityTargetAngle < 5 || velocityTargetAngle > 175))
                {
                    Stop();
                    aligned = true;
                    if (targetDistance < 2) stopTime = 0.1f;
                    StartCoroutine(CSetMoveDestination(stopTime, intermediateDestination, false, targetRotationY, predefinedState: BehaviourState.FullThrottle));
                    intermediateDestination = Vector3.zero;
                    print("First point reached, stoptime = " + stopTime);
                    print("finalDestination = " + finalDestination);
                }
            }
            else if (finalDestination == Vector3.zero && target == null)
            {
                if (
                targetDistance < 2 // погрешность достижения точки, либо она слишком близко
                || ((stopTime < speed / dragDecel * myDrag) && (velocityTargetAngle < 5 || velocityTargetAngle > 175)) // направление примерно на точку, чтобы не промахиваться по времени остановки
                    )
                {
                    Stop(true);
                    print("Final stop, targetDistance = " + targetDistance);
                }
            }
            else if (target != null)
            {
                if (targetDistance < attackRange)
                {
                    Stop();
                    print("Target in range!");
                }
            }
            else if (targetDistance < 2 || (stopTime < speed / dragDecel * myDrag) && (velocityTargetAngle < 5 || velocityTargetAngle > 175))
            {
                Stop();
                aligned = true;
                if (targetDistance < 2) stopTime = 0.1f;
                StartCoroutine(CSetMoveDestination(stopTime, finalDestination, false, targetRotationY, predefinedState: BehaviourState.FullThrottle));
                finalDestination = Vector3.zero;
                print("Second point reached, stoptime = " + stopTime);
            }

            #endregion Stop check

        }
        else if (target == null && !aligned) // Выравнивание без цели
        {
            //print(transform.name + " Aligning, angles = " + transform.localEulerAngles + ", targetRotationY = " + targetRotationY);
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
            else if (behaviour != BehaviourState.StillApproach) // Цель уплыла - подплыть на расстояние выстрела
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
                    //print("Trail forward");
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
                    //print("Trail reverse");
                }
                goto case BehaviourState.ReverseTurn;
        }

        if (!trail.isPlaying)
            trail.Play();

        // Тяга вперёд
        //subRb.AddForce(power * thrust * Mathf.Clamp01(targetDistance / borderDistance) * forwardDir);
        subRb.AddForce(power * thrust * forwardDir);
        //print("Applying power " + power * thrust * Mathf.Clamp01(targetDistance * thrustDistCoeff) * Mathf.Clamp(mode, -1, 1));

        // var distanceStep = (currentPos - oldPos).magnitude;
        var anglestep = 1.5f * turnRadius * Mathf.Sqrt(speed) * Time.deltaTime;

        // Поворот на цель + добавка угла, чтобы погасить проекцию скорости в бок от цели
        transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation, anglestep);
        //transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation, turnRadius * speed * Time.deltaTime);

        //print("speed = " + speed + ", sqrt(speed) = " + Mathf.Sqrt(speed));
        //print("step=" + ++angleStepCount + ", angle=" + anglestep + ", targetDistance = " + targetDistance);
        //print("speed = " + speed + ", angle=" + anglestep + ", step=" + ++angleStepCount);
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

            print(transform.name + " Current angles euler = " + transform.localEulerAngles + ", currentAngleY = " + currentAngleY);
            print(transform.name + " Turning to " + targetAngleY);

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

    public void Stop(bool completeStop = false)
    {
        print("ENGINES STOP, target = " + target + ", targetRotationY = " + targetRotationY);

        trail.Stop();
        canChangeState = true;
        behaviour = BehaviourState.Idle;
        subRb.angularVelocity = moveDestination = Vector3.zero;
        if (completeStop)
        {
            Invoke(nameof(SearchReset), stopTime);
            aligned = true;
        }
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
        torpedo.GetComponent<Rigidbody>().AddForce(subRb.linearVelocity + 10 * forwardDir.normalized, ForceMode.Impulse);

        //Debug.DrawRay(transform.position, subRb.velocity + 10 * forwardDir.normalized, Color.blue, 2);
        //print("subRb.velocity = " + subRb.velocity);
        //print("Torpedo Direction = " + (transform.position + subRb.velocity + 6 * forwardDir.normalized));
        //print("forwardDir = " + forwardDir.normalized);
        //print("10xforwardDir = " + 10 * forwardDir.normalized);
    }

    private IEnumerator CSetMoveDestination(float time, Vector3 moveDestination, bool withTartget, float recievedTargetRotationY = 0, bool moveInGroup = true, BehaviourState predefinedState = BehaviourState.Idle)
    {
        yield return new WaitForSeconds(time);
        SetMoveDestination(moveDestination, withTartget, recievedTargetRotationY, moveInGroup, predefinedState);
        //print("Setting next point = " + moveDestination);
    }

    public void SetMoveDestination(Vector3 moveDestination, bool withTartget, float recievedTargetRotationY = 0, bool moveInGroup = true, BehaviourState predefinedState = BehaviourState.Idle)
    {
        print("Entered SetMoveDestination, moveDestination = " + moveDestination);
        print("recievedTargetRotationY = " + recievedTargetRotationY);

        if (!withTartget)
        {
            target = null;
            fixTarget = false;
        }
        aligned = false;
        stopped = false;

        if (recievedTargetRotationY != 0 && predefinedState == BehaviourState.Idle)
        {
            float showtime = 15;
            Vector3 currentPos = transform.position;

            // Круги вплотную к подлодке
            Vector3 moveDir = transform.forward;
            Vector3 circleCenterMeRight = currentPos + Quaternion.AngleAxis(90, Vector3.up) * moveDir * turnRadius;
            Vector3 circleCenterMeLeft = currentPos + Quaternion.AngleAxis(-90, Vector3.up) * moveDir * turnRadius;
            Vector3 circleCenterMe, circleCenterMeOther;//, circleCenterMeAdjacent;

            DrawCircle(circleCenterMeRight, Color.blue, showtime);
            DrawCircle(circleCenterMeLeft, Color.blue, showtime);

            // Круги вплотную к точке назначения
            Vector3 targetRotationVec = Quaternion.Euler(new Vector3(0, recievedTargetRotationY, 0)) * Vector3.forward;
            Vector3 circleCenterDestinationLeft = moveDestination + Quaternion.AngleAxis(-90, Vector3.up) * targetRotationVec * turnRadius;
            Vector3 circleCenterDestinationRight = moveDestination + Quaternion.AngleAxis(90, Vector3.up) * targetRotationVec * turnRadius;
            Vector3 circleCenterDestination;

            // Компенсация низкой скорости поворота вконце
            circleCenterDestinationLeft += power / 2 * -targetRotationVec;
            circleCenterDestinationRight += power / 2 * -targetRotationVec;

            DrawCircle(circleCenterDestinationLeft, Color.red, showtime);
            DrawCircle(circleCenterDestinationRight, Color.red, showtime);

            float angleRelativeToDestination = Vector3.SignedAngle(targetRotationVec, currentPos - moveDestination, Vector3.up);
            circleCenterDestination = angleRelativeToDestination < 0 ? circleCenterDestinationLeft : circleCenterDestinationRight;

            bool leftHalf = angleRelativeToDestination < 0;
            bool myAdjacent_DCLeft, myMain_DCLeft;
            if (leftHalf)
                myAdjacent_DCLeft = myMain_DCLeft = true;
            else
                myAdjacent_DCLeft = myMain_DCLeft = false;

            float angleTangentToMyCircle = Vector3.Angle(currentPos, moveDestination - currentPos);
            print("angleRelativeToDestination = " + angleRelativeToDestination);
            float lengthMain, lengthAdjacent;
            Vector3 pointTangentMyToDC, pointTangentDCToMy, pointFirstAdjacent, pointTangentAdjacentToDC, pointTangentDCToAdjacent;
            string logStrStart = "BUILD TRAJECTORY: ";
            bool front = angleRelativeToDestination > -90 && angleRelativeToDestination < 90;

            #region old circles
            //if (front) // Передняя полуплоскость
            //{
            //    print(logStrStart + "left half = " + leftHalf + ", FRONT");

            //    // Касательная ко мне, чтобы определить сторону взгляда относительно неё
            //    Vector3 pointTangentDCToMe = FindTangentPoints(true, circleCenterDestination, currentPos, true, leftHalf)[0];

            //    float angleMyDirectionToTangent = Vector3.SignedAngle(currentPos - pointTangentDCToMe, moveDir, Vector3.up);
            //    Debug.DrawLine(pointTangentDCToMe, currentPos, Color.white, showtime);

            //    bool rightTurn;
            //    if (angleMyDirectionToTangent > 0) // Повёрнут направо от касательной. Выбрать правый и левый задний круги. 
            //    {
            //        rightTurn = true;
            //        circleCenterMe = circleCenterMeRight;
            //        circleCenterMeOther = circleCenterMeLeft;
            //    }
            //    else
            //    {
            //        rightTurn = false;
            //        circleCenterMe = circleCenterMeLeft;
            //        circleCenterMeOther = circleCenterMeRight;
            //    }
            //    print(logStrStart + "rightTurn = " + rightTurn);
            //    Debug.DrawRay(circleCenterMe, 30 * Vector3.up, Color.cyan, showtime);

            //    // Определение к какой стороне моего дальнего круга строить касательную на которой будет лежать доп. круг
            //    bool toLeftSide = false;
            //    float angleCenterMyOtherToMyTangent = Vector3.SignedAngle(currentPos - pointTangentDCToMe, circleCenterMeOther - currentPos, Vector3.up);
            //    if (angleCenterMyOtherToMyTangent > 0)
            //        toLeftSide = true;

            //    // Касательная между circleCenterDestination и circleCenterMeOther для построения доп. круга
            //    (Vector3 pointTangentMyOtherToDC, Vector3 pointTangentDCToMyOther) = Deconstruct(FindTangentPoints(false, circleCenterMeOther, circleCenterDestination, leftHalf == toLeftSide, toLeftSide));
            //    Vector3 vecTangentDCMyOther = pointTangentMyOtherToDC - pointTangentDCToMyOther;
            //    Debug.DrawRay(pointTangentMyOtherToDC, 30 * Vector3.up, Color.green, showtime);
            //    Debug.DrawRay(pointTangentDCToMyOther, 30 * Vector3.up, Color.green, showtime);

            //    // Круг дополнительный к другому
            //    Vector3 circleMyOtherAdjacent = circleCenterMeOther - 2 * turnRadius * vecTangentDCMyOther.normalized;
            //    DrawCircle(circleMyOtherAdjacent, Color.yellow, showtime);

            //    // Точка маршрута через доп. круг
            //    pointFirstAdjacent = circleCenterMeOther - vecTangentDCMyOther.normalized * turnRadius;
            //    //Vector3 pointSecondAdjacent = pointTangentDCToMyOther;

            //    Vector3 pointCircleMeToDest, pointCircleAdjacentToDest;
            //    float angleCMTDToDestination, angleCATDToDestination;

            //    if ((rightTurn && leftHalf) || (!rightTurn && !leftHalf))
            //    {
            //        // Построить к выбранному основному и доп. кругу касательную из точки назначения
            //        pointCircleMeToDest = FindTangentPoints(true, circleCenterMe, moveDestination, true, rightTurn)[0];
            //        pointCircleAdjacentToDest = FindTangentPoints(true, circleCenterMe, moveDestination, true, rightTurn)[0];

            //        // Они выпирают в противоположную четверть?
            //        angleCMTDToDestination = Vector3.SignedAngle(targetRotationVec, pointCircleMeToDest - moveDestination, Vector3.up);
            //        if ((rightTurn && angleCMTDToDestination > 0) || (!rightTurn && angleCMTDToDestination < 0))
            //            myMain_DCLeft = !rightTurn; // Да

            //        angleCATDToDestination = Vector3.SignedAngle(targetRotationVec, pointCircleAdjacentToDest - moveDestination, Vector3.up);
            //        if ((!rightTurn && angleCATDToDestination > 0) || (rightTurn && angleCATDToDestination < 0))
            //            myAdjacent_DCLeft = rightTurn; // Да
            //    }
            //    print(logStrStart + "myMain_DCLeft = " + myMain_DCLeft);
            //    print(logStrStart + "myAdjacent_DCLeft = " + myAdjacent_DCLeft);

            //    Vector3 circleDestinationToMyMain = myMain_DCLeft ? circleCenterDestinationLeft : circleCenterDestinationRight;
            //    Vector3 circleDestinationToAdjacent = myAdjacent_DCLeft ? circleCenterDestinationLeft : circleCenterDestinationRight;

            //    Debug.DrawRay(circleDestinationToAdjacent, 30 * Vector3.up, Color.cyan, showtime);

            //    bool leftside, inner_my, inner_adjacent;
            //    leftside = rightTurn; // Наоборот в задней полуплоскости
            //    inner_my = inner_adjacent = leftHalf == rightTurn;
            //    if (!leftHalf && !rightTurn)
            //    {
            //            inner_my = !myMain_DCLeft;
            //            inner_adjacent = !myAdjacent_DCLeft;
            //    }
            //    else if(leftHalf && rightTurn)
            //    {
            //            inner_my = myMain_DCLeft;
            //            inner_adjacent = myAdjacent_DCLeft;
            //    }


            //    #region readable
            //    //if (!leftHalf)
            //    //{
            //    //    if (rightTurn)
            //    //        inner_my = inner_adjacent = false;
            //    //    else
            //    //    {
            //    //        inner_my = !myMain_DCLeft;
            //    //        inner_adjacent = !myAdjacent_DCLeft;
            //    //    }
            //    //}
            //    //else
            //    //{
            //    //    if (rightTurn)
            //    //    {
            //    //        inner_my = myMain_DCLeft;
            //    //        inner_adjacent = myAdjacent_DCLeft;
            //    //    }
            //    //    else
            //    //        inner_my = inner_adjacent = false;
            //    //}
            //    #endregion readable

            //    // Длина пути вперёд
            //    (pointTangentMyToDC, pointTangentDCToMy) = Deconstruct(FindTangentPoints(false, circleCenterMe, circleDestinationToMyMain, inner_my, leftside));

            //    float angleDCtoMain = Vector3.Angle(moveDestination - circleDestinationToMyMain, pointTangentDCToMy - circleDestinationToMyMain);
            //    float angleMainToDC = Vector3.Angle(currentPos - circleCenterMe, pointTangentMyToDC - circleCenterMe);

            //    if (Vector3.Angle(targetRotationVec, pointTangentDCToMy - moveDestination) < 90)
            //        angleDCtoMain = 360 - angleDCtoMain;
            //    if (Vector3.Angle(moveDir, pointTangentMyToDC - currentPos) > 90)
            //        angleMainToDC = 360 - angleMainToDC;

            //    float arcDCToMain = angleDCtoMain * turnRadius * Mathf.PI / 180;
            //    float arcMainToDC = angleMainToDC * turnRadius * Mathf.PI / 180;
            //    float connectionToMain = (pointTangentMyToDC - pointTangentDCToMy).magnitude;

            //    DrawArc(circleCenterMe, currentPos, pointTangentMyToDC, Color.black, showtime);
            //    DrawArc(circleDestinationToMyMain, moveDestination, pointTangentDCToMy, Color.black, showtime);
            //    Debug.DrawLine(pointTangentMyToDC, pointTangentDCToMy, Color.grey, showtime);

            //    lengthMain = arcDCToMain + arcMainToDC + connectionToMain;


            //    // Длина пути назад
            //    (pointTangentAdjacentToDC, pointTangentDCToAdjacent) = Deconstruct(FindTangentPoints(false, circleMyOtherAdjacent, circleDestinationToAdjacent, inner_adjacent, leftside));

            //    float angleMyOtherToAdjacent = Vector3.Angle(currentPos - circleCenterMeOther, pointFirstAdjacent - circleCenterMeOther);
            //    float angleAdjacentToDC = 90;
            //    float angleDCToAdjacent = Vector3.Angle(moveDestination - circleDestinationToAdjacent, pointTangentDCToAdjacent - circleDestinationToAdjacent);

            //    if (Vector3.Angle(targetRotationVec, pointTangentDCToAdjacent - moveDestination) < 90)
            //        angleDCToAdjacent = 360 - angleDCToAdjacent;
            //    // Невозможно
            //    //if (Vector3.Angle(Quaternion.AngleAxis(90, Vector3.up) * (pointTangentAdjacentToDC - pointTangentDCToAdjacent), pointTangentAdjacentToDC - circleCenterMeAdjacent) > 90)
            //    //    angleAdjacentToDC = 360 - angleAdjacentToDC;

            //    float arcMyOtherToAdjacent = angleMyOtherToAdjacent * turnRadius * Mathf.PI / 180;
            //    float arcAdjacentToDC = angleAdjacentToDC * turnRadius * Mathf.PI / 180;
            //    float arcDCToAdjacent = angleDCToAdjacent * turnRadius * Mathf.PI / 180;
            //    float connectionToAdjacent = (pointTangentAdjacentToDC - pointTangentDCToAdjacent).magnitude;

            //    DrawArc(circleCenterMeOther, currentPos, pointFirstAdjacent, Color.white, showtime);
            //    print(logStrStart + "arcMyOtherToAdjacent = " + arcMyOtherToAdjacent);
            //    DrawArc(circleMyOtherAdjacent, pointFirstAdjacent, pointTangentAdjacentToDC, Color.white, showtime);
            //    print(logStrStart + "arcAdjacentToDC = " + arcAdjacentToDC);
            //    DrawArc(circleDestinationToAdjacent, moveDestination, pointTangentDCToAdjacent, Color.white, showtime);
            //    print(logStrStart + "arcDCToAdjacent = " + arcDCToAdjacent);
            //    Debug.DrawLine(pointTangentAdjacentToDC, pointTangentDCToAdjacent, Color.green, showtime);

            //    lengthAdjacent = arcMyOtherToAdjacent + arcAdjacentToDC + arcDCToAdjacent + connectionToAdjacent;

            //}
            //else // Задняя полуплоскость
            //{
            //    print(logStrStart + "left half = " + leftHalf + ", BACK");

            //    // Касательная ко мне, чтобы определить сторону взгляда относительно неё
            //    Vector3 pointTangentDCToMe = FindTangentPoints(true, circleCenterDestination, currentPos, true, leftHalf)[0];

            //    float angleMyDirectionToTangent = Vector3.SignedAngle(moveDir, pointTangentDCToMe - currentPos, Vector3.up);
            //    Debug.DrawLine(pointTangentDCToMe, currentPos, Color.white, showtime);

            //    bool rightTurn;
            //    //print(logStrStart + "angleMyDirectionToTangent = " + angleMyDirectionToTangent);
            //    if (angleMyDirectionToTangent > 0) // Повёрнут направо от касательной. Выбрать правый и левый задний круги. 
            //    {
            //        rightTurn = true;
            //        circleCenterMe = circleCenterMeRight;
            //        circleCenterMeOther = circleCenterMeLeft;
            //    }
            //    else
            //    {
            //        rightTurn = false;
            //        circleCenterMe = circleCenterMeLeft;
            //        circleCenterMeOther = circleCenterMeRight;
            //    }
            //    print(logStrStart + "rightTurn = " + rightTurn);
            //    Debug.DrawRay(circleCenterMe, 30 * Vector3.up, Color.cyan, showtime);

            //    // Определение к какой стороне моего дальнего круга строить касательную для построения доп. круга
            //    bool toLeftSide = false;
            //    float angleCenterMyOtherToMyTangent = Vector3.SignedAngle(currentPos - pointTangentDCToMe, circleCenterMeOther - currentPos, Vector3.up);
            //    if (angleCenterMyOtherToMyTangent > 0)
            //        toLeftSide = true;

            //    // Касательная между circleCenterDestination и circleCenterMeOther для построения доп. круга
            //    (Vector3 pointTangentMyOtherToDC, Vector3 pointTangentDCToMyOther) = Deconstruct(FindTangentPoints(false, circleCenterMeOther, circleCenterDestination, leftHalf == toLeftSide, toLeftSide));
            //    Vector3 vecTangentDCMyOther = pointTangentMyOtherToDC - pointTangentDCToMyOther;
            //    Debug.DrawRay(pointTangentMyOtherToDC, 30 * Vector3.up, Color.green, showtime);
            //    Debug.DrawRay(pointTangentDCToMyOther, 30 * Vector3.up, Color.green, showtime);

            //    // Круг дополнительный к другому
            //    Vector3 circleMyOtherAdjacent = circleCenterMeOther - 2 * turnRadius * vecTangentDCMyOther.normalized;
            //    DrawCircle(circleMyOtherAdjacent, Color.yellow, showtime);
            //    //Handles.


            //    // Точка маршрута через доп. круг
            //    pointFirstAdjacent = circleCenterMeOther - vecTangentDCMyOther.normalized * turnRadius;
            //    //Vector3 pointSecondAdjacent = pointTangentDCToMyOther;

            //    Vector3 pointCircleMeToDest, pointCircleAdjacentToDest;
            //    float angleCMTDToDestination, angleCATDToDestination;

            //    if ((!rightTurn && leftHalf) || (rightTurn && !leftHalf))
            //    {
            //        // Построить к выбранному основному и доп. кругу касательную из точки назначения
            //        pointCircleMeToDest = FindTangentPoints(true, circleCenterMe, moveDestination, true, rightTurn)[0];
            //        pointCircleAdjacentToDest = FindTangentPoints(true, circleCenterMe, moveDestination, true, rightTurn)[0];

            //        // Они выпирает в противоположную четверть?
            //        angleCMTDToDestination = Vector3.SignedAngle(targetRotationVec, pointCircleMeToDest - moveDestination, Vector3.up);
            //        if ((!rightTurn && angleCMTDToDestination > 0) || (rightTurn && angleCMTDToDestination < 0))
            //            myMain_DCLeft = rightTurn; // Да

            //        angleCATDToDestination = Vector3.SignedAngle(targetRotationVec, pointCircleAdjacentToDest - moveDestination, Vector3.up);
            //        if ((!rightTurn && angleCATDToDestination > 0) || (rightTurn && angleCATDToDestination < 0))
            //            myAdjacent_DCLeft = rightTurn; // Да
            //    }
            //    print(logStrStart + "myMain_DCLeft = " + myMain_DCLeft);
            //    print(logStrStart + "myAdjacent_DCLeft = " + myAdjacent_DCLeft);

            //    Vector3 circleDestinationToMyMain = myMain_DCLeft ? circleCenterDestinationLeft : circleCenterDestinationRight;
            //    Vector3 circleDestinationToAdjacent = myAdjacent_DCLeft ? circleCenterDestinationLeft : circleCenterDestinationRight;

            //    Debug.DrawRay(circleDestinationToAdjacent, 30 * Vector3.up, Color.cyan, showtime);

            //    bool leftside, inner_my, inner_adjacent;
            //    leftside = rightTurn;
            //    inner_my = inner_adjacent = leftHalf == rightTurn;
            //    if (!leftHalf && rightTurn)
            //    {
            //        inner_my = myMain_DCLeft;
            //        inner_adjacent = myAdjacent_DCLeft;
            //    } 
            //    else if (leftHalf && !rightTurn)
            //    {
            //        inner_my = !myMain_DCLeft;
            //        inner_adjacent = !myAdjacent_DCLeft;
            //    }

            //    // Длина пути вперёд
            //    (pointTangentMyToDC, pointTangentDCToMy) = Deconstruct(FindTangentPoints(false, circleCenterMe, circleDestinationToMyMain, inner_my, leftside));

            //    float angleDCtoMain = Vector3.Angle(moveDestination - circleDestinationToMyMain, pointTangentDCToMy - circleDestinationToMyMain);
            //    float angleMainToDC = Vector3.Angle(currentPos - circleCenterMe, pointTangentMyToDC - circleCenterMe);

            //    // Невозможно
            //    //if (Vector3.Angle(targetRotationVec, pointTangentDCToMy - moveDestination) < 90)
            //    //    angleDCtoMain = 360 - angleDCtoMain;
            //    if (Vector3.Angle(moveDir, pointTangentMyToDC - currentPos) > 90)
            //        angleMainToDC = 360 - angleMainToDC;

            //    float arcDCToMain = angleDCtoMain * turnRadius * Mathf.PI / 180;
            //    float arcMainToDC = angleMainToDC * turnRadius * Mathf.PI / 180;
            //    float connectionToMain = (pointTangentMyToDC - pointTangentDCToMy).magnitude;

            //    DrawArc(circleCenterMe, currentPos, pointTangentMyToDC, Color.black, showtime);
            //    DrawArc(circleDestinationToMyMain, moveDestination, pointTangentDCToMy, Color.black, showtime);
            //    Debug.DrawLine(pointTangentMyToDC, pointTangentDCToMy, Color.grey, showtime);

            //    lengthMain = arcDCToMain + arcMainToDC + connectionToMain;


            //    // Длина пути назад
            //    (pointTangentAdjacentToDC, pointTangentDCToAdjacent) = Deconstruct(FindTangentPoints(false, circleMyOtherAdjacent, circleDestinationToAdjacent, inner_adjacent, leftside));

            //    float angleMyOtherToAdjacent = Vector3.Angle(currentPos - circleCenterMeOther, pointFirstAdjacent - circleCenterMeOther);
            //    float angleAdjacentToDC = 90;
            //    float angleDCToAdjacent = Vector3.Angle(moveDestination - circleDestinationToAdjacent, pointTangentDCToAdjacent - circleDestinationToAdjacent);

            //    if (Vector3.Angle(targetRotationVec, pointTangentDCToAdjacent - moveDestination) < 90)
            //        angleDCToAdjacent = 360 - angleDCToAdjacent;
            //    // Невозможно
            //    //if (Vector3.Angle(Quaternion.AngleAxis(90, Vector3.up) * (pointTangentAdjacentToDC - pointTangentDCToAdjacent), pointTangentAdjacentToDC - circleCenterMeAdjacent) > 90)
            //    //    angleAdjacentToDC = 360 - angleAdjacentToDC;

            //    float arcMyOtherToAdjacent = angleMyOtherToAdjacent * turnRadius * Mathf.PI / 180;
            //    float arcAdjacentToDC = angleAdjacentToDC * turnRadius * Mathf.PI / 180;
            //    float arcDCToAdjacent = angleDCToAdjacent * turnRadius * Mathf.PI / 180;
            //    float connectionToAdjacent = (pointTangentAdjacentToDC - pointTangentDCToAdjacent).magnitude;


            //    // Компенсация низкой скорости поворота вконце
            //    //pointFirstAdjacent += power / 2 * -interCircleConnectionRight.normalized;

            //    DrawArc(circleCenterMeOther, currentPos, pointFirstAdjacent, Color.white, showtime);
            //    print(logStrStart + "arcMyOtherToAdjacent = " + arcMyOtherToAdjacent);
            //    DrawArc(circleMyOtherAdjacent, pointFirstAdjacent, pointTangentAdjacentToDC, Color.white, showtime);
            //    print(logStrStart + "arcAdjacentToDC = " + arcAdjacentToDC);
            //    DrawArc(circleDestinationToAdjacent, moveDestination, pointTangentDCToAdjacent, Color.white, showtime);
            //    print(logStrStart + "arcDCToAdjacent = " + arcDCToAdjacent);
            //    Debug.DrawLine(pointTangentAdjacentToDC, pointTangentDCToAdjacent, Color.green, showtime);

            //    lengthAdjacent = arcMyOtherToAdjacent + arcAdjacentToDC + arcDCToAdjacent + connectionToAdjacent;
            //}
            #endregion old circles

            print(logStrStart + "left half = " + leftHalf + ", front = " + front);

            // Касательная ко мне, чтобы определить сторону взгляда относительно неё
            Vector3 pointTangentDCToMe = FindTangentPoints(true, circleCenterDestination, currentPos, true, leftHalf)[0];

            float angleMyDirectionToTangent = Vector3.SignedAngle(currentPos - pointTangentDCToMe, moveDir, Vector3.up);
            Debug.DrawLine(pointTangentDCToMe, currentPos, Color.white, showtime);

            bool rightTurn;
            if (angleMyDirectionToTangent > 0) // Повёрнут направо от касательной. Выбрать правый и левый задний круги. 
            {
                rightTurn = true;
                circleCenterMe = circleCenterMeRight;
                circleCenterMeOther = circleCenterMeLeft;
            }
            else
            {
                rightTurn = false;
                circleCenterMe = circleCenterMeLeft;
                circleCenterMeOther = circleCenterMeRight;
            }
            print(logStrStart + "rightTurn = " + rightTurn);
            Debug.DrawRay(circleCenterMe, 30 * Vector3.up, Color.cyan, showtime);

            // Определение к какой стороне моего дальнего круга строить касательную на которой будет лежать доп. круг
            bool toLeftSide = false;
            float angleCenterMyOtherToMyTangent = Vector3.SignedAngle(currentPos - pointTangentDCToMe, circleCenterMeOther - currentPos, Vector3.up);
            if (angleCenterMyOtherToMyTangent > 0)
                toLeftSide = true;

            // Касательная между circleCenterDestination и circleCenterMeOther для построения доп. круга
            (Vector3 pointTangentMyOtherToDC, Vector3 pointTangentDCToMyOther) = Deconstruct(FindTangentPoints(false, circleCenterMeOther, circleCenterDestination, leftHalf == toLeftSide, toLeftSide));
            Vector3 vecTangentDCMyOther = pointTangentMyOtherToDC - pointTangentDCToMyOther;
            Debug.DrawRay(pointTangentMyOtherToDC, 30 * Vector3.up, Color.green, showtime);
            Debug.DrawRay(pointTangentDCToMyOther, 30 * Vector3.up, Color.green, showtime);

            // Круг дополнительный к другому
            Vector3 circleMyOtherAdjacent = circleCenterMeOther - 2 * turnRadius * vecTangentDCMyOther.normalized;
            DrawCircle(circleMyOtherAdjacent, Color.yellow, showtime);

            // Точка маршрута через доп. круг
            pointFirstAdjacent = circleCenterMeOther - vecTangentDCMyOther.normalized * turnRadius;
            //Vector3 pointSecondAdjacent = pointTangentDCToMyOther;

            Vector3 pointCircleMeToDest, pointCircleAdjacentToDest;
            float angleCMTDToDestination, angleCATDToDestination;

            if (front && ((rightTurn && leftHalf) || (!rightTurn && !leftHalf)) || !front && ((!rightTurn && leftHalf) || (rightTurn && !leftHalf)))
            {
                // Построить к выбранному основному и доп. кругу касательную из точки назначения
                pointCircleMeToDest = FindTangentPoints(true, circleCenterMe, moveDestination, true, rightTurn)[0];
                pointCircleAdjacentToDest = FindTangentPoints(true, circleCenterMe, moveDestination, true, rightTurn)[0];

                // Они выпирают в противоположную четверть?
                angleCMTDToDestination = Vector3.SignedAngle(targetRotationVec, pointCircleMeToDest - moveDestination, Vector3.up);
                angleCATDToDestination = Vector3.SignedAngle(targetRotationVec, pointCircleAdjacentToDest - moveDestination, Vector3.up);

                if (front)
                {
                    if ((rightTurn && angleCMTDToDestination > 0) || (!rightTurn && angleCMTDToDestination < 0))
                        myMain_DCLeft = !rightTurn; // Да

                    if ((!rightTurn && angleCATDToDestination > 0) || (rightTurn && angleCATDToDestination < 0))
                        myAdjacent_DCLeft = !rightTurn; // Да
                }
                else
                {
                    if ((!rightTurn && angleCMTDToDestination > 0) || (rightTurn && angleCMTDToDestination < 0))
                        myMain_DCLeft = rightTurn; // Да

                    angleCATDToDestination = Vector3.SignedAngle(targetRotationVec, pointCircleAdjacentToDest - moveDestination, Vector3.up);
                    if ((!rightTurn && angleCATDToDestination > 0) || (rightTurn && angleCATDToDestination < 0))
                        myAdjacent_DCLeft = rightTurn; // Да
                }
            }
            print(logStrStart + "myMain_DCLeft = " + myMain_DCLeft);
            print(logStrStart + "myAdjacent_DCLeft = " + myAdjacent_DCLeft);

            Vector3 circleDestinationToMyMain = myMain_DCLeft ? circleCenterDestinationLeft : circleCenterDestinationRight;
            Vector3 circleDestinationToAdjacent = myAdjacent_DCLeft ? circleCenterDestinationLeft : circleCenterDestinationRight;

            Debug.DrawRay(circleDestinationToAdjacent, 30 * Vector3.up, Color.cyan, showtime);

            bool leftside, inner_my, inner_adjacent;
            leftside = rightTurn;
            inner_my = inner_adjacent = leftHalf == rightTurn;
            if (front)
            {

                if (!leftHalf && !rightTurn)
                {
                    inner_my = !myMain_DCLeft;
                    inner_adjacent = !myAdjacent_DCLeft;
                }
                else if (leftHalf && rightTurn)
                {
                    inner_my = myMain_DCLeft;
                    inner_adjacent = myAdjacent_DCLeft;
                }
            }
            else
            {
                if (!leftHalf && rightTurn)
                {
                    inner_my = myMain_DCLeft;
                    inner_adjacent = myAdjacent_DCLeft;
                }
                else if (leftHalf && !rightTurn)
                {
                    inner_my = !myMain_DCLeft;
                    inner_adjacent = !myAdjacent_DCLeft;
                }
            }


            #region readable
            //if (!leftHalf)
            //{
            //    if (rightTurn)
            //        inner_my = inner_adjacent = false;
            //    else
            //    {
            //        inner_my = !myMain_DCLeft;
            //        inner_adjacent = !myAdjacent_DCLeft;
            //    }
            //}
            //else
            //{
            //    if (rightTurn)
            //    {
            //        inner_my = myMain_DCLeft;
            //        inner_adjacent = myAdjacent_DCLeft;
            //    }
            //    else
            //        inner_my = inner_adjacent = false;
            //}
            #endregion readable

            // Длина пути вперёд
            (pointTangentMyToDC, pointTangentDCToMy) = Deconstruct(FindTangentPoints(false, circleCenterMe, circleDestinationToMyMain, inner_my, leftside));

            float angleDCtoMain = Vector3.Angle(moveDestination - circleDestinationToMyMain, pointTangentDCToMy - circleDestinationToMyMain);
            float angleMainToDC = Vector3.Angle(currentPos - circleCenterMe, pointTangentMyToDC - circleCenterMe);

            if (front)
            {
                if (Vector3.Angle(targetRotationVec, pointTangentDCToMy - moveDestination) < 90)
                    angleDCtoMain = 360 - angleDCtoMain;
                if (Vector3.Angle(moveDir, pointTangentMyToDC - currentPos) > 90)
                    angleMainToDC = 360 - angleMainToDC;
            }
            else
            {
                // Невозможно
                //if (Vector3.Angle(targetRotationVec, pointTangentDCToMy - moveDestination) < 90)
                //    angleDCtoMain = 360 - angleDCtoMain;
                if (Vector3.Angle(moveDir, pointTangentMyToDC - currentPos) > 90)
                    angleMainToDC = 360 - angleMainToDC;
            }

            float arcDCToMain = angleDCtoMain * turnRadius * Mathf.PI / 180;
            float arcMainToDC = angleMainToDC * turnRadius * Mathf.PI / 180;
            float connectionToMain = (pointTangentMyToDC - pointTangentDCToMy).magnitude;

            DrawArc(circleCenterMe, currentPos, pointTangentMyToDC, Color.black, showtime);
            DrawArc(circleDestinationToMyMain, moveDestination, pointTangentDCToMy, Color.black, showtime);
            Debug.DrawLine(pointTangentMyToDC, pointTangentDCToMy, Color.grey, showtime);

            lengthMain = arcDCToMain + arcMainToDC + connectionToMain;


            // Длина пути назад
            (pointTangentAdjacentToDC, pointTangentDCToAdjacent) = Deconstruct(FindTangentPoints(false, circleMyOtherAdjacent, circleDestinationToAdjacent, inner_adjacent, leftside));

            float angleMyOtherToAdjacent = Vector3.Angle(currentPos - circleCenterMeOther, pointFirstAdjacent - circleCenterMeOther);
            float angleAdjacentToDC = 90;
            float angleDCToAdjacent = Vector3.Angle(moveDestination - circleDestinationToAdjacent, pointTangentDCToAdjacent - circleDestinationToAdjacent);

            if (Vector3.Angle(targetRotationVec, pointTangentDCToAdjacent - moveDestination) < 90)
                angleDCToAdjacent = 360 - angleDCToAdjacent;
            // Невозможно
            //if (Vector3.Angle(Quaternion.AngleAxis(90, Vector3.up) * (pointTangentAdjacentToDC - pointTangentDCToAdjacent), pointTangentAdjacentToDC - circleCenterMeAdjacent) > 90)
            //    angleAdjacentToDC = 360 - angleAdjacentToDC;

            float arcMyOtherToAdjacent = angleMyOtherToAdjacent * turnRadius * Mathf.PI / 180;
            float arcAdjacentToDC = angleAdjacentToDC * turnRadius * Mathf.PI / 180;
            float arcDCToAdjacent = angleDCToAdjacent * turnRadius * Mathf.PI / 180;
            float connectionToAdjacent = (pointTangentAdjacentToDC - pointTangentDCToAdjacent).magnitude;

            DrawArc(circleCenterMeOther, currentPos, pointFirstAdjacent, Color.white, showtime);
            print(logStrStart + "arcMyOtherToAdjacent = " + arcMyOtherToAdjacent);
            DrawArc(circleMyOtherAdjacent, pointFirstAdjacent, pointTangentAdjacentToDC, Color.white, showtime);
            print(logStrStart + "arcAdjacentToDC = " + arcAdjacentToDC);
            DrawArc(circleDestinationToAdjacent, moveDestination, pointTangentDCToAdjacent, Color.white, showtime);
            print(logStrStart + "arcDCToAdjacent = " + arcDCToAdjacent);
            Debug.DrawLine(pointTangentAdjacentToDC, pointTangentDCToAdjacent, Color.green, showtime);

            lengthAdjacent = arcMyOtherToAdjacent + arcAdjacentToDC + arcDCToAdjacent + connectionToAdjacent;

            print(logStrStart + "lengthMain = " + lengthMain + ", lengthAdjacent = " + lengthAdjacent);
            if (lengthMain - lengthAdjacent < 2)
            {
                // Компенсация низкой скорости поворота вначале
                pointTangentDCToMy += power / 2 * moveDir;

                this.moveDestination = pointTangentDCToMy;
                intermediateDestination = Vector3.zero;

                print(logStrStart + "Main chosen");
                //behaviour = BehaviourState.TurnToDirection;
                behaviour = BehaviourState.FullThrottle;
                print("Set behaviour: " + behaviour);
            }
            else
            {
                bool forward = Vector3.Angle(moveDir, pointFirstAdjacent - currentPos) < 90;
                // Компенсация низкой скорости поворота вначале
                pointFirstAdjacent += power / 2 * moveDir * (forward ? 1 : -1);
                pointTangentDCToAdjacent += power / 2 * (pointTangentDCToAdjacent - pointTangentAdjacentToDC).normalized;


                this.moveDestination = pointFirstAdjacent;
                intermediateDestination = pointTangentDCToAdjacent;
                print(logStrStart + "Adjacent chosen");
                if (forward)
                    behaviour = BehaviourState.FullThrottle;
                else
                    behaviour = BehaviourState.Reverse;
                print("Set behaviour: " + behaviour);
            }
            finalDestination = moveDestination;
            canChangeState = false;
            targetRotationY = recievedTargetRotationY;

            if (!withTartget)
            {
                target = null;
                fixTarget = false;
            }
            aligned = false;
            stopped = false;
            return;
        }
        else if (recievedTargetRotationY != 0)
        {
            print("Recieved direction: " + predefinedState);
            canChangeState = false;
            behaviour = predefinedState;
            this.moveDestination = moveDestination;
            targetRotationY = recievedTargetRotationY;
            print("Set behaviour: " + behaviour);
            print("Set targetRotationY  = " + targetRotationY);
            return;
        }
        //else if (predefinedState == BehaviourState.Idle)
        else
        {
            this.moveDestination = moveDestination;
            intermediateDestination = finalDestination = Vector3.zero;
            print("No specified rotation move order");

            canChangeState = true;
            forwardDir = transform.forward;
            currentPos = transform.position;
            moveTargetDir = moveDestination - currentPos;
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
        }
        //Debug.Log(behaviour + ", moveDestination " + moveDestination + ", targetDistance " + targetDistance + ", forwardTargetAngleStart " + forwardTargetAngleStart);
        Debug.Log("targetDistance " + targetDistance + ", forwardTargetAngleStart " + forwardTargetAngleStart + ", targetRotationY = " + targetRotationY);

        //oldmoveDestination = moveDestination;
    }

    public void SetAttackTarget(GameObject target, bool fixTarget)
    {
        print("Entered SetAttackTarget, target = " + target.name);
        this.target = target;
        if (fixTarget == true)
            this.fixTarget = fixTarget;

        //behaviour = BehaviourState.Attacking;
        if (Vector3.Distance(transform.position, target.transform.position) > attackRange)
            SetMoveDestination(target.transform.position, true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        stopped = false;
    }

    private Vector3[] FindTangentPoints(bool toPoint, Vector3 circle_1, Vector3 circle_2_orPoint, bool inner, bool left)
    {
        Vector3[] result = new Vector3[2];
        int sideCoeff = left ? -1 : 1;
        int innerCoeff = inner ? 1 : -1;

        Vector3 diff = circle_1 - circle_2_orPoint;
        float distance = toPoint ? diff.magnitude : diff.magnitude / 2;

        float angleForTangent;
        if (!inner)
            angleForTangent = 90;
        else
            angleForTangent = Mathf.Acos(turnRadius / distance) * 180 / Mathf.PI;

        result[0] = circle_1 + Quaternion.AngleAxis(sideCoeff * angleForTangent, Vector3.up) * -diff.normalized * turnRadius;

        if (!toPoint)
            result[1] = circle_2_orPoint + Quaternion.AngleAxis(innerCoeff * sideCoeff * angleForTangent, Vector3.up) * diff.normalized * turnRadius;

        return result;
    }

    private void DrawCircle(Vector3 center, Color color, float showtime)
    {
        Vector3 radiusPoint, vecRad = Vector3.forward * turnRadius;
        for (int u = 0; u < 90; u++)
        {
            vecRad = Quaternion.AngleAxis(4, Vector3.up) * vecRad;
            radiusPoint = center + vecRad;
            Debug.DrawLine(center, radiusPoint, color, showtime);
        }
    }
    private void DrawArc(Vector3 center, Vector3 point1, Vector3 point2, Color color, float showtime)
    {
        Vector3 vecRad1 = (point1 - center) * turnRadius;
        Vector3 vecRad2 = (point2 - center) * turnRadius;

        for (float u = 0; u < 20; u++)
        {
            Debug.DrawRay(center, Vector3.Lerp(vecRad1, vecRad2, u / 20).normalized * turnRadius, color, showtime);
        }
    }

    private (Vector3, Vector3) Deconstruct(Vector3[] vectors)
    {
        return (vectors[0], vectors[1]);
    }
}

// draw a red circle around the scene cube
[CustomEditor(typeof(MoveSubStandart))]
public class CubeEditor : Editor
{
    void OnSceneGUI()
    {
        MoveSubStandart cubeExample = (MoveSubStandart)target;

        Handles.color = Color.red;
        Handles.DrawWireDisc(cubeExample.transform.position, new Vector3(0, 1, 0), cubeExample.attackRange);
    }
}
