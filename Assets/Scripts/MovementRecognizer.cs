using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using PDollarGestureRecognizer;
using System.IO;
using UnityEngine.Events;

public class MovementRecognizer : MonoBehaviour
{
    public XRNode inputSource;
    public InputFeatureUsage<bool> inputButton = CommonUsages.triggerButton;
    public float inputThreshold = 0.1f;
    public Transform movementSource;

    public float newPositionThresholdDistance = 0.05f;
    public GameObject debugCubePrefab;

    public bool creationMode = true;
    public string newGestureName;

    public float recognitionThreshold = 0.85f;

    public float minimumGestureDistance = 0.30f;
    public float maximumGestureDistance = 1.20f;

    [System.Serializable]
    public class UnityStringEvent : UnityEvent<string> { }

    public UnityStringEvent OnRecognized;

    private List<Gesture> trainingSet = new List<Gesture>();

    private bool isMoving = false;
    private const string SPELLS_DIRNAME = "Spells";

    private List<Vector3> positionsList = new List<Vector3>();

    // Store the wand's rotation at each recorded point so spells can use
    // the direction the wand was facing during the gesture.
    private List<Quaternion> rotationsList = new List<Quaternion>();

    private List<GameObject> debugCubes = new List<GameObject>();

    void Start()
    {
        string directoryPath = Path.Combine(Application.dataPath, SPELLS_DIRNAME + "/");
        string[] gestureFiles =
            Directory.GetFiles(
                directoryPath,
                "*.xml"
            );

        foreach (var item in gestureFiles)
        {
            trainingSet.Add(
                GestureIO.ReadGestureFromFile(item)
            );
        }
    }

    void Update()
    {
        InputDevice device =
            InputDevices.GetDeviceAtXRNode(inputSource);

        device.TryGetFeatureValue(
            inputButton,
            out bool isPressed
        );

        if (!isMoving && isPressed)
        {
            StartMovement();
        }
        else if (isMoving && !isPressed)
        {
            EndMovement();
        }
        else if (isMoving && isPressed)
        {
            UpdateMovement();
        }
    }

    void StartMovement()
    {
        ClearDebugCubes();

        Debug.Log("Start Movement");

        isMoving = true;

        positionsList.Clear();
        rotationsList.Clear();

        positionsList.Add(
            movementSource.position
        );

        rotationsList.Add(
            movementSource.rotation
        );

        SpawnDebugCube();
    }

    void UpdateMovement()
    {
        Vector3 lastPosition =
            positionsList[positionsList.Count - 1];

        float distanceFromLastPoint =
            Vector3.Distance(
                movementSource.position,
                lastPosition
            );

        if (
            distanceFromLastPoint
            > newPositionThresholdDistance
        )
        {
            positionsList.Add(
                movementSource.position
            );

            rotationsList.Add(
                movementSource.rotation
            );

            SpawnDebugCube();
        }
    }

    void EndMovement()
    {
        Debug.Log("End Movement");

        isMoving = false;

        if (positionsList.Count < 2)
        {
            Debug.Log("Not enough points recorded.");
            return;
        }

        float gestureDistance =
            Vector3.Distance(
                positionsList[0],
                positionsList[positionsList.Count - 1]
            );

        Debug.Log(
            "Gesture Distance: "
            + gestureDistance
        );

        // Reject gestures that are physically too short or too long.
        if (!creationMode)
        {
            if (
                gestureDistance
                < minimumGestureDistance
            )
            {
                Debug.Log(
                    "Gesture rejected: too short."
                );

                return;
            }

            if (
                gestureDistance
                > maximumGestureDistance
            )
            {
                Debug.Log(
                    "Gesture rejected: too long."
                );

                return;
            }
        }

        Point[] pointArray =
            new Point[positionsList.Count];

        for (
            int i = 0;
            i < positionsList.Count;
            i++
        )
        {
            Vector2 screenPoint =
                Camera.main.WorldToScreenPoint(
                    positionsList[i]
                );

            pointArray[i] = new Point(
                screenPoint.x,
                screenPoint.y,
                0
            );
        }

        Gesture newGesture =
            new Gesture(pointArray);

        if (creationMode)
        {
            newGesture.Name =
                newGestureName;

            trainingSet.Add(
                newGesture
            );

            string fileName =
                Application.dataPath
                + "/"
                + SPELLS_DIRNAME
                + "/"
                + newGestureName
                + ".xml";

            GestureIO.WriteGesture(
                pointArray,
                newGestureName,
                fileName
            );

            Debug.Log(
                "Saved gesture: "
                + newGestureName
            );

            ClearDebugCubes();
        }
        else
        {
            Result result =
                PointCloudRecognizer.Classify(
                    newGesture,
                    trainingSet.ToArray()
                );

            Debug.Log(
                result.GestureClass
                + " "
                + result.Score
            );

            if (
                result.Score
                > recognitionThreshold
            )
            {
                Debug.Log(
                    "RECOGNIZED: "
                    + result.GestureClass
                );

                OnRecognized.Invoke(
                    result.GestureClass
                );

                ClearDebugCubes();
            }
            else
            {
                Debug.Log(
                    "Gesture rejected: recognition score too low."
                );
            }
        }
    }

    void SpawnDebugCube()
    {
        if (debugCubePrefab == null)
        {
            return;
        }

        GameObject cube =
            Instantiate(
                debugCubePrefab,
                movementSource.position,
                Quaternion.identity
            );

        debugCubes.Add(cube);
    }

    void ClearDebugCubes()
    {
        foreach (GameObject cube in debugCubes)
        {
            if (cube != null)
            {
                Destroy(cube);
            }
        }

        debugCubes.Clear();
    }

    public Vector3 GetGestureMidpoint()
    {
        if (positionsList.Count == 0)
        {
            return movementSource.position;
        }

        return positionsList[
            positionsList.Count / 2
        ];
    }

    public Quaternion GetGestureMidRotation()
    {
        if (rotationsList.Count == 0)
        {
            return movementSource.rotation;
        }

        return rotationsList[
            rotationsList.Count / 2
        ];
    }
}