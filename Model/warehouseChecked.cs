using Nop.Core.Domain.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.ImportProducts.Model
{
    public class warehouseChecked
    {
            public Warehouse warehouse { get; set; }
            public bool isChecked { get; set; }
    }
}
