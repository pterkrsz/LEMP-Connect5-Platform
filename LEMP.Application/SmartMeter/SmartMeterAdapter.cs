using System.Collections.Generic;
using LEMP.Domain.SmartMeter;

namespace LEMP.Application.SmartMeter;

public class SmartMeterAdapter
{
    private readonly ModbusRTUReader _reader;

    public SmartMeterAdapter(ModbusRTUReader reader)
    {
        _reader = reader;
    }

    public SmartMeterState ReadSmartMeterState()
    {
        var state = new SmartMeterState();
        bool allOk = true;

        var requests = new List<RegisterReadRequest<float>>
        {
            new() { SlaveId = 1, StartAddress = 0,   RegisterCount = 2, FunctionCode = 4, OnValue = v => state.VoltageLineToNeutral = v },
            new() { SlaveId = 1, StartAddress = 6,   RegisterCount = 2, FunctionCode = 4, OnValue = v => state.Current = v },
            new() { SlaveId = 1, StartAddress = 12,  RegisterCount = 2, FunctionCode = 4, OnValue = v => state.ActivePower = v },
            new() { SlaveId = 1, StartAddress = 18,  RegisterCount = 2, FunctionCode = 4, OnValue = v => state.ApparentPower = v },
            new() { SlaveId = 1, StartAddress = 24,  RegisterCount = 2, FunctionCode = 4, OnValue = v => state.ReactivePower = v },
            new() { SlaveId = 1, StartAddress = 30,  RegisterCount = 2, FunctionCode = 4, OnValue = v => state.PowerFactor = v },
            new() { SlaveId = 1, StartAddress = 70,  RegisterCount = 2, FunctionCode = 4, OnValue = v => state.Frequency = v },
            new() { SlaveId = 1, StartAddress = 72,  RegisterCount = 2, FunctionCode = 4, OnValue = v => state.ImportedActiveEnergy = v },
            new() { SlaveId = 1, StartAddress = 74,  RegisterCount = 2, FunctionCode = 4, OnValue = v => state.ExportedActiveEnergy = v },
            new() { SlaveId = 1, StartAddress = 76,  RegisterCount = 2, FunctionCode = 4, OnValue = v => state.ImportedReactiveEnergy = v },
            new() { SlaveId = 1, StartAddress = 78,  RegisterCount = 2, FunctionCode = 4, OnValue = v => state.ExportedReactiveEnergy = v },
            new() { SlaveId = 1, StartAddress = 342, RegisterCount = 2, FunctionCode = 4, OnValue = v => state.TotalActiveEnergy = v }
        };

        foreach (var req in requests)
        {
            if (!_reader.TryRead(req))
            {
                allOk = false;
            }
        }

        state.SmartMeterAlive = allOk;
        return state;
    }
}
