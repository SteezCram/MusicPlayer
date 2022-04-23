using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using MusicPlayer.ViewModels;
using MusicPlayer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MusicPlayer
{
    internal class BluetoothPlayer
    {
        public static Mutex BluetoothPlayerMutex = new Mutex();
        
        #region Private Members

        private readonly BluetoothClient _bluetoothClient;
        private readonly NetworkStream _bluetoothStream;
        private Task _handleTask;

        #endregion

        #region Constructor

        /// <summary>
        /// Bluetooth music player implementation.
        /// Contains all the methods available in the LPC1768 that can be used.
        /// </summary>
        /// 
        /// <param name="networkStream">Network stream of the bluetooth device to send and receive data. Must do a pair and authenticate procedure before.</param>
        public BluetoothPlayer(BluetoothClient bluetoothClient)
        {
            _bluetoothClient = bluetoothClient;
            _bluetoothStream = bluetoothClient.GetStream();

            if (!_bluetoothStream.CanRead || !_bluetoothStream.CanWrite)
                throw new Exception("Cannot read or write in the bluetooth stream.");
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Connects to a bluetooth device.
        /// </summary>
        /// 
        /// <param name="bluetoothAddress">Address of the bluetooth device in hex format</param>
        /// <param name="bluetoothPin">Pin code to pair the device</param>
        /// <param name="timeout">Timeout of the pairing</param>
        /// 
        /// <returns>The bluetooth client connected to the device or null if the connection failed</returns>
        public static async Task<BluetoothClient> ConnectToDevice(string bluetoothAddress, string bluetoothPin, int timeout = 10000)
        {
            BluetoothClient bluetoothClient = new BluetoothClient();
            BluetoothDeviceInfo bluetoothDeviceInfo = new BluetoothDeviceInfo(BluetoothAddress.Parse(bluetoothAddress));

            Task task = new Task(() => {
                BluetoothSecurity.PairRequest(bluetoothDeviceInfo.DeviceAddress, bluetoothPin);
            });
            task.Start();

            
            if (await Task.WhenAny(task, Task.Delay(10000)) != task || !bluetoothDeviceInfo.Authenticated)
                return null;

            await bluetoothClient.ConnectAsync(bluetoothDeviceInfo.DeviceAddress, BluetoothService.SerialPort);

            
            return bluetoothClient;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Handles each response from the bluetooth device.
        /// </summary>
        /// 
        /// <remarks>
        /// Create a thread to handle all the request.
        /// </remarks>
        public void BeginHandle(Action<string> execute)
        {
            _handleTask = new Task(async() =>
            {
                while (true)
                    execute(await ReadData());
            });
            _handleTask.Start();
        }

        /// <summary>
        /// Ends the handle of the request.
        /// </summary>
        public void EndHandle()
        {
            _handleTask.Wait();
        }
        

        /// <summary>
        /// Sends a note or a range of notes.
        /// </summary>
        /// 
        /// <param name="music">Music to use</param>
        /// <param name="baseIndex">Base index to start to send the note</param>
        /// <param name="count">Number of note to send, default = 1</param>
        /// 
        /// <returns>Pending task to wait</returns>
        public async Task MusicGetNote(Music music, int baseIndex, int count = 1)
        {
            BluetoothPlayerMutex.WaitOne();

            for (int i = 0; i < count; i++)
            {
                //Debug.WriteLine($"MUSIC GET NOTE - {baseIndex + i} >= {music.Notes.Count}");

                // Prevent index out of range exception
                if (baseIndex + i >= music.Notes.Count)
                    break;

                await SendCommand($"MGN:{music.Notes[baseIndex + i].Frequency},{music.Notes[baseIndex + i].Duration};");
            }

            BluetoothPlayerMutex.ReleaseMutex();
        }
        

        /// <summary>
        /// Sends the command to init the music player.
        /// </summary>
        /// 
        /// <returns>Pending task to wait</returns>
        public async Task PlayerInit()
        {
            BluetoothPlayerMutex.WaitOne();

            await SendCommand("PIN:;");

            BluetoothPlayerMutex.ReleaseMutex();
        }

        /// <summary>
        /// Sends the command to set the music list.
        /// </summary>
        /// 
        /// <param name="musics">Musics list to send</param>
        /// 
        /// <returns>Pending task to wait</returns>
        public async Task PlayerMusicList(Music[] musics)
        {
            BluetoothPlayerMutex.WaitOne();

            // Send all the music
            string str = "PML:";
            foreach (Music music in musics)
                str += music.Title + ',';
            
            str = str.Substring(0, str.Length - 1);
            str += ";";

            await SendCommand(str);

            BluetoothPlayerMutex.ReleaseMutex();
        }

        public async Task PlayerMusicMetadata(Music music)
        {
            BluetoothPlayerMutex.WaitOne();

            await SendCommand($"PMM:{music.Author},{music.Duration},{Convert.ToBase64String(music.Thumbnail.ToArray())};");

            BluetoothPlayerMutex.ReleaseMutex();
        }

        /// <summary>
        /// Sends the command to pause the music.
        /// </summary>
        /// 
        /// <returns>Pending task to wait</returns>
        public async Task PlayerPause()
        {
            BluetoothPlayerMutex.WaitOne();

            await SendCommand("PPA:;");

            BluetoothPlayerMutex.ReleaseMutex();
        }

        /// <summary>
        /// Sends the command to play the music.
        /// </summary>
        /// 
        /// <returns>Pending task to wait</returns>
        public async Task PlayerPlay()
        {
            BluetoothPlayerMutex.WaitOne();

            await SendCommand("PPL:;");

            BluetoothPlayerMutex.ReleaseMutex();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reads data from the network stream and returns it as a string.
        /// </summary>
        /// 
        /// <returns>Data read</returns>
        public async Task<string> ReadData()
        {
            byte[] buffer = new byte[1024];
            int bytesRead;
            string data = "";


            // Incoming message may be larger than the buffer size. 
            while (!data.Contains(";"))
            {
                bytesRead = await _bluetoothStream.ReadAsync(buffer, 0, buffer.Length);
                data += Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }

            
            return data;
        }

        /// <summary>
        /// Sends a command and wait for a return.
        /// </summary>
        /// 
        /// <param name="command">Command to send</param>
        /// 
        /// <returns>Pending task to wait</returns>
        private async Task SendCommand(string command)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(command);
            await _bluetoothStream.WriteAsync(buffer, 0, buffer.Length);

            Debug.WriteLine(await ReadData());
        }

        #endregion
    }
}
