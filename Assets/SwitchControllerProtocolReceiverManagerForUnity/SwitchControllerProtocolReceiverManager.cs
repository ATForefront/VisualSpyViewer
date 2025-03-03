using System;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SwitchControllerVisualizer
{
    public class SwitchControllerProtocolReceiverManager : AbstractProtocolReceiverManager
    {
        public TMP_InputField PortName;
        public TMP_InputField BaudRate;
        public Toggle RawProtocolToggle;
        public Button ReceivingToggle;
        public TMP_Text ReceivingToggleText;


        // ---

        void Awake()
        {
            // なぜだかわからないけど、これはうまく動作しなかった ... しかしなぜ？
            // 仕方がないの Inspector の UnityEvent から登録すると迂回できた
            // ReceivingToggle.onClick.AddListener(OnReceiveToggle);
            UIIsActive(false);
            ToggleTextIs(false);
        }

        private void UIIsActive(bool isActive)
        {
            ReceivingToggle.gameObject.SetActive(isActive);
            PortName.gameObject.SetActive(isActive);
            BaudRate.gameObject.SetActive(isActive);
            RawProtocolToggle.gameObject.SetActive(isActive);
        }

        IRegisterControllerProtocolReceiver _register;
        SwitchControllerProtocolReceiver _protocolReceiver;
        public override void ReceiverEnable(IRegisterControllerProtocolReceiver register)
        {
            _protocolReceiver?.Dispose();
            _protocolReceiver = null;

            _register = register;
            UIIsActive(true);
        }
        public override void ReceiverDisable()
        {
            _protocolReceiver?.Dispose();
            _protocolReceiver = null;
            _register = null;
            UIIsActive(false);
        }
        void OnDestroy()
        {
            _protocolReceiver?.Dispose();
            _protocolReceiver = null;
        }

        public void OnReceiveToggle()
        {
            if (_register == null) { return; }

            if (_protocolReceiver == null)
            {
                var protName = PortName.text;
                if (int.TryParse(BaudRate.text, out var baudRate) is false) { Debug.Log(BaudRate.text + " is not int"); return; }
                _protocolReceiver = new(protName, baudRate, Debug.Log);
                _protocolReceiver.RawProtocolMode = RawProtocolToggle.isOn;
                ToggleTextIs(true);
            }
            else
            {
                _protocolReceiver?.Dispose();
                _protocolReceiver = null;
                ToggleTextIs(false);
            }
            _register.Register(_protocolReceiver);
        }

        private void ToggleTextIs(bool isNowDoing)
        {
            if (isNowDoing) ReceivingToggleText.text = "Stop";
            else ReceivingToggleText.text = "ConnectSerialPort";
        }

        public void OnUpdateRawProtocolToggle(bool newValue)
        {
            _protocolReceiver.RawProtocolMode = newValue;
        }
        public void ResetControllerRotation()
        {
            _protocolReceiver.ResetControllerRotation();
        }
    }
}
