using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn
{
    public class NewOutStockModel
    {
        /// <summary>
        /// 新版本废除
        /// </summary>
        public string tagSn { get; set; }

        /// <summary>
        /// 新版本废除
        /// </summary>
        public string tagSnProducerCode { get; set; }

        /// <summary>
        /// 新版本废除
        /// </summary>
        public string parentTagSn { get; set; }

        /// <summary>
        /// 新版本废除
        /// </summary>
        public string parentTagSnProducerCode { get; set; }

        /// <summary>
        /// 新版本废除
        /// </summary>
        public string enterpriseCode { get; set; }

        /// <summary>
        /// 企业下场所（一证多照）的名称
        /// </summary>
        public string businessLocationName { get; set; }

        /// <summary>
        /// 企 业 注 册 时 证 照 号 ， 如 食 品 经 营 许 可 证 的 证 照 号 , JY23111111111111
        /// </summary>
        public string license { get; set; }

        /// <summary>
        /// 企业的所属领域
        /// </summary>
        public string fieldName { get; set; }

        public string productCode { get; set; }

        public string productName { get; set; }

        public string dataDate { get; set; }

        public ProductionModel production { get; set; }

        public SalesModel sales { get; set; }
    }

    public class SalesModel
    {
        public string quantity { get; set; }

        public string unit { get; set; }

        public string saleDate { get; set; }

        /// <summary>
        /// 统一社会信用代码
        /// </summary>
        public string customerSocialCreditCode { get; set; }

        public string customerName { get; set; }

        public string customerAddr { get; set; }

        public string customerTel { get; set; }

        public string stallNo { get; set; }

        public string operatorName { get; set; }

        public string operatorContactInfo { get; set; }
    }
}
