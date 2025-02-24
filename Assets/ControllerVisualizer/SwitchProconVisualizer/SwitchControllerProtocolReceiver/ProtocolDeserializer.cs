#nullable enable
using System;
using System.Buffers.Binary;
using System.Numerics;

namespace SwitchControllerVisualizer
{
    public static class ProtocolDeserializer
    {
        public static DeserializeResult TryDeserialize(ReadOnlySpan<byte> bytes, out SwitchControllerRawState state)
        {
            state = new();

            if (bytes[0] is not 0x30) { return DeserializeResult.OtherData; }

            state.Timer = bytes[1];
            state.RawBatteryVal = bytes[2];
            state.BatterLevel = (BatterLevel)(state.RawBatteryVal >> 4);//これは正しいのか ... ?
            state.ConnectionInfo = (ConnectionInfo)((state.RawBatteryVal >> 1) & 3);
            state.ButtonStateRight = new(bytes[3]);
            state.ButtonStateShared = new(bytes[4]);
            state.ButtonStateLeft = new(bytes[5]);
            state.AnalogStickStateLeft = new(bytes.Slice(6, 3));
            state.AnalogStickStateRight = new(bytes.Slice(9, 3));
            state.AccGyro1 = new(bytes.Slice(13, 12));
            state.AccGyro2 = new(bytes.Slice(13 + 12, 12));
            state.AccGyro3 = new(bytes.Slice(13 + 12 + 12, 12));

            return DeserializeResult.Source;
        }
    }

    [Serializable]
    public struct SwitchControllerRawState
    {
        public byte Timer;
        public byte RawBatteryVal;
        public BatterLevel BatterLevel;
        public ConnectionInfo ConnectionInfo;
        public ButtonStateRight ButtonStateRight;
        public ButtonStateShared ButtonStateShared;
        public ButtonStateLeft ButtonStateLeft;
        public AnalogStickState AnalogStickStateLeft;
        public AnalogStickState AnalogStickStateRight;
        public AccGyro AccGyro1;
        public AccGyro AccGyro2;
        public AccGyro AccGyro3;
    }
    public enum BatterLevel
    {
        Full = 8,
        Medium = 6,
        Low = 4,
        Critical = 2,
        Empty = 0,

        LSB = -1,
    }
    public enum ConnectionInfo
    {
        JC = 3,
        ProOrChrGrip = 0,
        SwitchOrUSB = 1,
    }
    [Serializable]
    public struct ButtonStateRight
    {
        public bool Y;
        public bool X;
        public bool B;
        public bool A;
        public bool SR;
        public bool SL;
        public bool R;
        public bool ZR;

        public ButtonStateRight(byte v)
        {
            Y = ((v >> 0) & 1) == 1;
            X = ((v >> 1) & 1) == 1;
            B = ((v >> 2) & 1) == 1;
            A = ((v >> 3) & 1) == 1;
            SR = ((v >> 4) & 1) == 1;
            SL = ((v >> 5) & 1) == 1;
            R = ((v >> 6) & 1) == 1;
            ZR = ((v >> 7) & 1) == 1;
        }
    }

    [Serializable]
    public struct ButtonStateShared
    {
        public bool Minus;
        public bool Plus;
        public bool RStick;
        public bool LStick;
        public bool Home;
        public bool Capture;
        public bool Missing;
        public bool ChargingGrip;

        public ButtonStateShared(byte v)
        {
            Minus = ((v >> 0) & 1) == 1;
            Plus = ((v >> 1) & 1) == 1;
            RStick = ((v >> 2) & 1) == 1;
            LStick = ((v >> 3) & 1) == 1;
            Home = ((v >> 4) & 1) == 1;
            Capture = ((v >> 5) & 1) == 1;
            Missing = ((v >> 6) & 1) == 1;
            ChargingGrip = ((v >> 7) & 1) == 1;
        }
    }

    [Serializable]
    public struct ButtonStateLeft
    {
        public bool Down;
        public bool Up;
        public bool Right;
        public bool Left;
        public bool SR;
        public bool SL;
        public bool L;
        public bool ZL;

        public ButtonStateLeft(byte v)
        {
            Down = ((v >> 0) & 1) == 1;
            Up = ((v >> 1) & 1) == 1;
            Right = ((v >> 2) & 1) == 1;
            Left = ((v >> 3) & 1) == 1;
            SR = ((v >> 4) & 1) == 1;
            SL = ((v >> 5) & 1) == 1;
            L = ((v >> 6) & 1) == 1;
            ZL = ((v >> 7) & 1) == 1;
        }
    }
    [Serializable]
    public struct AnalogStickState
    {
        public ushort Horizontal;
        public ushort Vertical;
        public AnalogStickState(ReadOnlySpan<byte> bytes)
        {
            Horizontal = (ushort)(bytes[0] | ((bytes[1] & 0xf) << 8));
            Vertical = (ushort)((bytes[1] >> 4) | (bytes[2] << 4));
        }
    }
    [Serializable]
    public struct AccGyro
    {
        public short AccelX;
        public short AccelY;
        public short AccelZ;
        public short GyroX;
        public short GyroY;
        public short GyroZ;

        public AccGyro(ReadOnlySpan<byte> readOnlySpan)
        {
            AccelX = BinaryPrimitives.ReadInt16LittleEndian(readOnlySpan.Slice(0, 2));
            AccelY = BinaryPrimitives.ReadInt16LittleEndian(readOnlySpan.Slice(2, 2));
            AccelZ = BinaryPrimitives.ReadInt16LittleEndian(readOnlySpan.Slice(4, 2));
            GyroX = BinaryPrimitives.ReadInt16LittleEndian(readOnlySpan.Slice(6, 2));
            GyroY = BinaryPrimitives.ReadInt16LittleEndian(readOnlySpan.Slice(8, 2));
            GyroZ = BinaryPrimitives.ReadInt16LittleEndian(readOnlySpan.Slice(10, 2));
        }
    }

    public enum DeserializeResult
    {
        None = 0,
        Source = 1,
        OtherData = 2,
    }
}
