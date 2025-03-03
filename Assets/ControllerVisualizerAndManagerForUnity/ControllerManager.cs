#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;

namespace SwitchControllerVisualizer
{
    public class ControllerManager : MonoBehaviour
    {
        public List<AbstractProtocolReceiverManager> ProtocolReceiverManagers = new();
        public List<AbstractUnityVisualizer> Visualizers = new();


        [Header("UI")]
        public TMP_Dropdown ProtocolReceiverSelectDropdown = null!;
        public Button ProtocolReceiverSwitchButton = null!;

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




        // この実装の感じ ... あんまり嬉しくないな ... もっと UI からマネージャーを触らせる方向がいいと思うけど ... 私にはそこまでの気持ちはないかな
        private void InitProtocolReceiverSelector()
        {
            // index を基準とする際 null は邪魔なので消します！！！
            ProtocolReceiverManagers.RemoveAll(m => m == null);

            ProtocolReceiverSelectDropdown.options = ProtocolReceiverManagers.Select(m => m.name).Select(m => new TMP_Dropdown.OptionData(m)).ToList();
            ProtocolReceiverSelectDropdown.value = 0;

            // こっちもだめかも
            // ProtocolReceiverSwitchButton.onClick.AddListener(OnReceiverChangeOrInit);
        }
        public void OnReceiverChangeOrInit()
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
            InitProtocolReceiverSelector();
        }


        void Start()
        {
            OnReceiverChangeOrInit();
        }

    }
}
