using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Station366.Model;

namespace Station366.States
{
    public class MainPageState
    {
        public List<Station> Stations { get; set; }

        public int? CurrentStationId { get; set; }
        public int? CurrentTrackRadioPosition { get; set; }

        public List<Track> Tracks { get; set; } 
    }
}
