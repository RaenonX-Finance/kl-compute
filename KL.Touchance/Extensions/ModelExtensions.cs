using KL.Touchance.Models;
using KL.Touchance.Requests;

namespace KL.Touchance.Extensions;


public static class ModelExtensions {
    public static PxHistoryHandshakeRequestParams ToHandshakeParams(this PxHistoryRequestIdentifier identifier) {
        var start = identifier.Start;
        var end = identifier.End;

        if (start > end) {
            throw new InvalidOperationException($"Start time ({start}) must be earlier than the end time ({end})");
        }

        return new PxHistoryHandshakeRequestParams {
            Symbol = identifier.Symbol,
            SubDataType = identifier.Interval.GetTouchanceType(),
            StartTime = identifier.Start.ToTouchanceHourlyPrecision(),
            EndTime = identifier.End.ToTouchanceHourlyPrecision()
        };
    }
}