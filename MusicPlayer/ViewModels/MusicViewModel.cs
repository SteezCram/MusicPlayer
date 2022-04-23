using MusicPlayer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MusicPlayer.ViewModels
{
    internal class MusicViewModel : BaseViewModel
    {
        #region Pairing

        public Visibility PairingVisibility
        {
            get => _pairingVisibility;
            set
            {
                if (value != _pairingVisibility)
                {
                    _pairingVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public Visibility ProgressVisibility
        {
            get => _progressVisibility;
            set
            {
                if (value != _progressVisibility)
                {
                    _progressVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                if (value != _statusText)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        
        private Visibility _pairingVisibility;
        private Visibility _progressVisibility;
        private string _statusText;

        #endregion

        #region Music

        public Music CurrentMusic
        {
            get => _currentMusic;
            set
            {
                if (value != _currentMusic)
                {
                    _currentMusic = value;
                    OnPropertyChanged();
                }
            }
        }


        public ObservableCollection<Music> Musics
        {
            get => _musics;
            set
            {
                if (value != _musics)
                {
                    _musics = value;
                    OnPropertyChanged();
                }
            }
        }

        public Visibility MusicVisibility
        {
            get => _musicVisibility;
            set
            {
                if (value != _musicVisibility)
                {
                    _musicVisibility = value;
                    OnPropertyChanged();
                }
            }
        }


        private Music _currentMusic;
        private Visibility _musicVisibility;
        private ObservableCollection<Music> _musics;

        #endregion
    }
}
