using System;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SwitchControllerVisualizer
{
    public class ProconVisualizer : AbstractProtocolReceiverManager
    {
        public TMP_InputField PortName;
        public TMP_InputField BaudRate;
        public Button ReceivingToggle;
        public TMP_Text ReceivingToggleText;


        // ---

        void Awake()
        {
            ReceivingToggle.onClick.AddListener(OnReceiveToggle);
            UIIsActive(false);
            ToggleTextIs(false);
        }

        private void UIIsActive(bool isActive)
        {
            ReceivingToggle.gameObject.SetActive(isActive);
            PortName.gameObject.SetActive(isActive);
            BaudRate.gameObject.SetActive(isActive);
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

        void OnReceiveToggle()
        {
            if (_register == null) { return; }

            if (_protocolReceiver == null)
            {
                var protName = PortName.text;
                if (int.TryParse(BaudRate.text, out var baudRate) is false) { Debug.Log(BaudRate.text + " is not int"); return; }
                _protocolReceiver = new(protName, baudRate);
                ToggleTextIs(true);
            }
            else
            {
                _protocolReceiver?.Dispose();
                _protocolReceiver = null;
                ToggleTextIs(false);
            }

        }

        private void ToggleTextIs(bool IsNowDoing)
        {
            if (IsNowDoing) ReceivingToggleText.text = "Stop";
            else ReceivingToggleText.text = "ConnectSerialPort";
        }
    }
}
