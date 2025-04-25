// Program.cs for the API project
using Microsoft.AspNetCore.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Define API endpoints
app.MapPost("/api/generate-image", async (HttpContext context, IHttpClientFactory httpClientFactory, IConfiguration config) =>
    {
        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        Console.WriteLine($"Received request: {requestBody}");

        // Parse the incoming JSON
        var requestData = JsonSerializer.Deserialize<ImageGenerationRequest>(requestBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        if (string.IsNullOrWhiteSpace(requestData?.Prompt))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Prompt is required" });
            return;
        }
        
        Console.WriteLine($"Parsed request: {requestData.Prompt}");

        try
        {
            // Get the API key from configuration
            string apiKey = config["EverArt:ApiKey"] ?? throw new Exception("EverArt API key not configured");

            // Create client and request
            var client = httpClientFactory.CreateClient();
            var everartRequest = new
            {
                prompt = requestData.Prompt,
                image_count = 1,
                type = "txt2img",
                height = 1024,
                width = 1024,
                response_format = "url",
            };

            // Print the serialized request to debug
            var serializedRequest = JsonSerializer.Serialize(everartRequest);
            Console.WriteLine($"Sending to EverArt: {serializedRequest}");

            var content = new StringContent(
                JsonSerializer.Serialize(everartRequest),
                Encoding.UTF8,
                "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.everart.ai/v1/models/266497667515949056/generations");
            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

            // Or this format if the above doesn't work:
           // httpRequest.Headers.Add("X-API-Key", apiKey);

            httpRequest.Content = content;

            // Before sending the request
            Console.WriteLine($"Sending request to: {httpRequest.RequestUri}");
            Console.WriteLine($"Headers: {string.Join(", ", httpRequest.Headers.Select(h => $"{h.Key}:{string.Join(",", h.Value)}"))}");
            Console.WriteLine($"Request body: {await httpRequest.Content.ReadAsStringAsync()}");

            // Send request to EverArt
            var response = await client.SendAsync(httpRequest);
            
            // Check if the initial request was successful
            if (!response.IsSuccessStatusCode)
            {
                context.Response.StatusCode = (int)response.StatusCode;
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error response: {errorContent}");
                await context.Response.WriteAsync(errorContent);
                return;
            }

            // Parse the initial response
            var initialResponseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Initial response: {initialResponseContent}");
            
            var initialResponse = JsonSerializer.Deserialize<EverArtInitialResponse>(
                initialResponseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (initialResponse?.Generations == null || initialResponse.Generations.Count == 0)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid response from EverArt API" });
                return;
            }
            
            // Get the generation ID
            var generationId = initialResponse.Generations[0].Id;
            Console.WriteLine($"Generation ID: {generationId}");
            
            // Poll until the generation is complete
            EverArtGenerationResponse generationResponse = null;
            bool isComplete = false;
            int maxAttempts = 30; // Limit to prevent infinite polling
            int attempt = 0;
            
            while (!isComplete && attempt < maxAttempts)
            {
                attempt++;
                // Wait before polling
                await Task.Delay(2000); // 2 seconds delay between polls
                
                // Create request to check generation status
                var statusRequest = new HttpRequestMessage(
                    HttpMethod.Get, 
                    $"https://api.everart.ai/v1/generations/{generationId}");
                statusRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                
                Console.WriteLine($"Polling generation status (attempt {attempt}): {statusRequest.RequestUri}");
                
                // Send the poll request
                var statusResponse = await client.SendAsync(statusRequest);
                
                if (!statusResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error checking status: {statusResponse.StatusCode}");
                    continue;
                }
                
                var statusContent = await statusResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Status response: {statusContent}");
                
                var generationStatusResponse = JsonSerializer.Deserialize<EverArtGenerationStatusResponse>(
                    statusContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (generationStatusResponse?.Success == true && 
                    generationStatusResponse.Generation?.Status == "SUCCEEDED" && 
                    !string.IsNullOrEmpty(generationStatusResponse.Generation.ImageUrl))
                {
                    isComplete = true;
                    generationResponse = generationStatusResponse.Generation;
                    Console.WriteLine($"Generation complete. Image URL: {generationResponse.ImageUrl}");
                }
            }
            
            if (!isComplete || generationResponse == null)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = "Image generation timed out or failed" });
                return;
            }
            
            // Transform to the response format expected by the client
            var clientResponse = new
            {
                data = new[] { new { url = generationResponse.ImageUrl } }
            };
            
            // Return the final response to the client
            await context.Response.WriteAsJsonAsync(clientResponse);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
    })
.WithName("GenerateImage");

app.Run();

// Request model
class ImageGenerationRequest
{
    public string? Prompt { get; set; }
}

// EverArt initial response model
class EverArtInitialResponse
{
    public bool Success { get; set; }
    public List<EverArtGeneration> Generations { get; set; }
    public string RequestId { get; set; }
}

class EverArtGeneration
{
    public string Id { get; set; }
    public string ModelId { get; set; }
    public string Status { get; set; }
    public string ImageUrl { get; set; }
    public string Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// EverArt generation status response
class EverArtGenerationStatusResponse
{
    public bool Success { get; set; }
    public EverArtGenerationResponse Generation { get; set; }
}

class EverArtGenerationResponse
{
    public string Id { get; set; }
    
    [JsonPropertyName("model_id")]
    public string ModelId { get; set; }
    
    public string Status { get; set; }

    [JsonPropertyName("image_url")]
    public string ImageUrl { get; set; }
    
    public string Type { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}