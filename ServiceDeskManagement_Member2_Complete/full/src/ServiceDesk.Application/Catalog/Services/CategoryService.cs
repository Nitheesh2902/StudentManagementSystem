using EmployeeServiceDesk.Application.Abstractions.Persistence;
using FluentValidation;
using ServiceDesk.Application.Abstractions;
using ServiceDesk.Application.Abstractions.Persistence;
using ServiceDesk.Application.Catalog.Dtos;
using ServiceDesk.Application.Common;
using ServiceDesk.Domain.Entities;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Application.Catalog.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _user;
    private readonly IValidator<CreateCategoryRequest> _createValidator;
    private readonly IValidator<UpdateCategoryRequest> _updateValidator;

    public CategoryService(
        ICategoryRepository categories,
        IUnitOfWork uow,
        IAuditService audit,
        ICurrentUserService user,
        IValidator<CreateCategoryRequest> createValidator,
        IValidator<UpdateCategoryRequest> updateValidator)
    {
        _categories = categories;
        _uow = uow;
        _audit = audit;
        _user = user;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<CategoryResponse>> GetPagedAsync(CategoryQuery query, CancellationToken ct = default)
    {
        _user.RequireAuthenticated();

        // Only administrators may look at deactivated categories.
        var includeInactive = query.IncludeInactive && _user.IsInRole(Roles.Administrator);

        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var (items, total) = await _categories.GetPagedAsync(
            query.Search?.Trim(), includeInactive, page, pageSize, ct);

        return new PagedResult<CategoryResponse>(
            items.Select(c => c.ToResponse()).ToList(), page, pageSize, total);
    }

    public async Task<CategoryResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        _user.RequireAuthenticated();
        var category = await GetOrThrowAsync(id, ct);

        if (!category.IsActive && !_user.IsInRole(Roles.Administrator))
            throw new NotFoundException(nameof(TicketCategory), id);

        return category.ToResponse();
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        _user.RequireAdmin();
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var name = request.Name.Trim();
        if (await _categories.NameExistsAsync(name, null, ct))
            throw new ConflictException($"A category named '{name}' already exists.");

        var category = new TicketCategory(name, request.Description);
        await _categories.AddAsync(category, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync("Create", nameof(TicketCategory), category.Id.ToString(), $"Name={category.Name}", ct);
        return category.ToResponse();
    }

    public async Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        _user.RequireAdmin();
        await _updateValidator.ValidateAndThrowAsync(request, ct);

        var category = await GetOrThrowAsync(id, ct);
        var name = request.Name.Trim();
        if (await _categories.NameExistsAsync(name, id, ct))
            throw new ConflictException($"A category named '{name}' already exists.");

        category.Update(name, request.Description);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync("Update", nameof(TicketCategory), id.ToString(), $"Name={category.Name}", ct);
        return category.ToResponse();
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        _user.RequireAdmin();
        var category = await GetOrThrowAsync(id, ct);
        if (!category.IsActive) return; // idempotent

        if (await _categories.HasActiveServicesAsync(id, ct))
            throw new ConflictException(
                "This category still has active services. Deactivate its services first.");

        category.Deactivate();
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("Deactivate", nameof(TicketCategory), id.ToString(), null, ct);
    }

    public async Task ActivateAsync(int id, CancellationToken ct = default)
    {
        _user.RequireAdmin();
        var category = await GetOrThrowAsync(id, ct);
        if (category.IsActive) return;

        category.Activate();
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("Activate", nameof(TicketCategory), id.ToString(), null, ct);
    }

    private async Task<TicketCategory> GetOrThrowAsync(int id, CancellationToken ct)
        => await _categories.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(TicketCategory), id);
}
