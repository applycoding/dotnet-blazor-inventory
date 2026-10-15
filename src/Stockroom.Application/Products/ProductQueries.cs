using MediatR;

using Microsoft.EntityFrameworkCore;

using Stockroom.Application.Abstractions;
using Stockroom.Application.Common;
using Stockroom.Contracts.Dtos;

namespace Stockroom.Application.Products;

public sealed record GetProductsQuery(string? Search, int Page, int PageSize) : IRequest<PagedResult<ProductDto>>;

public sealed class GetProductsHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IStockroomDbContext _db;

    public GetProductsHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PageRequest.Normalize(request.Page, request.PageSize);
        var query = _db.Products.AsNoTracking();
        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var term = search.ToLowerInvariant();
            query = query.Where(product => product.Sku.ToLower().Contains(term) || product.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(product => product.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items.Select(product => product.ToDto()).ToList(), page, pageSize, total);
    }
}

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

public sealed class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IStockroomDbContext _db;

    public GetProductByIdHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        return await ProductReader.WithMovementsAsync(_db, product, cancellationToken);
    }
}
