using Inventory.Application.Products;
using Inventory.Application.Validation;
using Inventory.Domain.Products;

namespace Inventory.UnitTests;

public class ValidatorAndArchitectureTests
{
    [Fact]
    public void Create_validator_rejects_an_empty_sku_and_a_negative_price()
    {
        var validator = new CreateProductCommandValidator();
        var result = validator.Validate(new CreateProductCommand(" ", "", null, -2m, -1, -3));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateProductCommand.Sku));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateProductCommand.UnitPrice));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateProductCommand.InitialQuantity));
    }

    [Fact]
    public void Domain_does_not_reference_the_other_projects()
    {
        var names = typeof(Product).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name).ToList();

        Assert.DoesNotContain(names, name => name is not null && name.StartsWith("Inventory.", StringComparison.Ordinal));
        Assert.DoesNotContain(names, name => name is not null && name.Contains("EntityFramework", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_references_domain_and_not_infrastructure()
    {
        var names = typeof(ProductService).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name).ToList();

        Assert.Contains("Inventory.Domain", names);
        Assert.DoesNotContain("Inventory.Infrastructure", names);
        Assert.DoesNotContain(names, name => name is not null && name.Contains("EntityFramework", StringComparison.Ordinal));
    }
}
