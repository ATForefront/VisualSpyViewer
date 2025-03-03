#nullable enable
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace SwitchControllerVisualizer
{
    public class SwitchControllerProtocolReceiver : IControllerProtocolReceiver
    {
        // COM3
        public readonly string COMPort;

        // 115200
        // 9600
        public readonly int BaudRate;


        // public bool EnablePosition;
        public bool RawProtocolMode;

        SerialPortReceiver _serialPortReceiver;
        Task _receiverTask;

        Action _updateCallBack = () => { };

        SwitchControllerState _controllerState;
        public ControllerState ControllerState => _controllerState;
        Action<string>? DebugLog;

        public SwitchControllerProtocolReceiver(string comPort, int baudRate, Action<string>? debugLog = null)
        {
            COMPort = comPort;
            BaudRate = baudRate;
            DebugLog = debugLog;
            _controllerState = new();

            _serialPortReceiver = new(COMPort, BaudRate, DebugLogCall);
            _receiverTask = Task.Run(ReceiveMainLoop);


            void DebugLogCall(string log) { DebugLog?.Invoke(log); }
        }
        public bool ReceiveContinue = true;
        public void ReceiveMainLoop()
        {
            while (ReceiveContinue)
            {
                try { _serialPortReceiver.Receive(ReceiveToParse); }
                catch (Exception e) { DebugLog?.Invoke(e.ToString()); }
            }
        }
        public void Dispose()
        {
            ReceiveContinue = false;
            _serialPortReceiver.Dispose();
            _serialPortReceiver = null!;
        }

        public ICallBackHandler RegisterUpdateCallBack(Action callback)
        {
            return new CallBackHandler(this, callback);
        }
        class CallBackHandler : ICallBackHandler
        {
            SwitchControllerProtocolReceiver _target;
            Action _callBack;
            bool disposed = false;
            public CallBackHandler(SwitchControllerProtocolReceiver target, Action callback) { _target = target; _callBack = callback; _target._updateCallBack += _callBack; }
            public void Dispose()
            {
                // なぜ必要なのかわからない ... VSCode の拡張が壊れてんのかな？
#pragma warning disable CS8601 // Possible null reference assignment.
                if (disposed is false) _target._updateCallBack -= _callBack;
#pragma warning restore CS8601 // Possible null reference assignment.
                disposed = true;
            }
        }

        public DeserializeResult LastDeserializeResult;
        public SwitchControllerRawState LastRawState;

        public Quaternion RawModeGyroValue = Quaternion.Identity;
        // public Vector3 AccelVector3;
        // public Vector3 GyroVector3;
        public Quaternion QuaternionModeBaseRotation = Quaternion.Identity;
        // public Quaternion GyroQuaternion;
        public int MaxIndex;
        public int DeltaTime;
        void ReceiveToParse(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0) { DebugLog?.Invoke("Data is empty"); return; }
            LastDeserializeResult = ProtocolDeserializer.TryDeserialize(bytes, out var newRawState);

            if (LastDeserializeResult is DeserializeResult.Source)
            {
                ReadFromRawState(newRawState);
                _updateCallBack();
            }
            else
            {
                DebugLog?.Invoke(LastDeserializeResult.ToString());
            }
        }

        public void ReadFromRawState(SwitchControllerRawState newRawState)
        {
            LastRawState = newRawState;

            var stdState = _controllerState.Standard;

            stdState.A = LastRawState.ButtonStateRight.A;
            stdState.B = LastRawState.ButtonStateRight.B;
            stdState.X = LastRawState.ButtonStateRight.X;
            stdState.Y = LastRawState.ButtonStateRight.Y;

            stdState.Right = LastRawState.ButtonStateLeft.Right;
            stdState.Left = LastRawState.ButtonStateLeft.Left;
            stdState.Up = LastRawState.ButtonStateLeft.Up;
            stdState.Down = LastRawState.ButtonStateLeft.Down;

            stdState.R = LastRawState.ButtonStateRight.R;
            stdState.ZR = LastRawState.ButtonStateRight.ZR;

            stdState.L = LastRawState.ButtonStateLeft.L;
            stdState.ZL = LastRawState.ButtonStateLeft.ZL;

            stdState.SystemL = LastRawState.ButtonStateShared.Minus;
            stdState.SystemR = LastRawState.ButtonStateShared.Plus;

            stdState.LStickClick = LastRawState.ButtonStateShared.LStick;
            stdState.RStickClick = LastRawState.ButtonStateShared.RStick;

            stdState.LStickX = Normalize(LastRawState.AnalogStickStateLeft.Horizontal);
            stdState.LStickY = Normalize(LastRawState.AnalogStickStateLeft.Vertical);

            stdState.RStickX = Normalize(LastRawState.AnalogStickStateRight.Horizontal);
            stdState.RStickY = Normalize(LastRawState.AnalogStickStateRight.Vertical);

            float Normalize(ushort val)
            {
                return (((float)val / 4096) - 0.5f) * 2f;
            }

            var switchExtension = _controllerState._switchControllerExtension;
            switchExtension.Capture = LastRawState.ButtonStateShared.Capture;
            switchExtension.Home = LastRawState.ButtonStateShared.Home;

            // var deltaTime = DeltaTime = AccGyroParser.GetDeltaTime(beforeState, State);
            // if (AccelOnlyMode)
            // {
            //     var vec = AccGyroParser.RawToVec(newRawState.AccGyro1).posVec;
            //     var eRot = AccGyroParser.AccToRot(vec);
            //     transform.localRotation = Quaternion.Euler(eRot);
            //     AccelVector3 = vec;
            //     GyroVector3 = eRot;
            // }
            // else
            // {
            if (RawProtocolMode)
            {
                var vec = AccGyroParser.RawToVec(LastRawState.AccGyro1);
                var radRot = vec.rotVex * (float)(Math.PI / 180f);
                RawModeGyroValue *= Quaternion.CreateFromYawPitchRoll(radRot.X, radRot.Y, radRot.Z);
                _controllerState._gyroExtension.ReadFromQuaternion(RawModeGyroValue);

                // if (EnablePosition) { transform.localPosition += transform.localRotation * vec.posVec; }
                // AccelVector3 = vec.posVec;
                // GyroVector3 = vec.rotVex;

                if (LastRawState.ButtonStateRight.Y) { _controllerState._gyroExtension.ReadFromQuaternion(Quaternion.Identity); }
            }
            else
            {
                var resultFFI = QuaternionParsee(LastRawState);
                if (resultFFI.is_ok)
                {
                    var quat = resultFFI.zero;
                    var gyroQuaternion = new Quaternion(quat.y, quat.z * -1, quat.x * -1, quat.w);

                    if (LastRawState.ButtonStateRight.Y) { QuaternionModeBaseRotation = Quaternion.Inverse(gyroQuaternion); }

                    _controllerState._gyroExtension.ReadFromQuaternion(QuaternionModeBaseRotation * gyroQuaternion);
                }
            }
            // }
        }
        private static JoyconQuat.QuaternionParseResultFFI QuaternionParsee(SwitchControllerRawState state)
        {
            Span<short> gyroBuffer = stackalloc short[9];
            gyroBuffer[0] = state.AccGyro1.GyroX;
            gyroBuffer[1] = state.AccGyro1.GyroY;
            gyroBuffer[2] = state.AccGyro1.GyroZ;
            gyroBuffer[3] = state.AccGyro2.GyroX;
            gyroBuffer[4] = state.AccGyro2.GyroY;
            gyroBuffer[5] = state.AccGyro2.GyroZ;
            gyroBuffer[6] = state.AccGyro3.GyroX;
            gyroBuffer[7] = state.AccGyro3.GyroY;
            gyroBuffer[8] = state.AccGyro3.GyroZ;

            unsafe
            {
                fixed (short* ptr = gyroBuffer)
                {
                    return JoyconQuat.NativeMethod.quaternion_parse((byte*)ptr);
                }
            }
        }
        class SwitchControllerState : ControllerState
        {
            public SwitchControllerState() : base(new()) { }

            public SwitchControllerExtension _switchControllerExtension = new();
            public GyroExtension _gyroExtension = new();

            public override T? GetExtentState<T>() where T : default
            {
                if (_switchControllerExtension is T t) { return t; }
                if (_gyroExtension is T t2) { return t2; }
                return default;
            }
        }


    }

}


/*
-------------------------------------------------------------------
USB Sniffer Lite. Built on May 31 2024 12:03:08.

Settings:
  e - Capture speed       : Full
  g - Capture trigger     : Disabled
  l - Capture limit       : Unlimited
  t - Time display format : Relative to the SOF
  a - Data display format : Full
  f - Fold empty frames   : Enabled

Commands:
  h - Print this help message
  b - Display buffer
  s - Start capture
  p - Stop capture
*/
