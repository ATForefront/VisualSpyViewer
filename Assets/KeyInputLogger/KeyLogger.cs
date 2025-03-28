#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace SwitchControllerVisualizer.KeyInputLogger
{
    /*
        これ単体だと監視し続けて差分を吐き出し続ける存在
        これを KeyInputDisplay につなぐことで初めてビジュアライズできる。
        差分の取得自体に Unity はいらないので C# generic 。
    */

    public class KeyLogger : IControllerVisualizer, IDisposable
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

            LStickUp = 19,
            LStickRightUp = 20,
            LStickRight = 21,
            LStickRightDown = 22,
            LStickDown = 23,
            LStickLeftDown = 24,
            LStickLeft = 25,
            LStickLeftUp = 26,

            RStickUp = 27,
            RStickRightUp = 28,
            RStickRight = 29,
            RStickRightDown = 30,
            RStickDown = 31,
            RStickLeftDown = 32,
            RStickLeft = 33,
            RStickLeftUp = 34,
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

                UpdateStick(KeyCode.LStickUp, stdState.LStickX, stdState.LStickY);
                UpdateStick(KeyCode.RStickUp, stdState.RStickX, stdState.RStickY);
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
        void UpdateStick(KeyCode stickUp, float stickX, float stickY)
        {
            var nowStickDirection = (int)Stick2OctDirection(stickX, stickY);

            // OctDirection と KeyCode は同じ順番で並んでいるので数値比較で誤魔化す。
            UpdateButton(stickUp + 0, nowStickDirection == 0);
            UpdateButton(stickUp + 1, nowStickDirection == 1);
            UpdateButton(stickUp + 2, nowStickDirection == 2);
            UpdateButton(stickUp + 3, nowStickDirection == 3);
            UpdateButton(stickUp + 4, nowStickDirection == 4);
            UpdateButton(stickUp + 5, nowStickDirection == 5);
            UpdateButton(stickUp + 6, nowStickDirection == 6);
            UpdateButton(stickUp + 7, nowStickDirection == 7);
        }



        public void Dispose()
        {
            SetControllerState(null);
        }




        // ここには改善の余地はあると思う
        static OctDirection Stick2OctDirection(float x, float y)
        {
            if (y > 0.5f)
            {
                if (x > 0.5f) { return OctDirection.RightUp; }
                else if (x < -0.5f) { return OctDirection.LeftUp; }
                return OctDirection.Up;
            }
            else if (y < -0.5f)
            {
                if (x > 0.5f) { return OctDirection.RightDown; }
                else if (x < -0.5f) { return OctDirection.LeftDown; }
                return OctDirection.Down;
            }

            if (x > 0.5f) { return OctDirection.Right; }
            else if (x < -0.5f) { return OctDirection.Left; }
            return OctDirection.NotDirection;
        }

        enum OctDirection
        {
            Up = 0,
            RightUp = 1,
            Right = 2,
            RightDown = 3,
            Down = 4,
            LeftDown = 5,
            Left = 6,
            LeftUp = 7,

            NotDirection = -1,
        }
    }
}
