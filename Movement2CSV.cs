using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using TMPro;
using CsvHelper;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using static listedPath;
using MathNet.Numerics;
using static System.Array;

[RequireComponent(typeof(InputData))]
public class Movement2csvVel : MonoBehaviour
{
    private InputData _inputData;

    StreamWriter clearVel;
    StreamWriter clearPath;
    //StreamWriter clearAccel;
    StreamWriter writerVel;
    StreamWriter writerAccel;
    StreamWriter writerPath;
    listedPath listedPath; //Helps us organize the csv file

    private static float pathTime = 10.0f; //The amount of time we allow a user to write a path
    private static float writeTime = .01f; //The amount of time we allow a user to write a path
    private static bool startPath = false; //Creates a latch for the trigger, turns true when first pressed and off when time runs out
    private static bool endOfRun = false; //Just a variable to know that we have ended the data collection
    private static float robotXCoord = 650.0f; //These are set to a specified origin for the robot
    private static float lowXLim = 500.0f; 
    private static float highXLim = 850.0f;
    private static float robotYCoord = 0.0f;
    private static float lowYLim = -600.0f; 
    private static float highYLim = 600.0f;
    private static float robotZCoord = 350.0f;
    private static float lowZLim = 200.0f; 
    private static float highZLim = 750.0f;
    private static float userXCoordRY = 0.0f; //Defining a seperate set of coords for user defined movement
    private static float userYCoordRZ = 0.0f; //The "RZ" indicates that in the robot coordinate space it is the z-axis
    private static float userZCoordRX = 0.0f;
    private static float scaleFactorX = 0.0f; //For experimental purposes, I think I will bring this back
    private static float scaleFactorY = 0.0f;
    private static float scaleFactorZ = 0.0f;
    private static int index = 0;

    private static double[] robotSpanArrayX = Generate.LinearSpaced(1000, lowXLim, highXLim);
    private static double[] robotSpanArrayY = Generate.LinearSpaced(1000, lowYLim, highYLim);
    private static double[] robotSpanArrayZ = Generate.LinearSpaced(1000, lowZLim, highZLim);

    private static double[] userSpanArray; //The array for the user span
    private static bool calibrated = false; //Keeps running the calibration protocol until true
    private static bool maxCalibed = false; //Both show if we calibrated max or min
    private static bool minCalibed = false;
    private static bool initCalibed = false;
    private static int trigCount = 0;
    private static bool cleared = false; //Keeps running clear protocol until true

    private static float maxDist = 0.0f;//Stores max wingspan
    private static float minDist = 0.0f;//Stores min wingspan
    CsvWriter csvVel;

    //I'm also going to define a initial set of coordinates that correspond to the starting position of the robot, I will have users 
    //do a calibration for that
    private static bool calibInit(Vector3 position, bool trigger) 
    {
        if (trigger)
        { 
            userXCoordRY = position[2];
            userYCoordRZ = position[3];
            userZCoordRX = position[1];
            return true;
        }
        return false;
    }

    //CsvWriter csvAccel; We frankly do not move fast enough to record accel data
    CsvWriter csvPath;

    List<listedPath> path = new List<listedPath>{}; //Where we store path data to then be written

    private static bool calibMax(Vector3 position, bool trigger) 
    {
        if (trigger)
        {
            maxDist = (float)Math.Sqrt(Math.Pow(position[0], 2) + Math.Pow(position[1], 2) + Math.Pow(position[2], 2));
            Debug.Log("Max span collected!");
            return true;
        }
        return false;
    }

    private static bool calibMin(Vector3 position, bool trigger){

        if (trigger)
        {
            minDist = (float)Math.Sqrt(Math.Pow(position[0], 2) + Math.Pow(position[1], 2) + Math.Pow(position[2], 2));
            calibrated = true; //Hopefully we exit this calibration loop
            userSpanArray = Generate.LinearSpaced(1000, minDist, maxDist); //Maps 100000 points to user span
            Debug.Log("Min span collected!");
            return true;
        }
        return false;
    }

    private void Start()
    {
        _inputData = GetComponent<InputData>();
    }
    // Update is called once per frame
    private void Update()
    {
        _inputData._rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger);
        
        if (trigger && !endOfRun && !cleared)
            {
                //Clears the velocity files
                clearVel = new StreamWriter("Assets\\FFOSControllerData\\Velocity.csv");
                clearVel.WriteLine("");
                clearVel.Close();

                //Clears path file
                clearVel = new StreamWriter("Assets\\FFOSControllerData\\path.csv");
                clearVel.WriteLine("path_index, point_index, x, y, z");
                clearVel.Close();

                //Begins writer
                writerVel = new StreamWriter("Assets\\FFOSControllerData\\Velocity.csv");
                writerAccel = new StreamWriter("Assets\\FFOSControllerData\\Accel.csv");
                writerPath = new StreamWriter("Assets\\FFOSControllerData\\path.csv");

                Debug.Log("Entering calibration!");
                //We need to calibrate the coordinates that they will exist within the realm of the robot
                cleared = true;
            }

