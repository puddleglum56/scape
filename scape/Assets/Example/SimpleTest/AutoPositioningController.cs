using System.Collections.Generic;
using System.Collections;
using UnityEngine;

//From https://github.com/gsongsong/mlat
using mlat;
// Distance
using MathNet.Numerics;
// Matrix<>
using MathNet.Numerics.LinearAlgebra;
// DenseMatrix
using MathNet.Numerics.LinearAlgebra.Double;


public class AutoPositioningController: MonoBehaviour
{
    //public string testBytes = "0200000000000000000000000000032D09D60D00006428842F0A000064238E5D0F000064";
    //public string SubscribeCharacteristic = "3f0afd88-7770-46b0-b5e7-9fc099598964";

    public string TagName = "DW5B19";
    public string[] AnchorNames = new string[] {"DW51A7","DW8428", "DW8E23", "DW092D" };
    public string ServiceUUID = "680c21d9-c946-4c1f-9c11-baa1c21329e7";
    public string SubscribeCharacteristic = "003bbdf2-c634-4b3d-ab56-7ec889b89a37";
    public string WriteCharacteristic = "f0f26c9b-2c8c49ac-ab60-fe03def1b40c";

    //hard-coding for now, maybe eventually we can have a better UI for this
    private Matrix<double> knownTagPositions = DenseMatrix.OfArray(new double[,] {
        {0, 0, 0},
        {393, 0, 0},
        {393, 306, 144},
        {0, 306, 144}
    });

    private Matrix<double> bounds = DenseMatrix.OfArray(new double[,]
    {
        {-2000, -1000, 0 },
        { 2000, 4000, 3000}
    });

    public BluetoothConnector BluetoothConnector = null;


    private Dictionary<string, Vector<double>> anchorToTestPoints = new Dictionary<string, Vector<double>>();
    private Dictionary<string, Vector<double>> anchorPositions = new Dictionary<string, Vector<double>>();

    private Dictionary<string, Vector<double>> testAnchorPositions = new Dictionary<string, Vector<double>>
    {
        { "DW51A7", null },
        { "DW8428", null },
        { "DW8E23", null },
        { "DW092D", null },
    };

    void InitializeTestValues()
    {
        testAnchorPositions["DW51A7"] = DenseVector.OfArray(new double[] { 1342, 2393, 1676 });
        testAnchorPositions["DW8428"] = DenseVector.OfArray(new double[] { 1019, 1777, 2792 });
        testAnchorPositions["DW8E23"] = DenseVector.OfArray(new double[] { -1309, -515, 1160 });
        testAnchorPositions["DW092D"] = DenseVector.OfArray(new double[] { 1854, -578, 1026 });

    }

    /*
    void shiftAnchorCoordinatesToPositive()
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        foreach (string anchorName in AnchorNames)
        {
            if (anchorPositions[anchorName][0] > minX)
                minX = anchorPositions[anchorName][0];
                minX = anchorPositions[anchorName][0];
            if (anchorPositions[anchorName][1] > minY)
                minY = anchorPositions[anchorName][1];
                minY = anchorPositions[anchorName][1];
        }
        if (minX < 0)
            foreach (string anchorName in AnchorNames)
            {
                anchorPositions[anchorName][0] += -minX;
                testAnchorPositions[anchorName][0] += -minX;
            }
        if (minY < 0)
            foreach (string anchorName in AnchorNames)
            {
                anchorPositions[anchorName][1] += -minY;
                testAnchorPositions[anchorName][1] += -minY;
            }
    }
    */

    BluetoothBytes CurrentBluetoothBytes()
    {
        return BluetoothBytes.MakeFromBytes(BluetoothConnector.currentBytes());
    }

