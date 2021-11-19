﻿using ShipIt.Models.ApiModels;

namespace ShipIt.Validators
{
    public class ProductValidator: BaseValidator<Product>
    {
        protected override void DoValidation(Product target)
        {
        AssertNotBlank("name", target.Name);
        AssertMaxLength("name", target.Name, 255);

        ValidateGtin(target.Gtin);

        ValidateGcp(target.Gcp);

        AssertNonNegative("m_g", target.WeightInKgs);

        AssertNonNegative("lowerThreshold", target.LowerThreshold);

        AssertNonNegative("minimumOrderQuantity", target.MinimumOrderQuantity);
        }
    }
}