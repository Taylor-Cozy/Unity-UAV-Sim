using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using UnityEngine.UI;

public class DroneVisualiserBACKUP : MonoBehaviour
{
    // Arrays for CSV file and data
    List<string> flightData;
    string[] positions;
    List<Image> images;
    public TextAsset csvFile;

    // Iterators / Time elements
    int dataIterator = 1; // First row has column titles
    float timeStep;
    public int screenshotIncrement = 2; // Take screenshots every (timeStep * screenshotIncrement) seconds
    float timeToWait;

    // Positions and transforms
    public Transform drone;
    public Rigidbody rb;

    // Desired positions and velocities (i.e. the next position)
    Vector3 desiredPos;
    Vector3 desiredVel;
    Vector3 desiredRot;

    // Start is called before the first frame update
    void Start()
    {
        // Read in CSV file
        TextAsset csv = csvFile;
        flightData = new List<string>(csv.text.Split(new char[] { '\n' }));
    }

    // Update is called once per frame
    void Update()
    {
        timeToWait -= Time.deltaTime;

        // If time step has been surpassed
        if (timeToWait <= 0)
        {
            // Split CSV string into Array
            positions = flightData[dataIterator].Split(new char[] { ',' });

            // Set Position
            desiredPos.x = float.Parse(positions[8]);
            desiredPos.y = float.Parse(positions[9]);
            desiredPos.z = float.Parse(positions[10]);

            // Set Velocity
            desiredVel.x = float.Parse(positions[11]);
            desiredVel.y = float.Parse(positions[12]);
            desiredVel.z = float.Parse(positions[13]);

            // Set rotation
            desiredRot.x = float.Parse(positions[2]);
            desiredRot.y = float.Parse(positions[5]);
            desiredRot.z = float.Parse(positions[3]);

            // Set Position and Velocity
            drone.position = desiredPos;
            rb.velocity = desiredVel;
            drone.rotation = Quaternion.Euler(desiredRot);

            // Prevents overflow
            if (dataIterator >= flightData.Count - 1)
            {
                desiredVel.x = 0;
                desiredVel.y = 0;
                desiredVel.z = 0;

                rb.velocity = desiredVel;

                // STITCH

            }
            else
            {
                dataIterator++;
            }

            // Set timestep
            timeToWait = float.Parse(flightData[dataIterator].Split(new char[] { ',' })[1]);

            // Decide to screenshot
            if (dataIterator % screenshotIncrement == 0)
            {
                rb.GetComponentInChildren<Photograph>().TakeScreenshot(rb.GetComponentInChildren<Photograph>().width, rb.GetComponentInChildren<Photograph>().height, 1, "test");
            }
        }
    }
}
