#nullable enable
using System;
using System.Numerics;

namespace SwitchControllerVisualizer
{
    public static class AccGyroParser
    {
        const float ACCEL_SCALER = 16000f / 65535f / 1000f;
        // const float GYRO_SCALER = 4000f / 65535f / 360f;
        // Unity の方の関数の都合かわからないけれど、 90f にしたほうが正しい姿勢情報が取得できる。
        const float GYRO_SCALER = 4000f / 65535f / 90f;

        // これは実測値の夜いい感じの値、コントローラーから提供されるやつがあるっぽいからそれを使えると良いね
        static readonly Vector3 GYRO_NEUTRAL = new Vector3(6, -3, -16);
        public static int GetDeltaTime(SwitchControllerRawState beforeState, SwitchControllerRawState nowState)
        {
            var deltaTime = nowState.Timer - beforeState.Timer;
            if (deltaTime <= 0) { deltaTime += byte.MaxValue; }
            return deltaTime;
        }
        public static (Vector3 posVec, Vector3 rotVex) RawToVec(AccGyro source)
        {
            var posVec = new Vector3(source.AccelY, source.AccelZ, source.AccelX) * ACCEL_SCALER;
            posVec.Y *= -1;
            posVec.Z *= -1;

            var rotVec3 = (new Vector3(source.GyroY, source.GyroZ, source.GyroX) - GYRO_NEUTRAL) * GYRO_SCALER;
            rotVec3.Y *= -1;
            rotVec3.Z *= -1;

            return (posVec, rotVec3);
        }
        // public static Vector3 AccToRot(Vector3 v)
        // {
        //     var x = MathF.Atan2(v.y, v.z) * Mathf.Rad2Deg;
        //     var y = MathF.Atan2(v.z, v.x) * Mathf.Rad2Deg;
        //     var z = MathF.Atan2(v.y, v.x * -1) * Mathf.Rad2Deg;
        //     x += 90f;
        //     x = x % 360f;
        //     if (x > 180f) { x -= 360f; }
        //     // y -= 90f;
        //     z += 90f;
        //     z = z % 360f;
        //     if (z > 180f) { z -= 360f; }
        //     // z *= -1;
        //     return new(x, 0, z);
        // }
    }
}
