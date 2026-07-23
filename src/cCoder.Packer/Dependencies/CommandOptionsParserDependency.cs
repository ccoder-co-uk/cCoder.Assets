// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Commands;

namespace cCoder.Packer.Dependencies;

internal static class CommandOptionsParserDependency
{
    public static CommandOptions Parse(string[] args)
    {
        Dictionary<string, string> values = new(comparer: StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith(value: '-'))
            {
                throw new ArgumentException(
                    message: $"Expected a value after '{args[index]}'.");
            }

            values[args[index].TrimStart(trimChar: '-')] = args[index + 1];
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
            Target: target,
            Source: EnsureTrailingSlash(source: sourceUri),
            User: user,
            Password: password,
            AppId: appId);
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