#nullable enable
namespace SwitchControllerVisualizer
{
    public class SwitchControllerExtension : IExtensionControllerState
    {
        public bool Home;
        public bool Capture;
    }
    public class SwitchJoyConExtension : IExtensionControllerState
    {
        public bool RightJoyconSR;
        public bool RightJoyconSL;
        public bool LeftJoyconSR;
        public bool LeftJoyconSL;
    }

}
