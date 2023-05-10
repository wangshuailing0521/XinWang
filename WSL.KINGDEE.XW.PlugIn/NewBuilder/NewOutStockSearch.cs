using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Transactions;

namespace WSL.KINGDEE.XW.PlugIn.NewBuilder
{
    [Description("新出货台账查询动态表单插件-20230505")]
    [HotUpdate]
    public class NewOutStockSearch : AbstractDynamicFormPlugIn
    {
        NewApiHelper newApiHelper = null;

        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);

            if (e.Key.ToUpper() == "FSEARCH")
            {
                SetData();
            }

            if (e.Key.ToUpper() == "FSYNCUP")
            {
                Sync();
                SetData();
            }
        }

        void SetData()
        {
            DynamicObject billObj = this.Model.DataObject;

            DynamicObjectCollection Entrys
                = billObj["FEntity"] as DynamicObjectCollection;

            Entity entity =
                this.View.BusinessInfo.GetEntity("FEntity");

            Entrys.Clear();

            string beginDate = billObj["FBeginDate"].ToString();
            string endDate = Convert.ToDateTime(billObj["FEndDate"].ToString()).AddDays(1).ToString("yyyy-MM-dd");
            string categroyIds = "";
            string customerId = "";
            string orgId = "";
            string stockId = "";
            string materialId = "";
            string cdName = "";
            string psOrgId = "";
            string syncStatus = "";
            DynamicObjectCollection categorys = billObj["FHeadCategory"] as DynamicObjectCollection;
            List<string> categoryList = new List<string>();
            if (categorys != null)
            {
                foreach (DynamicObject category in categorys)
                {
                    string categroyId = category["FHeadCategory_Id"].ToString();
                    categoryList.Add(categroyId);
                }
                categroyIds = string.Join(",", categoryList);
            }
            DynamicObject customer = billObj["FHeadCustomerID"] as DynamicObject;
            if (customer != null)
            {
                customerId = customer["Id"].ToString();
            }
            DynamicObject org = billObj["FHeadOrgID"] as DynamicObject;
            if (org != null)
            {
                orgId = org["Id"].ToString();
            }
            DynamicObject stock = billObj["FHeadStockID"] as DynamicObject;
            if (stock != null)
            {
                stockId = stock["Id"].ToString();
            }
            DynamicObject headMaterial = billObj["FHeadMaterialID"] as DynamicObject;
            if (headMaterial != null)
            {
                materialId = headMaterial["Id"].ToString();
            }
            DynamicObject psOrg = billObj["FHeadPSOrgID"] as DynamicObject;
            if (psOrg != null)
            {
                psOrgId = psOrg["Id"].ToString();
            }
            if (billObj["FHeadCDName"] != null)
            {
                cdName = billObj["FHeadCDName"].ToString();
            }

            syncStatus = billObj["FHeadSyncStatus"].ToString();

            #region 获取配送出库数据
            string sql = $@"
                SELECT  B.FEntryID
                       ,A.FBillNo
                       ,'PS' FType
                       ,A.FDate
                       ,B.FMaterialId
                       ,B.F_ora_SYJ FRealQty
                       ,B.FProduceDate
                       ,A.FCustomerID
                       ,ISNULL(B.FLot,0)FLot
                       ,G.FCategoryID
                       ,ISNULL(G.FBARCODE,'') FBARCODE
                       ,A.FApproveDate
                       ,A.FSaleOrgId FOrgId
                       ,I.FExpPeriod
                       ,B.F_ora_SYJ1 FUnitId
                       ,ISNULL(E.FDATAVALUE,'') FSCCJ
                       ,ISNULL(D.F_XW_SCCJ,'') FSCCJID
                       ,ISNULL(B.F_ora_xw6,'') FCD
                       ,ISNULL(M.FNumber,'') FCDNumber
                       ,K.FTEL
                       ,B.F_ora_XW7 FZJBM
                       ,ISNULL(B.F_ora_Comboxw5,'2')F_ora_Comboxw5
                       ,ISNULL(F1.FDataValue,'') FCountry
                       ,ISNULL(F2.FDataValue,'') FProvince
                       ,ISNULL(F3.FDataValue,'') FCity
                  FROM  T_SAL_OUTSTOCK A
                        INNER JOIN T_SAL_OUTSTOCKENTRY B
                        ON A.FID = B.FID
                        INNER JOIN T_BD_Material C
                        ON B.FMaterialId = C.FMaterialId
                        INNER JOIN t_bd_MaterialPurchase D
                        ON B.FMaterialID = D.FMaterialID
                        LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L E
                        ON D.F_XW_SCCJ = E.FENTRYID AND E.FLOCALEID = 2052
                        INNER JOIN t_BD_MaterialBase G
                        ON B.FMaterialID = G.FMaterialID
                        INNER JOIN T_BD_MATERIALCATEGORY H
                        ON G.FCategoryID = H.FCATEGORYID
                        INNER JOIN T_BD_MaterialStock I
                        ON B.FMaterialId = I.FMaterialId
                        INNER JOIN T_BD_CUSTOMER K
                        ON K.FCUSTID = A.FCUSTOMERID
                        LEFT JOIN T_BAS_ASSISTANTDATAENTRY M
                        ON B.F_ora_xw6 = E.FENTRYID 
                        LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L F1
                        ON B.F_ora_XW1 = F1.FENTRYID AND F1.FLocaleID = 2052
                        LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L F2
                        ON B.F_ora_XW2 = F2.FENTRYID AND F2.FLocaleID = 2052
                        LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L F3
                        ON B.F_ora_XW3 = F3.FENTRYID AND F3.FLocaleID = 2052
                 WHERE  A.FDOCUMENTSTATUS = 'C'
                   AND  A.FDate >= '{beginDate}'
                   AND  A.FDate < '{endDate}'
                   AND  H.FNumber <> '99999'
                ";
            if (!string.IsNullOrWhiteSpace(categroyIds))
            {
                sql = sql + $@" AND G.FCategoryID IN ({categroyIds}) ";
            }
            if (!string.IsNullOrWhiteSpace(customerId))
            {
                sql = sql + $@" AND A.FCustomerID = '{customerId}' ";
            }
            if (!string.IsNullOrWhiteSpace(orgId))
            {
                sql = sql + $@" AND A.FSaleOrgId = '{orgId}' ";
            }
            if (!string.IsNullOrWhiteSpace(stockId))
            {
                sql = sql + $@" AND B.FStockId = '{stockId}' ";
            }
            if (!string.IsNullOrWhiteSpace(materialId))
            {
                sql = sql + $@" AND B.FMaterialId = '{materialId}' ";
            }
            if (syncStatus == "A")
            {
                sql = sql + $@" AND B.F_ora_Comboxw5 = '{1}' ";
            }
            if (syncStatus == "B")
            {
                sql = sql + $@" AND B.F_ora_Comboxw5 = '{2}' ";
            }

            DynamicObjectCollection data
                = DBUtils.ExecuteDynamicObject(this.Context, sql);

            foreach (DynamicObject item in data)
            {
                DynamicObject newRow = new DynamicObject(entity.DynamicObjectType);

                newRow["FType"] = item["FType"].ToString();
                newRow["FDate"] = item["FDate"].ToString();
                newRow["FEntryID"] = item["FEntryID"].ToString();
                newRow["FMaterialID_Id"] = Convert.ToInt32(item["FMaterialId"]);
                newRow["FQty"] = Convert.ToDecimal(item["FRealQty"]);
                newRow["FSCCJ_Id"] = item["FSCCJID"].ToString();
                newRow["FCD_Id"] = item["FCD"].ToString();
                newRow["FCDNumber"] = item["FCDNumber"].ToString();
                newRow["FProduceDate"] = item["FProduceDate"].ToString();
                newRow["FLot_Id"] = Convert.ToInt32(item["FLot"]);
                newRow["FCategroyID_Id"] = Convert.ToInt32(item["FCategoryID"]);
                newRow["FCategroyID1_Id"] = Convert.ToInt32(item["FCategoryID"]);
                newRow["FCustomer_Id"] = Convert.ToInt32(item["FCustomerID"]);
                newRow["FBarcode"] = item["FBARCODE"].ToString();
                newRow["FJYNumber"] = item["FZJBM"].ToString();
                newRow["FSyncStatus"] = item["F_ora_Comboxw5"].ToString();
                newRow["FOrgId_Id"] = Convert.ToInt32(item["FOrgId"]);
                newRow["FBillNo"] = item["FBillNo"].ToString();
                newRow["FPeriodDate"] = Convert.ToInt32(item["FExpPeriod"]);
                newRow["FPhone"] = item["FTEL"].ToString();
                newRow["FUnit_Id"] = item["FUnitId"].ToString();

                Entrys.Add(newRow);
            }
            #endregion

            DBServiceHelper.LoadReferenceObject(
               Context,
               Entrys.ToArray(),
               Entrys.DynamicCollectionItemPropertyType,
               false
           );

            this.View.UpdateView("FEntity");

        }

        void Sync()
        {
            newApiHelper = new NewApiHelper("", "");

            DynamicObject billObj = this.Model.DataObject;

            DynamicObjectCollection Entrys
                = billObj["FEntity"] as DynamicObjectCollection;

            StringBuilder sb = new StringBuilder();

            if (Entrys.Where(x => Convert.ToBoolean(x["FCheckBox"])).ToList().Count <= 0)
            {
                sb.AppendLine("未选中任何数据!");
            }

            int i = 0;
            foreach (DynamicObject entry in Entrys)
            {
                i++;
                if (!Convert.ToBoolean(entry["FCheckBox"]))
                {
                    continue;
                }

                if (entry["FSyncStatus"].ToString() == "1")
                {
                    continue;
                }

                string billNo = entry["FBillNo"].ToString();
                string requestInfo = "";
                string responseInfo = "";
                string status = "S";
                string message = "";

                try
                {
                    DynamicObject org = entry["FOrgId"] as DynamicObject;
                    string orgId = org["Id"].ToString();
                    string url = "https://spzs.scjgj.sh.gov.cn/p4/api/v1/data/out";
                    string appId = "";
                    string appSecret = "";
                    string sql = "";
                    requestInfo = "";
                    responseInfo = "";
                    status = "S";
                    message = "";

                    #region 当组织上appId为空时，不传递

                    ISystemParameterService systemParameterService
                      = ServiceFactory.GetSystemParameterService(this.Context);

                    object autoValue = systemParameterService.GetParamter(this.Context,
                              Convert.ToInt64(orgId)
                              , 0L, "KCY_StockParameter", "F_ora_CheckBox", 0L);

                    object appIdValue = systemParameterService.GetParamter(this.Context,
                              Convert.ToInt64(orgId)
                              , 0L, "KCY_StockParameter", "FNewAppID", 0L);

                    object appSecretValue = systemParameterService.GetParamter(this.Context,
                             Convert.ToInt64(orgId)
                              , 0L, "KCY_StockParameter", "FNewAppSecret", 0L);

                    appId = appIdValue.ToString();
                    appSecret = appSecretValue.ToString();

                    if (string.IsNullOrWhiteSpace(appId))
                    {
                        continue;
                    }
                    #endregion


                    #region 物料处理，当物料未上传时，先上传物料
                    DynamicObject materialObj = entry["FMaterialID"] as DynamicObject;
                    NewSyncMaterial.Sync(this.Context, materialObj);
                    #endregion

                    NewOutStockModel outStockModel = new NewOutStockModel();

                    //outStockModel.tagSn = entry["FBillNo"].ToString() + "-" + entry["FEntryID"].ToString();
                    //outStockModel.tagSnProducerCode = enterpriseCode;
                    //outStockModel.enterpriseCode = enterpriseCode;

                    outStockModel.dataDate = Convert.ToDateTime(entry["FDate"]).ToString("yyyy-MM-dd");
                    outStockModel.productCode = materialObj["Number"].ToString().Replace(".", ""); ;
                    outStockModel.productName = materialObj["Name"].ToString();

                    #region 生产信息处理
                    ProductionModel production = new ProductionModel();

                    #region 生产厂家
                    DynamicObject sccj = entry["FSCCJ"] as DynamicObject;
                    if (sccj != null)
                    {
                        production.producerName = sccj["FDataValue"].ToString();
                    }
                    else
                    {
                        throw new Exception("生产厂家不能为空");
                    }
                    #endregion

                    #region 生产日期
                    production.productionDate = Convert.ToDateTime(entry["FProduceDate"]).ToString("yyyy-MM-dd");
                    #endregion

                    #region 批号
                    DynamicObject FLot = entry["FLot"] as DynamicObject;
                    if (FLot != null)
                    {
                        production.batch = FLot["Number"].ToString();
                    }
                    #endregion

                    #region 产地
                    string cdName = entry["FCountry"].ToString();
                    production.country = entry["FCountry"].ToString();
                    production.province = entry["FProvince"].ToString();
                    production.city = entry["FCity"].ToString();

                    DynamicObject cd = entry["FCD"] as DynamicObject;
                    if (cd != null)
                    {
                        production.origin = cd["FDataValue"].ToString();
                    }
                    #endregion

                    #region 检验检疫证书
                    if (!cdName.Contains("中国"))
                    {
                        if (entry["FJYNumber"] == null || string.IsNullOrWhiteSpace(entry["FJYNumber"].ToString()))
                        {
                            throw new KDException("错误", "产地为外国时，检验检疫证书不能为空");
                        }
                    }
                    if (entry["FJYNumber"] != null)
                    {
                        production.certNoOfQuarantine = entry["FJYNumber"].ToString();
                    }
                    #endregion

                    outStockModel.production = production;

                    #endregion

                    #region 销售信息处理
                    SalesModel sales = new SalesModel();

                    #region 出库数量
                    sales.quantity = Convert.ToDecimal(entry["FQty"]).ToString();
                    #endregion

                    #region 计量单位
                    DynamicObject unit = entry["FUnit"] as DynamicObject;
                    if (unit != null)
                    {
                        sales.unit = unit["Name"].ToString();
                    }
                    #endregion

                    #region 出库日期
                    sales.saleDate = Convert.ToDateTime(entry["FDate"]).ToString("yyyy-MM-dd");
                    #endregion

                    #region 客户
                    DynamicObject customerObj = entry["FCustomer"] as DynamicObject;
                    sales.customerSocialCreditCode = customerObj["SOCIALCRECODE"].ToString();
                    sales.customerName = customerObj["Name"].ToString();
                    sales.customerAddr = customerObj["ADDRESS"].ToString();

                    if (entry["FPhone"] != null)
                    {
                        sales.customerTel = entry["FPhone"].ToString();
                    }

                    VendCustModel vendCustModel = new VendCustModel
                    {
                        type = "Cust",
                        id = customerObj["Id"].ToString(),
                        code = customerObj["Number"].ToString(),
                        name = sales.customerName,
                        tel = sales.customerTel,
                        address = sales.customerAddr,
                        socialCreditCode = sales.customerSocialCreditCode
                    };
                    NewSyncVendCust.Sync(this.Context, appId, appSecret, vendCustModel);

                    #endregion

                    outStockModel.sales = sales;
                    #endregion

                    #region 调用接口
                    requestInfo = JsonConvert.SerializeObject(outStockModel);

                    responseInfo = newApiHelper.Post(url, requestInfo);
                    if (string.IsNullOrWhiteSpace(responseInfo))
                    {
                        throw new Exception("接口返回的消息为空值");
                    }
                    #endregion

                    #region 处理返回信息
                    NewResponse responseData = JsonConvert.DeserializeObject<NewResponse>(responseInfo);
                    if (responseData == null)
                    {
                        throw new Exception("接口返回的对象为空值");
                    }
                    if (responseData.success)
                    {
                        //成功 更新上传追溯系统标识
                        //更新单据明细上传标识
                        string entryId = entry["FEntryID"].ToString();
                        string type = entry["FType"].ToString();
                        if (type == "PS")
                        {
                            sql = $@"UPDATE T_SAL_OUTSTOCKEntry SET F_ora_Comboxw5 = '1' WHERE FEntryID = {entryId}";
                            DBUtils.Execute(this.Context, sql);
                        }

                    }
                    else
                    {
                        //失败
                        status = "E";
                        List<ResponseError> responseErrors = responseData.errors;
                        if (responseErrors != null)
                        {
                            message = string.Join(",", responseErrors.Select(x => x.message));
                        }
                        throw new KDException("错误", "上传数据失败：" + message);
                    }


                    #endregion

                }
                catch (Exception ex)
                {
                    status = "E";
                    message = ex.Message.Replace("'", "''");
                    sb.AppendLine($@"第{i}条：{ex.Message},{ex.StackTrace}");
                }
                finally
                {
                    using (KDTransactionScope kDTransactionScope = new KDTransactionScope(TransactionScopeOption.RequiresNew))
                    {
                        //记录日志
                        var ids = DBServiceHelper.GetSequenceInt64(this.Context, "ORA_T_InterfaceLog", 1);
                        long id = ids.ElementAt(0);
                        string logBillNo = billNo + id.ToString();
                        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        requestInfo = requestInfo.Replace("'", "''");
                        responseInfo = responseInfo.Replace("'", "''");
                        string sql = $@"
                        INSERT INTO ORA_T_InterfaceLog(
                            FID,FBillNo,FSyncBillNo,FTime,
                            FRequest,FResponse,FStatus,FMessage)
                        SELECT 
                            {id},'{logBillNo}','{billNo}','{date}',
                            '{requestInfo}','{responseInfo}','{status}','{message}'";
                        DBUtils.Execute(this.Context, sql);

                        kDTransactionScope.Complete();
                    }
                }

            }

            if (!string.IsNullOrWhiteSpace(sb.ToString()))
            {
                this.View.ShowNotificationMessage(sb.ToString());
            }
            else
            {
                this.View.ShowMessage("上传成功");
            }
        }

       
    }
}
