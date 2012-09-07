using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using GalaSoft.MvvmLight;
using Station366.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Station366
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class MainPage : Station366.Common.LayoutAwarePage
    {
        public MainPageViewModel ViewModel { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

            if (!ViewModelBase.IsInDesignModeStatic)
            {
                ViewModel = new MainPageViewModel();
                DataContext = ViewModel;
            }

            MediaControl.PlayPressed += MediaControlOnPlayPressed;
            MediaControl.PausePressed += MediaControlOnPausePressed;
            MediaControl.StopPressed += MediaControlOnStopPressed;
            MediaControl.NextTrackPressed += MediaControlOnNextTrackPressed;
            MediaControl.PlayPauseTogglePressed += MediaControlOnPlayPauseTogglePressed;

            Loaded += MainPage_Loaded;
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.AttachPlayerEvents();
        }

        private async void MediaControlOnPlayPauseTogglePressed(object sender, object o)
        {
            ViewModel.PlayPauseToggle();
        }

        private async void MediaControlOnNextTrackPressed(object sender, object o)
        {
            ViewModel.SkipAhead();
        }

        private async void MediaControlOnStopPressed(object sender, object o)
        {
            ViewModel.Pause();
        }

        private async void MediaControlOnPausePressed(object sender, object o)
        {
            ViewModel.Pause();
        }

        private async void MediaControlOnPlayPressed(object sender, object o)
        {
            ViewModel.Play();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override async void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO (if serious about creating a player): load a cached copy first, then get the stations online, compare, rebind if necessary
            await ViewModel.GetStations();
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        private void TrackImage_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var currentTrack = ViewModel.CurrentTrack;

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
