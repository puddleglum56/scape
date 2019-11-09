﻿/* This is a simple example to show the steps and one possible way of
 * automatically scanning for and connecting to a device to receive
 * notification data from the device.
 */

using System;
using System.Collections.Generic;
using UnityEngine;



public class SimpleTest : MonoBehaviour
{
    public string DeviceName = "DW5B19";
    public string TagName = "DW5B19";
    public string InitiatorName = "DW51A7";
    public string[] AnchorNames = new string[] { "DW51A7", "DW8428", "DW8E23", "DW092D" };
    public string ServiceUUID = "680c21d9-c946-4c1f-9c11-baa1c21329e7";
    public string SubscribeCharacteristic = "003bbdf2-c634-4b3d-ab56-7ec889b89a37";

    enum States
    {
        None,
        Scan,
        ScanRSSI,
        Connect,
        Subscribe,
        Unsubscribe,
        Disconnect,
    }

    private bool _connected = false;
    private float _timeout = 0f;
    private States _state = States.None;
    private string _deviceAddress;
    private bool _foundSubscribeID = false;
    private bool _foundWriteID = false;
    private byte[] _dataBytes = null;
    private bool _rssiOnly = false;
    private int _rssi = 0;

    private int[] prevDistances = null;
    private float[] prevVelocities = null;
    float prevTime = 0f;
    private float[] _accelerations = null;

    private float LowPassKernelWidthInSeconds = 0.1f;
    private float AccelerometerUpdateInterval = 1.0f / 60.0f;
    private Vector3 lowPassAcceleration = Vector3.zero;
    private float DistanceUpdateInterval = 0.3f; //note: verify the update interval
    private int[] lowPassDistances = null;


    void Reset()
    {
        _connected = false;
        _timeout = 0f;
        _state = States.None;
        _deviceAddress = null;
        _foundSubscribeID = false;
        _foundWriteID = false;
        _dataBytes = null;
        _rssi = 0;
    }

    void SetState(States newState, float timeout)
    {
        _state = newState;
        _timeout = timeout;
    }

    void StartProcess()
    {
        Reset();
        BluetoothLEHardwareInterface.Initialize(true, false, () =>
        {

            SetState(States.Scan, 0.1f);

        }, (error) =>
        {

            BluetoothLEHardwareInterface.Log("Error during initialize: " + error);
        });
    }

    // Use this for initialization
    void Start()
    {
        StartProcess();
        lowPassAcceleration = Input.acceleration;
    }

