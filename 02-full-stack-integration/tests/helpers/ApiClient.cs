using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EcommerceTests.Helpers
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;

        public ApiClient(string baseUrl, string apiKey = null)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(30),
            };
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<PromotionResponse> CreatePromotionAsync(object promotionData)
        {
            var json = JsonSerializer.Serialize(promotionData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("admin/promotions", content);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PromotionResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<PromotionResponse> GetPromotionAsync(string promotionId)
        {
            var response = await _httpClient.GetAsync($"admin/promotions/{promotionId}");
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PromotionResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task DeletePromotionAsync(string promotionId)
        {
            var response = await _httpClient.DeleteAsync($"admin/promotions/{promotionId}");
            if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                response.EnsureSuccessStatusCode();
        }
    }

    public class PromotionResponse
    {
        public string PromotionId { get; set; }
        public string Code { get; set; }
        public string DiscountType { get; set; }
        public int DiscountValue { get; set; }
        public string Category { get; set; }
        
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}