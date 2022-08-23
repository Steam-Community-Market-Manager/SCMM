using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CommandQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCMM.Shared.API.Extensions;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models;

namespace SCMM.Steam.API.Commands
{
    public class UploadImageToBlobStorageRequest : ICommand<UploadImageToBlobStorageResponse>
    {
        public string Name { get; set; }

        public string MimeType { get; set; }

        public byte[] Data { get; set; }

        /// <summary>
        /// If null, the image never expires
        /// </summary>
        public DateTimeOffset? ExpiresOn { get; set; }

        /// <summary>
        /// If true, we'll overwrite any existing file data of the same name
        /// </summary>
        public bool Overwrite { get; set; } = true;
    }

    public class UploadImageToBlobStorageResponse
    {
        public string ImageUrl { get; set; }
    }

    public class UploadImageToBlobStorage : ICommandHandler<UploadImageToBlobStorageRequest, UploadImageToBlobStorageResponse>
    {
        private readonly string _imagesStorageConnectionString;
        private readonly string _imagesStorageUrl;

        public UploadImageToBlobStorage(IConfiguration configuration)
        {
            _imagesStorageConnectionString = (configuration.GetConnectionString("ImagesStorageConnection") ?? Environment.GetEnvironmentVariable("ImagesStorageConnection"));
            _imagesStorageUrl = configuration.GetDataStoreUrl();
        }

        public async Task<UploadImageToBlobStorageResponse> HandleAsync(UploadImageToBlobStorageRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                throw new ArgumentNullException(nameof(request.Name));
            }
            if (string.IsNullOrEmpty(request.MimeType))
            {
                throw new ArgumentNullException(nameof(request.MimeType));
            }
            if (request.Data?.Length <= 0)
            {
                throw new ArgumentException("No image content", nameof(request.Data));
            }

            var blobContainer = new BlobContainerClient(_imagesStorageConnectionString, Constants.BlobContainerImages);
            await blobContainer.CreateIfNotExistsAsync();

            // Check if the image already exists, exit early if we aren't allowed to overwrite
            var blobName = $"{request.Name}.{request.MimeType.GetFileExtension()}";
            var blob = blobContainer.GetBlobClient(blobName);
            if (blob.Exists()?.Value == true && !request.Overwrite)
            {
                return new UploadImageToBlobStorageResponse()
                {
                    ImageUrl = new Uri($"{_imagesStorageUrl}{blob.Uri.AbsolutePath}").ToString()
                };
            }

            // Upload the image
            await blob.UploadAsync(
                new BinaryData(request.Data),
                overwrite: request.Overwrite
            );

            // Set image content/mime type header
            await blob.SetHttpHeadersAsync(new BlobHttpHeaders()
            {
                ContentType = request.MimeType
            });

            // Set image expiry date (if any)
            if (request.ExpiresOn != null)
            {
                await blob.SetMetadataAsync(new Dictionary<string, string>()
                {
                    { Constants.BlobMetadataAutoDelete, bool.TrueString.ToLower() },
                    { Constants.BlobMetadataExpiresOn, request.ExpiresOn.Value.ToString() }
                });
            }

            return new UploadImageToBlobStorageResponse()
            {
                ImageUrl = new Uri($"{_imagesStorageUrl}{blob.Uri.AbsolutePath}").ToString()
            };
        }
    }
}
