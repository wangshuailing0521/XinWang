using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
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
    [Description("新物料接口同步插件-20230505")]
    [HotUpdate]
    public class NewSyncMaterial
    {
        public static void Sync(Context context,DynamicObject materialObj)
        {
            string orgId = materialObj["UseOrgId_Id"].ToString();

            ISystemParameterService systemParameterService
              = ServiceFactory.GetSystemParameterService(context);

            object appIdValue = systemParameterService.GetParamter(context,
                              Convert.ToInt64(orgId)
                              , 0L, "KCY_StockParameter", "FNewAppID", 0L);

            object appSecretValue = systemParameterService.GetParamter(context,
                     Convert.ToInt64(orgId)
                      , 0L, "KCY_StockParameter", "FNewAppSecret", 0L);

            NewApiHelper newApiHelper = new NewApiHelper(appIdValue.ToString(), appSecretValue.ToString());
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
                = DBUtils.ExecuteDynamicObject(context, sql);

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
                = DBUtils.ExecuteDynamicObject(context, sql);


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
                string url = $@"https://spzs.scjgj.sh.gov.cn/p4/api/v1/products";

                requestInfo = JsonConvert.SerializeObject(material);

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

                    //更新物料上传标识
                    sql = $@"UPDATE T_BD_MATERIAL SET F_ora_ComboXW4 = '1' WHERE FMaterialId = '{materialId}'";
                    DBUtils.Execute(context, sql);
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
                    var ids = DBServiceHelper.GetSequenceInt64(context, "ORA_T_InterfaceLog", 1);
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
                    DBUtils.Execute(context, sql);

                    kDTransactionScope.Complete();
                }
            }


            #endregion
        }
    }
}
