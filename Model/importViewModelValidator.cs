namespace Nop.Plugin.Misc.ImportProducts.Model
{
    using FluentValidation;

    using Nop.Plugin.Misc.ImportProducts.Model.CentralaZabawek;
    using Nop.Plugin.Misc.ImportProducts.Model.MotyleKsiazkowe;
    using Nop.Services.Localization;

    public class importViewModelValidator : AbstractValidator<importData>
    {
        public importViewModelValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.link).NotEmpty().WithMessage(localizationService.GetResource("Plugin.Misc.ImportProducts.Required"));
            RuleFor(x => x.login).NotEmpty().WithMessage(localizationService.GetResource("Plugin.Misc.ImportProducts.Required"));
            RuleFor(x => x.password).NotEmpty().WithMessage(localizationService.GetResource("Plugin.Misc.ImportProducts.Required"));
        }
    }

    public class ImportViewModelValidatorProduct : AbstractValidator<Products>
    {
        public ImportViewModelValidatorProduct(ILocalizationService localizationService)
        {
            RuleFor(x => x.priceIncrease).NotEmpty().WithMessage(localizationService.GetResource("Plugin.Misc.ImportProducts.Required"));
        }
    }

    public class ImportViewModelValidatorProductMotyle : AbstractValidator<PRODUCTS>
    {
        public ImportViewModelValidatorProductMotyle(ILocalizationService localizationService)
        {
            RuleFor(x => x.priceIncrease).NotEmpty().WithMessage(localizationService.GetResource("Plugin.Misc.ImportProducts.Required"));
        }
    }
}
