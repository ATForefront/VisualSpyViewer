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
                    }
                    else
                    {
                        var vec = AccGyroParser.RawMode2ToVec(State);
                        transform.localRotation = vec.rot;
                        if (EnablePosition) { transform.localPosition += transform.localRotation * vec.posVec; }
                        AccelVector3 = vec.posVec;
                        GyroQuaternion = vec.rot;
                        MaxIndex = vec.maxIndex;
                    }
                }

                if (State.ButtonStateRight.Y)
                {
                    ResetPose();
                }
            }
        }
        public DeserializeResult DeserializeResult;
        public SwitchControllerState State;
        public Vector3 AccelVector3;
        public Vector3 GyroVector3;
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
