using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask layerMask;
    private List<GameObject> selectedUnits = new List<GameObject>();
    private List<MoveSubV6> moveSubScript;
    private Vector3 orderPoint;
    private Transform orderTransform;
    public GameObject subPrefab;
    bool isQDown = false;

    private float myTerrainHeight;
    // Start is called before the first frame update
    void Start()
    {
        print("???"+subPrefab);
        print(transform.position);
        print(gameObject.name);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, layerMask))
            {
                orderTransform = raycastHit.transform;
                foreach (Transform child in transform)
                {
                    if (child.gameObject.transform.Find("Selection Sprite").gameObject.activeSelf)
                        selectedUnits.Add(child.gameObject);
                }

                int totalUnits = selectedUnits.Count;
                float unitSpacing = 10.0f; // Adjust this value to control the spacing between units

                // Calculate the total length of the line
                float lineLength = (totalUnits - 1) * unitSpacing;

                Debug.Log("point " + raycastHit.point);

                // Arrange the units in a line
                int i;
                //List<Vector3> unitPos = new List<Vector3>();
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
                //Vector3 lineStart = raycastHit.point - (lineLength / 2) * orderTransform.right;
                Vector3 avgDirection = (wayLine - raycastHit.point);
                avgDirection.y = 0;
                //Debug.Log("avgDirection.normalized = " + avgDirection.normalized);
                Vector3 lineStart = raycastHit.point - (lineLength / 2) * Vector3.Cross(avgDirection.normalized, Vector3.up);
                //Debug.Log("lineStart " + lineStart);
                Debug.DrawLine(wayLine, raycastHit.point, Color.cyan, 3.0f);

                for (i = 0; i < totalUnits; i++)
                {
                    targetPointsInLine[i] = lineStart + i * unitSpacing * Vector3.Cross(avgDirection.normalized, Vector3.up);
                    targetPointsInLine[i].y += 3; //float depth
                    //print("targetPointsInLine[" + i + "] = " + targetPointsInLine[i]);
                }

                foreach (GameObject unit in selectedUnits)
                {
                    float minDistance = float.MaxValue;
                    int minDistanceIndex = 0;
                    for (i = 0; i < totalUnits; i++)
                    {
                        //targetPointsInLine[i] = lineStart + i * unitSpacing * orderTransform.right;
                        float distToTargetPoint = Vector3.Distance(unit.transform.position, targetPointsInLine[i]);
                        //print("compare: unitpos = " + unit.transform.position + ", target = " + targetPointsInLine[i]);
                        //print("compare: distToTargetPoint " + distToTargetPoint + " | " + minDistance + " = minDistance" + " unit: " + unit.name);
                        if (distToTargetPoint < minDistance)
                        {
                            minDistance = distToTargetPoint;
                            minDistanceIndex = i;
                        }
                    }
                    unit.GetComponent<MoveSubV8>().targetPosition = targetPointsInLine[minDistanceIndex];
                    targetPointsInLine[minDistanceIndex] = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
                    //print("Maxed target point " + minDistanceIndex + " : " + unit.GetComponent<MoveSubV7>().targetPosition + " for " + unit.name);
                    print("Target position = " + unit.GetComponent<MoveSubV8>().targetPosition + " for " + unit.name);
                    Debug.DrawLine(unit.transform.position, unit.GetComponent<MoveSubV8>().targetPosition, Color.black, 3.0f);
                }

                //i = 0;
                //foreach (GameObject unit in selectedUnits)
                //{
                //    unit.GetComponent<MoveSubV7>().targetPosition = lineStart + i * unitSpacing * orderTransform.right;
                //    i++;
                //}
                selectedUnits.Clear();
            }
        }

        if (Input.GetKeyDown(KeyCode.Q) & !isQDown)
        {
            isQDown = true;
            Ray ray_spawn = mainCamera.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray_spawn,out RaycastHit rayCastHit_spawn, float.MaxValue))
            {
                Vector3 spawnPoint = rayCastHit_spawn.point + new Vector3 (0, 3, 0);
                Instantiate(subPrefab, spawnPoint, subPrefab.transform.rotation);
            }
        }
        if (Input.GetKeyUp(KeyCode.Q)) isQDown = false;
    }
}
