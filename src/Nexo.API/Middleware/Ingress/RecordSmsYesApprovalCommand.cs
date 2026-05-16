using MediatR;
using Nexo.Contracts;

namespace Nexo.API.Middleware.Ingress;

public sealed record RecordSmsYesApprovalCommand(string From, string ApprovalToken, string? MessageSid)
    : IRequest<SmsInboundSimulationResponse>;
