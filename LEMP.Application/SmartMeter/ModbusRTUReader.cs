using System;
using System.IO;
using System.IO.Ports;

namespace LEMP.Application.SmartMeter;

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

    public bool TryRead<T>(RegisterReadRequest<T> request)
    {
        try
        {
            var frame = BuildFrame(request);
            _port.DiscardInBuffer();
            _port.Write(frame, 0, frame.Length);

            int dataBytesLength = request.FunctionCode == 1 || request.FunctionCode == 2
                ? (int)Math.Ceiling(request.RegisterCount / 8.0)
                : request.RegisterCount * 2;
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

            if (response[0] != request.SlaveId || response[1] != request.FunctionCode)
                return false;
            if (!ValidateCrc(response))
                return false;

            byte[] data;
            if (request.FunctionCode == 1 || request.FunctionCode == 2)
            {
                data = new byte[] { response[3] };
            }
            else
            {
                var byteCount = response[2];
                data = new byte[byteCount];
                Array.Copy(response, 3, data, 0, byteCount);
            }

            Array.Reverse(data);
            T value = ConvertBytes<T>(data);
            request.OnValue?.Invoke(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] BuildFrame<T>(RegisterReadRequest<T> request)
    {
        var frame = new byte[8];
        frame[0] = request.SlaveId;
        frame[1] = request.FunctionCode;
        frame[2] = (byte)(request.StartAddress >> 8);
        frame[3] = (byte)(request.StartAddress & 0xFF);
        frame[4] = (byte)(request.RegisterCount >> 8);
        frame[5] = (byte)(request.RegisterCount & 0xFF);
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
