package com.wanggaspa.receipt

import android.annotation.SuppressLint
import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothSocket
import java.util.UUID

/** EXP583 V2 over Bluetooth Classic SPP. Pair in Android Settings first. */
class BluetoothPrinterService(private val adapter: BluetoothAdapter?) {
    companion object {
        val SPP: UUID = UUID.fromString("00001101-0000-1000-8000-00805F9B34FB")
    }

    @SuppressLint("MissingPermission")
    fun bondedPrinters(): List<BluetoothDevice> =
        adapter?.bondedDevices?.toList() ?: emptyList()

    @SuppressLint("MissingPermission")
    fun print(device: BluetoothDevice, data: ByteArray) {
        var socket: BluetoothSocket? = null
        try {
            socket = device.createRfcommSocketToServiceRecord(SPP)
            adapter?.cancelDiscovery()
            socket.connect()
            socket.outputStream.write(data)
            socket.outputStream.flush()
            Thread.sleep(500)
        } finally {
            try { socket?.close() } catch (_: Exception) { }
        }
    }
}
