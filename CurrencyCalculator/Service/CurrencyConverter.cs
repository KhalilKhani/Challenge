using System.Collections.Concurrent;
using CurrencyCalculator.Model;
using CurrencyCalculator.Service.Interface;

namespace CurrencyCalculator.Service;

public class CurrencyConverter : ICurrencyConverter
{
    private readonly IList<ExRate> _configuration = new List<ExRate>();
    private readonly ConcurrentDictionary<string, List<string>> _graph = new();

    public void ClearConfiguration()
    {
        _configuration.Clear();
        _graph.Clear();
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
        UpdateGraph();
    }

    public double Convert(string fromCurrency, string toCurrency, double amount)
    {
        if (!_configuration.Any())
            throw new InvalidOperationException("Conversion rates are not configured");

        if (!_graph.ContainsKey(fromCurrency))
            throw new InvalidOperationException($"Currency {fromCurrency} not found");
        
        if (!_graph.ContainsKey(toCurrency))
            throw new InvalidOperationException($"Currency {toCurrency} not found");
        
        var rate = GetRate(fromCurrency, toCurrency);
        if (rate != 0) return amount * rate;
        
        throw new InvalidOperationException($"No conversion path found from {fromCurrency} to {toCurrency}");
    }

    private double GetRate(string from, string to)
    {
        if (_graph[from].Contains(to))
            return GetKnownRate(from, to);

        foreach (var currency in _graph[from])
        {
            var rate = GetRate(currency, to);
            if (rate != 0)
                return rate * GetKnownRate(from, currency);
        }

        return 0; // no path found
    }

    private double GetKnownRate(string from, string to)
    {
        var rate = _configuration.SingleOrDefault(fr => fr.From == from && fr.To == to);
        if (rate != null)
            return rate.Rate;
        
        var reverseRate = _configuration.SingleOrDefault(fr => fr.From == to && fr.To == from);
        if (reverseRate != null)
            return 1 / reverseRate.Rate;
        
        return 0;
    }

    private void UpdateGraph()
    {
        foreach (var rate in _configuration)
        {
            if (!_graph.ContainsKey(rate.From))
                _graph[rate.From] = new List<string>();
            if (!_graph.ContainsKey(rate.To))
                _graph[rate.To] = new List<string>();

            if (!_graph[rate.From].Contains(rate.To))
                _graph[rate.From].Add(rate.To);
            if (!_graph[rate.To].Contains(rate.From))
                _graph[rate.To].Add(rate.From);
        }
    }
}