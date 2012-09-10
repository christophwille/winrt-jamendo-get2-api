using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Station366.Model;

namespace Station366
{
    public class JamendoProxy
    {
        public const string BaseUrl = "http://api.jamendo.com/get2/";

        // Response document format:
        //
        //<?xml version="1.0" encoding="UTF-8"?>
        //    <data>
        //        <radio>
        //            <id>4</id>
        //            <name>Dance - Electro</name>
        //        </radio>
        public async Task<List<Station>> GetStationList()
        {
            const string radioListUrl = BaseUrl + "id+name+image/radio/xml/?n=all";

            var data = await Get2(radioListUrl);
            if (null == data)
                return new List<Station>();

            var stations = data.Elements("radio")
                .Select(r => new Station()
                {
                    Id = Convert.ToInt32((string)r.Element("id")),
                    Name = (string)r.Element("name"),
                    ImageUrl = (string)r.Element("image")
                });

            return stations.ToList();
        }

        // Response document format:
        //
        //<data>
        //    <track>
        //        <radioposition>0</radioposition>
        //        <id>212013</id>
        //        <stream>http://storage-new.newjamendo.com/?trackid=212013&format=mp31&u=0</stream>
        //    </track>
        public async Task<List<Track>> GetTracks(int stationId, int? lastRadioPosition=null)
        {
            // http://developer.jamendo.com/en/wiki/Musiclist2ApiJoins
            // However, n=X doesn't seem to be honored for radio stations, it returns five every time
            var baseQuery = String.Format(
                "radioposition+id+stream+name+album_id+album_name+album_image+album_url/track/xml/?radioid={0}&n={1}",
                stationId, 
                Constants.NumOfTracksToRetrievePerCall);

            string trackListUrl = BaseUrl + baseQuery;
            
            if (null != lastRadioPosition)
            {
                trackListUrl += "&radioposition=" + (lastRadioPosition.Value + 1).ToString();
            }

            var data = await Get2(trackListUrl);
            if (null == data)
                return new List<Track>();

            var tracks = data.Elements("track")
                .Select(r => new Track()
                {
                    Id = Convert.ToInt32((string)r.Element("id")),
                    RadioPosition = Convert.ToInt32((string)r.Element("radioposition")),
                    StreamUrl = (string)r.Element("stream"),
                    Name = (string)r.Element("name"),
                    Album = new Album()
                                {
                                    Id = Convert.ToInt32((string)r.Element("album_id")),
                                    Name = (string)r.Element("album_name"),
                                    ImageUrl = (string)r.Element("album_image"),
                                    Url = (string)r.Element("album_url")
                                }
                });

            return tracks.ToList();
        }

        private async Task<XElement> Get2(string url)
        {
            try
            {
                var client = new HttpClient();
                var response = await client.GetStringAsync(url);

                var data = XElement.Parse(response);
                return data;
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}
