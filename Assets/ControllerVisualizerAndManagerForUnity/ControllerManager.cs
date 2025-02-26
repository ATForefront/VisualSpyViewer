#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

namespace SwitchControllerVisualizer
{
    public class ControllerManager : MonoBehaviour
    {
        public List<AbstractProtocolReceiverManager> ProtocolReceiverManagers = new();
        public List<AbstractUnityVisualizer> Visualizers = new();


        [Header("UI")]
        public TMP_Dropdown ProtocolReceiverSelectDropdown;

        // ---

        private AbstractProtocolReceiverManager? _nowManager;
        private RegisterControllerProtocolReceiverHandler? _registerHandler;


        class RegisterControllerProtocolReceiverHandler : IRegisterControllerProtocolReceiver, IDisposable
        {
            ControllerManager? _parentManager;
            public RegisterControllerProtocolReceiverHandler(ControllerManager parentManager)
            {
                _parentManager = parentManager;
            }

            public void Register(IControllerProtocolReceiver? controllerProtocolReceiver)
            {
                if (_parentManager == null) { return; }
                foreach (var v in _parentManager.Visualizers) { v.SetControllerState(controllerProtocolReceiver); }
            }
            public void Dispose()
            {
                _parentManager = null;
            }
        }





        private void InitProtocolReceiverSelectDropdown()
        {
            // index を基準とする際 null は邪魔なので消します！！！
            ProtocolReceiverManagers.RemoveAll(m => m != null);

            ProtocolReceiverSelectDropdown.options = ProtocolReceiverManagers.Select(m => m.name).Select(m => new TMP_Dropdown.OptionData(m)).ToList();
            ProtocolReceiverSelectDropdown.value = 0;
        }
        void OnReceiverChangeOrInit()
        {
            var selected = ProtocolReceiverSelectDropdown.value;
            if (ProtocolReceiverManagers.Count <= selected) { return; }
            _nowManager?.ReceiverDisable();
            _registerHandler?.Dispose();

            _registerHandler = new(this);
            var protocolReceiverManager = ProtocolReceiverManagers[selected];
            protocolReceiverManager.ReceiverEnable(_registerHandler);
        }

        void Awake()
        {
            InitProtocolReceiverSelectDropdown();
        }


        void Start()
        {
            OnReceiverChangeOrInit();
        }

    }
}
