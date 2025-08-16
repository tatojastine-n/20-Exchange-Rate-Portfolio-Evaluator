using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Asset
{
    public string Name { get; }
    public string Currency { get; }
    public decimal Amount { get; }
    public DateTime ValuationDate { get; }

    public Asset(string name, string currency, decimal amount, DateTime valuationDate)
    {
        Name = name;
        Currency = currency.ToUpper();
        Amount = amount;
        ValuationDate = valuationDate.Date; 
    }
}

public class FxRate
{
    public DateTime Date { get; }
    public string Currency { get; }
    public decimal Rate { get; }

    public FxRate(DateTime date, string currency, decimal rate)
    {
        Date = date.Date;
        Currency = currency.ToUpper();
        Rate = rate;
    }
}

public class PortfolioEvaluator
{
    public const string HomeCurrency = "USD"; 
    private const int StaleRateDays = 3; 

    public List<(Asset Asset, decimal HomeValue)> Evaluate(
        List<Asset> assets,
        List<FxRate> fxRates,
        DateTime valuationDate)
    {
        if (assets == null || fxRates == null)
            throw new ArgumentNullException("Assets and FX rates cannot be null");

        var validatedAssets = new List<(Asset Asset, decimal HomeValue)>();
        valuationDate = valuationDate.Date;

        var mostRecentRates = fxRates
            .Where(r => r.Date <= valuationDate)
            .GroupBy(r => r.Currency)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.Date).FirstOrDefault());

        foreach (var asset in assets)
        {
            if (asset.Currency == HomeCurrency)
            {
                validatedAssets.Add((asset, asset.Amount));
                continue;
            }

            if (!mostRecentRates.TryGetValue(asset.Currency, out var rate) || rate == null)
            {
                Console.WriteLine($"Warning: No FX rate available for {asset.Currency}");
                continue;
            }

            if ((valuationDate - rate.Date).Days > StaleRateDays)
            {
                Console.WriteLine($"Warning: Stale FX rate for {asset.Currency} " +
                    $"(as of {rate.Date:yyyy-MM-dd}, needed for {valuationDate:yyyy-MM-dd})");
            }

            var homeValue = asset.Amount * rate.Rate;
            validatedAssets.Add((asset, homeValue));
        }

        return validatedAssets
            .OrderByDescending(x => x.HomeValue)
            .ToList();
    }

    public decimal CalculateTotalValue(List<(Asset Asset, decimal HomeValue)> evaluatedAssets)
    {
        return evaluatedAssets.Sum(x => x.HomeValue);
    }
}

namespace Exchange_Rate_Portfolio_Evaluator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var assets = new List<Asset>
        {
            new Asset("Tokyo Office", "JPY", 150000000, new DateTime(2023, 5, 15)),
            new Asset("Berlin Bonds", "EUR", 500000, new DateTime(2023, 5, 15)),
            new Asset("London Stock", "GBP", 250000, new DateTime(2023, 5, 15)),
            new Asset("NYC Treasury", "USD", 1000000, new DateTime(2023, 5, 15))
        };

            var fxRates = new List<FxRate>
        {
            new FxRate(new DateTime(2023, 5, 10), "JPY", 0.0075m),
            new FxRate(new DateTime(2023, 5, 12), "JPY", 0.0073m), 
            new FxRate(new DateTime(2023, 5, 11), "EUR", 1.12m),
            new FxRate(new DateTime(2023, 5, 10), "EUR", 1.15m), 
            new FxRate(new DateTime(2023, 5, 14), "GBP", 1.25m), 
            new FxRate(new DateTime(2023, 4, 1), "CAD", 0.75m)   
        };

            Console.Write("Enter valuation date (yyyy-MM-dd) or blank for today: ");
            DateTime valuationDate;
            while (!DateTime.TryParse(Console.ReadLine(), out valuationDate))
            {
                Console.Write("Invalid date format. Please enter a valid date (yyyy-MM-dd) or leave blank for today: ");
            }
            if (valuationDate == default)
            {
                valuationDate = DateTime.Today;
            }

            var evaluator = new PortfolioEvaluator();
            try
            {
                var evaluatedAssets = evaluator.Evaluate(assets, fxRates, valuationDate);
                decimal totalValue = evaluator.CalculateTotalValue(evaluatedAssets);

                Console.WriteLine("\nAsset Valuation Report");
                Console.WriteLine($"As of {valuationDate:yyyy-MM-dd} in {PortfolioEvaluator.HomeCurrency}");
                Console.WriteLine("Name".PadRight(25) + "Currency".PadRight(10) +
                    "Amount".PadRight(15) + "Value in USD".PadRight(15) + "Rate Date");

                foreach (var item in evaluatedAssets)
                {
                    var rateDate = item.Asset.Currency == PortfolioEvaluator.HomeCurrency
                        ? valuationDate
                        : fxRates
                            .Where(r => r.Currency == item.Asset.Currency && r.Date <= valuationDate)
                            .Max(r => r.Date);

                    Console.WriteLine(
                        item.Asset.Name.PadRight(25) +
                        item.Asset.Currency.PadRight(10) +
                        item.Asset.Amount.ToString("N2").PadRight(15) +
                        item.HomeValue.ToString("N2").PadRight(15) +
                        rateDate.ToString("yyyy-MM-dd"));
                }

                Console.WriteLine($"Total Portfolio Value: {totalValue.ToString("N2")} USD");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
