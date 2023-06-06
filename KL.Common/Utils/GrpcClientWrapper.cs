using Grpc.Core;

namespace KL.Common.Utils;


public class GrpcClientWrapper<TClient> where TClient : ClientBase<TClient> {
    public delegate TClient GrpcClientCreator();

    public TClient Client { get; private set; }

    private readonly GrpcClientCreator _grpcClientCreator;
    
    public GrpcClientWrapper(GrpcClientCreator fnCreateClient) {
        _grpcClientCreator = fnCreateClient;
        Client = fnCreateClient();
    }

    public void Reconnect() {
        Client = _grpcClientCreator();
    }
}