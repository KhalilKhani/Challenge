using CurrencyCalculator.Model;

namespace CurrencyCalculator.Service.Interface;

public interface IGraphNavigation
{
    void UpdateGraph(IEnumerable<ExRate> configuration);

    List<string> FindPath(string from, string to);

    bool HasNode(string node);
    
    void ClearGraph();

}