using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DV_SeriesOfPoints : MonoBehaviour
{
    List<Vector3> positions;
    public float timeStep;
    int iterator;
    public Transform drone;
    float timeToWait;

    // Start is called before the first frame update
    void Start()
    {
        // Create empty list, set iterator to 0, set wait time to time step
        positions = new List<Vector3>();
        iterator = 0;
        timeToWait = timeStep;

        // Fly straight up
        positions.Add(new Vector3(0, 0.5f, 0));
        positions.Add(new Vector3(0, 1, 0));
        positions.Add(new Vector3(0, 1.5f, 0));
        positions.Add(new Vector3(0, 2, 0));
        positions.Add(new Vector3(0, 2.5f, 0));
        positions.Add(new Vector3(0, 3, 0));
        positions.Add(new Vector3(0, 3.5f, 0));
        positions.Add(new Vector3(0, 4, 0));
        positions.Add(new Vector3(0, 4.5f, 0));
        positions.Add(new Vector3(0, 5, 0));

    }

    // Update is called once per frame
    void Update()
    {
        // Remove time since last update from wait time
        timeToWait -= Time.deltaTime;

        // If time to wait is less than 0
        if (timeToWait <= 0)
        {
            // If drone position and desired position are not the same then set drone position to desired position
            if (drone.position != positions[iterator])
            {
                drone.position = positions[iterator];
            }

            timeToWait = timeStep;
            if (iterator < positions.Count - 1)
            {
                iterator++;
            }
        }

    }
}
