﻿using Kingdee.BOS;
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
    [Description("新供应商客户接口同步插件-20230505")]
    [HotUpdate]
    public class NewSyncVendCust
    {
        public static void Sync(Context context,string appId,string appSecret, VendCustModel vendCustModel)
        {
            #region 调用接口
            NewApiHelper newApiHelper = new NewApiHelper(appId, appSecret);
            string requestInfo = "";
            string responseInfo = "";
            string status = "S";
            string message = "";
            string sql = "";
            string url = "";

            try
            {
                if (vendCustModel.type.Contains("Supplier"))
                {
                    sql = $@"
                        SELECT  ISNULL(F_ora_ComboXW3,'2')F_ora_ComboXW3
                          FROM  T_BD_Supplier 
                         WHERE  FSupplierID = '{vendCustModel.id}'
                           AND  ISNULL(F_ora_ComboXW3,'') <> '1'";
                    DynamicObjectCollection materialResults = DBUtils.ExecuteDynamicObject(context, sql);

                    if (materialResults.Count <= 0)
                    {
                        return;
                    }

                    url = $@"https://spzs.scjgj.sh.gov.cn/p4/api/v1/vendorcustomer/vendor";
                }

                if (vendCustModel.type.Contains("Cust"))
                {
                    sql = $@"
                        SELECT  ISNULL(F_ora_ComboXW3,'2')F_ora_ComboXW3
                          FROM  T_BD_Customer 
                         WHERE  FCustID = '{vendCustModel.id}'
                           AND  ISNULL(F_ora_ComboXW3,'') <> '1'";
                    DynamicObjectCollection materialResults = DBUtils.ExecuteDynamicObject(context, sql);

                    if (materialResults.Count <= 0)
                    {
                        return;
                    }

                    url = $@"https://spzs.scjgj.sh.gov.cn/p4/api/v1/vendorcustomer/customer";
                }

                

                requestInfo = JsonConvert.SerializeObject(vendCustModel);

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
                    if (vendCustModel.type.Contains("Supplier"))
                    {
                        sql = $@"UPDATE T_BD_Supplier SET F_ora_ComboXW3 = '1' WHERE FSupplierID = '{vendCustModel.id}'";
                        DBUtils.Execute(context, sql);
                    }

                    if (vendCustModel.type.Contains("Cust"))
                    {
                        sql = $@"UPDATE T_BD_Customer SET F_ora_ComboXW3 = '1' WHERE FCustID = '{vendCustModel.id}'";
                        DBUtils.Execute(context, sql);
                    }

                }
                else
                {
                    List<ResponseError> responseErrors = responseData.errors;
                    if (responseErrors != null)
                    {
                        message = string.Join(",", responseErrors.Select(x => x.message));
                    }
                    throw new KDException("错误", $@"上传供应商客户{vendCustModel.name}失败：{message}");
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
                    string logBillNo = vendCustModel.code + id.ToString();
                    string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    requestInfo = requestInfo.Replace("'", "''");
                    responseInfo = responseInfo.Replace("'", "''");
                    sql = $@"
                        INSERT INTO ORA_T_InterfaceLog(
                            FID,FBillNo,FSyncBillNo,FTime,
                            FRequest,FResponse,FStatus,FMessage)
                        SELECT 
                            {id},'{logBillNo}','{vendCustModel.code}','{date}',
                            '{requestInfo}','{responseInfo}','{status}','{message}'";
                    DBUtils.Execute(context, sql);

                    kDTransactionScope.Complete();
                }
            }


            #endregion
        }
    }
}
