using System.Text.Json;

namespace Ashlar.CLI.Commands;

/// <summary>The cloud targets the exporter can stage for.</summary>
public enum CloudTarget
{
    /// <summary>AWS — ECS Fargate one-shot task via ECR.</summary>
    Aws,

    /// <summary>Azure — Container Instances one-shot container via ACR.</summary>
    Azure,
}

/// <summary>
/// Stages a certified project into a one-command cloud deploy bundle: the governed app beside a
/// Dockerfile that layers it onto the published Ashlar runtime image, an entrypoint that
/// VERIFIES BEFORE IT RUNS (the cloud refuses a tampered app exactly like the native launcher
/// does), and a deploy script per target. Deterministic, offline, unit-testable — the deploy
/// script is where the cloud is actually touched, and only when the operator runs it.
/// </summary>
public static class CloudBundle
{
    /// <summary>The bundle format tag.</summary>
    public const string Format = "ashlar-cloud/v1";

    /// <summary>The published runtime image the Dockerfile layers the app onto.</summary>
    public const string RuntimeImage = "ghcr.io/ianfrelinger/nexo-cli:latest";

    /// <summary>Stages the full cloud bundle and returns the relative paths written.</summary>
    public static IReadOnlyList<string> Stage(string projectDir, string bundleDir, BundleInfo info, CloudTarget target)
    {
        Directory.CreateDirectory(bundleDir);
        var written = NativeBundle.StageApp(projectDir, bundleDir);

        // The container entrypoint: verify, then run the request passed as container args. A
        // container whose app or ledger was altered exits 65 at startup and never serves a run —
        // the self-proving contract holds in the cloud too.
        var entrypoint =
            "#!/bin/sh\n"
            + "set -e\n"
            + "dotnet /app/Ashlar.CLI.dll verify --path /work/app\n"
            + "if [ \"$#\" -gt 0 ]; then\n"
            + "  exec dotnet /app/Ashlar.CLI.dll run \"$@\" --path /work/app\n"
            + "else\n"
            + "  echo\n"
            + "  echo \"certified and ready. pass a request as the container command to run it.\"\n"
            + "fi\n";
        WriteText(Path.Combine(bundleDir, "entrypoint.sh"), entrypoint, written, bundleDir, unixExecutable: true);

        // The image: the published runtime plus THIS certified app. `USER root` only to place
        // files; execution returns to the base image's unprivileged user.
        var dockerfile =
            $"FROM {RuntimeImage}\n"
            + "USER root\n"
            + "COPY --chown=app:app app /work/app\n"
            + "COPY --chown=app:app entrypoint.sh /work/entrypoint.sh\n"
            + "RUN chmod +x /work/entrypoint.sh\n"
            + "USER $APP_UID\n"
            + "ENTRYPOINT [\"/work/entrypoint.sh\"]\n";
        WriteText(Path.Combine(bundleDir, "Dockerfile"), dockerfile, written, bundleDir);

        var name = Safe(info.Name);
        if (target == CloudTarget.Aws)
        {
            WriteText(Path.Combine(bundleDir, "deploy-aws.sh"), AwsScript(name), written, bundleDir, unixExecutable: true);
        }
        else
        {
            WriteText(Path.Combine(bundleDir, "deploy-azure.sh"), AzureScript(name), written, bundleDir, unixExecutable: true);
        }

        var descriptor = new
        {
            format = Format,
            target = target == CloudTarget.Aws ? "aws" : "azure",
            name = info.Name,
            verified = info.Verified,
            certified = info.Certified,
            signer = info.SignerFingerprint,
            ledgerEntries = info.LedgerEntries,
            runtimeImage = RuntimeImage,
            deploy = target == CloudTarget.Aws ? "./deploy-aws.sh \"<request>\"" : "./deploy-azure.sh \"<request>\"",
        };
        WriteText(Path.Combine(bundleDir, "bundle.json"),
            JsonSerializer.Serialize(descriptor, new JsonSerializerOptions { WriteIndented = true }), written, bundleDir);

        var certLine = info.Certified
            ? $"It is **certified**: signed {info.SignerFingerprint}, {info.LedgerEntries} signed ledger entr{(info.LedgerEntries == 1 ? "y" : "ies")}."
            : "It is **unsigned** — it verifies, but was not certified with an operator key.";
        var deployCmd = target == CloudTarget.Aws ? "./deploy-aws.sh \"<your request>\"" : "./deploy-azure.sh \"<your request>\"";
        var prereq = target == CloudTarget.Aws
            ? "- AWS CLI logged in (`aws sts get-caller-identity` works), Docker running, `AWS_REGION` set.\n"
            : "- Azure CLI logged in (`az account show` works), Docker running, `AZURE_RESOURCE_GROUP` and `AZURE_LOCATION` set.\n";
        var readme =
            $"# {info.Name} — one-command cloud deploy ({(target == CloudTarget.Aws ? "AWS" : "Azure")})\n\n"
            + $"{certLine}\n\n"
            + "## Deploy and run\n\n"
            + $"```\n{deployCmd}\n```\n\n"
            + "The script builds the image (the published Ashlar runtime + this app), pushes it to your\n"
            + "registry, and runs it as a one-shot cloud task. **The container verifies the app against its\n"
            + "contract and signed ledger before running** — a tampered app exits 65 in the cloud, same as\n"
            + "it would on your desk.\n\n"
            + "## Prerequisites\n\n"
            + prereq
            + "\n"
            + "- `app/` — the project: contract, operator-owned policy, signed ledger.\n"
            + "- `Dockerfile` — the runtime image + this app.\n"
            + "- `entrypoint.sh` — verify-then-run.\n"
            + "- `bundle.json` — what is inside and what certifies it.\n";
        WriteText(Path.Combine(bundleDir, "README.md"), readme, written, bundleDir);

        return written;
    }

