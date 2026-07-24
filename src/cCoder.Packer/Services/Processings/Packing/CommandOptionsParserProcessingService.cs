// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Commands;

namespace cCoder.Packer.Services.Processings.Packing;

internal sealed partial class CommandOptionsParserProcessingService
    : ICommandOptionsParserProcessingService
{
    public CommandOptions Parse(string[] args) =>
        TryCatch(operation: () =>
        {
            Validate(inputs: args);

            return ParseCommandOptions(args: args);
        });

    private static CommandOptions ParseCommandOptions(string[] args)
    {
        string? localCommand = ResolveLocalCommand(args: args);

        Dictionary<string, string> values = new(comparer: StringComparer.OrdinalIgnoreCase);

        for (int index = localCommand is null ? 0 : 1;
            index < args.Length;
            index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith(value: '-'))
            {
                throw new ArgumentException(
                    message: $"Expected a value after '{args[index]}'.");
            }

            values[args[index].TrimStart(trimChar: '-')] = args[index + 1];
        }

        values.TryGetValue(key: "dataPath", value: out string? dataPath);

        values.TryGetValue(key: "packagesPath", value: out string? packagesPath);

        if (localCommand == "create")
        {
            values.TryGetValue(key: "api", value: out string? api);
            values.TryGetValue(key: "name", value: out string? appName);
            values.TryGetValue(key: "tenant", value: out string? tenantId);
            values.TryGetValue(key: "baseline", value: out string? baselinePath);
            values.TryGetValue(key: "user", value: out string? createUser);

            string? createPassword = values.GetValueOrDefault(key: "pass")
                ?? values.GetValueOrDefault(key: "password");

            if (!Uri.TryCreate(
                uriString: api,
                uriKind: UriKind.Absolute,
                result: out Uri? apiUri)
                || apiUri.Scheme is not ("http" or "https"))
            {
                throw new ArgumentException(
                    message: "Use '-api' with an absolute HTTP or HTTPS URL.");
            }

            return new CommandOptions(
                Name: localCommand,
                Target: null,
                Source: EnsureTrailingSlash(source: apiUri),
                User: createUser,
                Password: createPassword,
                AppName: appName,
                TenantId: tenantId,
                BaselinePath: baselinePath,
                AppId: null,
                DataPath: null,
                PackagesPath: null);
        }

        if (localCommand is not null)
        {
            return new CommandOptions(
                Name: localCommand,
                Target: null,
                Source: null,
                User: null,
                Password: null,
                AppName: null,
                TenantId: null,
                BaselinePath: null,
                AppId: null,
                DataPath: dataPath,
                PackagesPath: packagesPath);
        }

        if (!values.TryGetValue(key: "unpack", value: out string? target)
            || target is not ("commoncache" or "app"))
        {
            throw new ArgumentException(
                message: "Use '-unpack commoncache' or '-unpack app'.");
        }

        if (!values.TryGetValue(key: "from", value: out string? source)
            || !Uri.TryCreate(
                uriString: source,
                uriKind: UriKind.Absolute,
                result: out Uri? sourceUri)
            || sourceUri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException(
                message: "Use '-from' with an absolute HTTP or HTTPS URL.");
        }

        int? appId = null;

        if (values.TryGetValue(key: "appId", value: out string? appIdValue))
        {
            if (!int.TryParse(s: appIdValue, result: out int parsedAppId))
            {
                throw new ArgumentException(
                    message: "The '-appId' value must be an integer.");
            }

            appId = parsedAppId;
        }

        values.TryGetValue(key: "user", value: out string? user);

        values.TryGetValue(key: "password", value: out string? password);

        return new CommandOptions(
            Name: "unpack",
            Target: target,
            Source: EnsureTrailingSlash(source: sourceUri),
            User: user,
            Password: password,
            AppName: null,
            TenantId: null,
            BaselinePath: null,
            AppId: appId,
            DataPath: dataPath,
            PackagesPath: packagesPath);
    }

    private static string? ResolveLocalCommand(string[] args)
    {
        if (args.Length == 0)
        {
            return null;
        }

        string candidate = args[0].TrimStart(trimChar: '-');

        return candidate.Equals(
            value: "pack",
            comparisonType: StringComparison.OrdinalIgnoreCase)
            || candidate.Equals(
                value: "report",
                comparisonType: StringComparison.OrdinalIgnoreCase)
            || candidate.Equals(
                value: "create",
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? candidate.ToLowerInvariant()
                : null;
    }

    private static Uri EnsureTrailingSlash(Uri source)
    {
        UriBuilder builder = new(uri: source)
        {
            Path = source.AbsolutePath.TrimEnd(trimChar: '/') + "/",
            Query = string.Empty,
            Fragment = string.Empty,
        };

        return builder.Uri;
    }
}