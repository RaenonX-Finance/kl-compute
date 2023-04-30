using KL.Proto;

namespace KL.Common.Extensions;


public static class GrpcDateExtensions {
    public static DateOnly ToDateOnly(this Date grpcDate) {
        return new DateOnly(grpcDate.Year, grpcDate.Month, grpcDate.Day);
    }

    public static Date ToGrpcDate(this DateOnly date) {
        return new Date {
            Year = date.Year,
            Month = date.Month,
            Day = date.Day
        };
    }
}