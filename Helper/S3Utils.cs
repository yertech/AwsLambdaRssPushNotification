using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using AWSLambdaRssNotification.Model;
using Newtonsoft.Json;

namespace AWSLambdaRssNotification.Helper
{
    public class S3Utils
    {        
        private const string bucketName = "s3-rss-push-notifs";

        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.APSoutheast1;
        private static IAmazonS3 _client;
        public S3Utils()
        {
            _client = new AmazonS3Client(bucketRegion);
        }

        public async Task<List<Item>> GetFileContent(string keyName)
        {
            List<Item> jobs = new List<Item>();
            try
            {
                GetObjectRequest request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName
                };

                using (GetObjectResponse response = await _client.GetObjectAsync(request))
                using (Stream responseStream = response.ResponseStream)
                using (StreamReader reader = new StreamReader(responseStream))
                {

                    var responseBody = reader.ReadToEnd();
                    jobs  = JsonConvert.DeserializeObject<S3Document>(responseBody).item;
                }

            }
            catch (AmazonS3Exception e)
            {
                // If bucket or object does not exist
                Console.WriteLine("Error encountered ***. Message:'{0}' when reading object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when reading object", e.Message);
            }

            return jobs;
        }

        public async Task UploadFile(List<Item> jobs, string keyName)
        {
            try
            {
                var s3Document = new S3Document {item = jobs};
                var putRequest1 = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName,
                    ContentBody = JsonConvert.SerializeObject(s3Document)
                };

                PutObjectResponse response1 = await _client.PutObjectAsync(putRequest1);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine(
                    "Error encountered ***. Message:'{0}' when writing an object"
                    , e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Unknown encountered on server. Message:'{0}' when writing an object"
                    , e.Message);
            }
        }



    }
}
