using System.Collections.Concurrent;
using System.Security.Cryptography;
using CurrencyCalculator.Model;
using CurrencyCalculator.Service.Interface;

namespace CurrencyCalculator.Service;

public class CurrencyConverter : ICurrencyConverter
{
    private readonly IList<ExRate> _configuration = new List<ExRate>();
    private readonly ConcurrentDictionary<string, List<string>> _graph = new();
    private readonly IList<string> _paths = new List<string>();
    private string _previousCurrency = "";

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

        if (fromCurrency == toCurrency) return amount;

        var path = FindPath(fromCurrency, toCurrency);
        if (path == null || path.Count == 0)
            throw new InvalidOperationException($"No conversion path found from {fromCurrency} to {toCurrency}");
        
        var rate = CalculatePathRate(path);
        var result = amount * rate;
        
        // print path
        var pathString = string.Join(" -> ", path);
        Console.WriteLine($"Conversion path: {pathString}");

        return result;
    }

    private double CalculatePathRate(IReadOnlyCollection<string> path)
    {
        double rate= 1;
        for (var i = 0; i < path.Count-1; i++)
        {
            rate *= GetKnownRate(path.ElementAt(i), path.ElementAt(i + 1));
        }

        return rate;
    }

    private List<string> FindPath(string from, string to)
    {
        if (_graph[from].Contains(to))
            return new List<string> {from, to};
        
        var origin = new List<string> {from};
        var target = new List<string> {to};

        return TwoWayGraphNavigation(origin, target);
    }

    private List<string> TwoWayGraphNavigation(List<string> origin, List<string> target)
    {
        while (true)
        {
            var originFinished = AddLevel(origin);
            var targetFinished = AddLevel(target);
            if (originFinished && targetFinished)
                return new List<string>();
            var originPaths = origin.Select(o => o.Split(",").ToList()).ToList();
            var targetPaths = target.Select(t => t.Split(",").ToList()).ToList();
            foreach (var originPath in originPaths)
            {
                foreach (var targetPath in targetPaths)
                {
                    if (originPath.Last() == targetPath.Last())
                    {
                        var path = new List<string>();
                        path.AddRange(originPath);
                        targetPath.Reverse();
                        path.AddRange(targetPath.Skip(1));
                        return path;
                    }
                }
            }

        }
    }

    private bool AddLevel(ICollection<string> tree)
    {
        var toAdd = new List<string>();
        var toRemove = new List<string>();
        foreach (var t in tree)
        {
            var lastNode = t.Split(",").ToList().Last();
            foreach (var currency in _graph[lastNode])
            {
                if (t.Contains(currency)) continue;
                toAdd.Add(t + ',' + currency);
            }
            toRemove.Add(t);
        }

        if (toAdd.Count == 0) return true;

        foreach (var t in toRemove)
            tree.Remove(t);

        foreach (var t in toAdd)
            tree.Add(t);

        return false;
    }


    private double GetKnownRate(string from, string to)
    {
        var rate = _configuration.SingleOrDefault(r => r.From == from && r.To == to);
        if (rate != null)
        {
            _paths.Add($"from {from} to {to}");
            return rate.Rate;
        }


        var reverseRate = _configuration.SingleOrDefault(r => r.From == to && r.To == from);
        if (reverseRate != null)
        {
            _paths.Add($"from {from} to {to}");
            return 1 / reverseRate.Rate;
        }

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