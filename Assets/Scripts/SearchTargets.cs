using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SearchTargets : MonoBehaviour
{
    //private bool searching = false;
    int N = 0;
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
            if (!unitScript)
                continue;
            if (unitScript.fixTarget == false && unitScript.stopped && !unitScript.searching)
            {
                print("Started search routine " + (++N) + " for unit: " + child.name);
                StartCoroutine(SearchTarget(child, N));
                unitScript.searching = true;
            }
        }
    }
    private IEnumerator SearchTarget(Transform unit, int Num)
    {
        MoveSubStandart unitScript = unit.GetComponent<MoveSubStandart>();
        int attackRange = unitScript.attackRange;

        print("Entered search routine " + Num + " for unit: " + unit.name + ", behaviour = " + unitScript.behaviour);
        while (unitScript.behaviour == MoveSubStandart.BehaviourState.Idle)
        {
            if (unit == null) break;
            float minDistance = float.MaxValue;
            float distance;
            GameObject currentTarget = null;

            foreach (Transform child in transform)
            {
                //print(unit.name + " Checking potential target: " + child);
                if (child.transform == unit || child==null || unit==null) continue;

                MoveSubStandart childScript = child.GetComponent<MoveSubStandart>();
                if (!childScript) continue;
                if (childScript.team == unitScript.team) continue;
                //print(unit.name + " PASSED CHECKS ");

                distance = Vector3.Distance(unit.position, child.position);
                if (distance < attackRange && distance < minDistance)
                {
                    minDistance = distance;
                    currentTarget = child.gameObject;
                    //print(unit.name + " recieved target option on distance = " + distance);
                }
            }
            //if (unitScript.target == null && currentTarget!=null)
            if (currentTarget!=null && currentTarget != unitScript.target)
            {
                unitScript.SetAttackTarget(currentTarget, false);
                print("Routine " + Num + " Setting target for " + unit.name + " - " + (currentTarget==null ? "null" : currentTarget.name));
            }
            yield return new WaitForSeconds(0.5f);
        }
        print("Finished search routine " + Num + " for unit: " + unit.name);
    }
}