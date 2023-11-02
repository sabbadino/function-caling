using Swashbuckle.AspNetCore.Annotations;

namespace FunctionCalling.Controllers.Dtos;

[SwaggerSchema(Required = new[] { "email" })]
public class GetQuotesByUserRequest
{
    [SwaggerSchema("The email of the user. Assistant must ask the user a value for email if not provided. Assistant must not not make up one", Nullable = false)]
    public string Email { get; init; } = "";

    [SwaggerSchema("The date from to search quotes for. If not provided no lower bound date filter is set", Nullable = true)]
    public DateTime? DateFrom{ get; init; }

    [SwaggerSchema("The date to to search quotes for. If not provided no lower bound date filter is set", Nullable = true)]
    public DateTime? DateTo{ get; init; }

    [SwaggerSchema("The booking Status", Nullable = true)]
    public QuoteStatusQuery QuoteStatus { get; init; }

}

