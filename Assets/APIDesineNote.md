# API 設計に関するメモ

## C#-Generic

### ControllerVisualizer

核となるインターフェースを持つ ここに実装はない。

### SwitchControllerProtocolReceiver

Switch のプロトコルを解析し  IControllerProtocolReceiver の実装とともに ControllerVisualizer.ControllerState を提供する。

## Require-Unity

### ControllerVisualizerAndManagerForUnity

ControllerVisualizer を参照し、それら　ProtocolReceiver の生成や管理、IControllerVisualizer を実装した MonoBehavior などをつなぐマネージャー。

### SwitchProconVisualizer

SwitchControllerProtocolReceiver を参照し、Procon の 3D モデルを制御し Visualize する IControllerVisualizer の実装の一つ。

### SwitchControllerProtocolReceiverManagerForUnity

[SwitchControllerProtocolReceiver](#switchcontrollerprotocolreceiver)  をUnity上でセットアップしながら扱う存在。
