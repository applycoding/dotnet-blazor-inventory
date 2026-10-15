using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Stockroom.Application.Abstractions;
using Stockroom.Application.Common;
using Stockroom.Contracts.Dtos;
using Stockroom.Domain.Entities;

namespace Stockroom.Application.Customers;

public sealed record CreateCustomerCommand(string Name, string Email, string? Phone) : IRequest<CustomerDto>;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(command => command.Phone).MaximumLength(40);
    }
}

public sealed class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly IStockroomDbContext _db;

    public CreateCustomerHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var exists = await _db.Customers.AnyAsync(customer => customer.Email == email, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"A customer with email '{email}' already exists.");
        }

        var customer = Customer.Create(command.Name, email, command.Phone);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);
        return customer.ToDto();
    }
}

public sealed record GetCustomersQuery(string? Search) : IRequest<IReadOnlyList<CustomerDto>>;

public sealed class GetCustomersHandler : IRequestHandler<GetCustomersQuery, IReadOnlyList<CustomerDto>>
{
    private readonly IStockroomDbContext _db;

    public GetCustomersHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Customers.AsNoTracking();
        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var term = search.ToLowerInvariant();
            query = query.Where(customer =>
                customer.Name.ToLower().Contains(term) || customer.Email.ToLower().Contains(term));
        }

        var customers = await query
            .OrderBy(customer => customer.Name)
            .Take(100)
            .ToListAsync(cancellationToken);

        return customers.Select(customer => customer.ToDto()).ToList();
    }
}
