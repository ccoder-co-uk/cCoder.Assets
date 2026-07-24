// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using cCoder.Packer.Models.Commands;
using cCoder.Packer.Models.Configurations;
using cCoder.Packer.Models.Exports;
using cCoder.Packer.Services.Processings.Packing;

namespace cCoder.Packer.Services.Processings.Commands;

internal sealed partial class UnpackCommandHandlingProcessingService(
    PackerConfiguration configuration)
    : IUnpackCommandHandlingProcessingService
{
    public Task<int> HandleCommandOptionsAsync(
        CommandOptions command,
        CancellationToken cancellationToken = default) =>
        TryCatch(operation: async () =>
        {
            Validate(inputs: [command, cancellationToken]);

            return await UnpackAsync(
                command: command,
                cancellationToken: cancellationToken);
        });

    private async Task<int> UnpackAsync(
        CommandOptions command,
        CancellationToken cancellationToken)
    {
        (string user, string password) = GetCredentials(command: command);

        using HttpClient httpClient = new();

        PackerApiClientProcessingService api = new(
            httpClient: httpClient,
            source: command.Source
                ?? throw new InvalidOperationException(
                    message: "An unpack source is required."));

        await api.LoginAsync(
            user: user,
            password: password,
            cancellationToken: cancellationToken);

        IReadOnlyList<ExportRecord> records =
            command.Target == "commoncache"
                ? await api.ExportCommonCacheAsync(
                    cancellationToken: cancellationToken)
                : await api.ExportAppAsync(
                    requestedAppId: command.AppId,
                    cancellationToken: cancellationToken);

        string dataPath = ResolvePath(
            suppliedPath: command.DataPath,
            configuredPath: configuration.DataPath
                ?? throw new InvalidOperationException(
                    message: "The configured data path is required."));

        ExportWriterProcessingService writer = new(dataPath: dataPath);

        IReadOnlyList<string> files = await writer.WriteExportRecordsAsync(
            records: records,
            cancellationToken: cancellationToken);

        Console.WriteLine(
            value: $"Unpacked {records.Count} business objects into " +
                $"{files.Count} files under '{dataPath}'.");

        return 0;
    }

    private static string ResolvePath(
        string? suppliedPath,
        string configuredPath) =>
        Path.GetFullPath(
            path: suppliedPath ?? configuredPath,
            basePath: AppContext.BaseDirectory);

    private static (string User, string Password) GetCredentials(
        CommandOptions command)
    {
        string? user = command.User
            ?? Environment.GetEnvironmentVariable(variable: "CCODER_USER");

        string? password = command.Password
            ?? Environment.GetEnvironmentVariable(variable: "CCODER_PASSWORD");

        if (string.IsNullOrWhiteSpace(value: user))
        {
            Console.Write(value: "User: ");

            user = Console.ReadLine();
        }

        if (string.IsNullOrWhiteSpace(value: password))
        {
            Console.Write(value: "Password: ");

            password = ReadSecret();

            Console.WriteLine();
        }

        if (string.IsNullOrWhiteSpace(value: user)
            || string.IsNullOrWhiteSpace(value: password))
        {
            throw new InvalidOperationException(
                message: "Credentials are required. Supply them through prompts, " +
                    "-user/-password, or CCODER_USER/CCODER_PASSWORD.");
        }

        return (user, password);
    }

    private static string ReadSecret()
    {
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine() ?? string.Empty;
        }

        List<char> value = [];

        ConsoleKeyInfo key;

        while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace)
            {
                if (value.Count > 0)
                {
                    value.RemoveAt(index: value.Count - 1);
                }

                continue;
            }

            if (!char.IsControl(c: key.KeyChar))
            {
                value.Add(item: key.KeyChar);
            }
        }

        return new string(value: [.. value]);
    }
}