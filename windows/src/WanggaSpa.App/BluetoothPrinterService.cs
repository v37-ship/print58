using System.IO;
using System.IO.Ports;

namespace WanggaSpa.App;

/// <summary>EXP583 V2 over Bluetooth SPP appears as a virtual COM port on Windows.</summary>
public sealed class BluetoothPrinterService
{
    public static string[] ListPorts() => SerialPort.GetPortNames().OrderBy(p => p).ToArray();

    public void Print(string portName, byte[] escPosBytes)
    {
        using var sp = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
        {
            WriteTimeout = 10000, ReadTimeout = 5000
        };
        sp.Open();
        sp.Write(escPosBytes, 0, escPosBytes.Length);
        sp.BaseStream.Flush();
        Thread.Sleep(500);
    }
}
