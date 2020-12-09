using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Linq;
using System.Text.RegularExpressions;

public class DroneVisualiser : MonoBehaviour
{
    // Arrays for CSV file and data
    GameObject[] drones;        // list of drone objects
    List<string> flightData;    // flight data
    string[] positions;         // each timestep array
    List<Image> images;         // list of images taken
    public TextAsset csvFile;   // csv file asset
    bool single = false;        // single or multiple drones involved
    int[,] location;            // Positions -> Velocity -> Rotation
    List<string> titles;        // List of column headers
    int numberOfDrones;         // number of drones
    
    public bool takeScreenshots = true;
    public bool stitchOnComplete = true;
    
    bool stitch = false;
    bool completed = false;
    
    public float fieldOfView = 70;

    // Drone prefab
    public GameObject dronePrefab;
    string currentRealTime;

    // Iterators / Time elements
    int dataIterator = 1; // First row has column titles
    float timeToWait;
    public float cameraTimeToWait = .5f; // .5 Seconds between screenshots
    float cameraCurrentTime;

    // Desired positions and velocities (i.e. the next position)
    Vector3 desiredPos;
    Vector3 desiredVel;
    Vector3 desiredRot;

    int currentCam = 0;
    Vector3 currentDronePos;

    int numberOfScreenshots = 0;
    public bool experimental = false;

    // Start is called before the first frame update
    void Start()
    {
        cameraCurrentTime = cameraTimeToWait;

        // Read in CSV file
        TextAsset csv = csvFile;
        flightData = new List<string>( csv.text.Split(new char[] { '\n' }) ); // Create array of each time step

        timeToWait = float.Parse(flightData[1].Split(new char[] { ',' })[1]);

        titles = flightData[0].Split(new char[] { ',' }).ToList(); // get column headers
        titles[titles.Count - 1] = titles[titles.Count - 1].Replace("\r", String.Empty); // replace carriage return with empty
        
        singleOrMultiple(); // create array of reference indexes for single or multiple drones
        
        currentRealTime = System.DateTime.Now.ToString().Replace("/", "").Replace(" ", "_").Replace(":", "");
        string makeDir = Application.dataPath.Replace("/", "\\") + "\\Screenshots\\" + currentRealTime + "\"\"";
        string makeDir2 = Application.dataPath.Replace("/", "\\") + "\\outputs\\" + currentRealTime + "\"\"";
        string com = "cmd /C \"mkdir \"" + makeDir;
        string com2 = "cmd /C \"mkdir \"" + makeDir2;
        System.Diagnostics.Process p = System.Diagnostics.Process.Start("cmd.exe", com);
        System.Diagnostics.Process p2 = System.Diagnostics.Process.Start("cmd.exe", com2);
        p.WaitForExit();
        p2.WaitForExit();

    }

    private void singleOrMultiple()
    {
        // Pattern Matching

        // Single or Multiple Check
        foreach (string s in titles)
        {
            // check to see if controller is referred to as controller or controller1 - latter implying multiple drones present
            if (s == "{uavCont}.controller.pitchOut")
            {
                // single
                single = true;
                break;
            }
        }

        // Positions -> Velocity -> Rotation

        if (single)
        {
            numberOfDrones = 1;
            drones = new GameObject[1];

            GameObject a = Instantiate(dronePrefab, new Vector3(0, 0, 0), Quaternion.identity);
            drones[0] = a;

            location = new int[1, 9];

            location[0, 0] = titles.IndexOf("{uav}.uav.posX");
            location[0, 1] = titles.IndexOf("{uav}.uav.posY");
            location[0, 2] = titles.IndexOf("{uav}.uav.posZ");

            location[0, 3] = titles.IndexOf("{uav}.uav.velX");
            location[0, 4] = titles.IndexOf("{uav}.uav.velY");
            location[0, 5] = titles.IndexOf("{uav}.uav.velZ");

            location[0, 6] = titles.IndexOf("{uavCont}.controller.pitchOut");
            location[0, 7] = titles.IndexOf("{uav}.uav.currentYaw");
            location[0, 8] = titles.IndexOf("{uavCont}.controller.rollOut");
        } 
        else
        {

            string finalDrone = titles[titles.Count - 1].Split(new char[] { '.' })[1]; // Get last drone
            numberOfDrones = int.Parse(finalDrone.Substring(finalDrone.Length - 1)); // Get number of that drone

            drones = new GameObject[numberOfDrones]; // Create list of drone objects

            for (int i = 0; i < numberOfDrones; i++)
            {
                GameObject a = Instantiate(dronePrefab, new Vector3(0, 0, 0), Quaternion.identity); // instantiate drone prefab
                drones[i] = a;
            }

            location = new int[numberOfDrones, 9];

            for (int i = 0; i < numberOfDrones; i++)
            {
                location[i, 0] = titles.IndexOf("{uav}.uav" + (i + 1) + ".posX");
                location[i, 1] = titles.IndexOf("{uav}.uav" + (i + 1) + ".posY");
                location[i, 2] = titles.IndexOf("{uav}.uav" + (i + 1) + ".posZ");

                location[i, 3] = titles.IndexOf("{uav}.uav" + (i + 1) + ".velX");
                location[i, 4] = titles.IndexOf("{uav}.uav" + (i + 1) + ".velY");
                location[i, 5] = titles.IndexOf("{uav}.uav" + (i + 1) + ".velZ");

                location[i, 6] = titles.IndexOf("{uav}.uav" + (i + 1) + ".currentPitch");
                location[i, 7] = titles.IndexOf("{uav}.uav" + (i + 1) + ".currentYaw");
                location[i, 8] = titles.IndexOf("{uav}.uav" + (i + 1) + ".currentRoll");
            }
        }


    }

