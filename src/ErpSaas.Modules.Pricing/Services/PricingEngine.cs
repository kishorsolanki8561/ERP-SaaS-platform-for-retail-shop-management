namespace ErpSaas.Modules.Pricing.Services;

public sealed class PricingEngine : IPricingEngine
{
    public CartCalculationResult Calculate(
        CartInput cart,
        IReadOnlyList<DiscountRuleDto> rules,
        IReadOnlyList<CreateExtraChargeDto> charges)
    {
        var activeRules = rules
            .Where(r => r.IsActive && r.StartDate.Date <= cart.Date.Date && r.EndDate.Date >= cart.Date.Date)
            .OrderBy(r => r.Priority)
            .ToList();

        var lineResults = new List<CartLineResult>();
        decimal subTotal = 0;
        decimal totalDiscount = 0;

        foreach (var line in cart.Lines)
        {
            var grossLine = Math.Round(line.Quantity * line.UnitPrice, 2);
            var applicableDiscounts = ResolveDiscounts(line, activeRules, cart.CustomerTypeId, grossLine);
            var discountAmount = applicableDiscounts.Sum(d => d.Amount);
            var taxable = Math.Round(grossLine - discountAmount, 2);
            var cgst = 0m;
            var sgst = 0m;
            var lineTotal = taxable + cgst + sgst;

            subTotal += grossLine;
            totalDiscount += discountAmount;

            lineResults.Add(new CartLineResult(
                line.ProductId, line.Quantity, line.UnitPrice,
                discountAmount, taxable, 0, cgst, sgst, lineTotal,
                applicableDiscounts));
        }

        var totalTaxable = lineResults.Sum(l => l.TaxableAmount);
        var totalTax = lineResults.Sum(l => l.CgstAmount + l.SgstAmount);

        var extraChargeResults = new List<ExtraChargeResult>();
        decimal totalExtra = 0;
        foreach (var charge in charges)
        {
            var amount = charge.Type switch
            {
                Enums.ChargeType.FixedAmount      => charge.Value,
                Enums.ChargeType.PercentOfInvoice => Math.Round(totalTaxable * charge.Value / 100, 2),
                Enums.ChargeType.PerItem          => Math.Round(lineResults.Sum(l => l.Quantity) * charge.Value, 2),
                _ => 0m,
            };
            extraChargeResults.Add(new ExtraChargeResult(charge.Name, amount, charge.IsTaxable));
            totalExtra += amount;
        }

        return new CartCalculationResult(
            lineResults, subTotal, totalDiscount,
            totalTaxable, totalTax,
            extraChargeResults, totalExtra,
            totalTaxable + totalTax + totalExtra);
    }

    private static List<AppliedDiscount> ResolveDiscounts(
        CartLineInput line, IReadOnlyList<DiscountRuleDto> rules, long? customerTypeId, decimal grossLine)
    {
        var applied = new List<AppliedDiscount>();
        foreach (var rule in rules)
        {
            bool applies = rule.Scope switch
            {
                Enums.DiscountScope.ProductLine => rule.PercentValue.HasValue,
                Enums.DiscountScope.Invoice => false,
                _ => false,
            };

            if (!applies) continue;

            var amount = rule.PercentValue.HasValue
                ? Math.Round(grossLine * rule.PercentValue.Value / 100, 2)
                : (rule.FixedValue ?? 0);

            applied.Add(new AppliedDiscount(rule.Name, rule.DiscountTypeCode, amount));

            if (!rules.Any(r => r.IsActive && r.Priority == rule.Priority + 1))
                break;
        }
        return applied;
    }
}
