using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.FileServer.Core.Object;
using Kingdee.BOS.FileServer.ProxyService;
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

using WSL.KINGDEE.XW.PlugIn.Model;

namespace WSL.KINGDEE.XW.PlugIn.NewBuilder
{
    [Description("新证明文化接口同步插件-20230505")]
    [HotUpdate]
    public class NewSyncFile
    {
        public static void Sync(Context context, string appId, string appSecret, NewFile newFile)
        {
           
            NewApiHelper newApiHelper = new NewApiHelper(appId, appSecret);
            string requestInfo = "";
            string responseInfo = "";
            string status = "S";
            string message = "";
            string sql = "";
            string url = "";

            try
            {
                #region 调用接口
                //判断物料是否已经传递
                sql = $@"
                SELECT  1
                  FROM  T_BAS_PREBDTWO WITH(NOLOCK)
                 WHERE  FNumber = '{newFile.certNo}'
                   AND  ISNULL(FInterfaceID,'') = ''";
                DynamicObjectCollection materialResults
                    = DBUtils.ExecuteDynamicObject(context, sql);

                if (materialResults.Count <= 0)
                {
                    return;
                }

                url = $@"https://spzs.scjgj.sh.gov.cn/p4/api/v1/certfile";
                requestInfo = JsonConvert.SerializeObject(newFile);

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
                    sql = $@"UPDATE T_BAS_PREBDTWO SET FInterfaceID = '{responseData.content}' WHERE FNumber = '{newFile.certNo}'";
                    DBUtils.Execute(context, sql);
                }

                if (!responseData.success)
                {
                    List<ResponseError> responseErrors = responseData.errors;
                    if (responseErrors != null)
                    {
                        message = string.Join(",", responseErrors.Select(x => x.message));
                    }
                    throw new KDException("错误", $@"上传证明文件{newFile.certNo}失败：{message}");
                }
                #endregion
            }
            catch (Exception ex)
            {
                status = "E";
                message = ex.Message.Replace("'", "''");
            }
            finally
            {
                #region 记录日志
                using (KDTransactionScope kDTransactionScope = new KDTransactionScope(TransactionScopeOption.RequiresNew))
                {
                    //记录日志
                    var ids = DBServiceHelper.GetSequenceInt64(context, "ORA_T_InterfaceLog", 1);
                    long id = ids.ElementAt(0);
                    string logBillNo = newFile.certNo + id.ToString();
                    string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    requestInfo = requestInfo.Replace("'", "''");
                    responseInfo = responseInfo.Replace("'", "''");

                    //图片文件日志太大，不记录请求报文
                    sql = $@"
                        INSERT INTO ORA_T_InterfaceLog(
                            FID,FBillNo,FSyncBillNo,FTime,
                            FRequest,FResponse,FStatus,FMessage)
                        SELECT 
                            {id},'{logBillNo}','{newFile.certNo}','{date}',
                            '','{responseInfo}','{status}','{message}'";
                    DBUtils.Execute(context, sql);

                    kDTransactionScope.Complete();
                }

                Logger.Info("", "请求信息:" + requestInfo);
                Logger.Info("", "返回信息:" + responseInfo);
                #endregion
            }
            
        }
    }
}
