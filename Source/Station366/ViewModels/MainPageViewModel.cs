using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Command;
using Station366.Model;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Station366.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private JamendoProxy _proxy = new JamendoProxy();

        public void AttachPlayerEvents()
        {
            Player.MediaEnded += Player_MediaEnded;
            Player.MediaOpened += Player_MediaOpened;
        }

        void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            // Could be useful later
            var trackDuration = Player.NaturalDuration.TimeSpan;
        }

        async void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            await SkipAhead();
        }

        public async Task GetStations()
        {
            var stations = await _proxy.GetStationList();
            Stations = stations;
        }

        public const string StationsPropertyName = "Stations";
        private List<Station> _stations = new List<Station>();
        public List<Station> Stations
        {
            get
            {
                return _stations;
            }
            set
            {
                Set(StationsPropertyName, ref _stations, value);
            }
        }

        public const string CurrentTrackPropertyName = "CurrentTrack";
        private Track _currentTrack = null;

        public Track CurrentTrack
        {
            get
            {
                return _currentTrack;
            }
            private set
            {
                if (value == _currentTrack) return;

                Set(CurrentTrackPropertyName, ref _currentTrack, value);
                SetPlayerSourceToCurrentTrack();
            }
        }

        public const string SelectedStationPropertyName = "SelectedStation";
        private Station _selectedStation = null;
        public Station SelectedStation
        {
            get { return _selectedStation; }
            set
            {
                if (value == _selectedStation) return;

                Set(SelectedStationPropertyName, ref _selectedStation, value);

                InitializePlaylistForStation();
            }
        }

        private List<Track> _playList = new List<Track>();

        private async Task InitializePlaylistForStation()
        {
            ResetPlayer();

            await FillPlaylist();

            CurrentTrack = _playList.FirstOrDefault(t => t.RadioPosition == 0);
        }

        private void ResetPlayer()
        {
            Pause();

            CurrentTrack = null;
            _playList.Clear();
        }

        private async Task FillPlaylist()
        {
            if (null == SelectedStation) return;
            int stationId = SelectedStation.Id;

            int? lastKnownRadioPosition = _playList.Count();
            if (0 == lastKnownRadioPosition.Value)
                lastKnownRadioPosition = null;
            else
                lastKnownRadioPosition--; // 0-based index
            
            var tracks = await _proxy.GetTracks(stationId, lastKnownRadioPosition);
            _playList.AddRange(tracks);
        }

        private MediaElement _theMediaElement;
        private MediaElement Player
        {
            get
            {
                if (_theMediaElement == null)
                {
                    DependencyObject rootGrid = VisualTreeHelper.GetChild(Window.Current.Content, 0);
                    _theMediaElement = (MediaElement)VisualTreeHelper.GetChild(rootGrid, 0);
                }
                return _theMediaElement;
            }
        }

        private RelayCommand _pauseCommand;
        public RelayCommand PauseCommand
        {
            get
            {
                return _pauseCommand
                    ?? (_pauseCommand = new RelayCommand(
                        () => Pause(),
                        () => CanPause));
            }
        }

        // TODO (applies to all "Can" property accessors): allow RaisePropertyChanged, and actually keep state for CanXXX
        public bool CanPause
        {
            get { return true; }
        }

        public void Pause()
        {
            Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Player.Pause());
        }

        private RelayCommand _skipaheadCommand;
        public RelayCommand SkipAheadCommand
        {
            get
            {
                return _skipaheadCommand
                    ?? (_skipaheadCommand = new RelayCommand(
                        async () => await SkipAhead(),
                        () => CanSkipAhead));
            }
        }

        public bool CanSkipAhead
        {
            get { return true; }
        }

        public async Task SkipAhead()
        {
            int numOfElements = _playList.Count();
            int radioposition = CurrentTrack != null ? CurrentTrack.RadioPosition : 0;
            radioposition++;

            CurrentTrack = _playList.FirstOrDefault(t => t.RadioPosition == radioposition);

            if (numOfElements - radioposition == 2)
            {
                await FillPlaylist();
            }
        }

        private RelayCommand _playCommand;
        public RelayCommand PlayCommand
        {
            get
            {
                return _playCommand
                    ?? (_playCommand = new RelayCommand(
                        () => Play(),
                        () => CanPlay));
            }
        }

        public bool CanPlay
        {
            get { return true; }
        }

        public void Play()
        {
            Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Player.Play());
        }

        private void SetPlayerSourceToCurrentTrack()
        {
            if (null == CurrentTrack)
            {
                Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Player.Source = null);
                return;
            }

            string streamUrl = CurrentTrack.StreamUrl;
            Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Player.Source = new Uri(streamUrl));
        }

        public void PlayPauseToggle()
        {
            Window.Current.Dispatcher
                .RunAsync(CoreDispatcherPriority.Normal, 
                () =>
                    {
                        try
                        {
                            if (Player.CurrentState == MediaElementState.Stopped)
                                Player.Play();
                            else
                                Player.Stop();
                        }
                        catch
                        {
                        } 
                    });
        }
    }
}
