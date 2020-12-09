using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DV_CubeToPoint : MonoBehaviour
{
    // Can enter variables on the interface
    public float x;
    public float y;
    public float z;
    public Transform drone;

    // Update is called once per frame
    void Update()
    {
        // Create position from given variables
        Vector3 newPos = new Vector3(x, y, z); 

        // If drone not in position
        if (drone.position != newPos) 
        {
            // Set drone to position
            drone.position = newPos; 
        }
    }
}
