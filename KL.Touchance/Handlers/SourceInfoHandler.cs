using System.Collections.Immutable;
using KL.Common.Controllers;
using KL.Common.Models;
using KL.Common.Models.Config;
using KL.Touchance.Extensions;
using KL.Touchance.Requests;
using KL.Touchance.Responses;
using Serilog;

namespace KL.Touchance.Handlers;


public static class SourceInfoHandler {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(SourceInfoHandler));

    public static async Task CheckSourceInfo(IImmutableList<PxSourceConfigModel> sources) {
        var sourceInfo = sources.Select(
            r => {
                Log.Information("Checking source info of {Symbol}", r.ExternalSymbol);
                var reply = TouchanceClient.RequestSocket.SendTcRequest<SourceInfoRequest, SourceInfoReply>(
                    new SourceInfoRequest {
                        SessionKey = TouchanceClient.SessionKey,
                        Symbol = r.ExternalSymbol
                    }
                );

                return new SourceInfoModel {
                    Symbol = r.ExternalSymbol,
                    MinTick = reply.Tick,
                    Decimals = reply.Decimals
                };
            }
        );

        await SourceInfoController.UpdateAll(sourceInfo.ToImmutableArray());
    }
}