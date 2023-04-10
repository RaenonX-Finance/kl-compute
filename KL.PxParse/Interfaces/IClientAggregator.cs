using KL.Common;

namespace KL.PxParse.Interfaces;


public interface IClientAggregator {
    public Task<bool> Subscribe(IEnumerable<string> symbols, OnUpdate onUpdate);

    public Task Start();
}