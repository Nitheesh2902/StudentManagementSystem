using ServiceDesk.Application.Catalog.Dtos;
using ServiceDesk.Application.Common;

namespace ServiceDesk.Application.Catalog.Services;

public interface ICategoryService
{
    Task<PagedResult<CategoryResponse>> GetPagedAsync(CategoryQuery query, CancellationToken ct = default);
    Task<CategoryResponse> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
    Task ActivateAsync(int id, CancellationToken ct = default);
}
