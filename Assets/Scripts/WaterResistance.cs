using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterResistance : MonoBehaviour
{
    List<MoveSubStandart> units;
    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform child in transform) 
            units.Add(child.GetComponent<MoveSubStandart>());
    }

    // Update is called once per frame
    void Update()
    {
        foreach (MoveSubStandart unit in units)
        {
            
        }
    }
}
