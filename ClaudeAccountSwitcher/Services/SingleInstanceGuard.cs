using System.IO.Pipes;
using System.Threading;

namespace ClaudeAccountSwitcher.Services;

public sealed class SingleInstanceGuard : IDisposable
{
    private const string MutexName = @"Global\ClaudeAccountSwitcher_SingleInstance_9F1B7C2E-4E1E-4B7E-9E3C-2B6A5B9D8C1A";
    private const string PipeName = "ClaudeAccountSwitcher_ActivationPipe_9F1B7C2E";
    private const string ActivationSignal = "ACTIVATE";

    private Mutex? _mutex;
    private CancellationTokenSource? _listenerCts;

    public bool TryAcquire()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);

        if (createdNew)
        {
            return true;
        }

        // Já existe uma instância; tenta pegar o mutex de qualquer forma para o caso
        // de ele ter sido abandonado (processo anterior morto via Task Manager).
        try
        {
            var acquired = _mutex.WaitOne(TimeSpan.Zero);
            return acquired;
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
    }

    public void NotifyExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(timeout: 1000);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine(ActivationSignal);
        }
        catch (Exception ex)
        {
            Logger.Log($"Falha ao sinalizar instância existente: {ex.Message}");
        }
    }

    public void StartListening(Action onActivationRequested)
    {
        _listenerCts = new CancellationTokenSource();
        var token = _listenerCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        PipeName, PipeDirection.In, maxNumberOfServerInstances: 1,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(token);
                    using var reader = new StreamReader(server);
                    var line = await reader.ReadLineAsync();

                    if (line == ActivationSignal)
                    {
                        onActivationRequested();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Erro no listener de ativação: {ex.Message}");
                }
            }
        }, token);
    }

    public void Dispose()
    {
        _listenerCts?.Cancel();
        _listenerCts?.Dispose();
        _mutex?.Dispose();
    }
}
