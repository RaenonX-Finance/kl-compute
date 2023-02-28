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
}