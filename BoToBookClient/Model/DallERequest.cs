using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoToBookClient.Model
{
    public class DallERequest
    {
        public string Prompt { get; set; }
        public int N { get; set; }
        public string Size { get; set; }
    }
}
