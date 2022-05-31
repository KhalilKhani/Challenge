using System.Collections.Concurrent;
using System.Security.Cryptography;
using CurrencyCalculator.Model;
using CurrencyCalculator.Service.Interface;

namespace CurrencyCalculator.Service;

public class CurrencyConverter : ICurrencyConverter
{
    private readonly IList<ExRate> _configuration = new List<ExRate>();
    private readonly IGraphNavigation _graphNavigation;

    public CurrencyConverter(IGraphNavigation graphNavigation)
    {
        _graphNavigation = graphNavigation;
    }

    public void ClearConfiguration()
    {
        _configuration.Clear();
        _graphNavigation.ClearGraph();
    }

    public void UpdateConfiguration(IEnumerable<ExRate> conversionRates)
    {
        foreach (var conversionRate in conversionRates)
        {
            var oldRate =
                _configuration.FirstOrDefault(c => c.From == conversionRate.From && c.To == conversionRate.To);
            if (oldRate != null)
                _configuration.Remove(oldRate);
            _configuration.Add(conversionRate);
        }

        _graphNavigation.UpdateGraph(_configuration);
    }

    public double Convert(string fromCurrency, string toCurrency, double amount)
    {
        CheckInputs(fromCurrency, toCurrency);

        if (fromCurrency == toCurrency) return amount;

        var path = _graphNavigation.FindPath(fromCurrency, toCurrency);
        if (path == null || path.Count == 0)
            throw new InvalidOperationException($"No conversion path found from {fromCurrency} to {toCurrency}");

        var rate = CalculatePathRate(path);
        var result = amount * rate;

        // print path
        var pathString = string.Join(" -> ", path);
        Console.WriteLine($"Conversion path: {pathString}");

        return result;
    }

    private void CheckInputs(string fromCurrency, string toCurrency)
    {
        if (!_configuration.Any())
            throw new InvalidOperationException("Conversion rates are not configured");

        if (!_graphNavigation.HasNode(fromCurrency))
            throw new InvalidOperationException($"Currency {fromCurrency} not found");

        if (!_graphNavigation.HasNode(toCurrency))
            throw new InvalidOperationException($"Currency {toCurrency} not found");
    }

    private double CalculatePathRate(IReadOnlyCollection<string> path)
    {
        double rate = 1;
        for (var i = 0; i < path.Count - 1; i++) 
            rate *= GetKnownRate(path.ElementAt(i), path.ElementAt(i + 1));

        return rate;
    }



    private double GetKnownRate(string from, string to)
    {
        var rate = _configuration.SingleOrDefault(r => r.From == from && r.To == to);
        if (rate != null)
            return rate.Rate;
        
        var reverseRate = _configuration.SingleOrDefault(r => r.From == to && r.To == from);
        if (reverseRate != null)
            return 1 / reverseRate.Rate;

        return 0;
    }
}