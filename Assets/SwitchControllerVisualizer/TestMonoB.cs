using System;
using System.IO.Ports;
using System.Text;
using UnityEngine;

namespace SwitchControllerVisualizer
{
    public class TestMonoB : MonoBehaviour
    {
        // COM3
        public string COMPort;

        // 115200
        // 9600
        public int BaudRate;
        SerialPortReceiver _serialPortReceiver;

        public bool EnablePosition;
        public bool RawProtocolMode;
        public bool AccelOnlyMode;

        public void Start()
        {
            Init();
        }

        [ContextMenu("Restart")]
        public void Init()
        {
            _serialPortReceiver?.Dispose();
            _serialPortReceiver = new(COMPort, BaudRate);
        }

        [ContextMenu("ResetPose")]
        public void ResetPose()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void OnDestroy()
        {
            _serialPortReceiver.Dispose();
        }
        public void Update()
        {
            if (_serialPortReceiver is null) { return; }
            _serialPortReceiver.Receive(ReceiveToParse);
        }
        void ReceiveToParse(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0) { Debug.Log("Data is empty"); return; }
            DeserializeResult = ProtocolDeserializer.TryDeserialize(bytes, out var state);

            if (DeserializeResult is DeserializeResult.Source)
            {
                var beforeState = State;
                State = state;
                // var deltaTime = DeltaTime = AccGyroParser.GetDeltaTime(beforeState, State);
                if (AccelOnlyMode)
                {
                    var vec = AccGyroParser.RawToVec(State.AccGyro1).posVec;
                    var eRot = AccGyroParser.AccToRot(vec);
                    transform.localRotation = Quaternion.Euler(eRot);
                    AccelVector3 = vec;
                    GyroVector3 = eRot;
                }
                else
                {
                    if (RawProtocolMode)
                    {
                        var vec = AccGyroParser.RawToVec(State.AccGyro1);
                        transform.localRotation *= Quaternion.Euler(vec.rotVex);
                        if (EnablePosition) { transform.localPosition += transform.localRotation * vec.posVec; }
                        AccelVector3 = vec.posVec;
                        GyroVector3 = vec.rotVex;

                        if (State.ButtonStateRight.Y)
                        {
                            ResetPose();
                        }
                    }
                    else
                    {
                        var resultFFI = QuaternionParsee(State);
                        if (resultFFI.is_ok)
                        {
                            var quat = resultFFI.zero;
                            GyroQuaternion = new Quaternion(quat.y, quat.z * -1, quat.x * -1, quat.w);
                        }
                        if (State.ButtonStateRight.Y)
                        {
                            BaseRotation = Quaternion.Inverse(GyroQuaternion);
                        }
                        transform.localRotation = BaseRotation * GyroQuaternion;
                    }

                }
            }
        }

        private static JoyconQuat.QuaternionParseResultFFI QuaternionParsee(SwitchControllerState state)
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

        public DeserializeResult DeserializeResult;
        public SwitchControllerState State;
        public Vector3 AccelVector3;
        public Vector3 GyroVector3;
        public Quaternion BaseRotation;
        public Quaternion GyroQuaternion;
        public int MaxIndex;
        public int DeltaTime;

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
