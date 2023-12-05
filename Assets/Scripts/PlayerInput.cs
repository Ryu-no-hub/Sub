using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask layerMask;
    private List<GameObject> selectedUnits = new List<GameObject>();

    //private List<MoveSubStandart> subScript = new List<MoveSubStandart>();

    public GameObject subPrefab;
    public GameObject subPrefab2;
    private int N = 0;
    private float timer=0;
    private float targetRotationY = 0, arrowSize;
    private bool arrowSpawned=false;

    public RTSSelection selection;
    public GameObject arrowPrefab;
    private GameObject arrowInstance;
    private Vector3 initialMovePoint, currentMovePoint, initialMousePos;
    
    void Start()
    {
        //print("???"+subPrefab);
        //print(transform.position);
        //print(gameObject.name);
        arrowSize = arrowPrefab.GetComponent<SpriteRenderer>().bounds.size.x;
    }

    // Update is called once per frame
    void Update()
    {
        // Selection
        if (Input.GetMouseButtonDown(0))
        {
            // Don't begin selecting if clicking on UI
            // TODO: Exclude World space UI from this check
            if (IsPointerOverUIElement())
                return;

            // Different modes (Default, additive, subtractive)
            RTSSelection.SelectionModifier mode = RTSSelection.SelectionModifier.Default;

            if (Input.GetKey(KeyCode.LeftShift))
                mode = RTSSelection.SelectionModifier.Additive;
            else
            if (Input.GetKey(KeyCode.LeftControl))
                mode = RTSSelection.SelectionModifier.Subtractive;

            selection.BeginSelection(mode);
        }

        // All selection confirms on mouse up
        if (Input.GetMouseButtonUp(0) && selection.selecting)
        {
            selection.ConfirmSelection();
        }


        // Move - Form a line
        if (Input.GetMouseButtonDown(1))
        {
            if (IsPointerOverUIElement())
                return;

            foreach (Transform child in transform)
            {
                if (child.gameObject.transform.Find("Selection Sprite").gameObject.activeSelf)
                    selectedUnits.Add(child.gameObject);
            }
            if (selectedUnits.Count == 0)
                return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, layerMask))
            {
                print("Order raycast point  = " + raycastHit.point + ", object hit = " + raycastHit.collider.gameObject.name + ", layer = " + raycastHit.collider.gameObject.layer);
                initialMousePos = Input.mousePosition;
                if (raycastHit.collider.gameObject.layer == 3) // Ground
                    initialMovePoint = raycastHit.point; //MoveOrder(raycastHit);
                else // Not Ground, therefore Unit
                {
                    GameObject target = raycastHit.collider.gameObject;
                    MoveSubStandart unitScript = target.GetComponent<MoveSubStandart>();

                    if (!unitScript)
                        return;
                    // Если объект из своей команды, двигаться на его место
                    if (unitScript.team == selectedUnits[0].GetComponent<MoveSubStandart>().team)
                        initialMovePoint = raycastHit.point; //MoveOrder(raycastHit);
                    else
                        AttackSingleOrder(target);
                }
            }            
        }

        if (Input.GetMouseButton(1))
        {
            timer += Time.deltaTime;
            if (initialMovePoint == Vector3.zero) return;
            //print(arrowSpawned + ", timer = " + timer + ", length = " + (currentMovePoint.magnitude - initialMovePoint.magnitude));
            Vector3 mousePos = Input.mousePosition, spawnPos;
            float stretch = (initialMousePos - mousePos).magnitude, angle, scale;
            int stretchStart = 100;

            angle = -Vector3.SignedAngle(Vector3.up, initialMousePos - mousePos, Vector3.forward);
            //print("Signed Angle = " + -angle);
            if (stretch > stretchStart)
            {
                spawnPos = initialMovePoint + 5 * Vector3.up + (Quaternion.AngleAxis(angle, Vector3.up) * Vector3.right * arrowSize * stretch / stretchStart / 2);
                if (!arrowSpawned)
                {
                    arrowInstance = Instantiate(arrowPrefab, spawnPos, Quaternion.AngleAxis(angle, Vector3.up));
                    arrowSpawned = true;
                }
                else
                {
                    scale = stretch / stretchStart;
                    arrowInstance.transform.SetPositionAndRotation(spawnPos, Quaternion.AngleAxis(angle, Vector3.up));
                    arrowInstance.transform.localScale = Vector3.one * scale;
                    //print("position = " + arrowInstance.transform.position + ", angle = " + angle + ", scale = " + scale + ", stretch = " + stretch);
                }
            }
            else if (arrowSpawned)
            {
                spawnPos = initialMovePoint + 5 * Vector3.up + (Quaternion.AngleAxis(angle, Vector3.up) * Vector3.right * arrowSize / 2);
                arrowInstance.transform.SetPositionAndRotation(spawnPos, Quaternion.AngleAxis(angle, Vector3.up));
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            timer = 0;
            if (arrowSpawned)
            {
                targetRotationY = arrowInstance.transform.eulerAngles.y + 90;
                Destroy(arrowInstance);
                arrowSpawned = false;
            }
            MoveOrder(initialMovePoint);
            print("initialMovePoint = " + initialMovePoint);
            initialMovePoint = Vector3.zero;
            selectedUnits.Clear();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            SpawnSub1();
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            SpawnSub2();
        }

        if (Input.GetKeyDown(KeyCode.Delete))
            foreach (Transform child in transform)
                if (child.gameObject.transform.Find("Selection Sprite").gameObject.activeSelf)
                    Destroy(child.gameObject);

        if (Input.GetKeyDown(KeyCode.S))
        {
            StopSub();
        }
    }


    private bool IsPointerOverUIElement()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raycastResults = new();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        for (int index = 0; index < raycastResults.Count; index++)
        {
            RaycastResult curRaysastResult = raycastResults[index];
            Debug.Log(curRaysastResult.gameObject.layer + ", " + curRaysastResult.gameObject.name);
            if (curRaysastResult.gameObject.layer == LayerMask.NameToLayer("UI"))
                return true;
        }
        return false;
    }

    private void SpawnSub1()
    {
        Ray ray_spawn = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray_spawn, out RaycastHit rayCastHit_spawn, float.MaxValue))
        {
            Vector3 spawnPoint = rayCastHit_spawn.point + new Vector3(0, 3, 0);
            var newSub = Instantiate(subPrefab, spawnPoint, subPrefab.transform.rotation);
            newSub.transform.SetParent(GameObject.Find("Units").transform);
            newSub.name = subPrefab.name + " " + N++;
        }
    }
    private void SpawnSub2()
    {
        Ray ray_spawn = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray_spawn, out RaycastHit rayCastHit_spawn, float.MaxValue))
        {
            Vector3 spawnPoint = rayCastHit_spawn.point + new Vector3(0, 3, 0);
            var newSub = Instantiate(subPrefab2, spawnPoint, subPrefab2.transform.rotation);
            newSub.transform.SetParent(GameObject.Find("Units").transform);
            newSub.name = subPrefab2.name + " " + N++;
        }
    }
        
    private void MoveOrder(Vector3 targetPosition)
    {
        int totalUnits = selectedUnits.Count;
        print("totalUnits = " + totalUnits);
        float unitSpacing = 7; // Adjust this value to control the spacing between units

        // Calculate the total length of the line
        float lineLength = (totalUnits - 1) * unitSpacing;

        // Arrange the units in a line
        int i;
        Vector3[] targetPointsInLine = new Vector3[totalUnits];
        Vector3 wayLine = Vector3.zero;

        // Debug.Log("Initial wayLine = " + wayLine);
        foreach (GameObject unit in selectedUnits)
        {
            wayLine += unit.transform.position;
            //Debug.Log("wayLine = " + wayLine);
        }
        wayLine /= totalUnits;
        wayLine.y = 0;
        //Debug.Log("wayLine final = " + wayLine);

        // Calculate the starting position of the line
        Vector3 avgDirection = wayLine - targetPosition;
        avgDirection.y = 0;
        Vector3 lineStart, targetDirection;
        if (targetRotationY == 0)
            targetDirection = avgDirection.normalized;
        else
            targetDirection = (currentMovePoint - initialMovePoint).normalized;
            //targetDirection = new Vector3(0, targetRotationY, 0);
        //Debug.Log("targetDirection = " + targetDirection);
        lineStart = targetPosition - (lineLength / 2) * Vector3.Cross(targetDirection, Vector3.up);
        //Debug.Log("Vector3.Cross(targetDirection, Vector3.up) = " + Vector3.Cross(targetDirection, Vector3.up));
        Debug.DrawLine(wayLine, targetPosition, Color.cyan, 3.0f);

        for (i = 0; i < totalUnits; i++)
        {
            targetPointsInLine[i] = lineStart + i * unitSpacing * Vector3.Cross(targetDirection, Vector3.up);
            targetPointsInLine[i].y += 3; 
            //print("targetPointsInLine[" + i + "] = " + targetPointsInLine[i]);
        }

        foreach (GameObject unit in selectedUnits)
        {
            float minDistance = float.MaxValue;
            int minDistanceIndex = 0;
            for (i = 0; i < totalUnits; i++)
            {
                float distToTargetPoint = Vector3.Distance(unit.transform.position, targetPointsInLine[i]);
                //print("compare: unitpos = " + unit.transform.position + ", target = " + targetPointsInLine[i]);
                //print("compare: distToTargetPoint " + distToTargetPoint + " | " + minDistance + " = minDistance" + " unit: " + unit.name);
                if (distToTargetPoint < minDistance)
                {
                    minDistance = distToTargetPoint;
                    minDistanceIndex = i;
                }
            }
            MoveSubStandart unitScriptStandart = unit.GetComponent<MoveSubStandart>();

            bool isGroup = selectedUnits.Count > 1;
            print("targetRotationY = " + targetRotationY);
            unitScriptStandart.SetMoveDestination(targetPointsInLine[minDistanceIndex], false, targetRotationY, moveInGroup: isGroup);
            
            //unit.GetComponent<MoveSubStandart>().moveDestination = targetPointsInLine[minDistanceIndex];
            //unit.GetComponent<MoveSubStandart>().target = null;
            //unit.GetComponent<MoveSubStandart>().stopped = false;
            //unit.GetComponent<MoveSubStandart>().moveMode = MoveSubStandart.MoveMode.TurnToDirection;

            print("Target position = " + targetPointsInLine[minDistanceIndex] + " for " + unit.name);
            //print("Maxed target point " + minDistanceIndex + " : " + unit.GetComponent<MoveSubV7>().moveDestinationition + " for " + unit.name);
            Debug.DrawLine(unit.transform.position, targetPointsInLine[minDistanceIndex], Color.black, 10.0f);
            targetPointsInLine[minDistanceIndex] = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
        }
        targetRotationY = 0;
    }
    private void AttackSingleOrder(GameObject target)
    {
        foreach(GameObject child in selectedUnits)
        {
            MoveSubStandart unitScript = child.GetComponent<MoveSubStandart>();
            if (child == target)
            {
                unitScript.Stop();
                continue;
            }
            unitScript.SetAttackTarget(target, true);
            print(target.name + " set as Fixed target for " + child.name);
            //print(child.GetComponent<MoveSubStandart>().target.name);

            //int unitAttackRange = child.GetComponent<MoveSubStandart>().attackRange;

            //if (Vector3.Distance(child.transform.position, target.transform.position) > unitAttackRange)
            //{
            //    Vector3 targetDir = target.transform.position - child.transform.position;
            //    child.GetComponent<MoveSubStandart>().moveDestination = target.transform.position - (unitAttackRange - 2) * targetDir.normalized ;
            //}
            //else
            //{
            //    child.GetComponent<MoveSubStandart>().Stop();
            //}
        }
    }

    private void StopSub()
    {
        selectedUnits.Clear();
        foreach (Transform child in transform)
        {
            if (child.transform.Find("Selection Sprite").gameObject.activeSelf)
                child.GetComponent<MoveSubStandart>().Stop();
        }


    }
}
