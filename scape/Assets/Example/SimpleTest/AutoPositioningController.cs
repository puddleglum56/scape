using System.Collections.Generic;
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

    void PushAnchorPositions()
    {
        foreach (string anchorName in AnchorNames)
        {
            //BluetoothConnector.Disconnect();
            //BluetoothConnector.StartProcess();
            //BluetoothConnector.InitializeWrite(anchorName, ServiceUUID, WriteCharacteristic);
        }
    }

    void OnGUI()
    {
        GUI.skin.textArea.fontSize = 26;
        GUI.skin.button.fontSize = 32;
        GUI.skin.toggle.fontSize = 32;
        GUI.skin.label.fontSize = 32;

        if (GUI.Button(new Rect(10, 10, 100, 50), "0"))
            GetAnchorDistancesForTestPoint(0);

        if (GUI.Button(new Rect(10, 70, 100, 50), "1"))
            GetAnchorDistancesForTestPoint(1);

        if (GUI.Button(new Rect(10, 130, 100, 50), "2"))
            GetAnchorDistancesForTestPoint(2);

        if (GUI.Button(new Rect(10, 190, 100, 50), "3"))
            GetAnchorDistancesForTestPoint(3);

        if (GUI.Button(new Rect(10, 250, 100, 50), "Done"))
        {
            CalculateAnchorPositions();
        }

        if (GUI.Button(new Rect(10, 250, 100, 50), "Disconnect"))
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

        GUI.TextArea(new Rect(10, 310, 100, 50), data);
    }
}
