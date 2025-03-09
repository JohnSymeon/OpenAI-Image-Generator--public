using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;

namespace PG_Image_Generator
{
  ///
  //This class fully takes advantage of all OpenAI's Image Generator API's functionalities. 
  //Request image from scratch using any model, or an image variant based on the image you want
  ///
    public class OpenAiService
    {
        private const string OpenAiApiKey = "sample_api_key"; // Replace with your API key
        private const string OpenAiApiUrl = "https://api.openai.com/v1/images/generations";
        private const string OpenAiVariationsUrl = "https://api.openai.com/v1/images/variations";

        public async Task<string> GenerateImageAsync(string prompt, string size, string selectedQuality, string selectedModel, byte[] imageBytes = null)
        {
            using (HttpClient client = new HttpClient())
            {           
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAiApiKey}");
                string apiUrl = imageBytes != null ? OpenAiVariationsUrl : OpenAiApiUrl;
                HttpContent content;

                if (imageBytes != null)
                {
                    // Create multipart/form-data content for image variation
                    var multipartContent = new MultipartFormDataContent();

                    // Add image as file content
                    var imageContent = new ByteArrayContent(imageBytes);
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                    multipartContent.Add(imageContent, "image", "input.png");

                    // Add other parameters as form data
                    multipartContent.Add(new StringContent(size), "size");

                    content = multipartContent;
                }
                else
                {
                    // Create JSON content for text-to-image generation
                    var requestData = new
                    {
                        prompt = prompt,
                        n = 1,
                        size = size,
                        model = selectedModel,
                        quality = selectedQuality
                    };

                    string jsonContent = JsonConvert.SerializeObject(requestData);
                    content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                }
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    string responseJson = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JObject.Parse(responseJson);
                    string imageUrl = jsonResponse["data"]?[0]?["url"]?.ToString();

                    return imageUrl;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API Error: {error}");
                }
            }
        }


        // Method to generate the image and save it to a file
        public async Task<string> GenerateAndSaveImageAsync(string prompt, string size, string selectedQuality, string selectedModel, string savePath, byte[] image = null)
        {
            // Call GenerateImageAsync with size
            string imageUrl = await GenerateImageAsync(prompt, size, selectedQuality, selectedModel, image);

            if (!string.IsNullOrEmpty(imageUrl))
            {
                using (HttpClient client = new HttpClient())
                {
                    // Download the image as byte array
                    byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);

                    // Ensure the directory exists
                    string directory = Path.GetDirectoryName(savePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Save the image to the specified path
                    await File.WriteAllBytesAsync(savePath, imageBytes);

                    return savePath; // Return the saved file path
                }
            }

            throw new Exception("Failed to retrieve image URL.");
        }

    }

}
