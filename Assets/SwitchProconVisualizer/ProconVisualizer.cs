using System;
using System.Numerics;
using UnityEngine;

namespace SwitchControllerVisualizer
{
    public class ProconVisualizer : AbstractUnityVisualizer
    {
        public Animator Animator;
        public Transform GyroTransform;


        readonly int _idA = Animator.StringToHash("A");
        readonly int _idB = Animator.StringToHash("B");
        readonly int _idX = Animator.StringToHash("X");
        readonly int _idY = Animator.StringToHash("Y");

        readonly int _idDPadX = Animator.StringToHash("DPadX");
        readonly int _idDPadY = Animator.StringToHash("DPadY");

        readonly int _idPlus = Animator.StringToHash("Plus");
        readonly int _idMinus = Animator.StringToHash("Minus");
        readonly int _idScreenCapture = Animator.StringToHash("ScreenCapture");
        readonly int _idHome = Animator.StringToHash("Home");

        readonly int _idRStickX = Animator.StringToHash("RStickX");
        readonly int _idRStickY = Animator.StringToHash("RStickY");

        readonly int _idLStickX = Animator.StringToHash("LStickX");
        readonly int _idLStickY = Animator.StringToHash("LStickY");

        readonly int _idZL = Animator.StringToHash("ZL");
        readonly int _idZR = Animator.StringToHash("ZR");
        readonly int _idL = Animator.StringToHash("L");
        readonly int _idR = Animator.StringToHash("R");

#nullable enable
        public override void SetControllerState(IControllerProtocolReceiver? controllerState)
        {
            _state = controllerState?.ControllerState;
            _swExState = _state?.GetExtentState<SwitchControllerExtension>();
            _gyroExtension = _state?.GetExtentState<GyroExtension>();
        }
        ControllerState? _state;
        SwitchControllerExtension? _swExState;
        GyroExtension? _gyroExtension;

        public void Update()
        {
            UpdateAnimator();
            UpdateGyro();
        }

        private void UpdateAnimator()
        {
            if (Animator == null) { return; }
            if (_state is not null)
            {
                Animator.SetFloat(_idA, _state.Standard.A ? 1f : 0f);
                Animator.SetFloat(_idB, _state.Standard.B ? 1f : 0f);
                Animator.SetFloat(_idX, _state.Standard.X ? 1f : 0f);
                Animator.SetFloat(_idY, _state.Standard.Y ? 1f : 0f);

                var dpad = UnityEngine.Vector2.zero;
                if (_state.Standard.Down) { dpad.y = -1f; }
                if (_state.Standard.Up) { dpad.y = 1f; }
                if (_state.Standard.Left) { dpad.x = -1f; }
                if (_state.Standard.Right) { dpad.x = 1f; }
                Animator.SetFloat(_idDPadX, dpad.x);
                Animator.SetFloat(_idDPadY, dpad.y);


                Animator.SetFloat(_idMinus, _state.Standard.SystemL ? 1f : 0f);
                Animator.SetFloat(_idPlus, _state.Standard.SystemR ? 1f : 0f);

                Animator.SetFloat(_idRStickX, _state.Standard.RStickX);
                Animator.SetFloat(_idRStickY, _state.Standard.RStickY);
                Animator.SetFloat(_idLStickX, _state.Standard.LStickX);
                Animator.SetFloat(_idLStickY, _state.Standard.LStickY);

                Animator.SetFloat(_idZL, _state.Standard.ZL ? 1f : 0f);
                Animator.SetFloat(_idZR, _state.Standard.ZR ? 1f : 0f);
                Animator.SetFloat(_idL, _state.Standard.L ? 1f : 0f);
                Animator.SetFloat(_idR, _state.Standard.R ? 1f : 0f);
            }
            if (_swExState is not null)
            {
                Animator.SetFloat(_idHome, _swExState.Home ? 1f : 0f);
                Animator.SetFloat(_idScreenCapture, _swExState.Capture ? 1f : 0f);
            }
        }

        private void UpdateGyro()
        {
            if (GyroTransform == null) { return; }
            if (_gyroExtension is null) { GyroTransform.localRotation = UnityEngine.Quaternion.identity; return; }
            var unityQuaternion = new UnityEngine.Quaternion(_gyroExtension.X, _gyroExtension.Y, _gyroExtension.Z, _gyroExtension.W);
            GyroTransform.localRotation = unityQuaternion;
        }

    }

}
