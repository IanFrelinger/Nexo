using MediatR;
using Ashlar.Contracts;

namespace Ashlar.API.Middleware.Ingress;

/// <summary>Handles record sms yes approval requests.</summary>
public sealed class RecordSmsYesApprovalHandler : IRequestHandler<RecordSmsYesApprovalCommand, SmsInboundSimulationResponse>
{
    private readonly ISmsIngressApprovalStore _store;

    /// <summary>Record sms yes approval handler.</summary>
    /// <param name="store">Store.</param>
    public RecordSmsYesApprovalHandler(ISmsIngressApprovalStore store) => _store = store;

    /// <summary>Handle.</summary>
    /// <param name="request">Request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<SmsInboundSimulationResponse> Handle(RecordSmsYesApprovalCommand request, CancellationToken cancellationToken) =>
        _store.TryRecordApprovalAsync(request.From, request.ApprovalToken, request.MessageSid, cancellationToken);
}
