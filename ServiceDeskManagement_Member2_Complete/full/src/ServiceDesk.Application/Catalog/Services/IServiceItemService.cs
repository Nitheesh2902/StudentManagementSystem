using ServiceDesk.Application.Catalog.Dtos;
using ServiceDesk.Application.Common;

namespace ServiceDesk.Application.Catalog.Services;

public interface IServiceItemService
{
    Task<PagedResult<ServiceItemResponse>> GetPagedAsync(ServiceItemQuery query, CancellationToken ct = default);
    Task<ServiceItemResponse> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ServiceItemResponse> CreateAsync(CreateServiceItemRequest request, CancellationToken ct = default);
    Task<ServiceItemResponse> UpdateAsync(int id, UpdateServiceItemRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
    Task ActivateAsync(int id, CancellationToken ct = default);
}
