using FluentValidation;
using FunctionCalling.Controllers.Dtos;
using FunctionCalling.ExternalServices.Mdm;
using FunctionCalling.Validators;
using FunctionCalling.Validators.CustomValidators;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Text.Json;
using FluentValidation.Results;
using FunctionCalling.ExternalServices.Mdm.Dto;
using FunctionCalling.Repository.Quotes;

namespace FunctionCalling.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuotesController : ControllerBase
    {

       

        private readonly ILogger<QuotesController> _logger;
        private readonly IValidator<GetQuotesByUserRequest> _getQuotesByUserRequestValidator;
        private readonly IValidator<QuotationQueryRequest> _quotationQueryRequestValidator;
        private readonly IValidator<SubmitQuoteRequest> _submitQuoteRequestValidator;
        private readonly IMdm _mdm;
        private readonly IQuoteRepository _quoteRepository;

        public QuotesController(ILogger<QuotesController> logger,
            IValidator<GetQuotesByUserRequest> getQuotesByUserRequestValidator,  IValidator<QuotationQueryRequest> quotationQueryRequestValidator
            , IValidator<SubmitQuoteRequest> submitQuoteRequestValidator, IMdm mdm,IQuoteRepository quoteRepository)
        {
            _logger = logger;
            _getQuotesByUserRequestValidator = getQuotesByUserRequestValidator;
            _quotationQueryRequestValidator = quotationQueryRequestValidator;
            _submitQuoteRequestValidator = submitQuoteRequestValidator;
            _mdm = mdm;
            _quoteRepository = quoteRepository;
        }
        private readonly Random _random = new Random();

        [HttpPost(template: "submitted-quotes", Name = nameof(GetQuotesByUser))]
        [SwaggerOperation(
            Description = "returns the list of quotes submitted by the user",
            OperationId = nameof(GetQuotesByUser))]
        public Results<BadRequest<IEnumerable<ValidationErrorInfo>>, Ok<List<SubmittedQuote>>> GetQuotesByUser(
            GetQuotesByUserRequest getQuotesByUserRequest)
        {
            var result = _getQuotesByUserRequestValidator.Validate(getQuotesByUserRequest);
            if (result.Errors.Any())
            {
                return TypedResults.BadRequest(result.Errors.Select(e =>
                    JsonSerializer.Deserialize<ValidationErrorInfo>(e.ErrorMessage) ?? new ValidationErrorInfo()));
            }
            return TypedResults.Ok(_quoteRepository.GetUserQuotes(getQuotesByUserRequest.Email, getQuotesByUserRequest.QuoteStatus== QuoteStatusQuery.All ? null : Enum.Parse<QuoteStatus>(getQuotesByUserRequest.QuoteStatus.ToString())));
        }


    

    [HttpPost(template: "available-quotes", Name = nameof(QuotationQueryRequest))]
        [SwaggerOperation(
            Description = "returns a list of available quotes according to the input values provided by the user",
            OperationId = nameof(QuotationQueryRequest))]
        public async Task<Results<BadRequest<IEnumerable<ValidationErrorInfo>>, Ok<List<AvailableQuote>>>> AvailableQuotes(QuotationQueryRequest quotationQueryRequest)
        {
            var result = await _quotationQueryRequestValidator.ValidateAsync(quotationQueryRequest);
            if (result.Errors.SingleOrDefault(e => e.PropertyName == nameof(QuotationQueryRequest.Destination)) == null)
            {
                result.Errors.AddRange((ValidatePort(nameof(QuotationQueryRequest.Destination), quotationQueryRequest.Destination)).Errors);
            }
            if (result.Errors.SingleOrDefault(e => e.PropertyName == nameof(QuotationQueryRequest.Origin)) == null)
            {
                result.Errors.AddRange((ValidatePort(nameof(QuotationQueryRequest.Origin), quotationQueryRequest.Origin)).Errors);
            }

            if (result.Errors.Any())
            {
                return TypedResults.BadRequest(result.Errors.Select(e => JsonSerializer.Deserialize<ValidationErrorInfo>(e.ErrorMessage) ?? new ValidationErrorInfo()));
            }

            var quotes = new List<AvailableQuote>();
            Enumerable.Range(0, _random.Next(2, 5)).ToList().ForEach(i =>
            {
                quotes.Add(new AvailableQuote
                {
                    Amount = RandomNumber(500, 1456),
                    Currency = "USD",
                    ShippingWindowsFrom = DateTime.Now.AddDays(5),
                    ShippingWindowsTo = DateTime.Now.AddDays(35),
                    TransitDays = RandomNumber(13, 21),
                    // checked by validator
                    ContainerType = quotationQueryRequest.ContainerType.Value, // validator will check this
                    Origin = TryMatchPort(quotationQueryRequest.Origin).Single().PortVersion.Name,
                    Destination= TryMatchPort(quotationQueryRequest.Destination).Single().PortVersion.Name,
                });
            });
            return TypedResults.Ok(quotes); 
        }

        private int RandomNumber(int from, int to)
        {
            return _random.Next(from, to);

        }

       

        [HttpPost(template: "submit-quote", Name = nameof(SubmitQuoteRequest))]
        [SwaggerOperation(
            Description = "call this function to let the user submit a quote",
            OperationId = nameof(SubmitQuoteRequest))]
        public Results<BadRequest<IEnumerable<ValidationErrorInfo>>, Ok<SubmittedQuote>> SubmitQuote(SubmitQuoteRequest submitQuoteRequest)
        {
            var result = _submitQuoteRequestValidator.Validate(submitQuoteRequest);
            if (result.Errors.Any())
            {
                return TypedResults.BadRequest(result.Errors.Select(e => JsonSerializer.Deserialize<ValidationErrorInfo>(e.ErrorMessage) ?? new ValidationErrorInfo()));
            }

           

            var quote = new SubmittedQuote
            {
                QuoteNumber = $"Q{RandomString(5)}",
                Id = _random.Next(0, int.MaxValue),
                Status = QuoteStatus.Submitted,
                CreationDate = DateTimeOffset.Now,
                Email = submitQuoteRequest.Email,
                ExpirationDate = DateTimeOffset.Now.AddDays(_random.Next(10, 50)),
                Amount = submitQuoteRequest.Amount,
                ContainerType = submitQuoteRequest.ContainerType,
                Currency = submitQuoteRequest.Currency,
                ShippingWindowsFrom = submitQuoteRequest.ShippingWindowsFrom,   
                ShippingWindowsTo = submitQuoteRequest.ShippingWindowsTo,   
                TransitDays=submitQuoteRequest.TransitDays ,
                Origin = submitQuoteRequest.Origin,
                Destination = submitQuoteRequest.Destination,


            };
            _quoteRepository.Add(quote);
            return TypedResults.Ok(quote);
        }

        private ValidationResult ValidatePort(string propertyName,string propertyValue)
        {
            var ret = new ValidationResult();
            if (!string.IsNullOrEmpty(propertyValue))
            {
                var candidatePorts = TryMatchPort(propertyValue);

                if (candidatePorts.Count == 0)
                {

                    ret.Errors.Add(new ValidationFailure(propertyName,
                        (new ValidationErrorInfo
                        {
                            ErrorCode = $"invalid value for {propertyName}",
                            AssistantAction =
                                $"reply to the user with these exact words: '{propertyValue} is an invalid value for {propertyName}'"
                        }.ToJson())));
                }
                else if (candidatePorts.Count > 1)
                {
                    ret.Errors.Add(new ValidationFailure(propertyName,
                        (new ValidationErrorInfo
                        {
                            ErrorCode = $"ambiguous value for {propertyName}",
                            AssistantAction =
                                $"reply to the user with these exact words: \"choose among the following values for {propertyName} : {string.Join(',',candidatePorts.Take(5).Select(FormatPort))}\""
                        }.ToJson())));
                }
            }


            return ret;
        }

        private IReadOnlyCollection<PortDetails> TryMatchPort(string propertyValue)
        {
            var candidatePorts = (_mdm.GetPortByUnCodeOrName(propertyValue));
            var searchMatchByUnCode = candidatePorts
                .Where(l => l.LongDisplayName.Contains($"[{propertyValue}]", StringComparison.OrdinalIgnoreCase)).ToList();
            if (searchMatchByUnCode.Count > 0)
            {
                candidatePorts = searchMatchByUnCode;
            }

            return candidatePorts;
        }

        private string FormatPort(PortDetails port)
        {
            return $"{port.LongDisplayName} (port)";
        }
        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }
       

        
    }

   
}