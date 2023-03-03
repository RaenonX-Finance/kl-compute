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

    public static async Task CheckSourceInfo(this TouchanceClient client, IImmutableList<PxSourceConfigModel> sources) {
        var sourceInfo = sources.Select(
            r => {
                Log.Information("Checking source info of {Symbol}", r.ExternalSymbol);
                var reply = client.RequestSocket.SendTcRequest<SourceInfoRequest, SourceInfoReply>(
                    new SourceInfoRequest {
                        SessionKey = client.SessionKey,
                        Symbol = r.ExternalSymbol
                    }
                );

                try {
                    return new SourceInfoModel {
                        Symbol = r.InternalSymbol,
                        MinTick = reply.Tick,
                        Decimals = reply.Decimals
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

        await SourceInfoController.UpdateAll(sourceInfo.ToImmutableArray());
    }
}