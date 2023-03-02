using Grpc.Core;
using KL.Calc.Computer;
using KL.Proto;

namespace KL.Calc.Services;


public class PxDataService : PxData.PxDataBase {
    private readonly ILogger<PxDataService> _logger;

    public PxDataService(ILogger<PxDataService> logger) {
        _logger = logger;
    }

    public override async Task<PxCalcReply> CalcAll(PxCalcRequestMulti request, ServerCallContext context) {
        _logger.LogInformation("Received gRPC request to calculate all data: {Symbols}", request.Symbols);

        await CalcRequestHandler.CalcAll(request.Symbols, context.CancellationToken);

        return new PxCalcReply { Message = "Done" };
    }

    public override async Task<PxCalcReply> CalcPartial(PxCalcRequestMulti request, ServerCallContext context) {
        _logger.LogInformation("Received gRPC request to calculate all data: {Symbols}", request.Symbols);

        // Should only needs 10 or even less data to calculate data for new bar
        await CalcRequestHandler.CalcPartial(request.Symbols, 10, context.CancellationToken);

        return new PxCalcReply { Message = "Done" };
    }

    public override async Task<PxCalcReply> CalcLast(PxCalcRequestSingle request, ServerCallContext context) {
        _logger.LogInformation("Received gRPC request to calculate last data: {Symbol}", request.Symbol);

        await CalcRequestHandler.CalcLast(request.Symbol, context.CancellationToken);

        return new PxCalcReply { Message = "Done" };
    }
}