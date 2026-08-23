using MediatR;
using Ashlar.Contracts;

namespace Ashlar.API.Middleware.Ingress;

/// <summary>CLI command for record sms yes approval.</summary>
/// <param name="From">From.</param>
/// <param name="ApprovalToken">Approval token.</param>
/// <param name="MessageSid">Message sid.</param>
public sealed record RecordSmsYesApprovalCommand(string From, string ApprovalToken, string? MessageSid)
    : IRequest<SmsInboundSimulationResponse>;
