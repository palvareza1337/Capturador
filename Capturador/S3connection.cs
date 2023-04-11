using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace Capturador
{


    internal class S3connection
    {
        private readonly IAmazonS3 s3Client;
        private readonly string bucketName;

        public S3connection(string accessKey, string secretAccessKey, string S3BucketName)
        {
            // The client uses the AWS Region of the default user.
            // If the Region where the buckets were created is different,
            // pass the Region to the client constructor. For example:
            // _s3Client = new AmazonS3Client(RegionEndpoint.USEast1);
            // por motivos de seguridad la accessKey y la secretAccessKey no deben estar en el codigo,
            // la forma correcta *guiño guiño* es usando variables de entorno
            s3Client = new AmazonS3Client(accessKey, secretAccessKey, Amazon.RegionEndpoint.SAEast1);
            // el nombre del bucket puede ser cambiado, pero por razones de testing y de que no necesitamos cambiarlo, lo deje hardcoded 
            bucketName = S3BucketName;

        }

        public async Task<bool> UploadDataAsync(
            string objectName,
            string[] data)
        {
            string contentData = String.Join("\n", data);
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectName,
                ContentBody = contentData,
            };

            var response = await s3Client.PutObjectAsync(request);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine($"Successfully uploaded {objectName} to {bucketName}.");
                return true;
            }
            else
            {
                Console.WriteLine($"Could not upload {objectName} to {bucketName}.");
                return false;
            }
        }

    }



}