    // ── deploy scripts ───────────────────────────────────────────────────────
    // These are the only place the cloud is touched, and only when the operator runs them. Both
    // are idempotent about the infrastructure they create and pass the request through as the
    // container command.

    private static string AwsScript(string name) =>
        "#!/usr/bin/env sh\n"
        + "# One-command deploy: build → push to ECR → run as an ECS Fargate one-shot task.\n"
        + "set -e\n"
        + ": \"${AWS_REGION:?set AWS_REGION, e.g. export AWS_REGION=us-east-1}\"\n"
        + $"NAME=\"{name}\"\n"
        + "ACCOUNT=$(aws sts get-caller-identity --query Account --output text)\n"
        + "REGISTRY=\"$ACCOUNT.dkr.ecr.$AWS_REGION.amazonaws.com\"\n"
        + "IMAGE=\"$REGISTRY/$NAME:latest\"\n"
        + "\n"
        + "aws ecr describe-repositories --repository-names \"$NAME\" >/dev/null 2>&1 \\\n"
        + "  || aws ecr create-repository --repository-name \"$NAME\" >/dev/null\n"
        + "aws ecr get-login-password | docker login --username AWS --password-stdin \"$REGISTRY\"\n"
        + "docker build -t \"$IMAGE\" \"$(dirname \"$0\")\"\n"
        + "docker push \"$IMAGE\"\n"
        + "\n"
        + "aws ecs describe-clusters --clusters \"$NAME\" --query 'clusters[?status==`ACTIVE`]' --output text | grep -q . \\\n"
        + "  || aws ecs create-cluster --cluster-name \"$NAME\" >/dev/null\n"
        + "aws logs create-log-group --log-group-name \"/ashlar/$NAME\" 2>/dev/null || true\n"
        + "\n"
        + "# Execution role (pull + logs); created once, reused after.\n"
        + "ROLE=\"$NAME-exec\"\n"
        + "if ! aws iam get-role --role-name \"$ROLE\" >/dev/null 2>&1; then\n"
        + "  aws iam create-role --role-name \"$ROLE\" --assume-role-policy-document '{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":{\"Service\":\"ecs-tasks.amazonaws.com\"},\"Action\":\"sts:AssumeRole\"}]}' >/dev/null\n"
        + "  aws iam attach-role-policy --role-name \"$ROLE\" --policy-arn arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy\n"
        + "  sleep 10\n"
        + "fi\n"
        + "ROLE_ARN=$(aws iam get-role --role-name \"$ROLE\" --query Role.Arn --output text)\n"
        + "\n"
        + "REQUEST=\"${1:-}\"\n"
        + "CMD='[]'\n"
        + "[ -n \"$REQUEST\" ] && CMD=\"[\\\"$REQUEST\\\"]\"\n"
        + "aws ecs register-task-definition --family \"$NAME\" --requires-compatibilities FARGATE \\\n"
        + "  --network-mode awsvpc --cpu 512 --memory 1024 --execution-role-arn \"$ROLE_ARN\" \\\n"
        + "  --container-definitions \"[{\\\"name\\\":\\\"$NAME\\\",\\\"image\\\":\\\"$IMAGE\\\",\\\"command\\\":$CMD,\\\"logConfiguration\\\":{\\\"logDriver\\\":\\\"awslogs\\\",\\\"options\\\":{\\\"awslogs-group\\\":\\\"/ashlar/$NAME\\\",\\\"awslogs-region\\\":\\\"$AWS_REGION\\\",\\\"awslogs-stream-prefix\\\":\\\"run\\\"}}}]\" >/dev/null\n"
        + "\n"
        + "VPC=$(aws ec2 describe-vpcs --filters Name=is-default,Values=true --query 'Vpcs[0].VpcId' --output text)\n"
        + "SUBNET=$(aws ec2 describe-subnets --filters Name=vpc-id,Values=\"$VPC\" --query 'Subnets[0].SubnetId' --output text)\n"
        + "aws ecs run-task --cluster \"$NAME\" --task-definition \"$NAME\" --launch-type FARGATE \\\n"
        + "  --network-configuration \"awsvpcConfiguration={subnets=[$SUBNET],assignPublicIp=ENABLED}\" >/dev/null\n"
        + "echo \"task launched — follow it with:  aws logs tail /ashlar/$NAME --follow\"\n";

    private static string AzureScript(string name) =>
        "#!/usr/bin/env sh\n"
        + "# One-command deploy: build → push to ACR → run as an Azure Container Instances one-shot.\n"
        + "set -e\n"
        + ": \"${AZURE_RESOURCE_GROUP:?set AZURE_RESOURCE_GROUP}\"\n"
        + ": \"${AZURE_LOCATION:?set AZURE_LOCATION, e.g. export AZURE_LOCATION=eastus}\"\n"
        + $"NAME=\"{name}\"\n"
        + "ACR=$(echo \"$NAME\" | tr -cd 'a-z0-9')acr\n"
        + "\n"
        + "az group show --name \"$AZURE_RESOURCE_GROUP\" >/dev/null 2>&1 \\\n"
        + "  || az group create --name \"$AZURE_RESOURCE_GROUP\" --location \"$AZURE_LOCATION\" >/dev/null\n"
        + "az acr show --name \"$ACR\" >/dev/null 2>&1 \\\n"
        + "  || az acr create --name \"$ACR\" --resource-group \"$AZURE_RESOURCE_GROUP\" --sku Basic --admin-enabled true >/dev/null\n"
        + "\n"
        + "# Build in the registry (no local Docker daemon needed on this path).\n"
        + "az acr build --registry \"$ACR\" --image \"$NAME:latest\" \"$(dirname \"$0\")\"\n"
        + "\n"
        + "REQUEST=\"${1:-}\"\n"
        + "SERVER=$(az acr show --name \"$ACR\" --query loginServer --output tsv)\n"
        + "USERNAME=$(az acr credential show --name \"$ACR\" --query username --output tsv)\n"
        + "PASSWORD=$(az acr credential show --name \"$ACR\" --query 'passwords[0].value' --output tsv)\n"
        + "az container delete --name \"$NAME\" --resource-group \"$AZURE_RESOURCE_GROUP\" --yes >/dev/null 2>&1 || true\n"
        + "if [ -n \"$REQUEST\" ]; then\n"
        + "  az container create --name \"$NAME\" --resource-group \"$AZURE_RESOURCE_GROUP\" \\\n"
        + "    --image \"$SERVER/$NAME:latest\" --registry-login-server \"$SERVER\" \\\n"
        + "    --registry-username \"$USERNAME\" --registry-password \"$PASSWORD\" \\\n"
        + "    --restart-policy Never --command-line \"/work/entrypoint.sh $REQUEST\" >/dev/null\n"
        + "else\n"
        + "  az container create --name \"$NAME\" --resource-group \"$AZURE_RESOURCE_GROUP\" \\\n"
        + "    --image \"$SERVER/$NAME:latest\" --registry-login-server \"$SERVER\" \\\n"
        + "    --registry-username \"$USERNAME\" --registry-password \"$PASSWORD\" \\\n"
        + "    --restart-policy Never >/dev/null\n"
        + "fi\n"
        + "echo \"container launched — follow it with:  az container logs --follow --name $NAME --resource-group $AZURE_RESOURCE_GROUP\"\n";

    private static void WriteText(string dest, string content, List<string> written, string bundleRoot, bool unixExecutable = false)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.WriteAllText(dest, content);
        written.Add(Path.GetRelativePath(bundleRoot, dest));
        if (unixExecutable && !OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(dest,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
    }

    private static string Safe(string name)
    {
        var chars = name.ToLowerInvariant().Where(c => char.IsAsciiLetterOrDigit(c) || c is '-').ToArray();
        return chars.Length > 0 ? new string(chars) : "app";
    }
}
