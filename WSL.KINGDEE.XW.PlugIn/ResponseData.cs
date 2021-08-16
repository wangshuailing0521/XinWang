using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn
{
    public class ResponseData
    {
        public int status { get; set; }
        public string messageCode { get; set; }

        public string message { get; set; }

        public string exception { get; set; }
    }
}