    // Start is called before the first frame update
    void Start()
    {
        BluetoothConnector.InitializeAction(TagName, ServiceUUID, SubscribeCharacteristic, BluetoothConnector.States.Subscribe);
        BluetoothConnector.StartProcess();
        foreach (string anchorName in AnchorNames)
        {
            anchorToTestPoints[anchorName] = Vector<double>.Build.Dense(knownTagPositions.RowCount);
            anchorPositions[anchorName] = Vector<double>.Build.Dense(3);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    void GetAnchorDistancesForTestPoint(int testPointIndex)
    {
        BluetoothConnector.States bluetoothConnectorAction = BluetoothConnector.currentAction();
        bool bluetoothConnectorSuccess = BluetoothConnector.actionSuccessful();

        if (bluetoothConnectorAction == BluetoothConnector.States.Subscribe && bluetoothConnectorSuccess)
        {
            BluetoothBytes bluetoothBytes = CurrentBluetoothBytes(); 
            foreach (string anchorName in AnchorNames)
            {
                int distance = DistanceUtil.DistanceToNode(anchorName, bluetoothBytes);
                anchorToTestPoints[anchorName][testPointIndex] = (double)distance;
            }
        }
    }

    void CalculateAnchorPositions()
    {
        //TODO: some assertion to make sure the ranges dict is full
        foreach (string anchorName in AnchorNames)
        {
            MLAT.GdescentResult result = MLAT.mlat(knownTagPositions, anchorToTestPoints[anchorName], bounds);
            anchorPositions[anchorName] = result.estimator;
        }
    }

    IEnumerator PushAnchorPositions()
    {
        BluetoothConnector.Disconnect();
        yield return new WaitUntil(() => BluetoothConnector.currentState() == BluetoothConnector.States.Disconnected);
        BluetoothConnector.Reset();

        foreach (string anchorName in AnchorNames)
        {
            Debug.Log("pushing anchor " + anchorName);
            BluetoothBytes positionBytes = BluetoothBytes.MakeFromLocation((int)anchorPositions[anchorName][0], (int)anchorPositions[anchorName][1], (int)anchorPositions[anchorName][2], 100);
            Debug.Log("would push " + positionBytes.ToHex());
            byte[] anchorPositionBytes = positionBytes.ToBytes();
            BluetoothConnector.InitializeAction(anchorName, ServiceUUID, WriteCharacteristic, BluetoothConnector.States.Write, bytesToWrite: anchorPositionBytes );
            BluetoothConnector.StartProcess();
            yield return new WaitUntil(() => BluetoothConnector.actionSuccessful());
            BluetoothConnector.Disconnect();
            yield return new WaitUntil(() => BluetoothConnector.currentState() == BluetoothConnector.States.Disconnected);
            BluetoothConnector.Reset();

        }
    }

    void OnGUI()
    {
        GUI.skin.textArea.fontSize = 20;
        GUI.skin.button.fontSize = 20;
        GUI.skin.toggle.fontSize = 20;
        GUI.skin.label.fontSize = 20;

        if (GUI.Button(new Rect(0, 10, Screen.width, 100), "0"))
            GetAnchorDistancesForTestPoint(0);

        if (GUI.Button(new Rect(0, 120, Screen.width, 100), "1"))
            GetAnchorDistancesForTestPoint(1);

        if (GUI.Button(new Rect(0, 230, Screen.width, 100), "2"))
            GetAnchorDistancesForTestPoint(2);

        if (GUI.Button(new Rect(0, 340, Screen.width, 100), "3"))
            GetAnchorDistancesForTestPoint(3);

        if (GUI.Button(new Rect(0, 450, Screen.width, 100), "Done"))
        {
            CalculateAnchorPositions();
            StartCoroutine(PushAnchorPositions());
        }

        if (GUI.Button(new Rect(0, 560, Screen.width, 100), "Disconnect"))
            BluetoothConnector.Disconnect();

        string data = "";
        if (anchorPositions != null)
        {
            foreach (string anchorName in AnchorNames)
            {
                data += anchorName;
                data += ": ";
                foreach (double component in anchorPositions[anchorName])
                {
                    data += component.ToString() + ",";
                }
                data += "\n";
            }
        }

        GUI.TextArea(new Rect(0, 670, Screen.width, 300), data);

        data = "";
        if (anchorToTestPoints != null)
        {
            foreach (string anchorName in AnchorNames)
            {
                data += anchorName;
                data += ": ";
                foreach (double component in anchorToTestPoints[anchorName])
                {
                    data += component.ToString() + ",";
                }
                data += "\n";
            }
        }

        GUI.TextArea(new Rect(0, 980, Screen.width, 300), data);
    }
}
