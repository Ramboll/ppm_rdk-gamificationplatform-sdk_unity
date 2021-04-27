using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RebarInterop
{
    [Serializable]
    public class StorageHelper
    {
        public int ElemId { get; set; }
        public List<ViewBasedData> LstViewBasedDataInfo { get; set; }
        public string HasCodeData { get; set; }
    }

    public class ViewBasedData
    {
        public List<int> lstFamInsId { get; set; }
        public bool IsFlipped { get; set; }
        public int ViewId { get; set; }
    }
}
