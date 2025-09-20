using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Nexo.Demo.Scripts.Utils;

/// <summary>
/// Seeds demo fixtures for the Feature Lab
/// </summary>
public class FixtureSeeder
{
    /// <summary>
    /// Seeds all fixtures for the demo
    /// </summary>
    public async Task<FixtureResult> SeedFixturesAsync(string projectRoot = null)
    {
        projectRoot ??= Directory.GetCurrentDirectory();
        
        var result = new FixtureResult
        {
            ProjectRoot = projectRoot,
            StartTime = DateTime.UtcNow
        };

        try
        {
            Console.WriteLine("🌱 NEXO FEATURE LAB FIXTURE SEEDER");
            Console.WriteLine("===================================");

            // Create fixture directories
            var inboxDir = Path.Combine(projectRoot, "demo/feature-lab/Playground.Server/wwwroot/fixtures/inbox");
            var contractsDir = Path.Combine(projectRoot, "demo/feature-lab/Playground.Server/wwwroot/fixtures/contracts");

            SystemUtils.SafeCreateDirectory(inboxDir);
            SystemUtils.SafeCreateDirectory(contractsDir);

            // Seed email fixtures
            Console.WriteLine("\nCreating sample email fixtures...");
            var emailResult = await SeedEmailFixturesAsync(inboxDir);
            result.EmailFixtures = emailResult;

            // Seed contract fixtures
            Console.WriteLine("\nCreating sample contract fixtures...");
            var contractResult = await SeedContractFixturesAsync(contractsDir);
            result.ContractFixtures = contractResult;

            result.Success = emailResult.Success && contractResult.Success;
            result.EndTime = DateTime.UtcNow;

            if (result.Success)
            {
                Console.WriteLine("\n✅ All fixtures created successfully!");
                Console.WriteLine($"Created files:");
                Console.WriteLine($"  📧 Inbox: {emailResult.Count} email samples");
                Console.WriteLine($"  📄 Contracts: {contractResult.Count} contract samples");
            }
            else
            {
                Console.WriteLine("\n❌ Some fixtures failed to create");
            }

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            result.EndTime = DateTime.UtcNow;
            return result;
        }
    }

