using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CurrencyConverterApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurrencyConversionController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CurrencyConversionController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("convert")]
        public async Task<IActionResult> ConvertCurrency([FromQuery] string? fromCurrency, [FromQuery] string toCurrency, [FromQuery] decimal amount, [FromQuery] string? date = null)
        {
            if (string.IsNullOrEmpty(toCurrency) || amount <= 0)
            {
                return BadRequest("Invalid input parameters.");
            }

            try
            {
                string baseUrl = "https://data.fixer.io/api/";
                string baseCurrency = fromCurrency ?? "EUR"; // Default to EUR if fromCurrency is not provided
                
                string apiUrl = string.IsNullOrEmpty(date) 
                    ? $"{baseUrl}latest?access_key=5004e0d8c64029fed873e269efa91312&symbols=USD,AUD,CAD,PLN,MXN&format=1" 
                    : $"{baseUrl}{date}?access_key=5004e0d8c64029fed873e269efa91312&symbols=USD,AUD,CAD,PLN,MXN&format=1";
                
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "Failed to fetch exchange rate data.");
                }

                var exchangeRates = await response.Content.ReadFromJsonAsync<ExchangeRateResponse>();

                if (exchangeRates == null || !exchangeRates.Rates.ContainsKey(toCurrency) || (baseCurrency != null && baseCurrency != "EUR"))
                {
                    return BadRequest("Unsupported currency.");
                }

                decimal toRate = exchangeRates.Rates[toCurrency];

                // Convert the amount
                decimal convertedAmount = amount  * toRate;


                return Ok(new
                {
                    FromCurrency = baseCurrency,
                    ToCurrency = toCurrency,
                    Amount = amount,
                    ConvertedAmount = convertedAmount,
                    Date = date ?? exchangeRates.Date
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    public class ExchangeRateResponse
    {
        public string Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }
}