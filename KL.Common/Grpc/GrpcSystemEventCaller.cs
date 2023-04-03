using Grpc.Net.Client;
using KL.Common.Controllers;
using KL.Common.Extensions;
using KL.Common.Models;
using KL.Common.Utils;
using KL.Proto;

namespace KL.Common.Grpc;


public static class GrpcSystemEventCaller {
    private static readonly SystemEvent.SystemEventClient Client = new(
        GrpcChannel.ForAddress($"http://localhost:{EnvironmentConfigHelper.Config.Grpc.SysPort}")
    );

    public static async Task OnRealtimeAsync(
        string symbol,
        PxRealtimeModel data,
        CancellationToken cancellationToken
    ) {
        GrpcHelper.CallWithDeadlineAsync(
            Client.RealtimeAsync,
            new RealtimeData {
                Data = {
                    [symbol] = new RealtimeDataSingle {
                        Open = data.Open.ToGrpcDecimal(),
                        High = data.High.ToGrpcDecimal(),
                        Low = data.Low.ToGrpcDecimal(),
                        Close = data.Close.ToGrpcDecimal(),
                        DiffVal = data.DiffVal.ToGrpcDecimal(),
                        DiffPct = data.DiffPct.ToGrpcDecimal(),
                        Momentum = (int)await RedisMomentumController.Get(symbol)
                    }
                }
            },
            nameof(Client.RealtimeAsync),
            cancellationToken,
            reason: $"{symbol} received realtime data (GrpcSystemEvent)"
        );
    }

    public static Task OnMinuteChanged(string symbol, long epochSec, CancellationToken cancellationToken) {
        var request = new MinuteChangeData { EpochSec = epochSec };
        request.Symbols.Add(symbol);

        return GrpcHelper.CallWithDeadline(
            Client.MinuteChangeAsync,
            request,
            nameof(Client.MinuteChangeAsync),
            cancellationToken,
            useTimeout: false,
            reason: $"`{symbol}` minute changed to {epochSec}"
        );
    }

    public static void OnCalculatedAsync(string symbol, string reason, CancellationToken cancellationToken) {
        OnCalculatedAsync(new[] { symbol }, reason, cancellationToken);
    }

    public static void OnCalculatedAsync(IEnumerable<string> symbols, string reason, CancellationToken cancellationToken) {
        var requestBody = new CalculatedData();
        requestBody.Symbols.AddRange(symbols);

        GrpcHelper.CallWithDeadlineAsync(
            Client.CalculatedAsync,
            requestBody,
            nameof(Client.CalculatedAsync),
            cancellationToken,
            reason: reason
        );
    }

    public static void OnErrorAsync(string message, CancellationToken cancellationToken) {
        GrpcHelper.CallWithDeadlineAsync(
            Client.ErrorAsync,
            new ErrorData { Message = message },
            nameof(Client.ErrorAsync),
            cancellationToken
        );
    }

    public static Task OnMarketDateCutoff(string symbol, CancellationToken cancellationToken) {
        var request = new MarketDateCutoffData();
        request.Symbols.Add(symbol);

        return GrpcHelper.CallWithDeadline(
            Client.MarketDateCutoffAsync,
            request,
            nameof(Client.MarketDateCutoffAsync),
            cancellationToken,
            reason: $"{symbol} market date cutoff"
        );
    }
}