using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn
{
    public class InStockModel
    {
        public Supplier supplier { get; set; }

        public List<Material> material { get; set; }

        public List<BatchDetail> batchDetail { get; set; }
    }

    

   
}
