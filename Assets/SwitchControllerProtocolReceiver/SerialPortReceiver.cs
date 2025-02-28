#nullable enable
using System;
using System.Globalization;
using System.IO.Ports;
using System.Text;

namespace SwitchControllerVisualizer
{
    public class SerialPortReceiver : IDisposable
    {
        static char[] CAPTURE_START = new char[] { 's', '\n' };
        static char[] CAPTURE_STOP = new char[] { 'p', '\n' };
        static char[] SHOW_DATA = new char[] { 'b', '\n' };
        SerialPort _serialPort;
        byte[] _readBuffer = new byte[1024];
        public SerialPortReceiver(string portName, int baudRate)
        {
            _serialPort = new(portName, baudRate);
            _serialPort.WriteTimeout = _serialPort.ReadTimeout = 100;
            _serialPort.DtrEnable = true;

            _serialPort.Open();
            _serialPort.Write(CAPTURE_START, 0, CAPTURE_START.Length);
            Receive((s) => { });
        }


        public delegate void DataCallBack(ReadOnlySpan<byte> bytes);
        public void Receive(DataCallBack dataCallBack)
        {
            _serialPort.Write(CAPTURE_STOP, 0, CAPTURE_STOP.Length);
            _serialPort.Write(SHOW_DATA, 0, SHOW_DATA.Length);
            _serialPort.Write(CAPTURE_START, 0, CAPTURE_START.Length);
            while (_serialPort.BytesToRead != 0)
            {
                var line = _serialPort.ReadLine();
                var bIndex = 0;
                for (var r = 0; line.Length > (r + 1) && bIndex != -1; r += 2)
                {
                    var span = line.AsSpan(r, 2);

                    if (byte.TryParse(span, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var b))
                    {
                        _readBuffer[bIndex] = b;
                        bIndex += 1;
                    }
                    else
                    {
                        bIndex = -1;
                    }
                }

                if (bIndex == -1 || bIndex == 0)
                {
                    // Debug.Log("Debug-Dump:" + line);
                    continue;
                }// ignore

                var receiveBytes = _readBuffer.AsSpan(0, bIndex);

                dataCallBack(receiveBytes);
            }
        }

        public void Dispose()
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
            _serialPort = null!;
        }
    }
}
