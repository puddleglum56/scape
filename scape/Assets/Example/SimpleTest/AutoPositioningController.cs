using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using BluetoothUtil.BluetoothBytes;
using DistanceUtil;

[System.Serializable] public class BluetoothControllerGetBytes: SerializableCallback<string[], BluetoothBytes> {}

public class DistanceMatrix
{
	//TODO 
	//handle no value yet (safe get)
	//actually use dictionary right
	public Dictionary<string, Dictionary<string, int>> FromToDistance; 
	public Dictionary<string, Dictionary<string, int>> FromToQuality; 

	void PutFromToDistance(string fromNode, string toNode, int distance)
	{
		FromToDistance[fromNode][toNode] = distance;
	}

	void PutFromToQuality(string fromNode, string toNode, int quality) 
	{
		FromToQuality[fromNode][toNode] = quality;
	}

	void PutFromToBoth(string fromNode, string toNode, int distance, int quality)
	{
		FromToDistance[fromNode][toNode] = distance;
		FromToQuality[fromNode][toNode] = quality;
	}

	int GetFromToDistance(string fromNode, string toNode)
	{
		return FromToDistance[fromNode][toNode];
		
	}

	int GetFromToQuality(string fromNode, string toNode)
	{
		return FromToQuality[fromNode][toNode];
		
	}

	int GetFromToBoth(string fromNode, string toNode)
	{
		return FromToQuality[fromNode][toNode];
		
	}
}

public class AutoPositioningController: MonoBehaviour
{
    public string TagName = "DW5B19";
    public string[] AnchorNames = new string[] {"DW51A7","DW8428", "DW8E23", "DW092D" };

    public string ServiceUUID = "680c21d9-c946-4c1f-9c11-baa1c21329e7";
    public string SubscribeCharacteristic = "003bbdf2-c634-4b3d-ab56-7ec889b89a37";


    // Start is called before the first frame update
    void Start()
    {
		private DistanceMatrix distanceMatrix = new DistanceMatrix();


		foreach (string anchorName in AnchorNames)
		{
			string[] accessorStrings = {anchorName, ServiceUUID, SubscribeCharacteristic};
			BluetoothBytes bluetoothBytes = BluetoothControllerGetBytes.Invoke(accessorStrings);
			foreach (string otherAnchor in OtherAnchors(anchorName))
			{
				int distance = InfoForNodeName(otherAnchor, bluetoothBytes, 4)
				distanceMatrix.PutFromToDistance(anchorName, otherAnchor, distance) 

			}
		}


        
    }

	string[] OtherAnchors(string thisAnchor)
	{
		string[] otherAnchors = new string[AnchorNames.Length - 1];
		int c = 0;
		foreach (string anchor in AnchorNames)
		{
			if (anchor != thisAnchor)
			{
				otherAnchors[c] = anchor;
			}
			c++;
		}

		return otherAnchors;
	}

    // Update is called once per frame
    void Update()
    {
        
    }


}
