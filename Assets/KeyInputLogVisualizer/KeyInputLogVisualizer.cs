using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace SwitchControllerVisualizer.KeyInputLogger.Visualizer
{
    /*
    Unity に依存しながら、 KeyLogger のログを表示する。
    */

    public abstract class KeyInputLogVisualizer : AbstractUnityVisualizer
    {
        public KeyInputVisual[] KeyInputVisuals = Array.Empty<KeyInputVisual>();
        KeyLogger.KeyCode[] _keyCodeHistory;
        KeyLogger _keyLogger = new();
        Queue<KeyLogger.KeyCode> _logQueue = new();
        void Awake()
        {
            var mainThread = SynchronizationContext.Current;
            _keyLogger.InputLog += EnQueueOnMainThread;

            void EnQueueOnMainThread(KeyLogger.KeyCode keyCode)
            {
                void EnQueue(object state) { _logQueue.Enqueue((KeyLogger.KeyCode)state); }
                mainThread.Post(EnQueue, keyCode);
            }
        }
        public override void SetControllerState(IControllerProtocolReceiver controllerState)
        {
            _keyLogger.SetControllerState(controllerState);
        }

        void OnDestroy() { _keyLogger.Dispose(); }


        void Update()
        {
            while (_logQueue.TryDequeue(out var keyCode))
            {
                HistoryUpdate(keyCode);
                if (_keyCodeHistory.Length > 0) { UpdateKeyInputVisuals(KeyInputVisuals, _keyCodeHistory); }
            }
        }

        void HistoryUpdate(KeyLogger.KeyCode newKeyCode)
        {
            if (_keyCodeHistory is null || _keyCodeHistory.Length != KeyInputVisuals.Length) { _keyCodeHistory = new KeyLogger.KeyCode[KeyInputVisuals.Length]; }
            if (_keyCodeHistory.Length <= 0) { return; }
            ArrayShift(_keyCodeHistory);
            _keyCodeHistory[0] = newKeyCode;
        }
        void ArrayShift<T>(T[] array)
        {
            for (var i = array.Length - 1; 0 >= i; i -= 1)
            {
                array[i] = array[i - 1];
            }
        }
        protected abstract void UpdateKeyInputVisuals(KeyInputVisual[] keyInputVisuals, KeyLogger.KeyCode[] keyCodeHistory);
    }
}
