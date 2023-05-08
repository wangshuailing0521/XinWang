using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WSL.KINGDEE.XW.PlugIn;
using WSL.KINGDEE.XW.PlugIn.NewBuilder;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            SyncProduct();

            #region 调用接口
            // string url = "https://ent.safe517.com/fdWebCompany/webservice/erpInputBatchDetail";

            // string content = @"{ ""supplier"":{ ""supName"":""上海XX供应商"",""supAddress"":""上海静安XXXXX路88号"",""supCateringCert"":null,""supFoodBusinessCert"":""JY123456789ASDFh"",""supFoodCircuCert"":null,""supFoodProdCert"":null,""supBizCertNum"":""123454456X12345"",""supCode"":null,""supNameAbbrev"":"" "",""supContactPerson"":""测试"",""supContactPhone"":""10086""},""material"":[{ ""name"":""上传测试物料"",""spec"":"" 12"",""manufacture"":""上海xxxx"",""guaranteeValue"":12,""guaranteeUnit"":""日"",""typeGeneral"":""70017"",""code"":null,""nameEn"":null,""placeOfProduction"":710502,""productionBarcode"":"" ""}],""batchDetail"":[{ ""recordDate"":""2021-08-06"",""materialName"":""上传测试物料"",""manufacture"":""上海xxxx"",""spec"":"" 12"",""quantity"":0.3300000000,""productionDate"":""2021-08-06"",""productionBatch"":""20210725"",""traceCode"":null,""quarantineCert"":""445566""}]}";

            // IDictionary<string, string> param = new Dictionary<string, string>();
            // param.Add("appID", "2016");
            // param.Add("key", "e5b7c09d31a7429eb07dfe85f2d9c75f");
            // param.Add("companyID", "18e867a");
            // param.Add("content", content);
            // string requestInfo = ApiHelper.BuildQuery(param);

            //string responseInfo = ApiHelper.HttpPostFrom(url, requestInfo);
            #endregion
        }

        static void SyncProduct()
        {
            NewApiHelper newApiHelper 
                = new NewApiHelper("shfda-e029132d-8172-4d8f-bcd8-fb247f51cc89", "e2391054-0618-41be-955a-e3d07332eddb");

        }
    }
}
