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
public class Movement2CSVRealtime : MonoBehaviour
{
    private InputData _inputData;
    StreamWriter clearVel;
    StreamWriter clearPathRealtime;
    StreamWriter clearPathCollected;
    //StreamWriter clearAccel;
    StreamWriter writerVel;
    StreamWriter writerAccel;
    StreamWriter writerPathRealtime;
    StreamWriter writerPathCollected;
    listedPath listedPath; //Helps us organize the csv file

    private static string filePathRealtime = "S:\\realtimePath.csv"; 
    private static string filePathCollected = "S:\\collectedPath.csv"; 
    private static float collectTime = .01f;
    private static float writeTime = .1f; //The amount of time we allow a user to write a path
    private static bool startPath = false; //Creates a latch for the trigger, turns true when first pressed and off when time runs out
    private static bool endOfRun = false; //Just a variable to know that we have ended the data collection

    private static float robotXCoord = 650.0f; //These are set to a specified origin for the robot
    private static float lowXLim = 400.0f; 
    private static float highXLim = 1050.0f;

    private static float robotYCoord = 0.0f;
    private static float lowYLim = -700.0f; 
    private static float highYLim = 700.0f;

    private static float robotZCoord = 350.0f;
    private static float lowZLim = 250.0f; 
    private static float highZLim = 600.0f;

    private static float userXCoordRY = 0.0f; //Defining a seperate set of coords for user defined movement
    private static float userYCoordRZ = 0.0f; //The "RZ" indicates that in the robot coordinate space it is the z-axis
    private static float userZCoordRX = 0.0f;

    private static float scaleFactorX = 0.0f; //For experimental purposes, I think I will bring this back
    private static float scaleFactorY = 0.0f;
    private static float scaleFactorZ = 0.0f;

    private static int index = 0;

    private static float maxSpan = 750.0f; //This represents the max length users are allowed to move

    private static double[] robotSpanArrayX = Generate.LinearSpaced(1000, lowXLim, highXLim);
    private static double[] robotSpanArrayY = Generate.LinearSpaced(1000, lowYLim, highYLim);
    private static double[] robotSpanArrayZ = Generate.LinearSpaced(1000, lowZLim, highZLim);

    private static double[] userSpanArray; //The array for the user span
    private static bool calibrated = false; //Keeps running the calibration protocol until true
    private static bool maxCalibed = false; //Both show if we calibrated max or min
    private static bool minCalibed = false;
    
    private static int trigCount = 0;
    //private static bool interrupt = false; //Allows us to interrupt the path
    private static bool initCleared = false; //Keeps running clear protocol until true
    //private static bool loopCleared = false; //This is the latch that clears the file every time a frame is ran

    private static bool initCalibed = false;

    private static float maxDist = 0.0f;//Stores max wingspan
    private static float minDist = 0.0f;//Stores min wingspan
    CsvWriter csvVel;
    CsvWriter csvPathRealtime;
    CsvWriter csvPathCollected;

//I want to translate the indexing of Unity to that of the robot
//Current Assumption Unity->Robot
//Y->Z
//X->Y
//Z->X
    private static int xIndex = 1; 
    private static int yIndex = 2; //These are robot coordinates as well
    private static int zIndex = 0;


    List<listedPath> path = new List<listedPath>{}; //Where we store path data to then be written
    List<listedPath> realtimePath = new List<listedPath>{};

    //I'm also going to define a initial set of coordinates that correspond to the starting position of the robot, I will have users 
    //do a calibration for that
    private static bool calibInit(Vector3 position, bool trigger) 
    {
        if (trigger)
        { 
            userXCoordRY = position[yIndex];
            userYCoordRZ = position[zIndex];
            userZCoordRX = position[xIndex];
            return true;
        }
        return false;
    }

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
        writeTime -= Time.deltaTime;
        collectTime -= Time.deltaTime;;

