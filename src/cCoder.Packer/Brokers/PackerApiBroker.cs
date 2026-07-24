// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Brokers;

internal sealed class PackerApiBroker(HttpClient httpClient)
    : IPackerApiBroker
{
    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default) =>
        httpClient.SendAsync(
            request: request,
            cancellationToken: cancellationToken);
}