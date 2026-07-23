namespace cCoder.Packer;

public sealed record CommandOptions(
    string Target,
    Uri Source,
    string? User,
    string? Password,
    int? AppId)
{
    public static CommandOptions Parse(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith('-'))
                throw new ArgumentException($"Expected a value after '{args[index]}'.");

            values[args[index].TrimStart('-')] = args[index + 1];
        }

        if (!values.TryGetValue("unpack", out string? target)
            || target is not ("commoncache" or "app"))
        {
            throw new ArgumentException(
                "Use '-unpack commoncache' or '-unpack app'.");
        }

        if (!values.TryGetValue("from", out string? source)
            || !Uri.TryCreate(source, UriKind.Absolute, out Uri? sourceUri)
            || sourceUri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException(
                "Use '-from' with an absolute HTTP or HTTPS URL.");
        }

        int? appId = null;
        if (values.TryGetValue("appId", out string? appIdValue))
        {
            if (!int.TryParse(appIdValue, out int parsedAppId))
                throw new ArgumentException("The '-appId' value must be an integer.");

            appId = parsedAppId;
        }

        values.TryGetValue("user", out string? user);
        values.TryGetValue("password", out string? password);

        return new CommandOptions(
            target,
            EnsureTrailingSlash(sourceUri),
            user,
            password,
            appId);
    }

    private static Uri EnsureTrailingSlash(Uri source)
    {
        UriBuilder builder = new(source)
        {
            Path = source.AbsolutePath.TrimEnd('/') + "/",
            Query = string.Empty,
            Fragment = string.Empty,
        };

        return builder.Uri;
    }
}
