using MediatR;
using Nexo.Contracts;

namespace Nexo.API.Middleware.Ingress;

public sealed class RecordSmsYesApprovalHandler : IRequestHandler<RecordSmsYesApprovalCommand, SmsInboundSimulationResponse>
{
    private readonly ISmsIngressApprovalStore _store;

    public RecordSmsYesApprovalHandler(ISmsIngressApprovalStore store) => _store = store;

    public Task<SmsInboundSimulationResponse> Handle(RecordSmsYesApprovalCommand request, CancellationToken cancellationToken) =>
        _store.TryRecordApprovalAsync(request.From, request.ApprovalToken, request.MessageSid, cancellationToken);
}
