using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Station366.Model
{
    public sealed class SampleDataSource
    {
        private static SampleDataSource _sampleDataSource = new SampleDataSource();

        private readonly List<Station> _stations = new List<Station>();
        public List<Station> Stations
        {
            get { return _stations; }
        }

        public SampleDataSource()
        {
            var dm = new Station()
            {
                Name = "Test Station",
                Id = 4
            };

            _stations.Add(dm);
        }
    }
}
