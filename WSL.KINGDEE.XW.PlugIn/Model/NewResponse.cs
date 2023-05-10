using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn
{
    public class NewResponse
    {
        public bool success { get; set; }

        public int affected { get; set; }

        public string tagSn { get; set; }

        public string tagSnProducerCode { get; set; }

        public string errorCode { get; set; }

        public string errorMsg { get; set; }

        public List<ResponseError> errors { get; set; }
    }

    public class ResponseError
    {
        public string code { get; set; }
        public string message { get; set; }
    }
}
