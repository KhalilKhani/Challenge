using CurrencyCalculator.Model;
using CurrencyCalculator.Service;
using CurrencyCalculator.Service.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyCalculator;

public class Program
{
    public static void Main(string[] args)
    {
        var convertor = Provider().GetService<ICurrencyConverter>();
        
        var initialRates = InitialRates();
        convertor.UpdateConfiguration(initialRates);

        const string from = "EUR";
        const string to = "GBP";
        const double amount = 100;
        var result = convertor.Convert(from, to, amount);
        
        Console.WriteLine($"{amount} {from} = {result:F2} {to}");
    }

    private static ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrencyConverter, CurrencyConverter>();
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider;
    }

    private static IEnumerable<ExRate> InitialRates()
    {
        return new List<ExRate>
        {
            new() {From = "USD", To = "CAD", Rate = 1.34},
            new() {From = "CAD", To = "GBP", Rate = 0.58},
            new() {From = "USD", To = "EUR", Rate = 0.86}
        };
    }
}