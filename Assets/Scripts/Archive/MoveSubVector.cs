#region orignal

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Order
{
    Vector3 destination;
    GameObject targetUnit;
    Order nextOrder;
}

public class MoveSubVector : MonoBehaviour, ISelectable
{
    private Rigidbody subRb;
    private ParticleSystem trail;
    private GameObject selectionSprite;

    // Movement parameters
    private float power = 600f;
    private float speed;
    private float myDrag = 0.6f;
    private int steadyYawSpeed, steadyPitchSpeed = 10, borderDistance = 10, turnRadius = 10, maxSpeed = 10;
    private float rotationCoeff;
    private short thrust = -1;

    // Attack parameters
    private float reloadTime = 3f;
    private int maxAmmo = 20, ammo = 20, angleStepCount = 0;
    public int attackRange = 40;

    private Vector3 currentPos, forwardDir, attackTargetDir, startVelocity, slowedVelocity, chasedDirection, tangentVec, circleCenterDestination;
    private Vector3 oldPos = Vector3.zero, firstDir, secondDir, targetRotation;
    private Quaternion newRotation;
    private float forwardTargetAngle, forwardTargetAngleStart, velocityTargetAngle;
    private float stopTime, lastShotTime;
    private float dragDecel;
    private float targetRotationY;
    private bool aligned = true;
    private bool isMaxSpeed = false;
    private float timer;
    private AudioSource submarineSource;
    public ThrustDirection thrustDirection;
    public RotateDirection rotateDirection;
    private string unitName;

    public enum ThrustDirection { Idle, Forward, Backward = -1 };
    public enum RotateDirection { Straight, Right, Left };
    public bool moving;

    public GameObject torpPrefab, target = null;
    public Vector3 moveDestination, intermediateDestination, finalDestination;
    public bool stopped = true, searching = false, fixTarget = false, forward, turnRight;
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
        moveDestination = finalDestination = intermediateDestination = tangentVec = circleCenterDestination = Vector3.zero;
        selectionSprite = transform.Find("Selection Sprite").gameObject;
        submarineSource = GetComponent<AudioSource>();
        targetRotationY = transform.localEulerAngles.y;
        rotationCoeff = 1.5f * turnRadius;
        stopped = false;
        unitName = gameObject.name;
        //if (unitName.IndexOf(' ') != -1) unitName = unitName.Substring(0, unitName.IndexOf(' '));
        steadyYawSpeed = unitName == "ST_Terminator 0" ? 0 : 10;
        //print(unitName + ", steadyYawSpeed = " + steadyYawSpeed);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        startVelocity = subRb.velocity;
        speed = startVelocity.magnitude;

        if (chasedDirection != Vector3.zero)
        {
            // Движение
            WaterResistance(); // Замедление
            if (startVelocity.magnitude < maxSpeed)
            {
                Accelerate();
                //print("Accelerating");
            }
            else
            {
                if (!isMaxSpeed)
                {
                    subRb.velocity = startVelocity.normalized * maxSpeed;
                    isMaxSpeed = true;
                    print("Max speed reached = " + speed);
                }

                Move();
                //print("Moving");
            }
            //print("Speed = " + speed);

            // Поворот
            if (targetRotation != Vector3.zero && Vector3.Angle(transform.forward, targetRotation) > 0.01f)
                Rotate();
        }
        else if (speed > 0.1f)
        {
            WaterResistance(); // Замедление
            //print("Slowing down");
        }
        else if (moving)
        {
            Stop(); // Остановка
        }


        forwardDir = transform.forward;
        currentPos = transform.position;

        ChangeBehaviour();
        //Move();

