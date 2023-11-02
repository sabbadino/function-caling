using FluentValidation;
using FunctionCalling.Controllers.Dtos;
using FunctionCalling.Validators.CustomValidators;

namespace FunctionCalling.Validators;

public class SubmitQuoteRequestValidator : AbstractValidator<SubmitQuoteRequest>
{
    public SubmitQuoteRequestValidator()
    {
    }
}

public class QuotationQueryRequestValidator : AbstractValidator<QuotationQueryRequest>
{
    private const int _minLengthForPortName = 3;
    public QuotationQueryRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage(d => new ValidationErrorInfo { ErrorCode = "invalid email", AssistantAction = "ask the user to provide a valid email address" }.ToJson());
        RuleFor(x => x.Email).EmailAddress().WithMessage(d => new ValidationErrorInfo { ErrorCode = "invalid email", AssistantAction = "ask the user to provide a valid email address" }.ToJson());
        RuleFor(x => x.Email).IsNotAChatGptGeneratedEmail();

        RuleFor(x => x.CommodityGroup).NotEmpty()
            .WithMessage(d => new ValidationErrorInfo
            {
                ErrorCode = "{PropertyName} cannot be null or empty",
                AssistantAction = "ask the user to provide a value for {PropertyName}"
            }.ToJson());

        RuleFor(x => x.Destination).Length(_minLengthForPortName,100).
            WithMessage(d => new ValidationErrorInfo{ ErrorCode = "{PropertyName} value is invalid",
                AssistantAction = "reply to the user with these exact words: '{PropertyName} must have a value of at least " + _minLengthForPortName + " characters"
            }.ToJson());
        
        RuleFor(x => x.Origin).Length(_minLengthForPortName, 100).
            WithMessage(d => new ValidationErrorInfo { ErrorCode = "{PropertyName} value is invalid",
                AssistantAction = "reply to the user with these exact words: '{PropertyName} must have a value of at least " + _minLengthForPortName + " characters"
            }.ToJson());

        RuleFor(x => x.ContainerType).NotNull()
            .WithMessage(d => new ValidationErrorInfo
            {
                ErrorCode = "{PropertyName} must be cannot be null or empty",
                AssistantAction = "ask the user to provide a valid value for {PropertyName}"
            }.ToJson());


        RuleFor(x => x.Weight).NotNull()
            .WithMessage(d => new ValidationErrorInfo
            {
                ErrorCode = "{PropertyName} cannot be null or empty",
                AssistantAction = "ask the user to provide a value for {PropertyName}"
            }.ToJson());

        RuleFor(x => x.Weight).GreaterThan(0)
            .WithMessage(d => new ValidationErrorInfo
            {
                ErrorCode = "{PropertyName} must be greater than 0",
                AssistantAction = "ask the user to provide a valid value for {PropertyName}"
            }.ToJson());

    }
}