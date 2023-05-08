using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn
{
    public class NewInStockModel
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

        public PurchaseModel purchase { get; set; }
    }

    public class ProductionModel
    {
        public string producerName { get; set; }

        public string productionDate { get; set; }

        public string batch { get; set; }

        #region 老版本
        /// <summary>
        /// 产地
        /// </summary>
        public string origin { get; set; }

        /// <summary>
        /// 产地证明编号
        /// </summary>
        public string certNoOfOrigin { get; set; }

        /// <summary>
        /// 检验检疫证书编号
        /// </summary>
        public string certNoOfQuarantine { get; set; }

        /// <summary>
        /// 质量安全检测
        /// </summary>
        public string certOfQuality { get; set; }
        #endregion

        #region 新版本
        /// <summary>
        /// 国家
        /// </summary>
        public string country { get; set; }

        public string province { get; set; }

        public string city { get; set; }

        /// <summary>
        /// 产地证明编号
        /// </summary>
        public string originCertNo { get; set; }

        /// <summary>
        /// 检验检疫证书编号
        /// </summary>
        public string iqCertNo { get; set; }

        /// <summary>
        /// 质量安全检测
        /// </summary>
        public string qualityCertNo { get; set; }

        /// <summary>
        /// 承诺达标合格证
        /// </summary>
        public string qualificationNo { get; set; }

        #endregion
    }

    public class PurchaseModel
    {
        public string quantity { get; set; }

        public string unit { get; set; }

        public string purchaseDate { get; set; }

        /// <summary>
        /// 统一社会信用代码
        /// </summary>
        public string vendorSocialCreditCode { get; set; }

        public string vendorName { get; set; }

        public string vendorAddr { get; set; }

        public string vendorTel { get; set; }

        public string stallNo { get; set; }

        public string operatorName { get; set; }

        public string operatorContactInfo { get; set; }
    }
}