        //if (chasedDirection == Vector3.zero && !aligned) // Выравнивание без цели
        //{
        //    //print(transform.name + " Aligning, angles = " + transform.localEulerAngles + ", targetRotationY = " + targetRotationY);
        //    TurnToAnglesXY(0, targetRotationY, 1, true);
        //}
        //else if (target != null && team == 1 && ammo > 0) // Выравнивание на цель
        //{
        //    currentPos = transform.position;
        //    attackTargetDir = target.transform.position - transform.position;
        //    float angleTarget = Vector3.Angle(transform.forward, attackTargetDir);

        //    if (angleTarget > 5) { aligned = false; }  // Не выровнен

        //    if (!aligned) // Поворот на цель
        //    {
        //        print(transform.name + " Turning to target, angle = " + angleTarget + ", attackTargetDir = " + attackTargetDir + ", target = " + target.name);
        //        //newRotation = Quaternion.LookRotation(moveTargetDir - slowedVelocity, Vector3.up); // Компенсация боковой скорости

        //        newRotation = Quaternion.LookRotation(attackTargetDir, Vector3.up);
        //        float targetAngleX = newRotation.eulerAngles.x;
        //        float targetAngleY = newRotation.eulerAngles.y;
        //        //print(transform.name + " newRotation.eulerAngles.y = " + newRotation.eulerAngles.y);

