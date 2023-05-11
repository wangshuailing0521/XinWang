using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn.Model
{
    public class NewFile
    {
        /// <summary>
        /// 证明文件 id
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// 证明文件编号
        /// </summary>
        public string certNo { get; set; }
        /// <summary>
        /// 证明文件类型
        /// </summary>
        public string certType { get; set; }
        /// <summary>
        /// 证明文件地址；base64 编码的 jpg/png/gif 格式的图片，单个图片最大 1024KB。
        /// </summary>
        public List<string> certUrls { get; set; }
        public string description { get; set; }
    }
}
