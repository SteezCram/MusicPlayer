using InTheHand.Net.Sockets;
using MusicPlayer.ViewModels;
using MusicPlayer.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MusicPlayer
{
    public partial class MainWindow : Window
    {
        const string BLUETOOTH_ADDRESS = "98:D3:31:F4:1B:7D";
        const string BLUETOOTH_PAIRING_CODE = "1234";

        //private BluetoothClient bluetoothClient = new BluetoothClient();
        //private BluetoothDeviceInfo bluetoothDeviceInfo = new BluetoothDeviceInfo(BluetoothAddress.Parse(BLUETOOTH_ADDRESS));
        //private NetworkStream stream = null;
        private BluetoothPlayer _bluetoothPlayer;
        private Music _currentMusic;
        private int _currentNoteIndex = 0;
        private MusicViewModel _viewModel;

        
        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MusicViewModel
            {
                PairingVisibility = Visibility.Visible,
                ProgressVisibility = Visibility.Visible,
                StatusText = "Appairage en cours",

                CurrentMusic = null,
                Musics = Music.ReadMusic(),
                MusicVisibility = Visibility.Collapsed,
            };

            DataContext = _viewModel;


#if DEBUG
            //IReadOnlyCollection<BluetoothDeviceInfo> dev = bluetoothClient.DiscoverDevices();
#endif
        }
        

        /// <summary>
        /// Trigger when the window is loaded.
        /// Start logic of the application.
        /// </summary>
        /// 
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BluetoothClient bluetoothClient = null;

            try {
                bluetoothClient = await BluetoothPlayer.ConnectToDevice(BLUETOOTH_ADDRESS, BLUETOOTH_PAIRING_CODE);
            }
            catch (SocketException ex)
            {
                if (bluetoothClient is null)
                {
                    _viewModel.ProgressVisibility = Visibility.Collapsed;
                    _viewModel.StatusText = "L'appairage à échoué";
                    return;
                }
            }

            
            try {
                _bluetoothPlayer = new BluetoothPlayer(bluetoothClient);
            }
            catch
            {
                _viewModel.ProgressVisibility = Visibility.Collapsed;
                _viewModel.StatusText = "Impossible de lire les données";

                bluetoothClient.Close();
                bluetoothClient.Dispose();
                return;
            }


            _viewModel.PairingVisibility = Visibility.Collapsed;
            _viewModel.MusicVisibility = Visibility.Visible;


            // Init the player
            _viewModel.CurrentMusic = _viewModel.Musics[0];
            _currentMusic = _viewModel.Musics[0]; // By default the first music is used
            _currentNoteIndex = 31; // Set it to 31 since we load 32 notes

            await _bluetoothPlayer.PlayerInit();
            await _bluetoothPlayer.PlayerMusicList(_viewModel.Musics.ToArray());
            await _bluetoothPlayer.PlayerMusicMetadata(_currentMusic);            
            await _bluetoothPlayer.MusicGetNote(_currentMusic, 0, 32); // Use the base index of 0 since we init the player before
            await _bluetoothPlayer.PlayerPlay();

            while (true)
                await Execute(await _bluetoothPlayer.ReadData());
        }



        /// <summary>
        /// Executes a command get by the bluetooth device.
        /// </summary>
        private async Task Execute(string line)
        {
            Debug.WriteLine(line);

            line = line.Substring(0, line.Length - 1);
            string[] splitted = line.Split(':');
            string command = splitted[0];
            string data = splitted[1];


            // Executes the command
            switch (command)
            {
                // Music Send Note
                case "MSN":
                    {
                        // Prevent index out of range exception
                        if (_currentNoteIndex + 1 >= _currentMusic.Notes.Count)
                            return;

                        await _bluetoothPlayer.MusicGetNote(_currentMusic, _currentNoteIndex, 16);
                        _currentNoteIndex += 16;
                    }
                    break;

                // Player Next Music
                case "PNE":
                    {
                        int index = 0;
                        int count = _viewModel.Musics.Count;
                        
                        for (int i = 0; i < count; i++) {
                            if (_viewModel.Musics[i].Title == _currentMusic.Title) {
                                index = i;
                                break;
                            }
                        }

                        _currentMusic = _viewModel.Musics[(index + 1) % count];
                        _currentNoteIndex = 31;
                        _viewModel.CurrentMusic = _currentMusic;

                        await _bluetoothPlayer.PlayerMusicMetadata(_currentMusic);
                        await _bluetoothPlayer.MusicGetNote(_currentMusic, 0, 32);
                        await _bluetoothPlayer.PlayerPlay();
                    }
                    break;

                // Player Previous Music
                case "PPR":
                    {
                        int index = 0;
                        int count = _viewModel.Musics.Count;
                        
                        for (int i = 0; i < count; i++)
                        {
                            if (_viewModel.Musics[i].Title == _currentMusic.Title)
                            {
                                index = i;
                                break;
                            }
                        }

                        int mod = (index - 1 + count) % count;
                        _currentMusic = _viewModel.Musics[mod];
                        _currentNoteIndex = 31;
                        _viewModel.CurrentMusic = _currentMusic;

                        await _bluetoothPlayer.PlayerMusicMetadata(_currentMusic);
                        await _bluetoothPlayer.MusicGetNote(_currentMusic, 0, 32);
                        await _bluetoothPlayer.PlayerPlay();
                    }
                    break;

                // Player Use Music
                case "PUM":
                    {
                        _currentMusic = _viewModel.Musics.Where(x => x.Title == data).ToArray()[0];
                        _currentNoteIndex = 31;
                        _viewModel.CurrentMusic = _currentMusic;

                        await _bluetoothPlayer.MusicGetNote(_currentMusic, 0, 32);
                        await _bluetoothPlayer.PlayerPlay();
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles all the bluetooth communication in background.
        /// </summary>
        //private async void Handle()
        //{
        //    while (true)
        //    {
        //        if (BluetoothPlayer.WaitReturn) {
        //            await Task.Delay(1);
        //            continue;
        //        }

        //        Execute(BluetoothPlayer.ReadData(stream));
        //    }
        //}
    }
}
