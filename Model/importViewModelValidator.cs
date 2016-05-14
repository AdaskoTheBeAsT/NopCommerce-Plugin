using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Nop.Services.Localization;

namespace Nop.Plugin.Misc.ImportProducts.Model
{
    public class importViewModelValidator : AbstractValidator<importData>
    {
        public importViewModelValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.link).NotEmpty().WithMessage(localizationService.GetResource("Plugin.Misc.ImportProducts.Required"));
            RuleFor(x => x.login).NotEmpty().WithMessage(localizationService.GetResource("Plugin.Misc.ImportProducts.Required"));
            RuleFor(x => x.password).NotEmpty().WithMessage(localizationService.GetResource("Plugin.Misc.ImportProducts.Required"));
            
        }
    }

    public class ImportViewModelValidatorProduct : AbstractValidator<Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek.products>
    {
        public ImportViewModelValidatorProduct(ILocalizationService localizationService)
        {
            RuleFor(x => x.priceIncrease).NotEmpty().WithMessage(localizationService.GetResource("Plugin.Misc.ImportProducts.Required"));
        }
    }

    public class ImportViewModelValidatorProductMotyle : AbstractValidator<Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe.PRODUCTS>
    {
        public ImportViewModelValidatorProductMotyle(ILocalizationService localizationService)
        {
            RuleFor(x => x.priceIncrease).NotEmpty().WithMessage(localizationService.GetResource("Plugin.Misc.ImportProducts.Required"));
        }
    }

}
