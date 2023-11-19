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
            MoveSubV12 unitScript = child.GetComponent<MoveSubV12>();
            if (unitScript.moveMode == 0 && !unitScript.searching)
            {
                unitScript.searching = true;
                print(child + " Started search routine");
                StartCoroutine(SearchTarget(child));
            }
        }
    }
    private IEnumerator SearchTarget(Transform unit)
    {
        print(unit.name + " Entered search routine");
        MoveSubV12 unitScript = unit.GetComponent<MoveSubV12>();
        print(unit.name + " moveMode = " + unitScript.moveMode);
        while (unitScript.moveMode == 0)
        {
            yield return new WaitForSeconds(0.5f);
            float minDistance = float.MaxValue;
            float distance;
            GameObject currentTarget = null;

            foreach (Transform child in transform)
            {
                //print(unit.name + " Checking potential target: " + child);
                if (child.transform == unit) continue;

                if (child.GetComponent<MoveSubV12>().team == unitScript.team) continue;
                //print(unit.name + " PASSED CHECKS ");

                distance = Vector3.Distance(unit.position, child.position);
                if (distance < 40 && distance < minDistance)
                {
                    minDistance = distance;
                    currentTarget = child.gameObject;
                }
            }
            if (unitScript.target == null)
                unitScript.target = currentTarget;
        }
    }
}