        _inputData._rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger);
        _inputData._rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed);
        _inputData._rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bPressed);
        
        if (trigger && !endOfRun && !initCleared)
            {
                //Clears the velocity files
                clearVel = new StreamWriter("Assets\\FFOSControllerData\\Velocity.csv");
                clearVel.WriteLine("");
                clearVel.Close();

                //Clears path file
                clearPathRealtime = new StreamWriter(filePathRealtime);
                clearPathRealtime.WriteLine("path_index, point_index, x, y, z");
                clearPathRealtime.Close();

                clearPathCollected = new StreamWriter(filePathCollected);
                clearPathCollected.WriteLine("path_index, point_index, x, y, z");
                clearPathCollected.Close();

                //Begins writer
                writerVel = new StreamWriter("Assets\\FFOSControllerData\\Velocity.csv");
                writerAccel = new StreamWriter("Assets\\FFOSControllerData\\Accel.csv");

                Debug.Log("Entering calibration!");
                //We need to calibrate the coordinates that they will exist within the realm of the robot
                initCleared = true;
            }

        if (initCleared && !endOfRun)
        {
            _inputData._rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);

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
                Debug.Log("Now Bring it to a comfortable position in front of you!");

                //Setting the scale factor
                scaleFactorX = (float)((robotSpanArrayX[1] - robotSpanArrayX[0])/(userSpanArray[1] - userSpanArray[0]));
                scaleFactorY = (float)((robotSpanArrayY[1] - robotSpanArrayY[0])/(userSpanArray[1] - userSpanArray[0]));
                scaleFactorZ = (float)((robotSpanArrayZ[1] - robotSpanArrayZ[0])/(userSpanArray[1] - userSpanArray[0]));

                trigCount++;
            }

        else if (maxCalibed && minCalibed && trigCount == 3 && !initCalibed)
            {
                initCalibed = calibInit(position, aPressed);

                if (initCalibed){
                    trigCount++;
                    calibrated = true;

                    robotXCoord = userZCoordRX;
                    robotYCoord = userXCoordRY;
                    robotZCoord = userYCoordRZ; //Just for visualization sake

                    Debug.Log("Calibrated!");
                }
                //Getting an initial calibration

                trigCount++;
            }
        }

         if (trigCount == 4 && trigger && calibrated)
        {
            startPath = true;
            trigCount++;
        }

        //Creates instances of the CSV helper tool
        csvVel = new CsvWriter(writerVel, CultureInfo.InvariantCulture);
        csvPathCollected = new CsvWriter(writerPathCollected, CultureInfo.InvariantCulture);

        if ((_inputData._rightController.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 rightVelocity) && startPath && initCleared && calibrated))
            {

                //We decrement the write time every time we call a frame, once we hit <0, we write
                // writeTime -= Time.deltaTime;
                //I will try to write to it as many times as possible

                //I'm writing the velocity as a 1x3 dimension to read easier
                var velocity = new List<float>();
                
                velocity.Add(rightVelocity[xIndex]);
                velocity.Add(rightVelocity[yIndex]);
                velocity.Add(rightVelocity[zIndex]);

                //The relative user position that can be defined
                // userXCoord = userXCoord + (velocity[0] * Time.deltaTime);
                // userYCoord = userYCoord + (velocity[1] * Time.deltaTime);
                // userZCoord = userZCoord + (velocity[2] * Time.deltaTime);

                //The written variable may not be the actual position we want, keep that in mind
                float robotXCoordWrite = 0.0f;
                float robotYCoordWrite = 0.0f;
                float robotZCoordWrite = 0.0f;

                robotXCoord = robotXCoord + (velocity[xIndex] * Time.deltaTime) * scaleFactorX;
                robotYCoord = robotYCoord + (velocity[yIndex] * Time.deltaTime) * scaleFactorY;
                robotZCoord = robotZCoord + (velocity[zIndex] * Time.deltaTime) * scaleFactorZ; 

                //Ensure that we don't cross singularity limits
                if (robotXCoord > highXLim) {
                    robotXCoordWrite = highXLim;
                }
                else if (robotYCoord > highYLim) {
                    robotYCoordWrite = highYLim;
                }
                else if (robotZCoord > highZLim) {
                    robotZCoordWrite = highZLim;
                }

                else if (robotXCoord < lowXLim) {
                    robotXCoordWrite = lowXLim;
                }
                else if (robotYCoord < lowYLim) {
                    robotYCoordWrite = lowYLim;
                }
                else if (robotXCoord < lowZLim) {
                    robotZCoordWrite = lowZLim;
                }
                else {
                robotXCoordWrite = robotXCoord;
                robotYCoordWrite = robotYCoord;
                robotZCoordWrite = robotZCoord;

                }

                //CSVHelper works best with objects contained within a class
                listedPath pathRow = new listedPath(0, index, robotXCoordWrite, robotYCoordWrite, robotZCoordWrite);

                if(collectTime <= 0)
                {
                //Maintaining a log of velocity just for redundancy
                csvVel.WriteRecords(velocity);
                csvVel.NextRecord();
                csvVel.NextRecord();
                writerVel.Flush();
                Debug.Log("Vel Written");
                
                //Here we only add the coordinates to a list, not to the document yet
                path.Add(pathRow);
                realtimePath.Add(pathRow);

                collectTime = .02f;
                index++; //We index the path index every time
                }

                if(writeTime <= 0 && index >= 3){
                clearPathRealtime = new StreamWriter(filePathRealtime);
                clearPathRealtime.WriteLine("path_index, point_index, x, y, z");
                clearPathRealtime.Close();

                writerPathRealtime = new StreamWriter(filePathRealtime);
                csvPathRealtime = new CsvWriter(writerPathRealtime, CultureInfo.InvariantCulture);
                csvPathRealtime.WriteRecords(realtimePath);

                writerPathRealtime.Flush();
                writerPathRealtime.Close();

                realtimePath.Clear();
                writeTime = .2f;
                index = 0;
                }

            }

        // else if ((pathTime <= 0 && !endOfRun))
        // {
        //     startPath = false;
        //     writerVel.Close();

        //     //We then write the path in one fell swoop, this prevents the header from being written each time
        //     csvPathCollected.WriteRecords(path);
        //     writerPathCollected.Flush();
        //     Debug.Log("Collected Path Written, NumCoords = " + index.ToString());

        //     writerPathRealtime.Close();
        //     Debug.Log("Done!");
        //     endOfRun = true;
        // }
    }
}