    /// <summary>
    /// Seeds email fixtures
    /// </summary>
    private async Task<EmailFixtureResult> SeedEmailFixturesAsync(string inboxDir)
    {
        var result = new EmailFixtureResult();
        
        try
        {
            var emails = new[]
            {
                new EmailFixture
                {
                    FileName = "spanish_inquiry.eml",
                    Subject = "Consulta sobre servicios",
                    From = "cliente@empresa.es",
                    Body = @"From: cliente@empresa.es
To: soporte@nexo.com
Subject: Consulta sobre servicios

Estimado equipo de Nexo,

Me pongo en contacto con ustedes para solicitar información sobre sus servicios de automatización de procesos. 

Nuestra empresa está interesada en implementar una solución que nos permita:
- Automatizar el procesamiento de documentos
- Mejorar la eficiencia en la gestión de contratos
- Reducir el tiempo de respuesta a clientes

¿Podrían enviarme más información sobre sus productos y precios?

Quedo a la espera de su respuesta.

Saludos cordiales,
María González
Gerente de Operaciones
Empresa Innovadora S.L."
                },
                new EmailFixture
                {
                    FileName = "english_support.eml",
                    Subject = "Technical Support Request",
                    From = "support@techcorp.com",
                    Body = @"From: support@techcorp.com
To: help@nexo.com
Subject: Technical Support Request

Hello Nexo Support Team,

I'm experiencing issues with the API integration. The authentication is failing with error code 401.

Here are the details:
- API Version: v2.1
- Endpoint: /api/features/execute
- Error: Unauthorized access
- Timestamp: 2024-01-15 14:30:00 UTC

I've verified that my API key is correct and the endpoint URL is accurate. Could you please help me resolve this issue?

Best regards,
John Smith
Senior Developer
TechCorp Solutions"
                },
                new EmailFixture
                {
                    FileName = "french_proposal.eml",
                    Subject = "Proposition de partenariat",
                    From = "partenariat@startup.fr",
                    Body = @"From: partenariat@startup.fr
To: business@nexo.com
Subject: Proposition de partenariat

Bonjour,

Je représente une startup française spécialisée dans l'intelligence artificielle et nous sommes très intéressés par votre plateforme Nexo.

Nous aimerions explorer une collaboration qui pourrait inclure:
- Intégration de nos modèles d'IA avec votre système
- Développement de nouvelles fonctionnalités
- Expansion sur le marché européen

Serait-il possible d'organiser une réunion pour discuter de cette opportunité?

Cordialement,
Sophie Dubois
Directrice des Partenariats
AI Startup France"
                },
                new EmailFixture
                {
                    FileName = "sensitive_data.eml",
                    Subject = "URGENT: Data Breach Notification",
                    From = "security@company.com",
                    Body = @"From: security@company.com
To: compliance@nexo.com
Subject: URGENT: Data Breach Notification

URGENT SECURITY ALERT

We have detected a potential data breach in our system. The following information may have been compromised:

- Customer names and email addresses
- Credit card numbers: 4532-1234-5678-9012
- Social Security Numbers: 123-45-6789
- Bank account details

Immediate action is required. Please contact our security team at +1-555-0123.

This email contains sensitive information and should be handled according to our data protection policies.

Security Team
Company Inc."
                }
            };

            foreach (var email in emails)
            {
                var filePath = Path.Combine(inboxDir, email.FileName);
                await File.WriteAllTextAsync(filePath, email.Body);
                result.CreatedFiles.Add(filePath);
            }

            result.Success = true;
            result.Count = emails.Length;
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Seeds contract fixtures
    /// </summary>
    private async Task<ContractFixtureResult> SeedContractFixturesAsync(string contractsDir)
    {
        var result = new ContractFixtureResult();
        
        try
        {
            var contracts = new[]
            {
                new ContractFixture
                {
                    FileName = "service_agreement.pdf",
                    Title = "Software Service Agreement",
                    Content = @"SOFTWARE SERVICE AGREEMENT

This Software Service Agreement (""Agreement"") is entered into on January 1, 2024, between Nexo Technologies Inc. (""Provider"") and Client Company (""Client"").

1. SERVICES
Provider agrees to provide software services including:
- Feature development and customization
- Technical support and maintenance
- Cloud hosting and infrastructure
- Data processing and analytics

2. TERM
This Agreement shall commence on the Effective Date and continue for a period of 12 months, unless terminated earlier in accordance with the terms herein.

3. PAYMENT
Client shall pay Provider $10,000 per month for the services provided under this Agreement. Payment is due within 30 days of invoice date.

4. LIABILITY LIMITATION
Provider's total liability under this Agreement shall not exceed the total amount paid by Client in the 12 months preceding the claim.

5. TERMINATION
Either party may terminate this Agreement with 30 days written notice.

6. CONFIDENTIALITY
Both parties agree to maintain the confidentiality of all proprietary information disclosed during the term of this Agreement.

IN WITNESS WHEREOF, the parties have executed this Agreement as of the date first written above.

Provider: _________________    Client: _________________
Date: _________________        Date: _________________"
                },
                new ContractFixture
                {
                    FileName = "nda_agreement.pdf",
                    Title = "Non-Disclosure Agreement",
                    Content = @"NON-DISCLOSURE AGREEMENT

This Non-Disclosure Agreement (""NDA"") is made effective as of January 1, 2024, between Nexo Technologies Inc. (""Disclosing Party"") and Partner Company (""Receiving Party"").

1. DEFINITION OF CONFIDENTIAL INFORMATION
Confidential Information includes all technical, business, and proprietary information disclosed by the Disclosing Party to the Receiving Party.

2. OBLIGATIONS OF RECEIVING PARTY
The Receiving Party agrees to:
- Hold all Confidential Information in strict confidence
- Not disclose Confidential Information to third parties
- Use Confidential Information solely for the purpose of evaluating potential business opportunities
- Return all Confidential Information upon request

3. EXCEPTIONS
This NDA does not apply to information that:
- Is publicly available
- Was known to the Receiving Party prior to disclosure
- Is independently developed by the Receiving Party
- Is required to be disclosed by law

4. TERM
This NDA shall remain in effect for a period of 3 years from the effective date.

5. REMEDIES
The Receiving Party acknowledges that breach of this NDA would cause irreparable harm to the Disclosing Party, and that monetary damages would be inadequate.

IN WITNESS WHEREOF, the parties have executed this NDA as of the date first written above.

Disclosing Party: _________________    Receiving Party: _________________
Date: _________________               Date: _________________"
                },
                new ContractFixture
                {
                    FileName = "employment_contract.pdf",
                    Title = "Employment Agreement",
                    Content = @"EMPLOYMENT AGREEMENT

This Employment Agreement (""Agreement"") is entered into on January 1, 2024, between Nexo Technologies Inc. (""Company"") and Jane Doe (""Employee"").

1. POSITION AND DUTIES
Employee shall serve as Senior Software Engineer and shall perform such duties as may be assigned by the Company from time to time.

2. COMPENSATION
Employee shall receive:
- Annual salary of $120,000
- Health insurance benefits
- 401(k) matching up to 6%
- 20 days paid vacation annually

3. TERM OF EMPLOYMENT
This Agreement shall commence on January 1, 2024, and continue until terminated by either party in accordance with the terms herein.

4. CONFIDENTIALITY
Employee agrees to maintain the confidentiality of all Company proprietary information and trade secrets.

5. NON-COMPETE
Employee agrees not to work for a competing company for a period of 12 months following termination of employment.

6. TERMINATION
Either party may terminate this Agreement with 2 weeks written notice. The Company may terminate immediately for cause.

7. INTELLECTUAL PROPERTY
All work product created by Employee during employment shall be the property of the Company.

IN WITNESS WHEREOF, the parties have executed this Agreement as of the date first written above.

Company: _________________    Employee: _________________
Date: _________________       Date: _________________"
                }
            };

            foreach (var contract in contracts)
            {
                var filePath = Path.Combine(contractsDir, contract.FileName);
                await File.WriteAllTextAsync(filePath, contract.Content);
                result.CreatedFiles.Add(filePath);
            }

            result.Success = true;
            result.Count = contracts.Length;
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            return result;
        }
    }
}

/// <summary>
/// Email fixture data
/// </summary>
public class EmailFixture
{
    public string FileName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// Contract fixture data
/// </summary>
public class ContractFixture
{
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Result of seeding fixtures
/// </summary>
public class FixtureResult
{
    public string ProjectRoot { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public EmailFixtureResult EmailFixtures { get; set; } = new();
    public ContractFixtureResult ContractFixtures { get; set; } = new();
    
    public TimeSpan Duration => EndTime - StartTime;
}

/// <summary>
/// Result of seeding email fixtures
/// </summary>
public class EmailFixtureResult
{
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<string> CreatedFiles { get; set; } = new();
}

/// <summary>
/// Result of seeding contract fixtures
/// </summary>
public class ContractFixtureResult
{
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<string> CreatedFiles { get; set; } = new();
}
