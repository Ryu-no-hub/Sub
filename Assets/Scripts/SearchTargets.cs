using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SearchTargets : MonoBehaviour
{
    //private bool searching = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        foreach (Transform child in transform)
        {
            //print("child = " + child.name);
            MoveSubStandart unitScript = child.GetComponent<MoveSubStandart>();
            if (unitScript.moveMode == 0 && unitScript.stopped && !unitScript.searching)
            {
                StartCoroutine(SearchTarget(child));
                unitScript.searching = true;
            }
        }
    }
    private IEnumerator SearchTarget(Transform unit)
    {
        MoveSubStandart unitScript = unit.GetComponent<MoveSubStandart>();
        int attackRange = unitScript.attackRange;

        print(unit.name + " Started search routine" + ", moveMode = " + unitScript.moveMode);
        while (unitScript.moveMode == 0)
        {
            if (unit == null) break;
            float minDistance = float.MaxValue;
            float distance;
            GameObject currentTarget = null;

            foreach (Transform child in transform)
            {
                //print(unit.name + " Checking potential target: " + child);
                if (child.transform == unit || child==null || unit==null) continue;

                if (child.GetComponent<MoveSubStandart>().team == unitScript.team) continue;
                //print(unit.name + " PASSED CHECKS ");

                distance = Vector3.Distance(unit.position, child.position);
                if (distance < attackRange && distance < minDistance)
                {
                    minDistance = distance;
                    currentTarget = child.gameObject;
                    //print(unit.name + " recieved target option on distance = " + distance);
                }
            }
            if (unitScript.target == null && currentTarget!=null)
            {
                unitScript.target = currentTarget;
                print("Setting target for " + unit.name + " - " + currentTarget==null ? "null" : currentTarget.name);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}