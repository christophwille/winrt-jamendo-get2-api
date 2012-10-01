using System.Diagnostics;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Command;
using Station366.Model;
using Station366.States;
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
            Player.MediaFailed += Player_MediaFailed;

            // AttachPlayerEvents runs after LoadState, we can now load the current track, if any
            var currentTrack = CurrentTrack;

            // We come back from Terminated, thus we assume to have been in the Stopped state for the Player
            if (null != currentTrack)
            {
                string streamUrl = currentTrack.StreamUrl;
                string trackName = currentTrack.Name;

                Window.Current.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, 
                    () =>
                        {
                            Player.AutoPlay = false;
                            Player.Source = new Uri(streamUrl);
                            Player.AutoPlay = true;

                            Windows.Media.MediaControl.TrackName = trackName;
                        });
            }
        }

        void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            // var errorMessage = e.ErrorMessage;  // not very useful
            IsPlayerInfoMessagesPaneVisible = true;
        }

        void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            SeekBarPosition = 0.0;

            if (Player.NaturalDuration.HasTimeSpan)
            {
                var trackDuration = Player.NaturalDuration.TimeSpan;
                SeekBarMaximum = trackDuration.TotalSeconds;
                SeekBarLargeChange = Math.Min(10, trackDuration.Seconds/10.0);
            }
        }

        public void UpdateSeekBarPosition()
        {
            SeekBarPosition = Player.Position.TotalSeconds;
        }

        public void SetSeekBarPosition(TimeSpan pos)
        {
            Player.Position = pos;
        }

        public const string SeekBarPositionPropertyName = "SeekBarPosition";
        private double _seekbarPosition = 0;
        public double SeekBarPosition
        {
            get
            {
                return _seekbarPosition;
            }
            private set
            {
                Set(SeekBarPositionPropertyName, ref _seekbarPosition, value);
            }
        }

        public const string SeekBarLargeChangePropertyName = "SeekBarLargeChange";
        private double _seekbarLargeChange = 0;
        public double SeekBarLargeChange
        {
            get
            {
                return _seekbarLargeChange;
            }
            private set
            {
                Set(SeekBarLargeChangePropertyName, ref _seekbarLargeChange, value);
            }
        }

        public const string SeekBarMaximumPropertyName = "SeekBarMaximum";
        private double _seekbarMax = 0;
        public double SeekBarMaximum
        {
            get
            {
                return _seekbarMax;
            }
            private set
            {
                Set(SeekBarMaximumPropertyName, ref _seekbarMax, value);
            }
        }

        async void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            await SkipAhead();
        }

        public void LoadState(MainPageState state)
        {
            if (null != state.Stations) _stations.AddRange(state.Stations);
            if (null != state.Tracks) _playList.AddRange(state.Tracks);
            if (null != state.CurrentStationId)
            {
                // Set the backing field to avoid running the logic in the setter, property is manually notified
                _selectedStation = Stations.FirstOrDefault(s => s.Id == state.CurrentStationId.Value);
                RaisePropertyChanged(SelectedStationPropertyName);
            }

            if (null != state.CurrentTrackRadioPosition)
            {
                // LoadState happens before the window is available, thus we cannot access Player. Therefore set the backing field only.
                // The current track (if any) is loaded into the Player in AttachPlayerEvents
                _currentTrack = _playList.FirstOrDefault(t => t.RadioPosition == state.CurrentTrackRadioPosition.Value);
                RaisePropertyChanged(CurrentTrackPropertyName);
            }
        }

        public MainPageState SaveState()
        {
            return new MainPageState()
            {
                Stations = this.Stations,
                Tracks = this._playList,
                CurrentStationId = SelectedStation != null ? SelectedStation.Id : (int?)null,
                CurrentTrackRadioPosition = CurrentTrack != null ? CurrentTrack.RadioPosition : (int?)null
            };
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
            IsPlayerInfoMessagesPaneVisible = false;

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
            var currentTrack = CurrentTrack;

            if (null == currentTrack)
            {
                Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Player.Source = null);

                Windows.Media.MediaControl.TrackName = "";

                return;
            }

            string streamUrl = currentTrack.StreamUrl;
            Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Player.Source = new Uri(streamUrl));

            Windows.Media.MediaControl.TrackName = currentTrack.Name;

            // Cannot be set as per the documentation http://msdn.microsoft.com/en-us/library/windows/apps/windows.media.mediacontrol.albumart(v=win.10).aspx
            // Windows.Media.MediaControl.AlbumArt = new Uri(currentTrack.Album.ImageUrl);
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

        public const string IsPlayerInfoMessagesPaneVisiblePropertyName = "IsPlayerInfoMessagesPaneVisible";
        private bool _isPlayerInfoMessagesPaneVisible = false;
        public bool IsPlayerInfoMessagesPaneVisible
        {
            get
            {
                return _isPlayerInfoMessagesPaneVisible;
            }
            private set
            {
                Set(IsPlayerInfoMessagesPaneVisiblePropertyName, ref _isPlayerInfoMessagesPaneVisible, value);
            }
        }

        public const string IsStationsInfoMessagesPaneVisiblePropertyName = "IsStationsInfoMessagesPaneVisible";
        private bool _isStationsInfoMessagesPaneVisible = false;
        public bool IsStationsInfoMessagesPaneVisible
        {
            get
            {
                return _isStationsInfoMessagesPaneVisible;
            }
            private set
            {
                Set(IsStationsInfoMessagesPaneVisiblePropertyName, ref _isStationsInfoMessagesPaneVisible, value);
            }
        }

        private RelayCommand _getStationscommand;
        public RelayCommand GetStationsCommand
        {
            get
            {
                return _getStationscommand
                    ?? (_getStationscommand = new RelayCommand(
                        async () => await GetStations()));
            }
        }

        public async Task GetStations()
        {
            IsStationsInfoMessagesPaneVisible = false;

            var stations = await _proxy.GetStationList();
            Stations = stations;

            if (stations.Count == 0)
            {
                IsStationsInfoMessagesPaneVisible = true;
            }
        }


        private RelayCommand _imageTappedCommand;
        public RelayCommand ImageTappedCommand
        {
            get
            {
                return _imageTappedCommand
                    ?? (_imageTappedCommand = new RelayCommand(
                        () => ImageTapped()));
            }
        }

        private void ImageTapped()
        {
            var currentTrack = CurrentTrack;

            if (null == currentTrack)
                return;

            var url = currentTrack.Album.Url;

            if (!String.IsNullOrWhiteSpace(url))
            {
                Windows.System.Launcher.LaunchUriAsync(new Uri(url));
            }
        }
    }
}
