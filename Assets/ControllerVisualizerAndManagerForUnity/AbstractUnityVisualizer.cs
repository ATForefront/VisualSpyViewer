#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwitchControllerVisualizer
{
    //このあたりは Unity ベッタリになるから interface じゃなくて abstract class
    public abstract class AbstractUnityVisualizer : MonoBehaviour, IControllerVisualizer
    {
        public abstract void SetControllerState(IControllerProtocolReceiver? controllerState);
    }
}
