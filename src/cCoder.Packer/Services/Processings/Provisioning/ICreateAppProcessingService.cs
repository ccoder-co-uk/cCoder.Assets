// ---------------------------------------------------------------
// Copyright (c) Paul.Ward@ccoder.co.uk
// ---------------------------------------------------------------

namespace cCoder.Packer.Services.Processings.Provisioning;

internal interface ICreateAppProcessingService
{
    Task<int> ProvisionAppAsync(
        Uri api,
        string name,
        string tenantId,
        string user,
        string password,
        string baselinePath,
        CancellationToken cancellationToken = default);
}