using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn
{
    public class NewOutStockModel
    {
        public string tagSn { get; set; }

        public string tagSnProducerCode { get; set; }

        public string parentTagSn { get; set; }

        public string parentTagSnProducerCode { get; set; }

        public string enterpriseCode { get; set; }

        public string productCode { get; set; }

        public string dataDate { get; set; }

        public ProductionModel production { get; set; }

        public SalesModel sales { get; set; }
    }

    public class SalesModel
    {
        public string quantity { get; set; }

        public string unit { get; set; }

        public string saleDate { get; set; }

        public string customerName { get; set; }

        public string customerAddr { get; set; }

        public string customerTel { get; set; }

        public string stallNo { get; set; }

        public string operatorName { get; set; }

        public string operatorContactInfo { get; set; }
    }
}
