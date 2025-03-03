#nullable enable
using System;

namespace SwitchControllerVisualizer
{
    public interface IControllerProtocolReceiver : IDisposable
    {
        public ControllerState ControllerState { get; }
        public ICallBackHandler RegisterUpdateCallBack(Action callback);
    }
    public interface ICallBackHandler : IDisposable { }
}
