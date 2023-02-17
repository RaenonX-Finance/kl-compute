using KL.Common.Controllers;
using KL.Common.Extensions;
using MongoDB.Driver;

namespace KL.Common.Utils;


public class MongoSession : IDisposable {
    private string? _sessionId;

    private MongoSession(IClientSessionHandle session) {
        session.StartTransaction();

        Session = session;
    }

    public IClientSessionHandle Session { get; }

    public string SessionId => _sessionId ??= Session.GetSessionId();

    public void Dispose() {
        Session.Dispose();

        GC.SuppressFinalize(this);
    }

    public static async Task<MongoSession> Create() {
        return new MongoSession(await MongoConst.Client.StartSessionAsync());
    }
}