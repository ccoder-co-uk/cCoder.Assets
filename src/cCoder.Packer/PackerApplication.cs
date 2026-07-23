namespace cCoder.Packer;

public static class PackerApplication
{
    public static async Task<int> RunAsync(
        string[] args,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CommandOptions command = CommandOptions.Parse(args);
            PackerSettings settings = PackerSettings.Load();
            (string user, string password) = GetCredentials(command);

            using HttpClient httpClient = new();
            PackerApiClient api = new(httpClient, command.Source);
            await api.LoginAsync(user, password, cancellationToken);

            IReadOnlyList<ExportRecord> records =
                command.Target == "commoncache"
                    ? await api.ExportCommonCacheAsync(cancellationToken)
                    : await api.ExportAppAsync(command.AppId, cancellationToken);

            ExportWriter writer = new(settings.DataPath);
            IReadOnlyList<string> files = await writer.WriteAsync(
                records,
                cancellationToken);

            Console.WriteLine(
                $"Unpacked {records.Count} business objects into " +
                $"{files.Count} files under '{settings.DataPath}'.");

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"packer: {exception.Message}");
            Console.Error.WriteLine(
                "Usage: packer -unpack <commoncache|app> -from <url> " +
                "[-user <user>] [-password <password>] [-appId <id>]");

            return 1;
        }
    }

    private static (string User, string Password) GetCredentials(
        CommandOptions command)
    {
        string? user = command.User
            ?? Environment.GetEnvironmentVariable("CCODER_USER");

        string? password = command.Password
            ?? Environment.GetEnvironmentVariable("CCODER_PASSWORD");

        if (string.IsNullOrWhiteSpace(user))
        {
            Console.Write("User: ");
            user = Console.ReadLine();
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            Console.Write("Password: ");
            password = ReadSecret();
            Console.WriteLine();
        }

        if (string.IsNullOrWhiteSpace(user)
            || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Credentials are required. Supply them through prompts, " +
                "-user/-password, or CCODER_USER/CCODER_PASSWORD.");
        }

        return (user, password);
    }

    private static string ReadSecret()
    {
        if (Console.IsInputRedirected)
            return Console.ReadLine() ?? string.Empty;

        List<char> value = [];
        ConsoleKeyInfo key;

        while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace)
            {
                if (value.Count > 0)
                    value.RemoveAt(value.Count - 1);

                continue;
            }

            if (!char.IsControl(key.KeyChar))
                value.Add(key.KeyChar);
        }

        return new string(value.ToArray());
    }
}
