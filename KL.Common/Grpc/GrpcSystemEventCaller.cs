using Grpc.Net.Client;
using KL.Common.Controllers;
using KL.Common.Extensions;
using KL.Common.Models;
using KL.Common.Utils;
using KL.Proto;
using Serilog;

namespace KL.Common.Grpc;


public static class GrpcSystemEventCaller {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(GrpcSystemEventCaller));

    private static readonly SystemEvent.SystemEventClient Client = new(
        GrpcChannel.ForAddress($"http://localhost:{EnvironmentConfigHelper.Config.Grpc.SysPort}")
    );

    public static async Task OnRealtime(string symbol, PxRealtimeModel data) {
        var requestBody = new RealtimeData {
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
        };

        await Client.RealtimeAsync(requestBody);
    }

    public static async Task OnMinuteChanged(long epochSec) {
        var requestBody = new MinuteChangeData { EpochSec = epochSec };

        await Client.MinuteChangeAsync(requestBody);
    }

    public static void OnCalculatedAsync(string symbol, CancellationToken cancellationToken) {
        OnCalculatedAsync(new[] { symbol }, cancellationToken);
    }

    public static void OnCalculatedAsync(IEnumerable<string> symbols, CancellationToken cancellationToken) {
        TaskHelper.FireAndForget(
            async () => {
                var requestBody = new CalculatedData();
                requestBody.Symbols.AddRange(symbols);

                await Client.CalculatedAsync(requestBody);
            },
            exception => {
                const string endpointName = "onCalculated";
                Log.Error(exception, "Error on gRPC system event: {Endpoint}", endpointName);
            },
            cancellationToken
        );
    }

    public static async Task OnError(string message) {
        var requestBody = new ErrorData { Message = message };

        await Client.ErrorAsync(requestBody);
    }
}