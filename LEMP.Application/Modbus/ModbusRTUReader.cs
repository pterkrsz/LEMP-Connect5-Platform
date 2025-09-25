using System;
using System.IO;
using System.IO.Ports;

namespace LEMP.Application.Modbus;

public class ModbusRTUReader : IDisposable
{
    private readonly SerialPort _port;

    public ModbusRTUReader(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
    {
        _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };
        _port.Open();
    }

    protected ModbusRTUReader(SerialPort port, bool openPort)
    {
        _port = port ?? throw new ArgumentNullException(nameof(port));

        if (openPort && !_port.IsOpen)
        {
            _port.Open();
        }

        if (_port.IsOpen)
        {
            _port.ReadTimeout = 1000;
            _port.WriteTimeout = 1000;
        }
    }

    public bool TryRead<T>(RegisterReadRequest<T> request)
    {
        try
        {
            if (!TryReadRegisters(request.SlaveId, request.FunctionCode, request.StartAddress,
                    request.RegisterCount, out var data))
            {
                return false;
            }

            var littleEndian = (byte[])data.Clone();
            Array.Reverse(littleEndian);
            T value = ConvertBytes<T>(littleEndian);
            request.OnValue?.Invoke(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public virtual bool TryReadRegisters(byte slaveId, byte functionCode, ushort startAddress, ushort registerCount,
        out byte[] data)
    {
        data = Array.Empty<byte>();
        try
        {
            if (registerCount == 0)
            {
                return false;
            }

            if ((functionCode == 3 || functionCode == 4) && registerCount > 125)
            {
                return false;
            }

            var frame = BuildFrame(slaveId, functionCode, startAddress, registerCount);
            _port.DiscardInBuffer();
            _port.Write(frame, 0, frame.Length);

            int dataBytesLength = functionCode == 1 || functionCode == 2
                ? (int)Math.Ceiling(registerCount / 8.0)
                : registerCount * 2;
            int responseLength = 5 + dataBytesLength;
            var response = new byte[responseLength];
            int bytesRead = 0;
            while (bytesRead < responseLength)
            {
                int read = _port.Read(response, bytesRead, responseLength - bytesRead);
                if (read == 0)
                    throw new IOException("No data received");
                bytesRead += read;
            }

            if (response[0] != slaveId || response[1] != functionCode)
                return false;
            if (!ValidateCrc(response))
                return false;

            if (functionCode == 1 || functionCode == 2)
            {
                data = new byte[dataBytesLength];
                Array.Copy(response, 3, data, 0, dataBytesLength);
            }
            else
            {
                var byteCount = response[2];
                if (byteCount != dataBytesLength)
                {
                    return false;
                }
                data = new byte[byteCount];
                Array.Copy(response, 3, data, 0, byteCount);
            }

            return true;
        }
        catch
        {
            data = Array.Empty<byte>();
            return false;
        }
    }

    private static byte[] BuildFrame(byte slaveId, byte functionCode, ushort startAddress, ushort registerCount)
    {
        var frame = new byte[8];
        frame[0] = slaveId;
        frame[1] = functionCode;
        frame[2] = (byte)(startAddress >> 8);
        frame[3] = (byte)(startAddress & 0xFF);
        frame[4] = (byte)(registerCount >> 8);
        frame[5] = (byte)(registerCount & 0xFF);
        var crc = CalculateCrc(frame, 6);
        frame[6] = crc[0];
        frame[7] = crc[1];
        return frame;
    }

    private static bool ValidateCrc(byte[] frame)
    {
        var length = frame.Length;
        var crc = CalculateCrc(frame, length - 2);
        return frame[length - 2] == crc[0] && frame[length - 1] == crc[1];
    }

    private static byte[] CalculateCrc(byte[] data, int length)
    {
        ushort crc = 0xFFFF;
        for (int pos = 0; pos < length; pos++)
        {
            crc ^= data[pos];
            for (int i = 0; i < 8; i++)
            {
                bool lsb = (crc & 0x0001) != 0;
                crc >>= 1;
                if (lsb)
                    crc ^= 0xA001;
            }
        }
        return new byte[] { (byte)(crc & 0xFF), (byte)(crc >> 8) };
    }

    private static T ConvertBytes<T>(byte[] data)
    {
        if (typeof(T) == typeof(float))
            return (T)(object)BitConverter.ToSingle(data, 0);
        if (typeof(T) == typeof(ushort))
            return (T)(object)BitConverter.ToUInt16(data, 0);
        if (typeof(T) == typeof(bool))
            return (T)(object)(data[0] != 0);
        throw new NotSupportedException($"Type {typeof(T)} is not supported");
    }

    public void Dispose()
    {
        if (_port.IsOpen)
            _port.Close();
        _port.Dispose();
    }
}
