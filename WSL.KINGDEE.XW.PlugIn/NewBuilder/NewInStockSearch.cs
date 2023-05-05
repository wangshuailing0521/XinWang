using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.Cache;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Transactions;

namespace WSL.KINGDEE.XW.PlugIn.NewBuilder
{
    [Description("新进货台账查询动态表单插件-20230505")]
    [HotUpdate]
    public class NewInStockSearch : AbstractDynamicFormPlugIn
    {
        NewApiHelper newApiHelper = new NewApiHelper("", "");

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
            string supplierId = "";
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
            DynamicObject supplier = billObj["FHeadSupplierID"] as DynamicObject;
            if (supplier != null)
            {
                supplierId = supplier["Id"].ToString();
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

            #region 获取采购入库数据
            string sql = $@"
                SELECT  B.FEntryID
                       ,A.FBillNo
                       ,'CG' FType
                       ,A.FDate
                       ,B.FMaterialId
                       ,B.FRealQty
                       ,ISNULL(B.F_ora_Assistantxw,'') FSCCJ
                       ,B.FProduceDate
                       ,A.FSupplierID
                       ,ISNULL(B.FLot,0)FLot
                       ,B.F_ora_Textxw12 FZJBM
                       ,G.FCategoryID
                       ,ISNULL(B.F_ora_cd,'') FCD
                       ,ISNULL(E.FNumber,'') FCDNumber
                       ,ISNULL(G.FBARCODE,'') FBARCODE
                       ,A.FApproveDate
                       ,ISNULL(B.F_ora_Comboxw2,'2')F_ora_Comboxw2
                       ,A.FPurchaseOrgId FOrgId
                       ,I.FExpPeriod
                       ,B.FUnitId
                  FROM  t_STK_InStock A
                        INNER JOIN T_STK_INSTOCKENTRY B
                        ON A.FID = B.FID
                        INNER JOIN T_BD_Material C
                        ON B.FMaterialId = C.FMaterialId
                        INNER JOIN t_BD_MaterialBase G
                        ON B.FMaterialID = G.FMaterialID
                        INNER JOIN T_BD_MATERIALCATEGORY H
                        ON G.FCategoryID = H.FCATEGORYID
                        LEFT JOIN T_BAS_ASSISTANTDATAENTRY E
                        ON B.F_ora_cd = E.FENTRYID 
                        LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L F
                        ON B.F_ora_cd = F.FENTRYID AND F.FLocaleID = 2052
                        INNER JOIN T_BD_MaterialStock I
                        ON B.FMaterialId = I.FMaterialId
                 WHERE  A.FDOCUMENTSTATUS = 'C'
                   AND  A.FDate >= '{beginDate}'
                   AND  A.FDate < '{endDate}'
                   AND  H.FNumber <> '99999'
                ";
            if (!string.IsNullOrWhiteSpace(categroyIds))
            {
                sql = sql + $@" AND G.FCategoryID IN ({categroyIds}) ";
            }
            if (!string.IsNullOrWhiteSpace(supplierId))
            {
                sql = sql + $@" AND A.FSupplierID = '{supplierId}' ";
            }
            if (!string.IsNullOrWhiteSpace(orgId))
            {
                sql = sql + $@" AND A.FPurchaseOrgId = '{orgId}' ";
            }
            if (!string.IsNullOrWhiteSpace(stockId))
            {
                sql = sql + $@" AND B.FStockId = '{stockId}' ";
            }
            if (!string.IsNullOrWhiteSpace(materialId))
            {
                sql = sql + $@" AND B.FMaterialId = '{materialId}' ";
            }
            if (!string.IsNullOrWhiteSpace(cdName))
            {
                sql = sql + $@" AND F.FDataValue LIKE '%{cdName}%' ";
            }
            if (syncStatus == "A")
            {
                sql = sql + $@" AND B.F_ora_Comboxw2 = '{1}' ";
            }
            if (syncStatus == "B")
            {
                sql = sql + $@" AND B.F_ora_Comboxw2 = '{2}' ";
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
                newRow["FUnit_Id"] = Convert.ToInt32(item["FUnitId"]);
                newRow["FQty"] = Convert.ToDecimal(item["FRealQty"]);
                newRow["FSCCJ_Id"] = item["FSCCJ"].ToString();
                newRow["FProduceDate"] = item["FProduceDate"].ToString();
                newRow["FLot_Id"] = Convert.ToInt32(item["FLot"]);
                newRow["FSZ"] = item["FZJBM"].ToString();
                newRow["FCategroyID_Id"] = Convert.ToInt32(item["FCategoryID"]);
                newRow["FCategroyID1_Id"] = Convert.ToInt32(item["FCategoryID"]);
                newRow["FSupplier_Id"] = Convert.ToInt32(item["FSupplierID"]);
                newRow["FCD_Id"] = item["FCD"].ToString();
                newRow["FCDNumber"] = item["FCDNumber"].ToString();
                newRow["FBarcode"] = item["FBARCODE"].ToString();
                newRow["FJYNumber"] = item["FZJBM"].ToString();
                newRow["FTime"] = item["FApproveDate"].ToString();
                newRow["FSyncStatus"] = item["F_ora_Comboxw2"].ToString();
                newRow["FOrgId_Id"] = Convert.ToInt32(item["FOrgId"]);
                newRow["FBillNo"] = item["FBillNo"].ToString();
                newRow["FPeriodDate"] = Convert.ToInt32(item["FExpPeriod"]);

                #region 获取供应商证件
                sql = $@"
                    SELECT  B.FDataValue FName
                           ,C.FNumber 
                           ,F_ora_TextXW1 FZSNumber
                      FROM  ora_t_Cust_EntryXW1 A
                            INNER JOIN T_BAS_ASSISTANTDATAENTRY_L B 
                            ON A.F_ora_Assistantxw1 = B.FENTRYID AND B.FLOCALEID = 2052
                            INNER JOIN T_BAS_ASSISTANTDATAENTRY C 
                            ON A.F_ora_Assistantxw1 = C.FENTRYID 
                     WHERE  A.FSupplierId = {item["FSupplierID"].ToString()}";

                DynamicObjectCollection yyzss
                    = DBUtils.ExecuteDynamicObject(this.Context, sql);
                foreach (var yyzs in yyzss)
                {
                    //营业执照
                    if (yyzs["FNumber"].ToString() == "1003")
                    {
                        newRow["FYYZH"] = yyzs["FZSNumber"].ToString();
                    }

                    //食品流通许可证/食品生产许可证/食品经营许可证
                    if (yyzs["FNumber"].ToString() == "1004")
                    {
                        newRow["FJZXKZ"] = yyzs["FZSNumber"].ToString();
                    }

                    //生产证
                    if (yyzs["FNumber"].ToString() == "1005")
                    {
                        newRow["FSCZ"] = yyzs["FZSNumber"].ToString();
                    }
                }
                #endregion

                #region 获取供应商联系人信息
                sql = $@"
                    SELECT  ISNULL(FContact,'')FContact
                           ,ISNULL(FMobile,'') FMobile
                      FROM  t_BD_SupplierContact A
                     WHERE  A.FSupplierId = {item["FSupplierID"].ToString()}
                       AND  A.FISDEFAULT = '1'";

                DynamicObjectCollection receivers
                    = DBUtils.ExecuteDynamicObject(this.Context, sql);
                foreach (var receiver in receivers)
                {
                    newRow["FPeople"] = receiver["FContact"].ToString();
                    newRow["FPhone"] = receiver["FMobile"].ToString();
                }
                #endregion


                Entrys.Add(newRow);
            }
            #endregion

            #region 获取门店收货数据
            sql = $@"
                SELECT  B.FEntryID
                       ,A.FBillNo
                       ,'MD' FType
                       ,A.FDate
                       ,B.FMaterialId
                       ,B.FRealQty
                       ,ISNULL(B.F_ora_sccj1,'') FSCCJ
                       ,B.FProduceDate
                       ,B.FDETAILSUPPLIERID FSupplierId
                       ,ISNULL(B.FLot,0)FLot
                       ,'' FZJBM
                       ,G.FCategoryID
                       ,ISNULL(B.F_ora_cd1,'') FCD
                       ,ISNULL(E.FNumber,'') FCDNumber
                       ,G.FBARCODE
                       ,A.FApproveDate
                       ,B.F_ora_bs1 F_ora_Comboxw2
                       ,A.FStockOrgId FOrgId
                       ,I.FExpPeriod
                       ,B.FUnitId
                  FROM  T_SCMS_ALLOTRECEIPT A
                        INNER JOIN T_SCMS_ALLOTRECEIPTENTRY B
                        ON A.FID = B.FID
                        INNER JOIN T_BD_Material C
                        ON B.FMaterialId = C.FMaterialId
                        INNER JOIN t_BD_MaterialBase G
                        ON B.FMaterialID = G.FMaterialID
                        INNER JOIN T_BD_MATERIALCATEGORY H
                        ON G.FCategoryID = H.FCATEGORYID
                        LEFT JOIN T_BAS_ASSISTANTDATAENTRY E
                        ON B.F_ora_cd1 = E.FENTRYID 
                        LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L F
                        ON B.F_ora_cd1 = F.FENTRYID AND F.FLocaleID = 2052
                        INNER JOIN T_BD_MaterialStock I
                        ON B.FMaterialId = I.FMaterialId
                 WHERE  A.FDOCUMENTSTATUS = 'C'
                   AND  A.FDate >= '{beginDate}'
                   AND  A.FDate < '{endDate}'
                   AND  H.FNumber <> '99999'
                ";
            if (!string.IsNullOrWhiteSpace(categroyIds))
            {
                sql = sql + $@" AND G.FCategoryID IN ({categroyIds}) ";
            }
            if (!string.IsNullOrWhiteSpace(supplierId))
            {
                sql = sql + $@" AND B.FDETAILSUPPLIERID = '{supplierId}' ";
            }
            if (!string.IsNullOrWhiteSpace(orgId))
            {
                sql = sql + $@" AND A.FStockOrgId = '{orgId}' ";
            }
            if (!string.IsNullOrWhiteSpace(stockId))
            {
                sql = sql + $@" AND B.FStockId = '{stockId}' ";
            }
            if (!string.IsNullOrWhiteSpace(materialId))
            {
                sql = sql + $@" AND B.FMaterialId = '{materialId}' ";
            }
            if (!string.IsNullOrWhiteSpace(cdName))
            {
                sql = sql + $@" AND F.FDataValue LIKE '%{cdName}%' ";
            }
            if (syncStatus == "A")
            {
                sql = sql + $@" AND B.F_ora_bs1 = '{1}' ";
            }
            if (syncStatus == "B")
            {
                sql = sql + $@" AND B.F_ora_bs1 = '{2}' ";
            }
            if (!string.IsNullOrWhiteSpace(psOrgId))
            {
                sql = sql + $@" AND A.FDeliveryOrgId = '{psOrgId}' ";
            }

            data
                = DBUtils.ExecuteDynamicObject(this.Context, sql);


            foreach (DynamicObject item in data)
            {
                DynamicObject newRow = new DynamicObject(entity.DynamicObjectType);

                newRow["FType"] = item["FType"].ToString();
                newRow["FDate"] = item["FDate"].ToString();
                newRow["FEntryID"] = item["FEntryID"].ToString();
                newRow["FMaterialID_Id"] = Convert.ToInt32(item["FMaterialId"]);
                newRow["FUnit_Id"] = Convert.ToInt32(item["FUnitId"]);
                newRow["FQty"] = Convert.ToDecimal(item["FRealQty"]);
                newRow["FSCCJ_Id"] = item["FSCCJ"].ToString();
                newRow["FProduceDate"] = item["FProduceDate"].ToString();
                newRow["FLot_Id"] = Convert.ToInt32(item["FLot"]);
                newRow["FSZ"] = item["FZJBM"].ToString();
                newRow["FCategroyID_Id"] = Convert.ToInt32(item["FCategoryID"]);
                newRow["FCategroyID1_Id"] = Convert.ToInt32(item["FCategoryID"]);
                newRow["FSupplier_Id"] = Convert.ToInt32(item["FSupplierID"]);
                newRow["FCD_Id"] = item["FCD"].ToString();
                newRow["FCDNumber"] = item["FCDNumber"].ToString();
                newRow["FBarcode"] = item["FBARCODE"].ToString();
                newRow["FJYNumber"] = item["FZJBM"].ToString();
                newRow["FTime"] = item["FApproveDate"].ToString();
                newRow["FSyncStatus"] = item["F_ora_Comboxw2"].ToString();
                newRow["FOrgId_Id"] = Convert.ToInt32(item["FOrgId"]);
                newRow["FBillNo"] = item["FBillNo"].ToString();
                newRow["FPeriodDate"] = Convert.ToInt32(item["FExpPeriod"]);

                #region 获取供应商证件
                sql = $@"
                    SELECT  B.FDataValue FName
                           ,C.FNumber 
                           ,F_ora_TextXW1 FZSNumber
                      FROM  ora_t_Cust_EntryXW1 A
                            INNER JOIN T_BAS_ASSISTANTDATAENTRY_L B 
                            ON A.F_ora_Assistantxw1 = B.FENTRYID AND B.FLOCALEID = 2052
                            INNER JOIN T_BAS_ASSISTANTDATAENTRY C 
                            ON A.F_ora_Assistantxw1 = C.FENTRYID 
                     WHERE  A.FSupplierId = {item["FSupplierID"].ToString()}";

                DynamicObjectCollection yyzss
                    = DBUtils.ExecuteDynamicObject(this.Context, sql);
                foreach (var yyzs in yyzss)
                {
                    //营业执照
                    if (yyzs["FNumber"].ToString() == "1003")
                    {
                        newRow["FYYZH"] = yyzs["FZSNumber"].ToString();
                    }

                    //食品流通许可证/食品生产许可证/食品经营许可证
                    if (yyzs["FNumber"].ToString() == "1004")
                    {
                        newRow["FJZXKZ"] = yyzs["FZSNumber"].ToString();
                    }

                    //生产证
                    if (yyzs["FNumber"].ToString() == "1005")
                    {
                        newRow["FSCZ"] = yyzs["FZSNumber"].ToString();
                    }
                }
                #endregion

                #region 获取供应商联系人信息
                sql = $@"
                    SELECT  ISNULL(FContact,'')FContact
                           ,ISNULL(FMobile,'')FMobile
                      FROM  t_BD_SupplierContact A
                     WHERE  A.FSupplierId = {item["FSupplierID"].ToString()}
                       AND  A.FISDEFAULT = '1'";

                DynamicObjectCollection receivers
                    = DBUtils.ExecuteDynamicObject(this.Context, sql);
                foreach (var receiver in receivers)
                {
                    newRow["FPeople"] = receiver["FContact"].ToString();
                    newRow["FPhone"] = receiver["FMobile"].ToString();
                }
                #endregion


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
                    string url = "https://spzs.scjgj.sh.gov.cn/p4/api/v1/data/in";
                    string token = newApiHelper.GetAccessToken();
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
                    NewSyncMaterial.Sync(this.Context, token, materialObj);
                    #endregion

                    NewInStockModel inStockModel = new NewInStockModel();

                    inStockModel.tagSn = entry["FBillNo"].ToString() + "-" + entry["FEntryID"].ToString();
                    inStockModel.tagSnProducerCode = enterpriseCode;
                    inStockModel.enterpriseCode = enterpriseCode;


                    inStockModel.dataDate = Convert.ToDateTime(entry["FDate"]).ToString("yyyy-MM-dd");
                    inStockModel.productCode = materialObj["Number"].ToString();


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

                    inStockModel.production = production;

                    #endregion

                    #region 采购信息处理
                    PurchaseModel purchase = new PurchaseModel();

                    #region 入库数量
                    purchase.quantity = Convert.ToDecimal(entry["FQty"]).ToString();
                    #endregion

                    #region 计量单位
                    DynamicObject unit = entry["FUnit"] as DynamicObject;
                    if (unit != null)
                    {
                        purchase.unit = unit["Name"].ToString();
                    }
                    #endregion

                    #region 入库日期
                    purchase.purchaseDate = Convert.ToDateTime(entry["FDate"]).ToString("yyyy-MM-dd");
                    #endregion

                    #region 供应商
                    DynamicObject supplierObj = entry["FSupplier"] as DynamicObject;
                    purchase.vendorName = supplierObj["Name"].ToString();

                    DynamicObjectCollection supplierBase = supplierObj["SupplierBase"] as DynamicObjectCollection;
                    purchase.vendorAddr = supplierBase[0]["RegisterAddress"].ToString();

                    if (entry["FPhone"] != null)
                    {
                        purchase.vendorTel = entry["FPhone"].ToString();
                    }

                    #endregion

                    inStockModel.purchase = purchase;
                    #endregion

                    #region 调用接口
                    requestInfo = JsonConvert.SerializeObject(inStockModel);

                    responseInfo = newApiHelper.Post(url, requestInfo, token);
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
                        if (type == "CG")
                        {
                            sql = $@"UPDATE T_STK_INSTOCKENTRY SET F_ora_Comboxw2 = '1' WHERE FEntryID = {entryId}";
                            DBUtils.Execute(this.Context, sql);
                        }
                        if (type == "MD")
                        {
                            sql = $@"UPDATE T_SCMS_ALLOTRECEIPTENTRY SET F_ora_bs1 = '1' WHERE FEntryID = {entryId}";
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

     
    }
}
