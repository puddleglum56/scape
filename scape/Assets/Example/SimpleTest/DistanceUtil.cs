using System;
using UnityEngine;

public class DistanceUtil
{
    public static int InfoForNodeSequence(string nodeIdSequence, BluetoothBytes rawData, int infoStartIndex, int infoLengthInBytes)
    {
        byte[] rawDataBytes = rawData.ToBytes();
        int nodeIndex = rawData.FindHex(nodeIdSequence);
        byte[] nodeInfoBytes = new byte[infoLengthInBytes];
        for (int i = 0; i < infoLengthInBytes; i++)
        {
            nodeInfoBytes[i] = rawDataBytes[nodeIndex + infoStartIndex + nodeIdSequence.Length/2 + i];
        }
        return BluetoothBytes.BytesToInt(nodeInfoBytes);
    }

    public static string NodeNameToNodeId(string nodeName)
    {
        string nodeId = "";
        for (int i = nodeName.Length; i > 2; i -= 2)
        {
            nodeId += String.Concat(nodeName[i - 2], nodeName[i - 1]);
        }
        return nodeId;

    }

    public static int DistanceToNode(string nodeName, BluetoothBytes distanceData)
    {
        return InfoForNodeSequence(NodeNameToNodeId(nodeName), distanceData, 0, 4);
    }

    public static int QualityToNode(string nodeName, BluetoothBytes distanceData)
    {
        //Debug.Log(distanceData.ToHex());
        return InfoForNodeSequence(NodeNameToNodeId(nodeName), distanceData, 4, 1);
    }

}
