using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask layerMask;
    private List<GameObject> selectedUnits = new List<GameObject>();
    //private List<MoveSubV6> moveSubScript;
    //private Transform orderTransform;
    public GameObject subPrefab;
    bool isQDown = false;
    int N = 0;

    //private float myTerrainHeight;
    // Start is called before the first frame update
    void Start()
    {
        //print("???"+subPrefab);
        //print(transform.position);
        //print(gameObject.name);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, layerMask))
            {
                print("Order raycast point  = " + raycastHit.point + ", object hit = " + raycastHit.collider.gameObject.name + ", layer = " + raycastHit.collider.gameObject.layer);

                foreach (Transform child in transform)
                {
                    if (child.gameObject.transform.Find("Selection Sprite").gameObject.activeSelf)
                        selectedUnits.Add(child.gameObject);
                }

                if (raycastHit.collider.gameObject.layer == 3)
                    MoveOrder(raycastHit);
                else
                {
                    GameObject target = raycastHit.collider.gameObject;
                    AttackSingleOrder(target);
                }
                selectedUnits.Clear();
            }
        }

        if (Input.GetKeyDown(KeyCode.Q) & !isQDown)
        {
            SpawnSub();
        }
        if (Input.GetKeyUp(KeyCode.Q)) isQDown = false;
    }

    private void SpawnSub()
    {
        isQDown = true;
        Ray ray_spawn = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray_spawn, out RaycastHit rayCastHit_spawn, float.MaxValue))
        {
            Vector3 spawnPoint = rayCastHit_spawn.point + new Vector3(0, 3, 0);
            var newSub = Instantiate(subPrefab, spawnPoint, subPrefab.transform.rotation);
            newSub.transform.parent = GameObject.Find("Units").transform;
            newSub.name = subPrefab.name + " " + N++;
        }
    }
    private void MoveOrder(RaycastHit raycastHit)
    {
        int totalUnits = selectedUnits.Count;
        float unitSpacing = 8; // Adjust this value to control the spacing between units

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
        Vector3 avgDirection = (wayLine - raycastHit.point);
        avgDirection.y = 0;
        //Debug.Log("avgDirection.normalized = " + avgDirection.normalized);
        Vector3 lineStart = raycastHit.point - (lineLength / 2) * Vector3.Cross(avgDirection.normalized, Vector3.up);
        Debug.DrawLine(wayLine, raycastHit.point, Color.cyan, 3.0f);

        for (i = 0; i < totalUnits; i++)
        {
            targetPointsInLine[i] = lineStart + i * unitSpacing * Vector3.Cross(avgDirection.normalized, Vector3.up);
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
            unit.GetComponent<MoveSubV13>().moveDestination = targetPointsInLine[minDistanceIndex];
            unit.GetComponent<MoveSubV13>().target = null;

            print("Target position = " + targetPointsInLine[minDistanceIndex] + " for " + unit.name);
            //print("Maxed target point " + minDistanceIndex + " : " + unit.GetComponent<MoveSubV7>().moveDestinationition + " for " + unit.name);
            Debug.DrawLine(unit.transform.position, targetPointsInLine[minDistanceIndex], Color.black, 10.0f);
            targetPointsInLine[minDistanceIndex] = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
        }
    }
    private void AttackSingleOrder(GameObject target)
    {
        foreach(GameObject child in selectedUnits)
        {
            if (child == target) continue;
            child.GetComponent<MoveSubV13>().target = target;
            print(target.name + " set as target for " + child.name);
            print(child.GetComponent<MoveSubV13>().target.name);

            int unitRange = child.GetComponent<MoveSubV13>().attackRange;

            if (Vector3.Distance(child.transform.position, target.transform.position) > unitRange)
            {
                Vector3 targetDir = target.transform.position - child.transform.position;
                child.GetComponent<MoveSubV13>().moveDestination = target.transform.position - (unitRange - 2) * targetDir.normalized ;
            }
            else
            {
                child.GetComponent<MoveSubV13>().Stop();
            }
        }
    }
}
