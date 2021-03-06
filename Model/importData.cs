﻿namespace Nop.Plugin.Misc.ImportProducts.Model
{
    using FluentValidation.Attributes;
    using Nop.Web.Framework;

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
