// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

using System.Diagnostics;
using System.Text;

namespace cCoder.Assets.UI.Tests.Infrastructure;

internal sealed class ExternalApplication(string name) : IAsyncDisposable
{
    private readonly StringBuilder output = new();
    private Process? process;

    internal string Output => output.ToString();

    internal async Task StartAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string> environment,
        Func<Task<bool>> readinessProbe,
        TimeSpan timeout)
    {
        process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        foreach ((string key, string value) in environment)
        {
            process.StartInfo.Environment[key] = value;
        }

        process.OutputDataReceived += (_, args) => Append(line: args.Data);
        process.ErrorDataReceived += (_, args) => Append(line: args.Data);

        if (!process.Start())
        {
            throw new InvalidOperationException(
                $"Failed to start published application '{name}'.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using CancellationTokenSource timeoutToken = new(timeout);

        while (!timeoutToken.IsCancellationRequested)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"Published application '{name}' exited during startup."
                    + Environment.NewLine
                    + Output);
            }

            if (await readinessProbe())
            {
                return;
            }

            await Task.Delay(
                millisecondsDelay: 250,
                cancellationToken: timeoutToken.Token);
        }

        throw new TimeoutException(
            $"Published application '{name}' did not become ready."
            + Environment.NewLine
            + Output);
    }

    public async ValueTask DisposeAsync()
    {
        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
        finally
        {
            process.Dispose();
        }
    }

    private void Append(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (output)
        {
            output.AppendLine(value: line);
        }
    }
}