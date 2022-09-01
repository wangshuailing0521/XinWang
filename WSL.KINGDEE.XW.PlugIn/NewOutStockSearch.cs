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

namespace WSL.KINGDEE.XW.PlugIn
{
    [Description("新出货台账查询动态表单插件")]
    [HotUpdate]
    public class NewOutStockSearch: AbstractDynamicFormPlugIn
    {
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
                       ,B.FRealQty
                       ,B.FProduceDate
                       ,A.FCustomerID
                       ,ISNULL(B.FLot,0)FLot
                       ,G.FCategoryID
                       ,ISNULL(G.FBARCODE,'') FBARCODE
                       ,A.FApproveDate
                       ,A.FSaleOrgId FOrgId
                       ,I.FExpPeriod
                       ,B.FUnitId
                       ,ISNULL(E.FDATAVALUE,'') FSCCJ
                       ,ISNULL(D.F_XW_SCCJ,'') FSCCJID
                       ,ISNULL(B.F_ora_xw6,'') FCD
                       ,ISNULL(M.FNumber,'') FCDNumber
                       ,K.FTEL
                       ,B.F_ora_XW7 FZJBM
                       ,ISNULL(B.F_ora_Comboxw5,'2')F_ora_Comboxw5
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
                    string url = "https://www.shfda.org/platform/rest/v2/tracedata/catering/out";
                    string token = "";
                    string enterpriseCode = "";
                    string sql = "";
                    requestInfo = "";
                    responseInfo = "";
                    status = "S";
                    message = "";

                    #region 当组织上companyId为空时，不传递

                    ISystemParameterService systemParameterService
                      = ServiceFactory.GetSystemParameterService(this.Context);

                    object autoValue = systemParameterService.GetParamter(this.Context,
                             Convert.ToInt64(orgId)
                             , 0L, "KCY_StockParameter", "F_ora_CheckBox", 0L);

                    object tokenValue = systemParameterService.GetParamter(this.Context,
                             Convert.ToInt64(orgId)
                              , 0L, "KCY_StockParameter", "F_ora_Text3", 0L);

                    object enterpriseCodeValue = systemParameterService.GetParamter(this.Context,
                               Convert.ToInt64(orgId)
                              , 0L, "KCY_StockParameter", "F_ora_Text4", 0L);

                    token = tokenValue.ToString();
                    enterpriseCode = enterpriseCodeValue.ToString();

                    //token = "9016229|4a7472ab-c0d3-4a8a-81c1-a31eaa4ee433";
                    //enterpriseCode = "91310000MA1FP4EB9H";

                    if (string.IsNullOrWhiteSpace(enterpriseCode))
                    {
                        continue;
                    }
                    #endregion


                    #region 物料处理，当物料未上传时，先上传物料
                    DynamicObject materialObj = entry["FMaterialID"] as DynamicObject;
                    SyncMaterial(token, materialObj);
                    #endregion

                    NewOutStockModel outStockModel = new NewOutStockModel();

                    outStockModel.tagSn = entry["FBillNo"].ToString() +"-"+ entry["FEntryID"].ToString();
                    outStockModel.tagSnProducerCode = enterpriseCode;
                    outStockModel.enterpriseCode = enterpriseCode;

                    outStockModel.dataDate = Convert.ToDateTime(entry["FDate"]).ToString("yyyy-MM-dd");
                    outStockModel.productCode = materialObj["Number"].ToString();

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
                    string cdName = "中国";
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
                    sales.customerName = customerObj["Name"].ToString();
                    sales.customerAddr = customerObj["ADDRESS"].ToString();

                    if (entry["FPhone"] != null)
                    {
                        sales.customerTel = entry["FPhone"].ToString();
                    }

                    #endregion

                    outStockModel.sales = sales;
                    #endregion

                    #region 调用接口
                    requestInfo = JsonConvert.SerializeObject(outStockModel);

                    responseInfo = ApiHelper.Post(url, requestInfo, token);
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
                        message = responseData.errorMsg;
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

