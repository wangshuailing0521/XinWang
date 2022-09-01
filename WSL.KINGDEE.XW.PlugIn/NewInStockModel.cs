using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn
{
    public class NewInStockModel
    {
        public string tagSn { get; set; }

        public string tagSnProducerCode { get; set; }

        public string parentTagSn { get; set; }

        public string parentTagSnProducerCode { get; set; }

        public string enterpriseCode { get; set; }

        public string productCode { get; set; }

        public string dataDate { get; set; }

        public ProductionModel production { get; set; }

        public PurchaseModel purchase { get; set; }
    }

    public class ProductionModel
    {
        public string producerName { get; set; }

        public string productionDate { get; set; }

        public string batch { get; set; }

        public string origin { get; set; }

        public string certNoOfOrigin { get; set; }

        public string certNoOfQuarantine { get; set; }

        public string certOfQuality { get; set; }
    }

    public class PurchaseModel
    {
        public string quantity { get; set; }

        public string unit { get; set; }

        public string purchaseDate { get; set; }

        public string vendorName { get; set; }

        public string vendorAddr { get; set; }

        public string vendorTel { get; set; }

        public string stallNo { get; set; }

        public string operatorName { get; set; }

        public string operatorContactInfo { get; set; }
    }
}
