using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Transactions;

namespace WSL.KINGDEE.XW.PlugIn
{
    [Description("进货台账查询动态表单插件")]
    public class InStockSearch: AbstractDynamicFormPlugIn
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
                i ++;
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
                    string url = "https://ent.safe517.com/fdWebCompany/webservice/erpInputBatchDetail";
                    string appID = "";
                    string key = "";
                    string companyID = "";
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

                    object appIDValue = systemParameterService.GetParamter(this.Context,
                             Convert.ToInt64(orgId)
                              , 0L, "KCY_StockParameter", "F_ora_Text", 0L);

                    object keyValue = systemParameterService.GetParamter(this.Context,
                               Convert.ToInt64(orgId)
                              , 0L, "KCY_StockParameter", "F_ora_Text1", 0L);

                    object companyIDValue = systemParameterService.GetParamter(this.Context,
                               Convert.ToInt64(orgId)
                              , 0L, "KCY_StockParameter", "F_ora_Text2", 0L);

                    appID = appIDValue.ToString();
                    key = keyValue.ToString();
                    companyID = companyIDValue.ToString();

                    if (string.IsNullOrWhiteSpace(companyID))
                    {
                        continue;
                    }
                    #endregion

                    InStockModel inStockModel = new InStockModel();

                    #region 供应商处理
                    Supplier supplier = new Supplier();
                    DynamicObject supplierObj = entry["FSupplier"] as DynamicObject;
                    string supplierId = supplierObj["Id"].ToString();
                    supplier.supName = supplierObj["Name"].ToString();
                    //判断供应商是否已经传递
                    
                    sql = $@"
                        SELECT  ISNULL(F_ora_ComboXW3,'2')F_ora_ComboXW3
                          FROM  T_BD_Supplier 
                         WHERE  FSupplierId = '{supplierId}'";
                    DynamicObjectCollection supResults
                        = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    //if (supResults[0]["F_ora_ComboXW3"].ToString() != "1")
                    //{
                        //supplier.supCode = supplierObj["Number"].ToString();
                        supplier.supNameAbbrev = supplierObj["ShortName"].ToString();
                        DynamicObjectCollection supplierBase = supplierObj["SupplierBase"] as DynamicObjectCollection;
                        supplier.supAddress = supplierBase[0]["RegisterAddress"].ToString();
                        //营业执照
                        if (entry["FYYZH"] != null)
                        {
                            supplier.supBizCertNum = entry["FYYZH"].ToString();
                        }
                        //食品流通许可证/食品生产许可证/食品经营许可证
                        if (entry["FJZXKZ"] != null)
                        {
                            supplier.supFoodBusinessCert = entry["FJZXKZ"].ToString();
                        }
                        //生产证
                        if (entry["FSCZ"] != null)
                        {
                            supplier.supFoodProdCert = entry["FSCZ"].ToString();
                        }
                        if (entry["FPeople"] != null)
                        {
                            supplier.supContactPerson = entry["FPeople"].ToString();
                        }
                        if (entry["FPhone"] != null)
                        {
                            supplier.supContactPhone = entry["FPhone"].ToString();
                        }
                    //}
                    inStockModel.supplier = supplier;
                    #endregion

                    #region 物料处理
                    List<Material> materials = new List<Material>();
                    Material material = new Material();
                    DynamicObject materialObj = entry["FMaterialID"] as DynamicObject;
                    string materialId = materialObj["Id"].ToString();
                    //判断物料是否已经传递
                    //sql = $@"
                    //    SELECT  ISNULL(F_ora_ComboXW3,'2')F_ora_ComboXW3
                    //      FROM  T_BD_MATERIAL 
                    //     WHERE  FMaterialId = '{materialId}'";
                    //DynamicObjectCollection materialResults
                    //    = DBUtils.ExecuteDynamicObject(this.Context, sql);
                    
                    material.name = materialObj["Name"].ToString();
                    material.spec = materialObj["Specification"].ToString();
                    DynamicObject sccj = entry["FSCCJ"] as DynamicObject;
                    if (sccj != null)
                    {
                        material.manufacture = sccj["FDataValue"].ToString();
                    }
                    else
                    {
                        throw new Exception("生产厂家不能为空");
                    }

                    //if (materialResults[0]["F_ora_ComboXW3"].ToString() == "2")
                    //{
                    //material.code = materialObj["Number"].ToString();
                    //if (materialObj["OldNumber"] != null && !string.IsNullOrWhiteSpace(materialObj["OldNumber"].ToString()))
                    //{
                    //    material.code = materialObj["OldNumber"].ToString();
                    //}