        void SyncMaterial(string token, DynamicObject materialObj)
        {
            NewMaterial material = new NewMaterial();

            #region 物料处理

            string materialId = materialObj["Id"].ToString();
            //判断物料是否已经传递
            string sql = $@"
                SELECT  ISNULL(F_ora_ComboXW4,'2')F_ora_ComboXW4
                  FROM  T_BD_MATERIAL 
                 WHERE  FMaterialId = '{materialId}'
                   AND  ISNULL(F_ora_ComboXW4,'') <> '1'";
            DynamicObjectCollection materialResults
                = DBUtils.ExecuteDynamicObject(this.Context, sql);

            if (materialResults.Count <= 0)
            {
                return;
            }

            sql = $@"
                SELECT  A.FNumber
                       ,B.FName
                       ,B.FSpecification
                       ,ISNULL(E.FDATAVALUE,'') FSCCJ
                       ,ISNULL(I.FNumber,0) FCD
                       ,F.FIsKFPeriod
                       ,F.FExpUnit
                       ,F.FExpPeriod
                       ,H.FNumber FGroup
                       ,ISNULL(G.FBARCODE,'')FBARCODE
                  FROM  T_BD_MATERIAL A
                        INNER JOIN T_BD_MATERIAL_L B
                        ON A.FMaterialID = B.FMaterialID AND B.FLOCALEID = 2052
                        INNER JOIN t_bd_MaterialPurchase D
                        ON A.FMaterialID = D.FMaterialID
                        LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L E
                        ON D.F_XW_SCCJ = E.FENTRYID AND E.FLOCALEID = 2052
                        INNER JOIN t_BD_MaterialStock F
                        ON A.FMaterialID = F.FMaterialID
                        INNER JOIN t_BD_MaterialBase G
                        ON A.FMaterialID = G.FMaterialID
                        INNER JOIN T_BD_MATERIALCATEGORY H
                        ON G.FCategoryID = H.FCATEGORYID
                        LEFT JOIN T_BAS_ASSISTANTDATAENTRY I
                        ON D.F_ora_cd = I.FENTRYID
                        LEFT JOIN T_BAS_PREBDONE K
                        ON K.FID = A.F_ora_BaseXW9
                 WHERE  A.FMaterialId = {materialId} 
                ";
            DynamicObjectCollection materialDatas
                = DBUtils.ExecuteDynamicObject(this.Context, sql);


            material.code = materialObj["Number"].ToString();
            material.name = materialObj["Name"].ToString();
            material.standard = materialObj["Specification"].ToString();
            material.barcode = materialDatas[0]["FBARCODE"].ToString();
            material.producerName = materialDatas[0]["FSCCJ"].ToString();
            material.category = materialDatas[0]["FGroup"].ToString();

            string expUnit = materialDatas[0]["FExpUnit"].ToString();
            material.guaranteeDays = materialDatas[0]["FExpPeriod"].ToString();
            if (expUnit == "M")
            {
                material.guaranteeDays = (Convert.ToInt32(materialDatas[0]["FExpPeriod"]) * 30).ToString();
            }
            if (expUnit == "Y")
            {
                material.guaranteeDays = (Convert.ToInt32(materialDatas[0]["FExpPeriod"]) * 365).ToString();
            }



            #endregion

            #region 调用接口

            string requestInfo = "";
            string responseInfo = "";
            string status = "S";
            string message = "";

            try
            {
                string enterpriseCode = "XWHQ913100006315557312";
                string url = $@"https://www.shfda.org/platform/rest/v2/enterprises/{enterpriseCode}/products";

                requestInfo = JsonConvert.SerializeObject(material);

                responseInfo = ApiHelper.Post(url, requestInfo, token);
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

                    //更新物料上传标识
                    sql = $@"UPDATE T_BD_MATERIAL SET F_ora_ComboXW4 = '1' WHERE FMaterialId = '{materialId}'";
                    DBUtils.Execute(this.Context, sql);
                }
                else
                {
                    message = responseData.errorMsg;
                    throw new KDException("错误", $@"上传物料{materialObj["Name"]}失败：{message}");
                }
            }
            catch (Exception ex)
            {
                status = "E";
                message = ex.Message.Replace("'", "''");
            }
            finally
            {
                using (KDTransactionScope kDTransactionScope = new KDTransactionScope(TransactionScopeOption.RequiresNew))
                {
                    //记录日志
                    var ids = DBServiceHelper.GetSequenceInt64(this.Context, "ORA_T_InterfaceLog", 1);
                    long id = ids.ElementAt(0);
                    string logBillNo = material.code + id.ToString();
                    string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    requestInfo = requestInfo.Replace("'", "''");
                    responseInfo = responseInfo.Replace("'", "''");
                    sql = $@"
                        INSERT INTO ORA_T_InterfaceLog(
                            FID,FBillNo,FSyncBillNo,FTime,
                            FRequest,FResponse,FStatus,FMessage)
                        SELECT 
                            {id},'{logBillNo}','{material.code}','{date}',
                            '{requestInfo}','{responseInfo}','{status}','{message}'";
                    DBUtils.Execute(this.Context, sql);

                    kDTransactionScope.Complete();
                }
            }

           
            #endregion
        }
    }
}