    // Update is called once per frame
    void Update()
    {
        if (_timeout > 0f)
        {
            _timeout -= Time.deltaTime;
            if (_timeout <= 0f)
            {
                _timeout = 0f;

                switch (_state)
                {
                    case States.None:
                        break;

                    case States.Scan:
                        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null, (address, name) =>
                        {

                            // if your device does not advertise the rssi and manufacturer specific data
                            // then you must use this callback because the next callback only gets called
                            // if you have manufacturer specific data

                            if (!_rssiOnly)
                            {
                                if (name.Contains(DeviceName))
                                {
                                    BluetoothLEHardwareInterface.StopScan();

                                    // found a device with the name we want
                                    // this example does not deal with finding more than one
                                    _deviceAddress = address;
                                    SetState(States.Connect, 0.5f);
                                }
                            }

                        }, (address, name, rssi, bytes) =>
                        {

                            // use this one if the device responses with manufacturer specific data and the rssi

                            if (name.Contains(DeviceName))
                            {
                                if (_rssiOnly)
                                {
                                    _rssi = rssi;
                                }
                                else
                                {
                                    BluetoothLEHardwareInterface.StopScan();

                                    // found a device with the name we want
                                    // this example does not deal with finding more than one
                                    _deviceAddress = address;
                                    SetState(States.Connect, 0.5f);
                                }
                            }

                        }, _rssiOnly); // this last setting allows RFduino to send RSSI without having manufacturer data

                        if (_rssiOnly)
                            SetState(States.ScanRSSI, 0.5f);
                        break;

                    case States.ScanRSSI:
                        break;

                    case States.Connect:
                        // set these flags
                        _foundSubscribeID = false;
                        _foundWriteID = false;

                        // note that the first parameter is the address, not the name. I have not fixed this because
                        // of backwards compatiblity.
                        // also note that I am note using the first 2 callbacks. If you are not looking for specific characteristics you can use one of
                        // the first 2, but keep in mind that the device will enumerate everything and so you will want to have a timeout
                        // large enough that it will be finished enumerating before you try to subscribe or do any other operations.
                        BluetoothLEHardwareInterface.ConnectToPeripheral(_deviceAddress, null, null, (address, serviceUUID, characteristicUUID) =>
                        {

                            if (IsEqual(serviceUUID, ServiceUUID))
                            {
                                _foundSubscribeID = _foundSubscribeID || IsEqual(characteristicUUID, SubscribeCharacteristic);

                                // if we have found both characteristics that we are waiting for
                                // set the state. make sure there is enough timeout that if the
                                // device is still enumerating other characteristics it finishes
                                // before we try to subscribe
                                if (_foundSubscribeID)
                                {
                                    _connected = true;
                                    SetState(States.Subscribe, 2f);
                                }
                            }
                        });
                        break;

                    case States.Subscribe:
                        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(_deviceAddress, ServiceUUID, SubscribeCharacteristic, null, (address, characteristicUUID, bytes) =>
                        {

                            // we don't have a great way to set the state other than waiting until we actually got
                            // some data back. For this demo with the rfduino that means pressing the button
                            // on the rfduino at least once before the GUI will update.
                            _state = States.None;

                            // we received some data from the device
                            _dataBytes = bytes;
                            if (_dataBytes != null)
                            {
                                int[] distances = new int[AnchorNames.Length];
                                float[] velocities = new float[AnchorNames.Length];
                                float[] accelerations = new float[AnchorNames.Length];
                                float deltaTime = Time.time - prevTime;
                                prevTime = Time.time;

                                for (int i = 0; i < AnchorNames.Length; i++)
                                {
                                    distances[i] = DistanceToNode(NodeNameToNodeId(AnchorNames[i]), _dataBytes);
                                }


                                if (prevDistances != null)
                                {
                                    distances = LowPassFilterDistances(distances);
                                    for (int i = 0; i < distances.Length; i++)
                                    {
                                        velocities[i] = getVelocity(distances[i], prevDistances[i], deltaTime);
                                    }
                                    prevDistances = distances;

                                    if (prevVelocities != null)
                                    {
                                        for (int i = 0; i < velocities.Length; i++)
                                        {
                                            accelerations[i] = getAcceleration(velocities[i], prevVelocities[i], deltaTime);
                                        }
                                        _accelerations = accelerations;
                                        prevVelocities = velocities;
                                    }
                                    else
                                    {
                                        prevVelocities = velocities;
                                    }
                                }
                                else
                                {
                                    prevDistances = distances;
                                    lowPassDistances = distances;

                                }
                            }
                        });
                        break;

                    case States.Unsubscribe:
                        BluetoothLEHardwareInterface.UnSubscribeCharacteristic(_deviceAddress, ServiceUUID, SubscribeCharacteristic, null);
                        SetState(States.Disconnect, 4f);
                        break;

                    case States.Disconnect:
                        if (_connected)
                        {
                            BluetoothLEHardwareInterface.DisconnectPeripheral(_deviceAddress, (address) =>
                            {
                                BluetoothLEHardwareInterface.DeInitialize(() =>
                                {

                                    _connected = false;
                                    _state = States.None;
                                });
                            });
                        }
                        else
                        {
                            BluetoothLEHardwareInterface.DeInitialize(() =>
                            {

                                _state = States.None;
                            });
                        }
                        break;
                }
            }
        }
    }

    Vector3 LowPassFilterAccelerometer()
    {
        float lowPassFilterFactor = AccelerometerUpdateInterval / LowPassKernelWidthInSeconds;
        lowPassAcceleration = Vector3.Lerp(lowPassAcceleration, Input.acceleration, lowPassFilterFactor);
        return lowPassAcceleration;
    }

    int[] LowPassFilterDistances(int[] distances)
    {
        /*
        float lowPassFilterFactor = DistanceUpdateInterval / LowPassKernelWidthInSeconds;
        Debug.Log(lowPassFilterFactor);
        Debug.Log(lowPassDistances[0]);
        Debug.Log(distances[0]);
        Debug.Log(Mathf.Lerp((float)lowPassDistances[0], (float)distances[0], lowPassFilterFactor));
        for (int i = 0; i < distances.Length; i++)
        {
            lowPassDistances[i] = (int) Mathf.Lerp((float) lowPassDistances[i], (float) distances[i], lowPassFilterFactor);

        }
        return lowPassDistances;
        */
        return distances;
    }

    private bool ledON = false;
    /*
	public void OnLED ()
	{
		ledON = !ledON;
		if (ledON)
		{
			SendByte ((byte)0x01);
		}
		else
		{
			SendByte ((byte)0x00);
		}
	}
    */

    string FullUUID(string uuid)
    {
        return "0000" + uuid + "-0000-1000-8000-00805f9b34fb";
    }

    bool IsEqual(string uuid1, string uuid2)
    {
        if (uuid1.Length == 4)
            uuid1 = FullUUID(uuid1);
        if (uuid2.Length == 4)
            uuid2 = FullUUID(uuid2);

        return (uuid1.ToUpper().CompareTo(uuid2.ToUpper()) == 0);
    }

    /*
    void SendByte (byte value)
	{
		byte[] data = new byte[] { value };
		BluetoothLEHardwareInterface.WriteCharacteristic (_deviceAddress, ServiceUUID, WriteCharacteristic, data, data.Length, true, (characteristicUUID) => {
			
			BluetoothLEHardwareInterface.Log ("Write Succeeded");
		});
	}
    */

    byte[] HexStringToBytes(string hexString)
    {
        byte[] bytes = new byte[hexString.Length / 2];

        int c = 0;
        for (int i = 0; i < (hexString.Length); i += 2)
        {
            string hexChar = String.Concat(hexString[i], hexString[i + 1]);
            byte byteRepresentation = byte.Parse(hexChar, System.Globalization.NumberStyles.AllowHexSpecifier);
            bytes[c] = byteRepresentation;
            c++;
        }
        return bytes;
    }

    int BytesToInt(byte[] bytes)
    {
        //Array.Reverse(bytes); // Bluetooth convention is little-endian so need to reverse order to big-endian
        return BitConverter.ToInt32(bytes, 0);
    }

    string BytesToHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "");
    }

    int DistanceToNode(string nodeIdSequence, byte[] distanceData)
    {
        string distanceDataHex = BytesToHex(distanceData);
        int nodeIndex = distanceDataHex.IndexOf(nodeIdSequence);
        int distanceLengthInBytes = 4;
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

    float getVelocity(int currentDistance, int prevDistance, float deltaTime)
    {
        return ((float)currentDistance - (float)prevDistance) / deltaTime;
    }

    float getAcceleration(float currentVelocity, float prevVelocity, float deltaTime)
    {
        return (currentVelocity - prevVelocity) / deltaTime;
    }

    void OnGUI()
    {
        GUI.skin.textArea.fontSize = 32;
        GUI.skin.button.fontSize = 32;
        GUI.skin.toggle.fontSize = 32;
        GUI.skin.label.fontSize = 32;

        if (_connected)
        {
            if (_state == States.None)
            {
                if (GUI.Button(new Rect(10, 10, Screen.width - 10, 100), "Disconnect"))
                    SetState(States.Unsubscribe, 1f);

                /*
				if (GUI.Button (new Rect (10, 210, Screen.width - 10, 100), "Write Value"))
					OnLED ();
                */

                if (_dataBytes != null)
                {


                    string data = "";

                    for (int i = 0; i < AnchorNames.Length; i++)
                    {
                        data += String.Concat(AnchorNames[i], " ", _accelerations[i], "\n");
                    }
                    /*
                    foreach (string anchorName in AnchorNames)
                    {
                        data += String.Concat(anchorName," ", DistanceToNode(NodeNameToNodeId(anchorName), _dataBytes),"\n");
                    }
                    */
                    Vector3 gravity = Input.gyro.gravity;
                    Vector3 acceleration = LowPassFilterAccelerometer() - gravity;
                    data += String.Concat("x ", acceleration.x, "\n");
                    data += String.Concat("y ", acceleration.y, "\n");
                    data += String.Concat("z ", acceleration.z, "\n");

                    GUI.TextArea(new Rect(10, 400, Screen.width - 10, Screen.height - 200), data);
                    transform.rotation = GyroToUnity(Input.gyro.attitude);
                }
            }
            else if (_state == States.Subscribe && _timeout == 0f)
            {
                GUI.TextArea(new Rect(50, 100, Screen.width - 100, Screen.height - 200), "Press the button on the RFduino");
            }
        }
        else if (_state == States.ScanRSSI)
        {
            if (GUI.Button(new Rect(10, 10, Screen.width - 10, 100), "Stop Scanning"))
            {
                BluetoothLEHardwareInterface.StopScan();
                SetState(States.Disconnect, 0.5f);
            }

            if (_rssi != 0)
                GUI.Label(new Rect(10, 300, Screen.width - 10, 50), string.Format("RSSI: {0}", _rssi));
        }
        else if (_state == States.None)
        {
            if (GUI.Button(new Rect(10, 10, Screen.width - 10, 100), "Connect"))
                StartProcess();

            _rssiOnly = GUI.Toggle(new Rect(10, 200, Screen.width - 10, 50), _rssiOnly, "Just Show RSSI");
        }
    }

    private static Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }

}
