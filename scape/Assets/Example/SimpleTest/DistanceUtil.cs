using System.Collections;
using System.Collections.Generic;
using BluetoothUtil.BluetoothBytes;

int InfoForNodeSequence(string nodeIdSequence, BluetoothBytes distanceData, int distanceLengthInBytes)
{
	string distanceDataHex = distanceData.ToHex();
	int nodeIndex = distanceDataHex.IndexOf(nodeIdSequence);
	byte[] nodeDistanceBytes = new byte[distanceLengthInBytes];
	for (int i = 0; i < distanceLengthInBytes; i++)
	{
		nodeDistanceBytes[i] = distanceData[nodeIndex / 2 + nodeIdSequence.Length / 2 + i];
	}
	return BytesToInt(nodeDistanceBytes);
}

string NodeNameToNodeId(string nodeName)
{
	string nodeId = "";
	for (int i = nodeName.Length; i > 2; i -= 2)
	{
		nodeId += String.Concat(nodeName[i - 2], nodeName[i - 1]);
	}
	return nodeId;

}

int InfoForNodeName(string nodeName, BluetoothBytes distanceData, int distanceLengthInBytes)
{
	return InfoForNodeSequence(NodeNameToNodeId(nodeName), distanceData, distanceLengthInBytes);
}
