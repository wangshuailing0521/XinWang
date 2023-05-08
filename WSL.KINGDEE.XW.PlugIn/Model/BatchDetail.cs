using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WSL.KINGDEE.XW.PlugIn
{
    public class BatchDetail
    {
        public string recordDate { get; set; }

        public string materialName { get; set; }

        public string manufacture { get; set; }

        public string spec { get; set; }

        public decimal quantity { get; set; }

        public string productionDate { get; set; }

        public string productionBatch { get; set; }

        public string traceCode { get; set; }

        public string quarantineCert { get; set; }
    }
}
