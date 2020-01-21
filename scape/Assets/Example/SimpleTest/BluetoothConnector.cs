/* This is a simple example to show the steps and one possible way of
 * automatically scanning for and connecting to a device to receive
 * notification data from the device.
 */

using System;
using UnityEngine;
using System.Linq;

public class BluetoothConnector: MonoBehaviour
{
    private string DeviceName;
    private string ServiceUUID;
    private string ActionCharacteristic;

    public enum States
    {
        None,
        Scan,
        ScanRSSI,
        Connect,
        Read,
        Subscribe,
        Write,
        Unsubscribe,
        Disconnect,
        Disconnected,
    }

    private States[] actionStates =
    {
        States.Read,
        States.Subscribe,
        States.Write
    };

    private bool _initialized = false;
    private bool _connected = false;
    private float _timeout = 0f;
    private States _state = States.None;
    private string _deviceAddress;
    private bool _foundSubscribeID = false;
    private bool _foundWriteID = false;
    byte[] _dataBytes = null;
    byte[] _dataToWrite = null;
    private bool _rssiOnly = false;
    private int _rssi = 0;
    private States _action = States.Subscribe;
    private bool _actionSuccess = false;

    private string StateToString(States state)
    {
        switch (state)
        {
            case States.None: return "None";
            case States.Scan: return "Scan";
            case States.ScanRSSI: return "ScanRSSI";
            case States.Connect: return "Connect";
            case States.Read: return "Read";
            case States.Subscribe: return "Subscribe";
            case States.Write: return "Write";
            case States.Unsubscribe: return "Unsubscribe";
            case States.Disconnect: return "Disconnect";
            case States.Disconnected: return "Disconnected";
            default: return "Default";
        }
    }

    public void Disconnect()
    {
        SetState(States.Unsubscribe, 1f);
    }

	public void InitializeAction(string DeviceName, string ServiceUUID, string ActionCharacteristic, States action, byte[] bytesToWrite = null)
	{
        if (!(actionStates.Contains(action)))
            Debug.Log("WARNING: InitializeAction called with not an action state (ie. not read, write, or subscribe)");
        _dataToWrite = bytesToWrite;
        this.DeviceName = DeviceName;
        this.ServiceUUID = ServiceUUID;
        this.ActionCharacteristic = ActionCharacteristic;
        this._action = action;
        Debug.Log("initialize called for anchor name "+DeviceName+"with action "+StateToString(action));
	}

    public void Reset()
    {
        _connected = false;
        _timeout = 0f;
        _state = States.None;
        _deviceAddress = null;
        _foundSubscribeID = false;
        _foundWriteID = false;
        _dataBytes = null;
        _rssi = 0;
        _action = States.Subscribe;
        _actionSuccess = false;
    }

    void SetState(States newState, float timeout)
    {
        _state = newState;
        _timeout = timeout;
    }

    public void StartProcess()
    {
        Debug.Log("before reset");
        Reset();
        if (!_initialized)
        {
            Debug.Log("before conditional");
            BluetoothLEHardwareInterface.Initialize(true, false, () =>
            {

                Debug.Log("before initialized set");
                _initialized = true;
                SetState(States.Scan, 0.1f);
                Debug.Log("after set state");

            }, (error) =>
            {

                BluetoothLEHardwareInterface.Log("Error during initialize: " + error);
            });
        }
        else
        {
            SetState(States.Scan, 0.1f);
        }
    }

    public States currentAction()
    {
        return _action;
    }

    public bool actionSuccessful()
    {
        return _actionSuccess;
    }

    public byte[] currentBytes()
    {
        return _dataBytes;
    }

    public States currentState()
    {
        return _state;
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
                        Debug.Log("right before the scan");
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
                                _foundSubscribeID = _foundSubscribeID || IsEqual(characteristicUUID, ActionCharacteristic);

                                // if we have found both characteristics that we are waiting for
                                // set the state. make sure there is enough timeout that if the
                                // device is still enumerating other characteristics it finishes
                                // before we try to subscribe
                                if (_foundSubscribeID)
                                {
                                    _connected = true;
                                    SetState(_action, 2f);
                                }
                            }
                        });
                        break;

                    case States.Read:
                        Debug.Log("trying to read " + ActionCharacteristic);
                        BluetoothLEHardwareInterface.ReadCharacteristic(_deviceAddress, ServiceUUID, ActionCharacteristic, (characteristic, bytes) =>
                        {
                            Debug.Log("read complete");
                            _state = States.None;
                            _dataBytes = bytes;
                            _actionSuccess = true;

                        });
                        break;

                    case States.Subscribe:
                        Debug.Log("trying to subscribe " + ActionCharacteristic);
                        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(_deviceAddress, ServiceUUID, ActionCharacteristic, null, (address, characteristicUUID, bytes) =>
                        {

                            // we don't have a great way to set the state other than waiting until we actually got
                            // some data back. For this demo with the rfduino that means pressing the button
                            // on the rfduino at least once before the GUI will update.
                            _state = States.None;

                            // we received some data from the device
                            _dataBytes = bytes;
                            _actionSuccess = true;
                        });
                        break;

                    case States.Write:
                        BluetoothLEHardwareInterface.WriteCharacteristic (_deviceAddress, ServiceUUID, ActionCharacteristic, _dataToWrite, _dataToWrite.Length, true, (characteristicUUID) => {
                            BluetoothLEHardwareInterface.Log ("Write Succeeded");
                            _actionSuccess = true;
                        });
                        break;

                    case States.Unsubscribe:
                        BluetoothLEHardwareInterface.UnSubscribeCharacteristic(_deviceAddress, ServiceUUID, ActionCharacteristic, null);
                        SetState(States.Disconnect, 4f);
                        break;

                    case States.Disconnect:
                        if (_connected)
                        {
                            BluetoothLEHardwareInterface.DisconnectPeripheral(_deviceAddress, (address) =>
                            {
                                _connected = false;
                                _state = States.Disconnected;
                            });
                        }
                        else
                        {
                            _state = States.Disconnected;
                        }
                        break;
                }
            }
        }
    }

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

    void OnGUI()
    {
        GUI.skin.textArea.fontSize = 10;
        GUI.skin.button.fontSize = 32;
        GUI.skin.toggle.fontSize = 32;
        GUI.skin.label.fontSize = 32;



        string data = "";
        data += StateToString(_state) + "\n";
        if (_connected)
        {
            if (_state == States.None)
            {
                if (_dataBytes != null)
                {
                    data += "getting bytes\n";
                    data += BluetoothBytes.MakeFromBytes(_dataBytes).ToHex();
                }
            }

        }
        GUI.TextArea(new Rect(0, Screen.height - 200, Screen.width, 200), data);
    }
}

