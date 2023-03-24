namespace KL.PxParse.Interfaces;


public interface IClientAggregator {
    public Task<bool> Subscribe(IEnumerable<string> symbols);

    public Task Start();
}