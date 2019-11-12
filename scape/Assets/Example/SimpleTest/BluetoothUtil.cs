using System.Collections;
using System.Collections.Generic;

public class BluetoothBytes 
{
	public byte[] _bytes;
	string _hexString;

	void FromBytes(byte[] _bytes) 
	{
		_hexString = ToHex()
	}

	void FromString(string _hexString) 
	{
		_bytes = HexStringToBytes(_hexString)
	}

	string ToBytes()
	{
		return _bytes;
	}

    string ToHex()
    {
        return BitConverter.ToString(_bytes).Replace("-", "");
    }

	int ToInt32()
	{
        //Array.Reverse(bytes); // Bluetooth convention is little-endian so need to reverse order to big-endian
        return BitConverter.ToInt32(_bytes, 0);
	}

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


}
