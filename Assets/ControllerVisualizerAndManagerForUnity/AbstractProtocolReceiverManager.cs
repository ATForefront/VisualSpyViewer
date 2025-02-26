#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwitchControllerVisualizer
{
    //このあたりは Unity ベッタリになるから interface じゃなくて abstract class
    public abstract class AbstractProtocolReceiverManager : MonoBehaviour
    {
        public abstract void ReceiverEnable(IRegisterControllerProtocolReceiver register);
        public abstract void ReceiverDisable();
    }
    public interface IRegisterControllerProtocolReceiver
    {
        void Register(IControllerProtocolReceiver? controllerProtocolReceiver);
    }
}