    // Update is called once per frame
    void Update()
    {
        currentDronePos = drones[currentCam].transform.position + new Vector3(0, 2f, -5);
        gameObject.GetComponent<Camera>().transform.position = currentDronePos;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentCam++;
            if (currentCam >= drones.Length)
                currentCam = 0;
        }

        if (flightData[dataIterator].Split(new char[] { ',' })[0] == "")
        {
            desiredVel.x = 0;
            desiredVel.y = 0;
            desiredVel.z = 0;

            for (int i = 0; i < numberOfDrones; i++)
            {
                drones[i].GetComponent<Rigidbody>().velocity = desiredVel;
            }

            stitch = true;
        }

        if (!stitch) { 
            timeToWait -= Time.deltaTime;

            // If time step has been surpassed
            if (timeToWait <= 0)
            {
                updateDrones();
            }
        } else
        {
            if (!completed && stitchOnComplete)
            {
                stitchImages();
            }
        }
    }

    private void updateDrones()
    {
        // Split CSV string into Array
        positions = flightData[dataIterator].Split(new char[] { ',' });

        for (int i = 0; i < numberOfDrones; i++)
        {
            // Set Position
            desiredPos.x = float.Parse(positions[location[i, 0]]);
            desiredPos.y = float.Parse(positions[location[i, 1]]);
            desiredPos.z = float.Parse(positions[location[i, 2]]);

            // Set Velocity
            desiredVel.x = float.Parse(positions[location[i, 3]]);
            desiredVel.y = float.Parse(positions[location[i, 4]]);
            desiredVel.z = float.Parse(positions[location[i, 5]]);

            // Set rotation
            desiredRot.x = float.Parse(positions[location[i, 6]]); // Pitch
            desiredRot.y = float.Parse(positions[location[i, 7]]); // Yaw
            desiredRot.z = float.Parse(positions[location[i, 8]]); // Roll

            // Set Position and Velocity
            drones[i].transform.position = desiredPos;
            drones[i].GetComponent<Rigidbody>().velocity = desiredVel;
            drones[i].transform.rotation = Quaternion.Euler(desiredRot);

            cameraCurrentTime -= Time.deltaTime;
            // Decide to screenshot
            if (cameraCurrentTime <= 0 && takeScreenshots)
            {
                cameraCurrentTime = cameraTimeToWait;
                if (experimental)
                {
                    drones[i].GetComponentInChildren<Photograph>().TakeScreenshot(drones[i].GetComponentInChildren<Photograph>().width, drones[i].GetComponentInChildren<Photograph>().height, i + 1, currentRealTime, true);
                }
                else
                {
                    drones[i].GetComponentInChildren<Photograph>().TakeScreenshot(drones[i].GetComponentInChildren<Photograph>().width, drones[i].GetComponentInChildren<Photograph>().height, i + 1, currentRealTime);
                }

                numberOfScreenshots++;
            }
        }

        // Prevents overflow
        if (dataIterator >= flightData.Count - 1)
        {
            Debug.Log("Finished");

            desiredVel.x = 0;
            desiredVel.y = 0;
            desiredVel.z = 0;

            for (int i = 0; i < numberOfDrones; i++)
            {
                drones[i].GetComponent<Rigidbody>().velocity = desiredVel;
            }

            // STITCH
            stitch = true;

        }
        else
        {
            // Set timestep
            timeToWait = float.Parse(flightData[dataIterator].Split(new char[] { ',' })[1]);
            dataIterator++;
        }
    }

    private void stitchImages()
    {
        Debug.Log("Start Stitch");
        DateTime timeStart = System.DateTime.UtcNow;

        int passNumber = 0;

        if (!experimental)
        {
            //stitch each drones images all at once
            string prefix = "\"" + Application.dataPath + "/Hugin/bin/";
            string pto_gen = prefix + "pto_gen\" -f " + fieldOfView + " -o \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\" \"" + Application.dataPath + "/Screenshots/" + currentRealTime + "/*.png\"";
            string cpfind = prefix + "cpfind\" -o \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\" \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\"";
            string pto_var = prefix + "pto_var\" --opt=\"y,p,r,TrX,TrY,TrZ\" --output=\"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\" \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\"";
            string autooptimiser = prefix + "autooptimiser\" -n -o \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\" \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\"";
            string pano_modify = prefix + "pano_modify\" --projection=0 --fov=AUTO --canvas=AUTO --blender=INTERNAL -o \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\" \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\"";
            string hugin_executor = prefix + "hugin_executor\" --stitching \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\"";

            string full_command = pto_gen + " & " + cpfind + " & " + pto_var + " & " + autooptimiser + " & " + pano_modify + " & " + hugin_executor;
            System.Diagnostics.Process p = System.Diagnostics.Process.Start("cmd.exe", "cmd /C \"" + full_command + "\"");
            p.WaitForExit();
        }
        else
        {
            //batch
            int numberOfImages = numberOfScreenshots / numberOfDrones;
            

            for (int j = 0; j < numberOfDrones; j++)
            {
                for (int i = 0; i < Math.Ceiling(numberOfImages / 10.0f); i++)
                {
                    passNumber++;

                    //stitch each drones images all at once
                    string prefix = "\"" + Application.dataPath + "/Hugin/bin/";
                    string pto_gen = prefix + "pto_gen\" -f " + fieldOfView + " -o \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\" \"" + Application.dataPath + "/Screenshots/" + currentRealTime + "/drone_" + (j + 1) + "_Screenshot_" + i + "0.png\" " +
                        "\"" + Application.dataPath + "/Screenshots/" + currentRealTime + "/drone_" + (j + 1) + "_Screenshot_" + i + "1.png\" " +
                        "\"" + Application.dataPath + "/Screenshots/" + currentRealTime + "/drone_" + (j + 1) + "_Screenshot_" + i + "2.png\" " +
                        "\"" + Application.dataPath + "/Screenshots/" + currentRealTime + "/drone_" + (j + 1) + "_Screenshot_" + i + "3.png\" " +
                        "\"" + Application.dataPath + "/Screenshots/" + currentRealTime + "/drone_" + (j + 1) + "_Screenshot_" + i + "4.png\" " +
                        "\"" + Application.dataPath + "/Screenshots/" + currentRealTime + "/drone_" + (j + 1) + "_Screenshot_" + i + "5.png\" " +
                        "\"" + Application.dataPath + "/Screenshots/" + currentRealTime + "/drone_" + (j + 1) + "_Screenshot_" + i + "6.png\" " +
                        "\"" + Application.dataPath + "/Screenshots/" + currentRealTime + "/drone_" + (j + 1) + "_Screenshot_" + i + "7.png\" " +
                        "\"" + Application.dataPath + "/Screenshots/" + currentRealTime + "/drone_" + (j + 1) + "_Screenshot_" + i + "8.png\" " +
                        "\"" + Application.dataPath + "/Screenshots/" + currentRealTime + "/drone_" + (j + 1) + "_Screenshot_" + i + "9.png\"";

                    string cpfind = prefix + "cpfind\" -o \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\" \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\"";
                    string pto_var = prefix + "pto_var\" --opt=\"y,p,r,TrX,TrY,TrZ\" --output=\"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\" \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\"";
                    string autooptimiser = prefix + "autooptimiser\" -n -o \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\" \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\"";
                    string pano_modify = prefix + "pano_modify\" --projection=0 --fov=AUTO --canvas=AUTO --blender=INTERNAL -o \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\" \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\"";
                    string hugin_executor = prefix + "hugin_executor\" --stitching \"" + Application.dataPath + "/outputs/" + currentRealTime + "/output.pto\"";

                    string full_command = pto_gen + " & " + cpfind + " & " + pto_var + " & " + autooptimiser + " & " + pano_modify + " & " + hugin_executor;
                    System.Diagnostics.Process p = System.Diagnostics.Process.Start("cmd.exe", "cmd /C \"" + full_command + "\"");
                    p.WaitForExit();

                    string command = "cmd /C \"ren \"" + Application.dataPath.Replace("/", "\\") + "\\outputs\\" + currentRealTime + "\\output.tif\" pass_" + (i) + "_drone_" + (j + 1) + ".tif\"";
                    System.Diagnostics.Process p2 = System.Diagnostics.Process.Start("cmd.exe", command);
                    p2.WaitForExit();
                    System.Diagnostics.Process p3 = System.Diagnostics.Process.Start("cmd.exe", "cmd /C \"ren \"" + Application.dataPath.Replace("/", "\\") + "\\outputs\\" + currentRealTime + "\\output.pto\" pass_" + (i) + "_drone_" + (j + 1) + ".pto\"");
                    p3.WaitForExit();
                }
            }
        }

        DateTime timeEnd = System.DateTime.UtcNow;
        Debug.Log("End Stitch");

        TimeSpan timeTaken = timeEnd - timeStart;
        Debug.Log("Time Taken to Stitch: " + timeTaken.TotalSeconds + " seconds");

        completed = true;
    }
}
