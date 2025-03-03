#nullable enable
using System;
using System.Numerics;

namespace SwitchControllerVisualizer
{
    public abstract class ControllerState
    {
        // 基本的に class の取得は何度も行われないことを想定する
        // 一度 Visualizer に見せたらそれに対する参照を持っておいてもらってそれをずっと見つめて反映してもらう形

        public readonly StandardControllerState Standard;
        public abstract T? GetExtentState<T>() where T : notnull, IExtensionControllerState;
        protected ControllerState(StandardControllerState standardControllerState) { Standard = standardControllerState; }
    }
    public class StandardControllerState
    {
        // Switch ならそのままだが、そうでないなら
        // 右側にある四つのボタンの 右が A 左が Y 、上が X 下が B になるように
        public bool A;
        public bool B;
        public bool X;
        public bool Y;

        // 十時キーまたはそれとなるもの
        public bool Up;
        public bool Down;
        public bool Right;
        public bool Left;

        // いわゆる コントローラの奥にあるやつ
        public bool R;
        public bool L;
        public bool ZR;
        public bool ZL;

        /*
        Switch だったら L が - 、R が + を差し、そうでなかったら Select が L 、 Start が R
        */
        public bool SystemR;
        public bool SystemL;


        // それぞれ　正規化された -1 ~ 1 を想定する
        public float RStickY;
        public float RStickX;

        public float LStickY;
        public float LStickX;

        public bool RStickClick;
        public bool LStickClick;
    }
    public interface IExtensionControllerState { }
    public class GyroExtension : IExtensionControllerState
    {
        // Quatrain の絶対値を記述するようにね！
        public float X;
        public float Y;
        public float Z;
        public float W;

        public void ReadFromQuaternion(Quaternion quaternion)
        {
            X = quaternion.X;
            Y = quaternion.Y;
            Z = quaternion.Z;
            W = quaternion.W;
        }
        public Quaternion ReadToQuaternion() { return new(X, Y, Z, W); }
    }
}