                    DynamicObjectCollection materialStock = materialObj["MaterialStock"] as DynamicObjectCollection;
                    material.guaranteeValue = Convert.ToInt32(materialStock[0]["ExpPeriod"]);
                    string expUnit = materialStock[0]["ExpUnit"].ToString();
                    if (expUnit == "D")
                    {
                        expUnit = "日";
                    }
                    if (expUnit == "M")
                    {
                        expUnit = "月";
                    }
                    if (expUnit == "Y")
                    {
                        expUnit = "年";
                    }
                    material.guaranteeUnit = expUnit;

                    DynamicObject categroy = entry["FCategroyID"] as DynamicObject;
                    if (categroy != null)
                    {
                        material.typeGeneral = categroy["Number"].ToString();
                    }

                    string cdName = "中国";
                    DynamicObject cd = entry["FCD"] as DynamicObject;
                    if (cd != null)
                    {
                        cdName = cd["FDataValue"].ToString();
                        material.placeOfProduction = Convert.ToInt32(cd["FNumber"]);
                    }

                    if (entry["FBarcode"] != null)
                    {
                        material.productionBarcode = entry["FBarcode"].ToString();
                    }
                    // }
                    materials.Add(material);
                    inStockModel.material = materials;
                    #endregion

                    #region 明细处理
                    List<BatchDetail> batchDetails = new List<BatchDetail>();
                    BatchDetail batchDetail = new BatchDetail();
                    batchDetail.recordDate = Convert.ToDateTime(entry["FDate"]).ToString("yyyy-MM-dd");
                    batchDetail.materialName = materialObj["Name"].ToString();
                    if (sccj != null)
                    {
                        batchDetail.manufacture = sccj["FDataValue"].ToString();
                    }
                    
                    batchDetail.spec = materialObj["Specification"].ToString();
                    batchDetail.quantity = Convert.ToDecimal(entry["FQty"]);
                    batchDetail.productionDate = Convert.ToDateTime(entry["FProduceDate"]).ToString("yyyy-MM-dd");
                    DynamicObject FLot = entry["FLot"] as DynamicObject;
                    if (FLot != null)
                    {
                        batchDetail.productionBatch = FLot["Number"].ToString();
                    }
                    if (!cdName.Contains("中国"))
                    {
                        if (entry["FJYNumber"] == null || string.IsNullOrWhiteSpace(entry["FJYNumber"].ToString()))
                        {
                            throw new KDException("错误", "产地为外国时，检验检疫证书不能为空");
                        }
                    }
                    if (entry["FJYNumber"] != null)
                    {
                        batchDetail.quarantineCert = entry["FJYNumber"].ToString();
                    }
                    
                    batchDetails.Add(batchDetail);
                    inStockModel.batchDetail = batchDetails;
                    #endregion

                    #region 调用接口
                    string content = JsonConvert.SerializeObject(inStockModel);

                    IDictionary<string, string> param = new Dictionary<string, string>();
                    param.Add("appID", appID);
                    param.Add("key", key);
                    param.Add("companyID", companyID);
                    param.Add("content", content);
                    requestInfo = ApiHelper.BuildQuery(param);

                    responseInfo = ApiHelper.HttpPostFrom(url, requestInfo);
                    if (string.IsNullOrWhiteSpace(responseInfo))
                    {
                        throw new Exception("接口返回的消息为空值");
                    }
                    #endregion

                    #region 处理返回信息
                    ResponseData responseData = JsonConvert.DeserializeObject<ResponseData>(responseInfo);
                    if (responseData == null)
                    {
                        throw new Exception("接口返回的对象为空值");
                    }
                    if (responseData.status == 0)
                    {
                        //成功 更新上传追溯系统标识
                        //更新供应商上传标识
                        sql = $@"UPDATE T_BD_SUPPLIER SET F_ora_ComboXW3 = '1' WHERE FSupplierId = '{supplierId}'";
                        DBUtils.Execute(this.Context, sql);
                        //更新物料上传标识
                        sql = $@"UPDATE T_BD_MATERIAL SET F_ora_ComboXW3 = '1' WHERE FMaterialId = '{materialId}'";
                        DBUtils.Execute(this.Context, sql);
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

                    if (responseData.status == 1)
                    {
                        //失败
                        status = "E";
                        message = responseData.message;
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
