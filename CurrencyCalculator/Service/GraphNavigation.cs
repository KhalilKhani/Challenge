using System.Collections.Concurrent;
using CurrencyCalculator.Model;
using CurrencyCalculator.Service.Interface;

namespace CurrencyCalculator.Service;

class GraphNavigation : IGraphNavigation

{
    private readonly ConcurrentDictionary<string, List<string>> _graph = new();
    private List<List<string>> _baseNavigation = new ();
    private List<List<string>> _targetNavigation =new ();

    public void UpdateGraph(IEnumerable<ExRate> configuration)
    {
        foreach (var rate in configuration)
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

    public List<string> FindPath(string from, string to)
    {
        if (_graph[from].Contains(to))
            return new List<string> {from, to};
        
        _baseNavigation = new List<List<string>> {new() {from}};
        _targetNavigation = new List<List<string>> {new() {to}};

        return TwoWayGraphNavigation();
    }

    public bool HasNode(string node) => _graph.ContainsKey(node);
    
    public void ClearGraph() => _graph.Clear();

    private List<string> TwoWayGraphNavigation()
    {
        while (AddLevel(_baseNavigation) || AddLevel(_targetNavigation))
        {
            foreach (var b in _baseNavigation)
            foreach (var t in _targetNavigation)
                if (b.Last() == t.Last())
                {
                    var path = new List<string>();
                    path.AddRange(b);
                    t.Reverse();
                    path.AddRange(t.Skip(1));
                    return path;
                }
        }

        return new List<string>();
    }

    private bool AddLevel(List<List<string>> subGraph)
    {
        var toAdd = new List<List<string>>();
        var toRemove = new List<List<string>>();
        foreach (var t in subGraph)
        {
            foreach (var currency in _graph[t.Last()])
            {
                if (t.Contains(currency)) continue;
                toAdd.Add(new List<string>(t) {currency});
            }
            toRemove.Add(t);
        }

        if (toAdd.Count == 0) return false;
        subGraph.AddRange(toAdd);

        foreach (var t in toRemove) subGraph.Remove(t);

        return true;
    }
}