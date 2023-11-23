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
            MoveSubV13 unitScript = child.GetComponent<MoveSubV13>();
            if (unitScript.moveMode == 0 && !unitScript.searching)
            {
                unitScript.searching = true;
                //print(child + " Started search routine");
                StartCoroutine(SearchTarget(child));
            }
        }
    }
    private IEnumerator SearchTarget(Transform unit)
    {
        MoveSubV13 unitScript = unit.GetComponent<MoveSubV13>();
        int attackRange = unitScript.attackRange;

        print(unit.name + " Started search routine" + ", moveMode = " + unitScript.moveMode);
        while (unitScript.moveMode == 0)
        {
            if (unit == null) break;
            yield return new WaitForSeconds(0.5f);
            float minDistance = float.MaxValue;
            float distance;
            GameObject currentTarget = null;

            foreach (Transform child in transform)
            {
                //print(unit.name + " Checking potential target: " + child);
                if (child.transform == unit || child==null || unit==null) continue;

                if (child.GetComponent<MoveSubV13>().team == unitScript.team) continue;
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
        }
    }
}