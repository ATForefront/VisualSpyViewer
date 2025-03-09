#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

namespace SwitchControllerVisualizer.KeyInputLogger
{
    /*
        これ単体だと監視し続けて差分を吐き出し続ける存在
        これを KeyInputDisplay につなぐことで初めてビジュアライズできる。
        差分の取得自体に Unity はいらないので C# generic 。
    */

    public class KeyLogger : IControllerVisualizer , IDisposable
    {
        public event Action<KeyCode> InputLog = k => { };
        public enum KeyCode
        {
            Unknown = 0,

            A = 1,
            B = 2,
            X = 3,
            Y = 4,

            Up = 5,
            Down = 6,
            Right = 7,
            Left = 8,

            R = 9,
            L = 10,
            ZR = 11,
            ZL = 12,

            SystemR = 13,
            SystemL = 14,

            RStickClick = 15,
            LStickClick = 16,

            Capture = 17,
            Home = 18,
        }

        Dictionary<KeyCode, bool> _keyState = new();
        IDisposable? _previousCallBackHandler = null;
        IControllerProtocolReceiver? _controllerProtocolReceiver = null;
        public void SetControllerState(IControllerProtocolReceiver? controllerState)
        {
            _previousCallBackHandler?.Dispose();
            _keyState.Clear();
            _previousCallBackHandler = controllerState?.RegisterUpdateCallBack(Update);
            _controllerProtocolReceiver = controllerState;
        }

        void Update()
        {
            if (_controllerProtocolReceiver == null) { return; }
            var state = _controllerProtocolReceiver.ControllerState;

            var stdState = state.Standard;
            {
                UpdateButton(KeyCode.A, stdState.A);
                UpdateButton(KeyCode.B, stdState.B);
                UpdateButton(KeyCode.X, stdState.X);
                UpdateButton(KeyCode.Y, stdState.Y);

                UpdateButton(KeyCode.Up, stdState.Up);
                UpdateButton(KeyCode.Down, stdState.Down);
                UpdateButton(KeyCode.Right, stdState.Right);
                UpdateButton(KeyCode.Left, stdState.Left);

                UpdateButton(KeyCode.R, stdState.R);
                UpdateButton(KeyCode.L, stdState.L);
                UpdateButton(KeyCode.ZR, stdState.ZR);
                UpdateButton(KeyCode.ZL, stdState.ZL);

                UpdateButton(KeyCode.SystemR, stdState.SystemR);
                UpdateButton(KeyCode.SystemL, stdState.SystemL);

                UpdateButton(KeyCode.RStickClick, stdState.RStickClick);
                UpdateButton(KeyCode.LStickClick, stdState.LStickClick);
            }

            var switchExtraState = state.GetExtentState<SwitchControllerExtension>();
            if (switchExtraState is not null)
            {
                UpdateButton(KeyCode.Capture, switchExtraState.Capture);
                UpdateButton(KeyCode.Home, switchExtraState.Home);
            }
        }
        void UpdateButton(KeyCode keyCode, bool state)
        {
            if (_keyState.TryGetValue(keyCode, out var previousState) is false) { _keyState[keyCode] = state; return; }
            if (state == previousState) { return; }
            _keyState[keyCode] = state;
            if (state) InputLog?.Invoke(keyCode);
        }

        public void Dispose()
        {
            SetControllerState(null);
        }
    }
}
