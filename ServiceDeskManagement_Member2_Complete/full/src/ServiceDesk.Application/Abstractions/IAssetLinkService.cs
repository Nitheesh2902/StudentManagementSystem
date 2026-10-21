namespace ServiceDesk.Application.Abstractions;

/// <summary>Owner: Member 4. Lets a ticket reference an asset.</summary>
public interface IAssetLinkService
{
    Task<bool> AssetExistsAsync(int assetId, CancellationToken ct = default);
    Task LinkTicketAsync(int assetId, int ticketId, CancellationToken ct = default);
}
