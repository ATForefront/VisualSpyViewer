#nullable enable
using System;
using UnityEngine;

namespace SwitchControllerVisualizer
{
    public static class AccGyroParser
    {
        const float ACCEL_SCALER = 16000f / 65535f / 1000f;
        // const float GYRO_SCALER = 4000f / 65535f / 360f;
        // Unity の方の関数の都合かわからないけれど、 90f にしたほうが正しい姿勢情報が取得できる。
        const float GYRO_SCALER = 4000f / 65535f / 90f;
        static readonly Vector3 GYRO_NEUTRAL = new Vector3(6, -3, -16);
        public static int GetDeltaTime(SwitchControllerState beforeState, SwitchControllerState nowState)
        {
            var deltaTime = nowState.Timer - beforeState.Timer;
            if (deltaTime <= 0) { deltaTime += byte.MaxValue; }
            return deltaTime;
        }
        public static (Vector3 posVec, Vector3 rotVex) RawToVec(AccGyro source)
        {
            var posVec = new Vector3(source.AccelY, source.AccelZ, source.AccelX) * ACCEL_SCALER;
            posVec.y *= -1;
            posVec.z *= -1;

            var rotVec3 = (new Vector3(source.GyroY, source.GyroZ, source.GyroX) - GYRO_NEUTRAL) * GYRO_SCALER;
            rotVec3.y *= -1;
            rotVec3.z *= -1;

            return (posVec, rotVec3);
        }
        public static (Vector3 posVec, int maxIndex, Quaternion rot) RawMode2ToVec(SwitchControllerState state)
        {
            int one, two, three;
            int maxIndex = (state.AccGyro1.GyroX >> 2) & 3;// 3 == 2bit

            one = state.AccGyro1.GyroX >> 4; // 12bit
            one |= (state.AccGyro1.GyroY & 511) << 12;//511 == 9bit

            two = (state.AccGyro1.GyroY >> 9) & 127;// 127 == 7bit
            two |= (state.AccGyro1.GyroZ & 16383) << 7;// 16383 == 14bit

            three = (state.AccGyro1.GyroZ >> 14) & 3;//3 == 2bit
            three |= state.AccGyro2.GyroX << 2;//16bit
            three |= (state.AccGyro2.GyroY & 7) << (2 + 16);// 7 == 3bit


            Span<float> quaternionComponents = stackalloc float[3];
            quaternionComponents[0] = (float)(one << 10) / 0x40000000;
            quaternionComponents[1] = (float)(two << 10) / 0x40000000;
            quaternionComponents[2] = (float)(three << 10) / 0x40000000;

            Span<float> quaternionOriginal = stackalloc float[4];

            var rIndex = 0;
            for (var i = 0; i < 4; i += 1)
            {
                if (maxIndex == i)
                {
                    // これ適当に書いたけどほか要素から復元しないといけないのでは ... ?
                    quaternionOriginal[i] = (one < 0) ? -1.0f : 1.0f;
                }
                else
                {
                    quaternionOriginal[i] = quaternionComponents[rIndex];
                    rIndex += 1;
                }
            }

            // nintendo のカスタムされた Quaternion の復元方法がわからぬ

            var rot = new Quaternion(quaternionOriginal[0], quaternionOriginal[1], quaternionOriginal[2], quaternionOriginal[3]);


            var posVec = new Vector3(state.AccGyro1.AccelY, state.AccGyro1.AccelZ, state.AccGyro1.AccelX) * ACCEL_SCALER;
            posVec.y *= -1;
            posVec.z *= -1;

            return (posVec, maxIndex, rot);
        }
    }
}
