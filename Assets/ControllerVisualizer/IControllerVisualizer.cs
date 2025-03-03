#nullable enable
using System;

namespace SwitchControllerVisualizer
{
    public interface IControllerVisualizer
    {
        // このコントローラーの表示を行えという指示とも言える
        // null が入っている可能性がありそれはコントローラーが切り離されたときは null に更新するということ。
        // ここで RegisterUpdateCallBack を登録して更新してもよいが、 Update の方でやったほうが良いだろう。
        void SetControllerState(IControllerProtocolReceiver? controllerState);
    }
}