        //        aligned = TurnToAnglesXY(targetAngleX, targetAngleY, 1, false);
        //    }
        //    else if (Vector3.Distance(currentPos, target.transform.position) < attackRange) // На дистанции выстрела
        //    {
        //        moveDestination = Vector3.zero;
        //        if (timer - lastShotTime > reloadTime - 0.1f || lastShotTime == 0) // Не идёт перезарядка
        //        {
        //            //stopped = false;
        //            lastShotTime = timer;
        //            print("SHOOT! Time = " + timer);
        //            //Invoke("Shoot", 0.1f);
        //            if (steadyYawSpeed == 0)
        //            {
        //                submarineSource.PlayOneShot(launchTorpedoSound, 0.1f);
        //                StartCoroutine(Shoot(target, 0.1f));
        //            }
        //            else
        //            {
        //                submarineSource.PlayOneShot(launchFastTorpedoSound, 0.1f);
        //                StartCoroutine(Shoot(target, 0.25f));
        //            }
        //        }
        //        //else if(team==1) print("timer - lastShotTime = " + (timer - lastShotTime));
        //    }
        //    else if (/*thrustDirection == BehaviourState.StillApproach*/ true) // Цель уплыла - подплыть на расстояние выстрела
        //    {
        //        moveDestination = target.transform.position - (attackRange - 2) * attackTargetDir.normalized;
        //        chasedDirection = target.transform.position - currentPos;
        //        SetMoveParams(ThrustDirection.Forward);
        //        print("Setting move destination to approach target for " + transform.name + " target = " + target);
        //    }
        //}
        //else if (transform.eulerAngles.x != 0 || transform.eulerAngles.z != 0)
        //{
        //    aligned = false;
        //}
    }
    private void ChangeBehaviour()
    {
        if (chasedDirection == Vector3.zero) return;
        Debug.DrawRay(currentPos, chasedDirection * 100, Color.red);

        if (firstDir == Vector3.zero)
        {
            if (secondDir == Vector3.zero)
            {
                //print("distance = " + (transform.position - moveDestination).magnitude);
                if ((transform.position - moveDestination).magnitude < 2)
                {
                    SetMoveParams(ThrustDirection.Idle);
                    chasedDirection = moveDestination = Vector3.zero;
                    print("Close to target, stop");
                }
            }
            else
            {
                //print("distance to radius = " + (transform.position - circleCenterDestination).magnitude);
                if ((transform.position - circleCenterDestination).magnitude < turnRadius + 1)
                {
                    print("On circle edge");
                    //Vector3 targetRotationVec = Quaternion.Euler(new Vector3(0, targetRotationY, 0)) * Vector3.forward;
                    chasedDirection = targetRotation;
                    secondDir = Vector3.zero;
                }
            }
        }
        else if (secondDir != Vector3.zero)
        {
            Debug.DrawRay(currentPos, firstDir, Color.black);
            float angle = Vector3.SignedAngle(transform.forward, firstDir, Vector3.up);
            //print("angle to firstDir = " + angle);
            if ((angle > -80 && angle < 0) || (angle < 80 && angle > 0))
            {
                chasedDirection = secondDir;
                SetMoveParams(ThrustDirection.Forward);
                firstDir = Vector3.zero;
                print("Turned to secondDir");
            }
        }
    }

    private void WaterResistance()
    {
        // Симуляция сопротивления среды
        float alignSin = Mathf.Sin(Vector3.Angle(transform.forward, startVelocity) * Mathf.PI / 180);
        //print("alignSin = " + alignSin + ", angle = " + Vector3.Angle(forwardDir, startVelocity));

        slowedVelocity = startVelocity * (1f - myDrag * (1 + alignSin) * Time.fixedDeltaTime);
        //slowedVelocity = Vector3.RotateTowards(slowedVelocity, transform.forward * slowedVelocity.magnitude, alignSin * Time.fixedDeltaTime, 0f);
        slowedVelocity = Vector3.RotateTowards(slowedVelocity, transform.forward, alignSin * Time.fixedDeltaTime, 0f);
        subRb.velocity = slowedVelocity;

        Debug.DrawRay(transform.position, slowedVelocity * 10, Color.yellow);
        //print("slow = " + slowedVelocity.magnitude/startVelocity.magnitude);
        //print("slowed = " + slowedVelocity.magnitude + ", start = " + startVelocity.magnitude);
        //print("slowedVelocity after turn = " + slowedVelocity);// + ", rotation = " + Vector3.Angle(slowedVelocity, forwardDir));
        //print("rotation = " + Vector3.Angle(slowedVelocity, forwardDir) + ", turn step = " + alignSin * Time.fixedDeltaTime);
        //print(transform.name + " speed = " + speed);
    }

    private void Accelerate()
    {
        subRb.AddForce((int)thrustDirection * power * Time.fixedDeltaTime * transform.forward);
        //print("Applying force: " + (int)thrustDirection * power * Time.fixedDeltaTime);
    }

    private void Move()
    {
        //transform.position += maxSpeed * Time.fixedDeltaTime * transform.forward;
        //print("Moving: " + maxSpeed * Time.fixedDeltaTime);
    }

    private void Rotate()
    {
        //newRotation = Quaternion.LookRotation(chasedDirection - slowedVelocity);
        newRotation = Quaternion.LookRotation(chasedDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation, rotationCoeff * Mathf.Sqrt(speed) * Time.fixedDeltaTime);
        //subRb.velocity = Vector3.RotateTowards(subRb.velocity, chasedDirection, rotationCoeff * 0.02f * Mathf.Sqrt(speed) * Time.fixedDeltaTime, 0f);
        //print("Rotating: " + maxSpeed * Time.fixedDeltaTime);
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
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotationY, turnSpeedYaw * Time.fixedDeltaTime);
            //print(transform.name + " turnSpeedYaw = " + turnSpeedYaw);


            Quaternion targetRotationX = Quaternion.Euler(targetAngleX, transform.localEulerAngles.y, 0);

            float turnSpeedPitch = steadyPitchSpeed;
            if (speed > 1)
            {
                turnSpeedPitch += Mathf.Sqrt(speed); // Компонента скорости поворота, зависящая от скорости
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotationX, turnSpeedPitch * Time.fixedDeltaTime);
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
        print("Kill velocity");
        moving = false;
        moveDestination = firstDir = secondDir = subRb.velocity = slowedVelocity = targetRotation = chasedDirection = Vector3.zero;
        if (!fixTarget)
            searching = false;

        SetMoveParams(ThrustDirection.Idle);
    }

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
        SetMoveDestination(moveDestination, withTartget, recievedTargetRotationY, moveInGroup);
        //print("Setting next point = " + moveDestination);
    }

    public void SetMoveDestination(Vector3 moveDestination, bool withTartget, float recievedTargetRotationY = 0, bool moveInGroup = true)
    {
        print("Entered SetMoveDestination, moveDestination = " + moveDestination);
        recievedTargetRotationY = recievedTargetRotationY < 90 ? recievedTargetRotationY + 270 : recievedTargetRotationY - 90;
        print("recievedTargetRotationY = " + recievedTargetRotationY);

        if (!withTartget)
        {
            target = null;
            fixTarget = false;
        }
        aligned = false;
        moving = true;
        this.moveDestination = moveDestination;
        Vector3 currentPos = transform.position;
        Vector3 targetRotationVec = Quaternion.Euler(0, recievedTargetRotationY, 0) * Vector3.right;

        print(targetRotationVec);
        print(moveDestination - currentPos);
        if (targetRotationVec == moveDestination - currentPos)
        {
            return;
        }

        if (recievedTargetRotationY != 0)
        {
            float showtime = 15;
            //recievedTargetRotationY = recievedTargetRotationY < 180 ? recievedTargetRotationY : recievedTargetRotationY - 360;
            targetRotation = targetRotationVec;
            Debug.DrawRay(moveDestination, targetRotationVec * 100, Color.yellow, showtime);
            string logStrStart = "BUILD TRAJECTORY: ";


            // Определение отностительного расположения
            float angleRelativeToDestination = Vector3.SignedAngle(targetRotationVec, currentPos - moveDestination, Vector3.up);

            bool leftHalf = angleRelativeToDestination < 0;
            bool front = angleRelativeToDestination > -90 && angleRelativeToDestination < 90;
            bool swapDestCircle = false;
            print(logStrStart + "left half = " + leftHalf + ", front = " + front);

            // Круг у точки назначения
            float angleForDestinationCircle = angleRelativeToDestination < 0 ? -90 : 90;
            Vector3 centerDCDir = Quaternion.AngleAxis(angleForDestinationCircle, Vector3.up) * targetRotationVec * turnRadius;
            Vector3 circleCenterDestinationMe = moveDestination + centerDCDir, circleCenterDestinationMeOther = moveDestination - centerDCDir;

            // Касательная ко мне, чтобы определить сторону взгляда относительно неё
            Vector3 pointTangentDCToMe = FindTangentPoints(true, circleCenterDestinationMe, currentPos, true, leftHalf)[0];
            float angleMyDirectionToTangent = Vector3.SignedAngle(currentPos - pointTangentDCToMe, forwardDir, Vector3.up);
            Debug.DrawLine(pointTangentDCToMe, currentPos, Color.white, showtime);

            bool rightTurn = angleMyDirectionToTangent > 0;
            print(logStrStart + "rightTurn = " + rightTurn);



            float angleForMyCircle = rightTurn ? 90 : -90;
            Vector3 centerMyDir = Quaternion.AngleAxis(angleForMyCircle, Vector3.up) * forwardDir * turnRadius;
            Vector3 circleCenterMeMain = currentPos + centerMyDir, circleCenterMeOther = currentPos - centerMyDir;
            Vector3 point_1, point_2;
            float a = Mathf.Abs(angleMyDirectionToTangent);

            #region fullcheck
            //if ((front && rightTurn != leftHalf) || (!front && rightTurn == leftHalf)) // Не надо проверять на пересечение границы
            //{
            //    if (a > 60) // Вперёд
            //    {
            //        SetMoveParams(ThrustDirection.Forward);
            //        firstDir = Vector3.zero;
            //        (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMe, circleCenterDestinationMe, !front, rightTurn != leftHalf));
            //    }
            //    else // Назад
            //    {
            //        SetMoveParams(ThrustDirection.Backward);
            //        firstDir = Quaternion.AngleAxis(front == leftHalf ? -90 : 90, Vector3.up) * (currentPos - pointTangentDCToMe);
            //        (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMeOther, circleCenterDestinationMe, !front, rightTurn != leftHalf));
            //    }
            //}
            //else // Проверка на пересечение границы сторон
            //{
            //    float angleForward_TargetRotation = Vector3.Angle(targetRotationVec, forwardDir), x;
            //    if (a > 60) // Вперёд
            //    {
            //        SetMoveParams(ThrustDirection.Forward);
            //        firstDir = Vector3.zero;
            //        if (front)
            //        {
            //            x = angleForward_TargetRotation > 90 ? turnRadius * Mathf.Sin(angleForward_TargetRotation) : turnRadius * (1 + Mathf.Cos(angleForward_TargetRotation));
            //            print(logStrStart + "front" + ", x = " + x);
            //            if (Mathf.Abs(angleRelativeToDestination) < Mathf.Asin(x / (currentPos - moveDestination).magnitude))
            //            {
            //                swapDestCircle = !leftHalf;
            //                (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMe, circleCenterDestinationMeOther, false, leftHalf));
            //            }
            //            else
            //            {
            //                (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMe, circleCenterDestinationMe, true, leftHalf));
            //            }
            //        }
            //        else
            //        {
            //            x = angleForward_TargetRotation < 90 ? turnRadius * Mathf.Sin(angleForward_TargetRotation) : turnRadius * (1 + Mathf.Cos(angleForward_TargetRotation));
            //            print(logStrStart + "back" + ", x = " + x);
            //            if (Mathf.Abs(180 - angleRelativeToDestination) < Mathf.Asin(x / (currentPos - moveDestination).magnitude))
            //            {
            //                swapDestCircle = !leftHalf;
            //                (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMe, circleCenterDestinationMeOther, true, !leftHalf));
            //            }
            //            else
            //            {
            //                (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMe, circleCenterDestinationMe, false, !leftHalf));
            //            }
            //        }
            //    }
            //    else // Назад
            //    {
            //        SetMoveParams(ThrustDirection.Backward);
            //        if (front)
            //        {
            //            firstDir = Quaternion.AngleAxis(front == !leftHalf ? -90 : 90, Vector3.up) * (currentPos - pointTangentDCToMe);
            //            (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMeOther, circleCenterDestinationMe, true, leftHalf));
            //        }
            //        else
            //        {
            //            x = turnRadius * Mathf.Sin(angleForward_TargetRotation);
            //            print(logStrStart + "back" + ", x = " + x);
            //            if (Mathf.Abs(180 - angleRelativeToDestination) < Mathf.Asin(x / (currentPos - moveDestination).magnitude))
            //            {
            //                swapDestCircle = !leftHalf;
            //                firstDir = Quaternion.AngleAxis(front == !leftHalf ? -90 : 90, Vector3.up) * (currentPos - pointTangentDCToMe);
            //                (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMeOther, circleCenterDestinationMeOther, true, !leftHalf));
            //            }
            //            else
            //            {
            //                firstDir = Quaternion.AngleAxis(front == !leftHalf ? -90 : 90, Vector3.up) * (currentPos - pointTangentDCToMe);
            //                (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMeOther, circleCenterDestinationMe, false, !leftHalf));
            //            }
            //        }
            //    }
            //}
            #endregion fullcheck

            bool forward = a > 60;
            print("Anglestart = " + a);
            SetMoveParams(forward ? ThrustDirection.Forward : ThrustDirection.Backward);
            firstDir = Vector3.zero;
            if ((front && rightTurn != leftHalf) || (!front && rightTurn == leftHalf)) // Не надо проверять на пересечение границы
            {
                if (!forward)
                    firstDir = Quaternion.AngleAxis(front == leftHalf ? -180 : 180, Vector3.up) * (currentPos - pointTangentDCToMe);
                (point_1, point_2) = Deconstruct(FindTangentPoints(false, forward ? circleCenterMeMain : circleCenterMeOther, circleCenterDestinationMe, !front, rightTurn != leftHalf));
            }
            else // Проверка на пересечение границы сторон
            {
                float angleForward_TargetRotation = Vector3.Angle(front ? targetRotationVec : -targetRotationVec, forwardDir), x;
                if (forward) // Вперёд
                {
                    x = angleForward_TargetRotation > 90 ? turnRadius * Mathf.Sin(angleForward_TargetRotation) : turnRadius * (1 + Mathf.Cos(angleForward_TargetRotation));
                    print(logStrStart + "front" + ", x = " + x);
                    if (Mathf.Abs(angleRelativeToDestination) < Mathf.Asin(x / (currentPos - moveDestination).magnitude))
                    {
                        swapDestCircle = true;
                        (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMeMain, circleCenterDestinationMeOther, !front, front == leftHalf));
                    }
                    else
                    {
                        (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMeMain, circleCenterDestinationMe, front, front == leftHalf));
                    }
                }
                else // Назад
                {
                    if (front)
                    {
                        firstDir = Quaternion.AngleAxis(front == !leftHalf ? -180 : 180, Vector3.up) * (currentPos - pointTangentDCToMe);
                        (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMeOther, circleCenterDestinationMe, true, leftHalf));
                    }
                    else
                    {
                        x = turnRadius * Mathf.Sin(angleForward_TargetRotation);
                        print(logStrStart + "back" + ", x = " + x);
                        if (Mathf.Abs(180 - angleRelativeToDestination) < Mathf.Asin(x / (currentPos - moveDestination).magnitude))
                        {
                            swapDestCircle = true;
                            firstDir = Quaternion.AngleAxis(front == !leftHalf ? -180 : 180, Vector3.up) * (currentPos - pointTangentDCToMe);
                            (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMeOther, circleCenterDestinationMeOther, true, !leftHalf));
                        }
                        else
                        {
                            firstDir = Quaternion.AngleAxis(front == !leftHalf ? -180 : 180, Vector3.up) * (currentPos - pointTangentDCToMe);
                            (point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMeOther, circleCenterDestinationMe, false, !leftHalf));
                        }
                    }
                }
            }

            //Vector3 circleCenterMe = forward ? circleCenterMeMain : circleCenterMeOther;
            circleCenterDestination = swapDestCircle ? circleCenterDestinationMeOther : circleCenterDestinationMe;
            Debug.DrawRay(circleCenterDestination, 10 * Vector3.up, Color.cyan, showtime);
            DrawCircle(circleCenterDestination, Color.blue, showtime);


            //(point_1, point_2) = Deconstruct(FindTangentPoints(false, circleCenterMeMain, circleCenterMe, !front, rightTurn != leftHalf));
            secondDir = point_2 - point_1;
            Debug.DrawRay(currentPos, firstDir, Color.cyan, showtime);
            Debug.DrawLine(point_1, point_2, Color.green, showtime);
            if (firstDir == Vector3.zero)
            {
                chasedDirection = secondDir;
                //thrustDirection = ThrustDirection.Forward;
                print("chasedDirection = secondDir = " + secondDir);
            }
            else
            {
                chasedDirection = firstDir;
                //thrustDirection = ThrustDirection.Backward;
                print("chasedDirection = firstDir = " + firstDir);
            }
            targetRotationY = recievedTargetRotationY;

            //circleCenterDestination = swapDestCircle ? 
            return;
        }
        else
        {
            print("No specified rotation move order");

            targetRotationY = recievedTargetRotationY;

            Vector3 moveTargetDir = moveDestination - currentPos;
            float targetDistance = moveTargetDir.magnitude;
            forwardTargetAngleStart = Vector3.Angle(forwardDir, moveTargetDir);

            if (forwardTargetAngleStart < 90)
            {
                print("angle limit = " + 90 / turnRadius * targetDistance + ", angle = " + forwardTargetAngleStart);
                if (forwardTargetAngleStart < 90 / turnRadius * targetDistance) // Вперёд
                {
                    thrustDirection = ThrustDirection.Forward;
                    targetRotationY = recievedTargetRotationY == 0 ? Quaternion.LookRotation(moveTargetDir, Vector3.up).eulerAngles.y : recievedTargetRotationY;
                }
                else  // Назад, разворачиваясь к точке передом
                {
                    thrustDirection = ThrustDirection.Backward;
                    targetRotationY = recievedTargetRotationY == 0 ? Quaternion.LookRotation(moveTargetDir, Vector3.up).eulerAngles.y : recievedTargetRotationY;
                }
            }
            else if ((targetDistance < turnRadius && forwardTargetAngleStart < 150) || targetDistance > turnRadius)
            {
                thrustDirection = ThrustDirection.Backward; // Назад, разворачиваясь к точке передом
                targetRotationY = recievedTargetRotationY == 0 ? Quaternion.LookRotation(moveTargetDir, Vector3.up).eulerAngles.y : recievedTargetRotationY;
            }
            else
            {
                thrustDirection = ThrustDirection.Backward; // Задом на точку
                if (recievedTargetRotationY != 0)
                    targetRotationY = recievedTargetRotationY;
                else if (moveInGroup)
                    targetRotationY = Quaternion.LookRotation(moveTargetDir, Vector3.up).eulerAngles.y;
                else
                    targetRotationY = transform.eulerAngles.y;
            }
        }
        //Debug.Log(behaviour + ", moveDestination " + moveDestination + ", targetDistance " + targetDistance + ", forwardTargetAngleStart " + forwardTargetAngleStart);
        //Debug.Log("targetDistance " + targetDistance + ", forwardTargetAngleStart " + forwardTargetAngleStart + ", targetRotationY = " + targetRotationY);

        //oldmoveDestination = moveDestination;
    }

    private void SetMoveParams(ThrustDirection direction)
    {
        print("SetMoveParams " + direction);
        switch (direction)
        {
            case ThrustDirection.Forward:
                switch (thrustDirection)
                {
                    case ThrustDirection.Backward:
                        trail.transform.rotation = Quaternion.Inverse(transform.rotation);
                        break;
                    case ThrustDirection.Idle:
                        trail.Play();
                        break;
                }
                break;
            case ThrustDirection.Backward:
                switch (thrustDirection)
                {
                    case ThrustDirection.Forward:
                        trail.transform.rotation = transform.rotation;
                        break;
                    case ThrustDirection.Idle:
                        trail.transform.rotation = Quaternion.Inverse(transform.rotation);
                        trail.Play();
                        break;
                }
                break;
            case ThrustDirection.Idle:
                switch (thrustDirection)
                {
                    case ThrustDirection.Forward:
                        trail.Stop();
                        break;
                    case ThrustDirection.Backward:
                        trail.Stop();
                        trail.transform.rotation = transform.rotation;
                        break;
                }
                chasedDirection = firstDir = secondDir = Vector3.zero;
                break;
        }
        thrustDirection = direction;
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
        moving = true;
    }

    private Vector3[] FindTangentPoints(bool toPoint, Vector3 circle_1, Vector3 circle_2_orPoint, bool inner, bool left)
    {
        Vector3[] result = new Vector3[2];
        int sideCoeff = left ? -1 : 1;
        int innerCoeff = inner ? 1 : -1;

        Vector3 diff = circle_1 - circle_2_orPoint;
        float distance = toPoint ? diff.magnitude : diff.magnitude / 2;
        float angleForTangent = !inner ? 90 : Mathf.Acos(turnRadius / distance) * 180 / Mathf.PI;

        result[0] = circle_1 + Quaternion.AngleAxis(sideCoeff * angleForTangent, Vector3.up) * -diff.normalized * turnRadius;
        if (!toPoint) result[1] = circle_2_orPoint + Quaternion.AngleAxis(innerCoeff * sideCoeff * angleForTangent, Vector3.up) * diff.normalized * turnRadius;
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
#endregion orignal

#region gpt
//using UnityEngine;

//public class MoveSubStandart : MonoBehaviour, ISelectable
//{
//    public Rigidbody submarineRigidbody;
//    private ParticleSystem trail;
//    private GameObject selectionSprite;
//    public float moveForce = 10f;
//    public float maxSpeed = 5f;
//    public float rotationSpeedMultiplier = 2f;
//    public float staticRotationSpeed = 90f;
//    public int attackRange = 40;
//    public bool stopped = true, searching = false, fixTarget = false;
//    public GameObject torpPrefab, target = null;
//    private bool hasDestination = false;
//    public float stopThreshold = 0.1f;

//    private Vector3 destination;
//    private Quaternion targetRotation;
//    public int team = 1;
//    public int Team
//    {
//        get { return team; }
//    }

//    public bool Alive
//    {
//        get { return this != null; }
//    }
//    public void Select()
//    {
//        selectionSprite.SetActive(true);
//    }

//    public void Deselect()
//    {
//        selectionSprite.SetActive(false);
//    }

//    private void Start()
//    {
//        trail = transform.Find("Trail").GetComponent<ParticleSystem>();
//        submarineRigidbody = GetComponent<Rigidbody>();
//        selectionSprite = transform.Find("Selection Sprite").gameObject;
//    }
//    public void MoveTo(Vector3 destinationPoint, float targetAngleY)
//    {
//        destination = destinationPoint;

//        targetAngleY = targetAngleY < 180 ? targetAngleY : targetAngleY - 360;
//        targetRotation = Quaternion.Euler(new Vector3(0, targetAngleY, 0));
//        hasDestination = true;
//    }

//    private void FixedUpdate()
//    {
//        if (hasDestination)
//        {
//            Move();
//            Rotate();
//        }
//    }

//    private void Move()
//    {
//        float distance = Vector3.Distance(transform.position, destination);

//        if (distance > stopThreshold)
//        {
//            // Only move forward along the submarine's local z-axis
//            Vector3 forwardDirection = transform.forward;
//            submarineRigidbody.AddForce(forwardDirection * moveForce, ForceMode.Force);

//            // Apply water resistance based on the current speed
//            float resistanceForce = -submarineRigidbody.velocity.magnitude * submarineRigidbody.velocity.magnitude;
//            submarineRigidbody.AddForce(resistanceForce * submarineRigidbody.velocity.normalized);
//        }
//        else
//        {
//            // Stop the submarine when it reaches the destination
//            submarineRigidbody.velocity = Vector3.zero;
//            hasDestination = false;
//        }

//        // Limit the maximum speed
//        if (submarineRigidbody.velocity.magnitude > maxSpeed)
//        {
//            submarineRigidbody.velocity = submarineRigidbody.velocity.normalized * maxSpeed;
//        }
//    }

//    private void Rotate()
//    {
//        float rotationSpeed = Mathf.Abs(submarineRigidbody.velocity.magnitude) * rotationSpeedMultiplier + staticRotationSpeed;

//        Quaternion deltaRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
//        submarineRigidbody.MoveRotation(deltaRotation);
//    }

//    public void SetAttackTarget(GameObject target, bool fixTarget)
//    {
//        print("Entered SetAttackTarget, target = " + target.name);
//        this.target = target;
//        if (fixTarget == true)
//            this.fixTarget = fixTarget;

//        //behaviour = BehaviourState.Attacking;
//        if (Vector3.Distance(transform.position, target.transform.position) > attackRange)
//            MoveTo(target.transform.position, Quaternion.LookRotation(target.transform.position - transform.position).eulerAngles.y);
//    }

//    public void Stop(bool input = false)
//    {

//    }
//}
#endregion gpt