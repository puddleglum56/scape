using System;
using System.Collections;
using System.Collections.Generic;

public class BluetoothBytes 
{
	public byte[] _bytes;
	string _hexString;

    public BluetoothBytes(byte[] _bytes)
    {
        FromBytes(_bytes);
    }

    public BluetoothBytes(string _hexString)
    {
        FromString(_hexString);
    }

	void FromBytes(byte[] _bytes) 
	{
        this._bytes = _bytes;
        this._hexString = ToHex();
	}

	void FromString(string _hexString) 
	{
        this._hexString = _hexString;
        this._bytes = HexStringToBytes(_hexString);
	}

    public static BluetoothBytes MakeFromBytes(byte[] bytes)
    {
        BluetoothBytes newBluetoothBytes = new BluetoothBytes(bytes);
        return newBluetoothBytes;
    }

	public byte[] ToBytes()
	{
		return _bytes;
	}

    public string ToHex()
    {
        return BitConverter.ToString(_bytes).Replace("-", "");
    }

	public int ToInt()
	{
        return BytesToInt(_bytes);
	}

    public static int BytesToInt(byte[] bytes)
    {
        //Array.Reverse(bytes); // Bluetooth convention is little-endian so need to reverse order to big-endian
        if (bytes.Length > 2) return BitConverter.ToInt32(bytes, 0);
        else if (bytes.Length > 1) return BitConverter.ToInt16(bytes, 0);
        else
        {
            byte[] lfillBytes = { 0x00, 0x00 };
            lfillBytes[0] = bytes[0];
            return BitConverter.ToInt16(lfillBytes, 0);
        }
        
    }

    public static byte[] HexStringToBytes(string hexString)
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

    public int FindHex(string hexBytes)
    {
        return FindBytes(HexStringToBytes(hexBytes));
    }

    public int FindBytes(byte[] bytes)
    {
        var len = bytes.Length;
        var limit = _bytes.Length - len;
        for (var i = 0; i <= limit; i++)
        {
            var k = 0;
            for (; k < len; k++)
            {
                if (bytes[k] != _bytes[i + k]) break;
            }
            if (k == len) return i;
        }
        return -1;
    }
}


