using KL.Common.Controllers;
using KL.Common.Models;
using KL.Common.Models.Config;
using KL.Touchance.Extensions;
using KL.Touchance.Requests;
using KL.Touchance.Responses;
using Serilog;

namespace KL.Touchance.Handlers;


internal static class SourceInfoHandler {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(SourceInfoHandler));

    internal static async Task CheckSourceInfo(this TouchanceClient client, IEnumerable<PxSourceConfigModel> sources) {
        var sourceInfo = sources.Select(
            r => {
                Log.Information("Checking source info of {Symbol}", r.ExternalSymbol);

                try {
                    var reply = client.RequestSocket.SendTcRequest<SourceInfoRequest, SourceInfoReply>(
                        new SourceInfoRequest {
                            SessionKey = client.SessionKey,
                            Symbol = r.ExternalSymbol
                        }
                    );

                    return new SourceInfoModel {
                        Symbol = r.InternalSymbol,
                        MinTick = reply.ProductInfo.TickSize,
                        Decimals = reply.ProductInfo.Decimals,
                        ExchangeSymbol = reply.ProductInfo.ExchangeSymbol,
                        ExchangeName = reply.ProductInfo.ExchangeSymbol,
                    };
                } catch (InvalidOperationException e) {
                    Log.Error(
                        e,
                        "Failed to deserialize source info. Symbol ({Symbol}) could be invalid.",
                        r.ExternalSymbol
                    );
                    Environment.Exit(1);
                    throw;
                }
            }
        );

        await SourceInfoController.UpdateAll(sourceInfo.ToArray());
    }
}