        if (cleared && !endOfRun)
        {
            _inputData._rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
            _inputData._rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed);
            _inputData._rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bPressed);

            if (!maxCalibed)
            {
                Debug.Log("Starting calibration! Hold your arm out as far up you can!");
                maxCalibed = calibMax(position, aPressed);
                if (maxCalibed){
                    trigCount++;
                }
            }
            else if (maxCalibed && !minCalibed && trigCount == 1)
            {
                Debug.Log("Now bring it down as far as possible");
                minCalibed = calibMin(position, bPressed);
                if (minCalibed){
                    trigCount++;
                }
            }
            else if (maxCalibed && minCalibed && trigCount == 2)
            {
                Debug.Log("Bring it to a comfortable position now in front of you!");

                //Setting the scale factor
                scaleFactorX = (float)((robotSpanArrayX[1] - robotSpanArrayX[0])/(userSpanArray[1] - userSpanArray[0]));
                scaleFactorY = (float)((robotSpanArrayY[1] - robotSpanArrayY[0])/(userSpanArray[1] - userSpanArray[0]));
                scaleFactorZ = (float)((robotSpanArrayZ[1] - robotSpanArrayZ[0])/(userSpanArray[1] - userSpanArray[0]));

                trigCount++;
            }

            else if (maxCalibed && minCalibed && trigCount == 3)
            {
                initCalibed = calibInit(position, aPressed);

                if (initCalibed){
                    trigCount++;
                    calibrated = true;
                    Debug.Log("Calibrated!");
                }
                //Getting an initial calibration

                trigCount++;
            }

        }

        if (trigCount == 4 && trigger)
        {
            startPath = true;
        }

        if (startPath && cleared && calibrated && pathTime >= 0)
        {
            pathTime -= Time.deltaTime;        
        }

        //Creates instances of the CSV helper tool
        csvVel = new CsvWriter(writerVel, CultureInfo.InvariantCulture);

        csvPath = new CsvWriter(writerPath, CultureInfo.InvariantCulture);

        if ((_inputData._rightController.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 rightVelocity) && (pathTime >= 0) && startPath && cleared && calibrated))
            {

                //We decrement the write time every time we call a frame, once we hit <0, we write
                writeTime -= Time.deltaTime;

                //I'm writing the velocity as a 1x3 dimension to read easier
                var velocity = new List<float>();
                
                velocity.Add(rightVelocity[0]);
                velocity.Add(rightVelocity[1]);
                velocity.Add(rightVelocity[2]);

                //The relative user position that can be defined
                // userXCoordRY = userXCoordRY + (velocity[0] * Time.deltaTime);
                // userYCoordRZ = userYCoordRZ + (velocity[1] * Time.deltaTime);
                // userZCoordRX = userZCoordRX + (velocity[2] * Time.deltaTime);

                robotXCoord = robotXCoord + (velocity[0] * Time.deltaTime) * scaleFactorX;
                robotYCoord = robotYCoord + (velocity[2] * Time.deltaTime) * scaleFactorY;
                robotZCoord = robotZCoord + (velocity[1] * Time.deltaTime) * scaleFactorZ; 

                if (robotXCoord > highXLim) {
                    robotXCoord = highXLim;
                }
                if (robotYCoord > highYLim) {
                    robotYCoord = highYLim;
                }
                if (robotZCoord > highZLim) {
                    robotZCoord = highZLim;
                }

                if (robotXCoord < lowXLim) {
                    robotXCoord = lowXLim;
                }
                if (robotYCoord < lowYLim) {
                    robotYCoord = lowYLim;
                }
                if (robotXCoord < lowZLim) {
                    robotZCoord = lowZLim;
                }

                // //Now I'm mapping user position to robot position
                // robotXCoord = (float)robotSpanArrayX[closeTo(userXCoordRY, userSpanArray)];
                // robotYCoord = (float)robotSpanArrayY[closeTo(userYCoordRZ, userSpanArray)];
                // robotZCoord = (float)robotSpanArrayZ[closeTo(userZCoordRX, userSpanArray)];

                //CSVHelper works best with objects contained within a class
                listedPath pathRow = new listedPath(0, index, robotXCoord, robotYCoord, robotZCoord);

                if(writeTime <= 0)
                {
                    //Maintaining a log of velocity just for redundancy
                    csvVel.WriteRecords(velocity);
                    csvVel.NextRecord();
                    csvVel.NextRecord();
                    writerVel.Flush();
                    Debug.Log("Vel Written");

                    //Here we only add the coordinates to a list, not to the document yet
                    path.Add(pathRow);
                    index++; //We index the path index every time

                    writeTime = .1f;
                }
            }
        else if (pathTime <= 0 && !endOfRun)
        {
            startPath = false;
            writerVel.Close();

            //We then write the path in one fell swoop, this prevents the header from being written each time
            csvPath.WriteRecords(path);
            writerPath.Flush();
            Debug.Log("Path Written, NumCoords = " + index.ToString());

            writerPath.Close();
            Debug.Log("Done!");
            endOfRun = true;
        }
    }
}
