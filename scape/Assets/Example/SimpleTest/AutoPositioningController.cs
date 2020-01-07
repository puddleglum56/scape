using System.Collections.Generic;
using UnityEngine;

public class InfoMatrix
{
    //Stores info (currently distance or quality) from each key node to each value node
    //Example: int distanceFromAToB = infoMatrix["a","b"]
    //Can't figure out how to make it like infoMatrix["a"]["b"]

    private Dictionary<string, Dictionary<string, int>> innerDictionary;

    public InfoMatrix(string[] anchorNames)
    {
        innerDictionary = new Dictionary<string, Dictionary<string, int>>();
        foreach (string anchorName in anchorNames)
        {
            innerDictionary.Add(anchorName, new Dictionary<string, int>());
            foreach (string otherAnchor in AutoPositioningUtil.OtherAnchors(anchorName, anchorNames))
            {
                innerDictionary[anchorName].Add(otherAnchor, 0);
            }
        }
    }

    public int this[string key1, string key2]
    {
        get {return innerDictionary[key1][key2]; }
        set { innerDictionary[key1][key2] = value; }
    }
}

public class AutoPositioningController: MonoBehaviour
{
    public string TagName = "DW5B19";
    public string[] AnchorNames = new string[] {"DW51A7","DW8428", "DW8E23", "DW092D" };
    public string ServiceUUID = "680c21d9-c946-4c1f-9c11-baa1c21329e7";
    public string SubscribeCharacteristic = "003bbdf2-c634-4b3d-ab56-7ec889b89a37";
    //public string testBytes = "0200000000000000000000000000032D09D60D00006428842F0A000064238E5D0F000064";
    //public string SubscribeCharacteristic = "3f0afd88-7770-46b0-b5e7-9fc099598964";

    public BluetoothConnector BluetoothConnector = null;

    private InfoMatrix distanceMatrix = null;
    private InfoMatrix qualityMatrix = null;

    private int currentAnchorIndex = 0;

    string CurrentAnchorName()
    {
        return AnchorNames[currentAnchorIndex];
    }

    BluetoothBytes CurrentBluetoothBytes()
    {
        return BluetoothBytes.MakeFromBytes(BluetoothConnector.CurrentBytes());
    }

    // Start is called before the first frame update
    void Start()
    {
        // For some reason Decawave says you have to subscribe for the initiator distances, but just read the characteristic from other anchors
        Debug.Log("Autopositioning controller starting");
        BluetoothConnector.InitializeReadOrSubscribeValues(CurrentAnchorName(), ServiceUUID, SubscribeCharacteristic, "subscribe");
        Debug.Log("Initialize called");
        BluetoothConnector.StartProcess();
        distanceMatrix = new InfoMatrix(AnchorNames);
        qualityMatrix = new InfoMatrix(AnchorNames);
    }

    // Update is called once per frame
    void Update()
    {
        BluetoothConnector.States bluetoothConnectorState = BluetoothConnector.currentState();
        string currentAnchorName = AnchorNames[currentAnchorIndex];
        byte[] rawBluetoothBytes = BluetoothConnector.CurrentBytes();

        if (bluetoothConnectorState == BluetoothConnector.States.None && rawBluetoothBytes != null)
        {
            BluetoothBytes bluetoothBytes = CurrentBluetoothBytes(); 
            foreach (string otherAnchor in AutoPositioningUtil.OtherAnchors(currentAnchorName, AnchorNames))
            {
                int distance = DistanceUtil.DistanceToNode(otherAnchor, bluetoothBytes);
                int quality = DistanceUtil.QualityToNode(otherAnchor, bluetoothBytes);
                distanceMatrix[currentAnchorName, otherAnchor] =  distance;
                qualityMatrix[currentAnchorName, otherAnchor] = quality;

            }
            BluetoothConnector.Disconnect();
        }
        if (bluetoothConnectorState == BluetoothConnector.States.Disconnected)
        {
            currentAnchorIndex++;
            BluetoothConnector.InitializeReadOrSubscribeValues(CurrentAnchorName(), ServiceUUID, SubscribeCharacteristic, "read");
            BluetoothConnector.StartProcess();
        }
    }

    void OnGUI()
    {
        GUI.skin.textArea.fontSize = 26;
        GUI.skin.button.fontSize = 32;
        GUI.skin.toggle.fontSize = 32;
        GUI.skin.label.fontSize = 32;

        string data = "";
        foreach (string anchorName in AnchorNames)
        {
            data += "Anchor name: " + anchorName + "\n";
            foreach (string otherAnchor in AutoPositioningUtil.OtherAnchors(anchorName, AnchorNames))
            {
                data += otherAnchor + ": ";
                data += distanceMatrix[anchorName, otherAnchor];
                data += "--";
                data += qualityMatrix[anchorName, otherAnchor];
                data += "\n";

            }
            data += "\n";
        }
        GUI.TextArea(new Rect(0, 0, Screen.width, 600), data);
    }
}

public class AutoPositioningUtil
{
    private static string[] anchorNames = { "DW51A7", "DW8428", "DW8E23", "DW092D" };


    public static string[] OtherAnchors(string thisAnchor, string[] anchorNames)
	{
		string[] otherAnchors = new string[anchorNames.Length - 1];
		int c = 0;
		foreach (string anchor in anchorNames)
		{
			if (anchor != thisAnchor)
			{
				otherAnchors[c] = anchor;
                c++;
			}
		}

		return otherAnchors;
	}

    public static bool CheckOrthogonal(Vector3 position1, Vector3 position2, Vector3 position3, float minimumThirdTrilateralPointAngle)
    {
        float yDiff1 = position1.y - position2.y;
        float xDiff1 = position1.x - position2.x;
        float yDiff2 = position2.y - position3.y;
        float xDiff2 = position2.x - position3.x;

        if (xDiff1 == 0f && xDiff2 == 0f) 
        {
            return false;
        }
        else
        {
            if (Mathf.Abs(Mathf.Atan2(yDiff1, xDiff1) - Mathf.Atan2(yDiff2, xDiff2)) < minimumThirdTrilateralPointAngle)
            {
                return false;
            }
        }
        return true;
    }

}
