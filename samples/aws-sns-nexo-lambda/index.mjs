/**
 * Forwards Lambda SNS subscription events into the HTTP-shaped JSON Nexo.API expects for signature verification.
 * Set env NEXO_SMS_URL to your Nexo POST /api/ingress/sms/sns endpoint (TLS). Prefer private networking + mTLS in production.
 */
export const handler = async (event) => {
  const url = process.env.NEXO_SMS_URL;
  if (!url) {
    throw new Error("Missing NEXO_SMS_URL");
  }

  for (const record of event.Records ?? []) {
    const sns = record.Sns;
    if (!sns) continue;

    const body = {
      Type: sns.Type,
      MessageId: sns.MessageId,
      TopicArn: sns.TopicArn,
      Subject: sns.Subject,
      Message: sns.Message,
      Timestamp: sns.Timestamp,
      SignatureVersion: sns.SignatureVersion,
      Signature: sns.Signature,
      SigningCertURL: sns.SigningCertURL,
      SubscribeURL: sns.SubscribeURL,
      Token: sns.Token,
    };

    const res = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      const text = await res.text();
      throw new Error(`Nexo returned ${res.status}: ${text.slice(0, 500)}`);
    }
  }

  return { ok: true };
};
