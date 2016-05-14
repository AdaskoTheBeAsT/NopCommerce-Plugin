using FluentValidation.Attributes;
using Nop.Plugin.Misc.ImportProducts.Controllers;
using Nop.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.ImportProducts.Model
{
    [Validator(typeof(importViewModelValidator))]
    public class importData
    {
        [NopResourceDisplayName("Plugin.Misc.ImportProducts.Required")]
        public string link { get; set; }
        [NopResourceDisplayName("Plugin.Misc.ImportProducts.Required")]
        public string login { get; set; }
        [NopResourceDisplayName("Plugin.Misc.ImportProducts.Required")]
        public string password { get; set; }
        [NopResourceDisplayName("Plugin.Misc.ImportProducts.Required")]
        public string filePath { get; set; }
        [NopResourceDisplayName("Plugin.Misc.ImportProducts.Required")]
        public string targetWholesale { get; set; }

    }
}
