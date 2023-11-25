using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSubV12 : MonoBehaviour, ISelectable
{
    private Rigidbody subRb;
    private ParticleSystem bubblesLeft, bubblesRight;
    private GameObject selectionSprite;
    public GameObject torpPrefab, target = null;

    // Movement parameters
    private float force = 4f;
    private float speed;
    private float myDrag = 0.8f;
    private float ThrustDistCoeff = 0.1f, ThrustDirectionCoeff;
    private int rotationSpeedCoeff = 1500, steadyRotationCoeff = 2;

    private float angleUp;

    // Attack parameters
    private float reloadTime = 3f;
    private int ammo = 20;

    private Vector3 currentPos, forwardDir, targetDir, startVelocity, slowedVelocity;
    private Vector3 oldTargetPosition;
    private Quaternion new_rotation;
    private float forwardTargetAngle, velocityTargetAngle;
    private float targetDistance;
    private float stopTime, lastShotTime;
    private float dragDeceleration;
    private bool aligned = true;
    public bool searching = false;
    private float timer;
    //private bool atacking = false;   

    public Vector3 targetPosition;
    public int moveMode;
    

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
        targetPosition = Vector3.zero;
        forwardDir = transform.forward;
        currentPos = transform.position;
        targetDir = targetPosition - currentPos;
        forwardTargetAngle = Vector3.Angle(forwardDir, targetDir);
        targetDistance = targetDir.magnitude;
        selectionSprite = transform.Find("Selection Sprite").gameObject;
        //mainCamera = Camera.main;
        moveMode = 0;
        //searching ;
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
        targetDir = targetPosition - currentPos;
        targetDistance = targetDir.magnitude;
        //print("Timer = " + timer);
        //print("counter = " + counter);
        //print("Time.frameCount = " + Time.frameCount);
        timer += Time.deltaTime;
        //counter++;

        if (moveMode != 0)
        {
            new_rotation = Quaternion.LookRotation(targetDir - slowedVelocity);
            forwardDir = transform.forward;
            forwardTargetAngle = Vector3.Angle(forwardDir, targetDir);

            Debug.DrawRay(currentPos, forwardDir * 100, Color.green);

            target = null;
            print(gameObject.name + " target = null");
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
            dragDeceleration = (startVelocity.magnitude - slowedVelocity.magnitude) / Time.deltaTime;
            velocityTargetAngle = Vector3.Angle(slowedVelocity, targetDir);

            //print("startVelocity.magnitude = " + startVelocity.magnitude + ", slowedVelocity.magnitude = " + slowedVelocity.magnitude);
            //print("stopTime = " + stopTime + ", targetDistance = " + targetDistance  + ", speed = " + speed );
            //print("real stop time = " + speed / dragDeceleration * myDrag);
            //print("dragDeceleration = " + dragDeceleration);

            if (targetDistance < 2 || // погрешность достижения точки
                    ((stopTime < speed / dragDeceleration * myDrag)
                    && (velocityTargetAngle < 5 || velocityTargetAngle > 175)) // направление примерно на точку, чтобы не промахиваться
                    )
            {
                moveMode = 0;
                print("ENGINES STOP");
                targetPosition = Vector3.zero;
                bubblesLeft.Stop();
                bubblesRight.Stop();
            }
        }
        else if (target == null && angleUp > 1) // Плавное выравнивание
        {
            float xDegreeDelta = (transform.localEulerAngles.x - 180) * 0.2f;
            float zDegreeDelta = (transform.localEulerAngles.z - 180) * 0.2f;

            print(gameObject.name + " Aligning... Angle up = " + angleUp + "," );
            //print("Target = " + target.name);
            print("x, z = " + transform.localEulerAngles.x + " " + transform.localEulerAngles.z + ", correction Vec = " + new Vector3(xDegreeDelta, 0, zDegreeDelta) * Time.deltaTime);
            transform.localEulerAngles += new Vector3(xDegreeDelta, 0, zDegreeDelta) * Time.deltaTime;
            subRb.angularVelocity = Vector3.zero;
        }
        else if (!aligned && angleUp != 0) // Жёсткое выравнивание и остановка вращения
        {
            transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
            subRb.angularVelocity = Vector3.zero;
            aligned = true;
            print(gameObject.name + " Hard alignment");
            searching = false;
        }
        else if (target != null )//&& Vector3.Distance(currentPos, target.transform.position) < 40) //angleUp == 0, Поиск целей
        {
            currentPos = transform.position;
            targetDir = target.transform.position - currentPos;
            if (Vector3.Angle(transform.forward, targetDir) > 0.5) // Поворот на цель
            {
                //print(transform.name + " Turning to target " + moveMode);
                //print("Vector3.Angle(transform.forward, targetDir) = " + Vector3.Angle(transform.forward, targetDir));
                new_rotation = Quaternion.LookRotation(targetDir - slowedVelocity);
                forwardDir = transform.forward;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, steadyRotationCoeff * Mathf.Sqrt(rotationSpeedCoeff) * Time.deltaTime);
                Debug.DrawRay(currentPos, (targetDir - slowedVelocity) * 100, Color.grey);
            }
            else if (team == 1 && ammo > 0 && (lastShotTime == 0 || timer - lastShotTime > reloadTime))
            {
                ammo--;
                //atacking = true;
                searching = false;
                //lastTimerRemainder = timer % 3;
                //transform.rotation = Quaternion.RotateTowards(transform.rotation, new_rotation, rotationSpeedCoeff);
                //timer = 0;
                lastShotTime = timer;
                print("SHOOT!");
                Shoot();

                //StartCoroutine(Shoot_C());
            }
            //else if(team==1) print("timer - lastShotTime = " + (timer - lastShotTime));
        }

        // Проверка на новую точку назначения
        if (targetPosition != Vector3.zero && oldTargetPosition != targetPosition)
        {
            aligned = false;
            //atacking = false;
            forwardDir = transform.forward;
            currentPos = transform.position;
            targetDir = targetPosition - currentPos;
            forwardTargetAngle = Vector3.Angle(forwardDir, targetDir);
            targetDistance = targetDir.magnitude;
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

    //private IEnumerator Shoot_C()
    //{
    //    //if (ammo>0 && target != null && moveMode==0)
    //    while (atacking)
    //    {
    //        GameObject torpedo = Instantiate(torpPrefab, transform.position + 2 * transform.forward, transform.rotation);
    //        Physics.IgnoreCollision(gameObject.GetComponent<BoxCollider>(), torpedo.transform.Find("model").gameObject.GetComponent<BoxCollider>());
    //        forwardDir = transform.forward;
    //        //torpedo.GetComponent<Rigidbody>().velocity = subRb.velocity + 8 * forwardDir.normalized;
    //        torpedo.GetComponent<Rigidbody>().AddForce(subRb.velocity + 50 * forwardDir.normalized, ForceMode.Impulse);

    //        //Debug.DrawRay(transform.position, subRb.velocity + 8 * forwardDir.normalized, Color.blue, 2);

    //        //print("subRb.velocity = " + subRb.velocity);
    //        //print("Torpedo Direction = " + (transform.position + subRb.velocity + 6 * forwardDir.normalized));
    //        //print("forwardDir = " + forwardDir.normalized);
    //        //print("10xforwardDir = " + 10 * forwardDir.normalized);

    //        torpedo.GetComponent<MoveTorpV1>().SetTarget(target);
    //        print(gameObject.name + " Instantiate TORPEDO with target = " + target.name);
    //        yield return new WaitForSeconds(reloadTime);
    //    }
    //}

    private void Shoot()
    {
        GameObject torpedo = Instantiate(torpPrefab, transform.position + transform.forward, transform.rotation);
        Physics.IgnoreCollision(gameObject.GetComponent<BoxCollider>(), torpedo.transform.Find("model").gameObject.GetComponent<BoxCollider>());
        forwardDir = transform.forward;
        //torpedo.GetComponent<Rigidbody>().velocity = subRb.velocity + 8 * forwardDir.normalized;
        torpedo.GetComponent<Rigidbody>().AddForce(subRb.velocity + 10 * forwardDir.normalized, ForceMode.Impulse);

        //Debug.DrawRay(transform.position, subRb.velocity + 8 * forwardDir.normalized, Color.blue, 2);

        //print("subRb.velocity = " + subRb.velocity);
        //print("Torpedo Direction = " + (transform.position + subRb.velocity + 6 * forwardDir.normalized));
        //print("forwardDir = " + forwardDir.normalized);
        //print("10xforwardDir = " + 10 * forwardDir.normalized);

        torpedo.GetComponent<MoveTorpV1>().SetTarget(target);
        print(gameObject.name + " Instantiate TORPEDO with target = " + target.name);
    }
}