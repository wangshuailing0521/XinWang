using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn
{
    public class VendCustModel
    {
        /// <summary>
        /// 金蝶自加
        /// </summary>
        [JsonIgnore]
        public string type { get; set; }

        /// <summary>
        /// 金蝶自加
        /// </summary>
        [JsonIgnore]
        public string id { get; set; }

        /// <summary>
        /// 金蝶自加
        /// </summary>
        [JsonIgnore]
        public string code { get; set; }
        public string name { get; set; }
        public string tel { get; set; }
        public string address { get; set; }
        public string socialCreditCode { get; set; }
    }
}
