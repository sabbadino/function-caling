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
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace FunctionCalling.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherController : ControllerBase
    {

       

        private readonly ILogger<WeatherController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IValidator<GetWeatherRequest> _getWeatherRequestValidator;
     

        public WeatherController(ILogger<WeatherController> logger,IHttpClientFactory httpClientFactory,
            IValidator<GetWeatherRequest> getWeatherRequestValidator)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _getWeatherRequestValidator = getWeatherRequestValidator;
        }
        private static readonly Random Random = new ();
        private static readonly float MinTemperature= -15f;
        private static readonly float MaxTemperature = 40f;

        [HttpPost(template: "get-weather", Name = nameof(GetWeather))]
        [SwaggerOperation(
            Description = "returns the weather given a town or region name",
            OperationId = nameof(GetWeather))]
        public async Task<Results<BadRequest<IEnumerable<ValidationErrorInfo>>, Ok<GetWeatherResponse>>> GetWeather(
            [FromBody] GetWeatherRequest getWeatherRequest)
        {
            var result = _getWeatherRequestValidator.Validate(getWeatherRequest);
            if (result.Errors.Any())
            {
                return TypedResults.BadRequest(result.Errors.Select(e =>
                    JsonSerializer.Deserialize<ValidationErrorInfo>(e.ErrorMessage) ?? new ValidationErrorInfo()));
            }

            return TypedResults.Ok(await WeatherStack(getWeatherRequest.Location));
        }
        


        private async Task<GetWeatherResponse> WeatherStack(string location )
        {
            var client = _httpClientFactory.CreateClient();
            var ret = await client.GetAsync($"http://api.weatherstack.com/current?access_key=7ea70979788db31811aef63a3a676686&query={location}&units=m");
            if (!ret.IsSuccessStatusCode)
            {
                throw new Exception($"{ret.StatusCode} + {await ret.Content.ReadAsStringAsync()}");
            }
            return JsonSerializer.Deserialize<GetWeatherResponse>(await ret.Content.ReadAsStreamAsync()) ??new();
        }

    

   

       

     
     




    }

    public class GetWeatherRequest
    {
        public string Location { get; init; } = "";
    }


    public class Current
    {
        public string observation_time { get; set; }
        public float temperature { get; set; }
        public int weather_code { get; set; }
        public List<string> weather_icons { get; set; }
        public List<string> weather_descriptions { get; set; }
        public float wind_speed { get; set; }
        public float wind_degree { get; set; }
        public string wind_dir { get; set; }
        public float pressure { get; set; }
        public float precip { get; set; }
        public float humidity { get; set; }
        public float cloudcover { get; set; }
        public float feelslike { get; set; }
        public float uv_index { get; set; }
        public float visibility { get; set; }
    }

    public class Location
    {
        public string name { get; set; }
        public string country { get; set; }
        public string region { get; set; }
        public string lat { get; set; }
        public string lon { get; set; }
        public string timezone_id { get; set; }
        public string localtime { get; set; }
        public int localtime_epoch { get; set; }
        public string utc_offset { get; set; }
    }

    public class Request
    {
        public string type { get; set; }
        public string query { get; set; }
        public string language { get; set; }
        public string unit { get; set; }
    }

    public class GetWeatherResponse
    {
        public Request request { get; set; }
        public Location location { get; set; }
        public Current current { get; set; }
    }



}