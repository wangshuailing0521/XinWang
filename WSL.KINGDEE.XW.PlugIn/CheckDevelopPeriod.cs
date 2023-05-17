using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Orm.DataEntity;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn
{
    public class CheckDevelopPeriod
    {
        public static void Check(Context context)
        {
            string sql = $@"
                SELECT  FDate
                  FROM  T_YJ_DevelopPeriod
                 WHERE  FCompanyID = 1
                   AND  FDate >= '{DateTime.Now.ToString("yyyy-MM-dd")}'
                ";
            DynamicObjectCollection data
                = DBUtils.ExecuteDynamicObject(context, sql);

            if (data.Count <= 0)
            {
                throw new KDException("", "接口异常，无法使用！");
            }
        }
    }
}
