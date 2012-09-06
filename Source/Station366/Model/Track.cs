using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Station366.Model
{
    public class Track : JamendoBaseModel
    {
        public int RadioPosition { get; set; }
        public string StreamUrl { get; set; }

        // Reference Info
        public Album Album { get; set; }
    }
}
