using System;
using System.Collections.Generic;
using System.Linq;
using LEMP.Application.Modbus;
using LEMP.Domain.Inverter;

namespace LEMP.Application.Inverter;

public class InverterModbusAdapter
{
    private readonly ModbusRTUReader _reader;
    private readonly byte _slaveId;
    private readonly IReadOnlyList<ReadBatch> _plan;

    public InverterModbusAdapter(
        ModbusRTUReader reader,
        IReadOnlyDictionary<string, List<DeyeModbusRegisterDefinition>> groups,
        byte slaveId)
    {
        _reader = reader;
        _slaveId = slaveId;
        _plan = BuildReadPlan(groups);
        if (_plan.Count == 0)
        {
            throw new InvalidOperationException("No readable Modbus registers were generated from the inverter map.");
        }
    }

    public InverterSnapshot ReadSnapshot()
    {
        var snapshot = new InverterSnapshot
        {
            InverterAlive = true,
            Timestamp = DateTimeOffset.UtcNow
        };

        var valuesByGroup = new Dictionary<string, Dictionary<string, InverterRegisterValue>>(StringComparer.OrdinalIgnoreCase);

        foreach (var batch in _plan)
        {
            if (!_reader.TryReadRegisters(_slaveId, batch.FunctionCode, batch.StartAddress, batch.RegisterCount, out var data))
            {
                snapshot.InverterAlive = false;
                continue;
            }

            var span = data.AsSpan();
            if (span.Length < batch.ExpectedByteLength)
            {
                snapshot.InverterAlive = false;
                continue;
            }

            foreach (var slice in batch.Registers)
            {
                var offset = slice.ByteOffset;
                if (offset < 0 || offset + slice.Definition.ByteLength > span.Length)
                {
                    snapshot.InverterAlive = false;
                    continue;
                }

                var segment = span.Slice(offset, slice.Definition.ByteLength);
                if (!slice.Definition.TryConvert(segment, out var value))
                {
                    snapshot.InverterAlive = false;
                    continue;
                }

                if (!valuesByGroup.TryGetValue(batch.Group, out var groupValues))
                {
                    groupValues = new Dictionary<string, InverterRegisterValue>(StringComparer.OrdinalIgnoreCase);
                    valuesByGroup[batch.Group] = groupValues;
                }

                groupValues[slice.Definition.Name] = CreateRegisterValue(slice.Definition, value);
            }
        }

        foreach (var group in valuesByGroup)
        {
            if (group.Value.Count > 0)
            {
                snapshot.Groups[group.Key] = group.Value;
            }
        }

        if (snapshot.Groups.Count == 0)
        {
            snapshot.InverterAlive = false;
        }

        return snapshot;
    }

    private static IReadOnlyList<ReadBatch> BuildReadPlan(
        IReadOnlyDictionary<string, List<DeyeModbusRegisterDefinition>> groups)
    {
        var batches = new List<ReadBatch>();

        foreach (var group in groups.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var ordered = group.Value
                .OrderBy(r => r.FunctionCode)
                .ThenBy(r => r.Address)
                .ThenBy(r => r.Length)
                .ToList();

            foreach (var functionGroup in ordered.GroupBy(r => r.FunctionCode))
            {
                ReadBatch? current = null;
                foreach (var definition in functionGroup)
                {
                    if (current == null || !current.CanAdd(definition))
                    {
                        current = new ReadBatch(group.Key, definition.FunctionCode, definition.Address);
                        if (!current.CanAdd(definition))
                        {
                            current = null;
                            continue;
                        }

                        current.Add(definition);
                        batches.Add(current);
                        continue;
                    }

                    current.Add(definition);
                }
            }
        }

        return batches;
    }

    private static InverterRegisterValue CreateRegisterValue(DeyeModbusRegisterDefinition definition, double scaledValue)
    {
        var scale = definition.Scale;
        double rawValue;
        if (Math.Abs(scale) > double.Epsilon)
        {
            rawValue = scaledValue / scale;
        }
        else
        {
            rawValue = scaledValue;
        }

        if (double.IsNaN(rawValue) || double.IsInfinity(rawValue))
        {
            rawValue = scaledValue;
        }

        return new InverterRegisterValue(
            scaledValue,
            rawValue,
            definition.DataType,
            definition.Scale,
            definition.Unit);
    }

    private sealed class ReadBatch
    {
        private const int MaxRegistersPerRequest = 120;
        private const int MaxGapBetweenRegisters = 8;

        public ReadBatch(string group, byte functionCode, ushort startAddress)
        {
            Group = group;
            FunctionCode = functionCode;
            StartAddress = startAddress;
        }

        public string Group { get; }

        public byte FunctionCode { get; }

        public ushort StartAddress { get; }

        public ushort RegisterCount { get; private set; }

        public List<RegisterSlice> Registers { get; } = new();

        public int ExpectedByteLength => RegisterCount * 2;

        public bool CanAdd(DeyeModbusRegisterDefinition definition)
        {
            if (definition.FunctionCode != FunctionCode)
            {
                return false;
            }

            if (definition.Length <= 0)
            {
                return false;
            }

            if (definition.Address < StartAddress)
            {
                return false;
            }

            if (Registers.Count > 0)
            {
                var nextRegisterStart = StartAddress + RegisterCount;
                if (definition.Address > nextRegisterStart &&
                    definition.Address - nextRegisterStart > MaxGapBetweenRegisters)
                {
                    return false;
                }
            }

            var requiredCount = definition.Address + definition.Length - StartAddress;
            return requiredCount <= MaxRegistersPerRequest;
        }

        public void Add(DeyeModbusRegisterDefinition definition)
        {
            if (Registers.Count == 0)
            {
                RegisterCount = definition.Length;
            }
            else
            {
                var requiredCount = definition.Address + definition.Length - StartAddress;
                if (requiredCount > RegisterCount)
                {
                    RegisterCount = (ushort)requiredCount;
                }
            }

            var byteOffset = (definition.Address - StartAddress) * 2;
            Registers.Add(new RegisterSlice(definition, byteOffset));
        }
    }

    private sealed record RegisterSlice(DeyeModbusRegisterDefinition Definition, int ByteOffset);
}
