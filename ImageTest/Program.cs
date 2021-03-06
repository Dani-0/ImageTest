﻿using ImageTest.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace CSHttpClientSample
{
    static class Program
    {
        // Add your Computer Vision subscription key and endpoint to your environment variables.
        static string subscriptionKey = "";

        static string endpoint = "";

        // the Batch Read method endpoint
        static string uriBase = endpoint + "/vision/v3.1/read/analyze";

        // Add a local image with text here (png or jpg is OK)
        static string imageFilePath = @"C:\\Users\\DanielleC\\Desktop\\Test\\tweets_final.pdf";

        // Create a list to store the tweets 
        //static Dictionary<int, string> _tweet_dict = new Dictionary<int, string>();
        static List<string> _tweets;

        static async Task Main(string[] args)  
        {


            // Call the REST API method.
            Console.WriteLine("\nExtracting text...\n");
            await ReadText(imageFilePath);  //.Wait();  

            //Console.ReadLine();

            while (true)
            {
                // Write search term
                Console.WriteLine("Enter search term or press (Q) to quit:");


                // Create Stopwatch
                var sw = new Stopwatch();

                // Create sting variable to hold user input
                string term = Console.ReadLine();


                if (Equals(term, "Q"))
                {
                    Environment.Exit(0);
                }

                // Start stopwatch
                sw.Start();
               

                term = term.ToLower();
                IEnumerable<string> terms = term.Split("-");

                List<string> results = new List<string>();
                foreach (var t in terms)
                {
                    results.AddRange(_tweets.Where(a => a.ToLower().Contains(t)));
                }

                // Ensure duplicate matches are taken care of (e.g. "barack-obama" -> "barack", "obama" matches doubly )
                results = results.Distinct().ToList();

                sw.Stop();


                 Console.WriteLine("Found " + results.Count() + " tweets");
                 Console.WriteLine("----------------------------------");
                foreach (var result in results)
                {
                    Console.WriteLine(result);
                }
                

                Console.WriteLine("\n" + "Search took " + sw.ElapsedMilliseconds + " milliseconds to find " + results.Count() + " tweets");
                
                Console.WriteLine("Search took " + (sw.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000)) + " microseconds to find " + results.Count() + " tweets\n");

            }


        }

        /// <summary>
        /// Gets the text from the specified image file by using
        /// the Computer Vision REST API.
        /// </summary>
        /// <param name="imageFilePath">The image file with text.</param>
        static async Task ReadText(string imageFilePath)
        {
            try
            {
                HttpClient client = new HttpClient();

                // Request headers.
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", subscriptionKey);

                string url = uriBase;

                HttpResponseMessage response;

                // Two REST API methods are required to extract text.
                // One method to submit the image for processing, the other method
                // to retrieve the text found in the image.

                // operationLocation stores the URI of the second REST API method,
                // returned by the first REST API method.
                string operationLocation;

                // Reads the contents of the specified local image
                // into a byte array.
                byte[] byteData = GetImageAsByteArray(imageFilePath);

                // Adds the byte array as an octet stream to the request body.
                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    // This example uses the "application/octet-stream" content type.
                    // The other content types you can use are "application/json"
                    // and "multipart/form-data".
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    // The first REST API method, Batch Read, starts
                    // the async process to analyze the written text in the image.
                    response = await client.PostAsync(url, content);
                }

                // The response header for the Batch Read method contains the URI
                // of the second method, Read Operation Result, which
                // returns the results of the process in the response body.
                // The Batch Read operation does not return anything in the response body.
                if (response.IsSuccessStatusCode)
                    operationLocation =
                        response.Headers.GetValues("Operation-Location").FirstOrDefault();
                else
                {
                    // Display the JSON error data.
                    string errorString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("\n\nResponse:\n{0}\n",
                        JToken.Parse(errorString).ToString());
                    return;
                }

                // If the first REST API method completes successfully, the second 
                // REST API method retrieves the text written in the image.
                //
                // Note: The response may not be immediately available. Text
                // recognition is an asynchronous operation that can take a variable
                // amount of time depending on the length of the text.
                // You may need to wait or retry this operation.
                //
                // This example checks once per second for ten seconds.
                string contentString;   
                int i = 0;
                do
                {
                    System.Threading.Thread.Sleep(1000);
                    response = await client.GetAsync(operationLocation);
                    contentString = await response.Content.ReadAsStringAsync();
                    ++i;
                }
                while (i < 60 && contentString.IndexOf("\"status\":\"succeeded\"") == -1);

                if (i == 60 && contentString.IndexOf("\"status\":\"succeeded\"") == -1)
                {
                    Console.WriteLine("\nTimeout error.\n");
                    return;
                }


                // Deserialise JSON - compose into new C# classes and flatten into one list
                _tweets = JsonConvert.DeserializeObject<Root>(contentString).analyzeResult.readResults.
                    SelectMany(a => a.lines).Select(z => z.text).ToList();

            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message);
            }
        }

        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            // Open a read-only file stream for the specified file.
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                // Read the file's contents into a byte array.
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }
    }
}