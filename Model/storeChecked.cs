using Nop.Core.Domain.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.ImportProducts.Model
{
    public class storeChecked
    {
        public Store store { get; set; }
        public bool isChecked { get; set; }
    }
}
