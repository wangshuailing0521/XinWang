﻿using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Log;
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
    [Description("采购入库单 审核")]
    [HotUpdate]
    public class PurInStockAudit: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);

            e.FieldKeys.Add("FDate");

            e.FieldKeys.Add("FSupplierId");
            e.FieldKeys.Add("FMaterialId");
            e.FieldKeys.Add("FRealQty");
            e.FieldKeys.Add("F_ora_Assistantxw");//生产厂家
            e.FieldKeys.Add("F_ora_Textxw");//产地
            e.FieldKeys.Add("FProduceDate");
            e.FieldKeys.Add("FLot");
            e.FieldKeys.Add("F_ora_Textxw12");//证件编码
            e.FieldKeys.Add("FPurchaseOrgId");
            e.FieldKeys.Add("F_ora_Comboxw2");
        }

        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);

            if (e.DataEntitys == null || e.DataEntitys.Count() <= 0)
            {
                return;
            }

            for (int i = 0; i < e.DataEntitys.Count(); i++)
            {
                DynamicObject billObj = e.DataEntitys[i];

                try
                {
                    Sync(billObj);
                }
                catch (Exception ex)
                {
                    Logger.Error("", ex.Message, ex);
                    throw new KDException("错误", ex.Message);
                }
            }
        }

        void Sync(DynamicObject billObj)
        {
            string billNo = billObj["BillNo"].ToString();
            string billId = billObj["Id"].ToString();
            string requestInfo = "";
            string responseInfo = "";
            string status = "S";
            string message = "";
            try
            {
                DynamicObject org = billObj["PurchaseOrgId"] as DynamicObject;
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
                InStockModel inStockModel = new InStockModel();
                DynamicObject supplierObj = billObj["SupplierId"] as DynamicObject;
                DynamicObjectCollection entrys
                 = billObj["InStockEntry"] as DynamicObjectCollection;

                #region 当组织上companyId为空时，不传递

                ISystemParameterService systemParameterService
                  = ServiceFactory.GetSystemParameterService(this.Context);

                object autoValue = systemParameterService.GetParamter(this.Context,
                          (Convert.ToInt64(orgId) == 1) ? 0 : Convert.ToInt64(orgId)
                          , 0L, "KCY_StockParameter", "F_ora_CheckBox", 0L);

                object appIDValue = systemParameterService.GetParamter(this.Context,
                          (Convert.ToInt64(orgId) == 1) ? 0 : Convert.ToInt64(orgId)
                          , 0L, "KCY_StockParameter", "F_ora_Text", 0L);

                object keyValue = systemParameterService.GetParamter(this.Context,
                          (Convert.ToInt64(orgId) == 1) ? 0 : Convert.ToInt64(orgId)
                          , 0L, "KCY_StockParameter", "F_ora_Text1", 0L);

                object companyIDValue = systemParameterService.GetParamter(this.Context,
                          (Convert.ToInt64(orgId) == 1) ? 0 : Convert.ToInt64(orgId)
                          , 0L, "KCY_StockParameter", "F_ora_Text2", 0L);

                bool auto = Convert.ToBoolean(autoValue);
                if (!auto)
                {
                    return;
                }

                appID = appIDValue.ToString();
                key = keyValue.ToString();
                companyID = companyIDValue.ToString();

                if (string.IsNullOrWhiteSpace(companyID))
                {
                    return;
                }
                #endregion

                #region 当前单据数据已传递时，不再传递
                foreach (var entry in entrys)
                {
                    if (entry["F_ora_Comboxw2"].ToString() == "1")
                    {
                        return;
                    }
                }

                #endregion

                #region 供应商处理
                if (supplierObj == null)
                {
                    return;
                }
                string supplierId = supplierObj["Id"].ToString();
                sql = $@"
                SELECT  A.FNumber
                       ,B.FName
                       ,C.FRegisterAddress FAddress
                       ,B.FShortName
                  FROM  T_BD_Supplier A
                        INNER JOIN T_BD_Supplier_L B
                        ON A.FSupplierId = B.FSupplierId AND B.FLocaleID = 2052
                        INNER JOIN t_BD_SupplierBase C
                        ON A.FSupplierId = C.FSupplierId
                 WHERE  A.FSupplierId = {supplierId}";

                DynamicObjectCollection data
                    = DBUtils.ExecuteDynamicObject(this.Context, sql);
                foreach (DynamicObject item in data)
                {
                    sql = $@"
                    SELECT  B.FDataValue FName
                           ,C.FNumber 
                           ,F_ora_TextXW1 FZSNumber
                      FROM  ora_t_Cust_EntryXW1 A
                            INNER JOIN T_BAS_ASSISTANTDATAENTRY_L B 
                            ON A.F_ora_Assistantxw1 = B.FENTRYID AND B.FLOCALEID = 2052
                            INNER JOIN T_BAS_ASSISTANTDATAENTRY C 
                            ON A.F_ora_Assistantxw1 = C.FENTRYID 
                     WHERE  A.FSupplierId = {supplierId}";

                    DynamicObjectCollection yyzss
                        = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    Supplier supplier = new Supplier();
                    supplier.supCode = item["FNumber"].ToString();
                    supplier.supName = item["FName"].ToString();
                    supplier.supNameAbbrev = item["FShortName"].ToString();
                    supplier.supAddress = item["FAddress"].ToString();
                    foreach (var yyzs in yyzss)
                    {
                        //食品流通许可证/食品生产许可证/食品经营许可证
                        if (yyzs["FNumber"].ToString() == "1004")
                        {
                            supplier.supFoodBusinessCert = yyzs["FZSNumber"].ToString();
                        }

                        //生产证
                        if (yyzs["FNumber"].ToString() == "1005")
                        {
                            supplier.supFoodProdCert = yyzs["FZSNumber"].ToString();
                        }
                    }

                    inStockModel.supplier = supplier;
                }

                #endregion

                #region 物料处理
                List<Material> materials = new List<Material>();
                foreach (DynamicObject entry in entrys)
                {
                    DynamicObject materialObj = entry["MaterialId"] as DynamicObject;
                    string materialId = materialObj["Id"].ToString();
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
                       ,G.FBARCODE
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
                 WHERE  A.FMaterialId = {materialId} 
                ";
                    DynamicObjectCollection materialDatas
                        = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    foreach (DynamicObject item in materialDatas)
                    {
                        Material material = new Material();
                        material.code = item["FNumber"].ToString();
                        material.name = item["FName"].ToString();
                        material.spec = item["FSpecification"].ToString();
                        material.manufacture = item["FSCCJ"].ToString();
                        material.guaranteeValue = Convert.ToInt32(item["FExpPeriod"]);
                        string expUnit = item["FExpUnit"].ToString();
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
                        material.typeGeneral = item["FGroup"].ToString();
                        material.placeOfProduction = Convert.ToInt32(item["FCD"]);
                        material.productionBarcode = item["FBARCODE"].ToString();
                        materials.Add(material);
                    }

                }

                inStockModel.material = materials;

                #endregion

                #region 明细处理
                List<BatchDetail> batchDetails = new List<BatchDetail>();
                foreach (DynamicObject entry in entrys)
                {
                    DynamicObject materialObj = entry["MaterialId"] as DynamicObject;
                    string materialId = materialObj["Id"].ToString();
                    sql = $@"
                    SELECT  A.FNumber
                           ,B.FName
                           ,B.FSpecification
                           ,ISNULL(E.FDATAVALUE,'') FSCCJ
                           ,D.F_XW_CD FCD
                           ,F.FIsKFPeriod
                           ,F.FExpUnit
                           ,F.FExpPeriod
                           ,H.FNumber FGroup
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
                     WHERE  A.FMaterialId = {materialId} 
                    ";
                    DynamicObjectCollection materialDatas
                        = DBUtils.ExecuteDynamicObject(this.Context, sql);

                    string sccj = "";
                    string spec = "";
                    foreach (var item in materialDatas)
                    {
                        sccj = item["FSCCJ"].ToString();
                        spec = item["FSpecification"].ToString();
                    }

                    string date = Convert.ToDateTime(billObj["Date"]).ToString("yyyy-MM-dd");
                    string materialName = materialObj["Name"].ToString();
                    int qty = Convert.ToInt32(entry["RealQty"]);
                    string produceDate = "";
                    if (entry["ProduceDate"] != null)
                    {
                        produceDate = Convert.ToDateTime(entry["ProduceDate"]).ToString("yyyy-MM-dd");
                    }
                    string quarantineCert = entry["F_ora_Textxw12"].ToString();

                    string lot = "";
                    DynamicObject lotObj = entry["Lot"] as DynamicObject;
                    if (lotObj != null)
                    {
                        lot = lotObj["Number"].ToString();
                    }

                    BatchDetail batchDetail = new BatchDetail();
                    batchDetail.recordDate = date;
                    batchDetail.materialName = materialName;
                    batchDetail.manufacture = sccj;
                    batchDetail.spec = spec;
                    batchDetail.quantity = qty;
                    batchDetail.productionDate = produceDate;
                    batchDetail.productionBatch = lot;
                    batchDetail.quarantineCert = quarantineCert;
                    batchDetails.Add(batchDetail);
                }

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
                #endregion

                #region 处理返回信息
                ResponseData responseData = JsonConvert.DeserializeObject<ResponseData>(responseInfo);
                if (responseData.status == 0)
                {
                    //成功 更新上传追溯系统标识
                    sql = $@"UPDATE T_STK_INSTOCKENTRY SET F_ora_Comboxw2 = '1' WHERE FID = {billId}";
                    DBUtils.Execute(this.Context, sql);
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
                throw new KDException("错误", ex.Message);
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
    }
}

