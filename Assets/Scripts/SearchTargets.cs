using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SearchTargets : MonoBehaviour
{
    private List<GameObject> allUnits;
    // Start is called before the first frame update
    void Start()
    {
        //foreach (Transform child in transform)
        //{
        //    allUnits.Add(child.gameObject);
        //}
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Transform child in transform)
        {
            //print("child = " + child.name);
            StartCoroutine(SearchTarget(child));
        }
    }
    private IEnumerator SearchTarget(Transform unit)
    {
        yield return new WaitForSeconds(1);
        //print(unit.name);
        MoveSubV10 unitScript = unit.GetComponent<MoveSubV10>();
        if (unitScript.moveMode == 0)
        {
            float minDistance = float.MaxValue;
            float distance;
            GameObject currentTarget = null;

            foreach (Transform child in transform)
            {
                //print("Checking targets for: " + unit.name + ", child: " + child);
                if (child.transform == unit) continue;

                if (child.GetComponent<MoveSubV10>().team == unitScript.team) continue;
                //print("PASSD ");

                distance = Vector3.Distance(unit.position, child.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    currentTarget = child.gameObject;
                }
            }
            unitScript.target = currentTarget;
        }

    }
}
