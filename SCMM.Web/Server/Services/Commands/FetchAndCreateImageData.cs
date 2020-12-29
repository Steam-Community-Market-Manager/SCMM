using CommandQuery;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Community.Requests.Blob;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Commands
{
    public class FetchAndCreateImageDataRequest : ICommand<FetchAndCreateImageDataResponse>
    {
        public string Url { get; set; }

        /// <summary>
        /// If true, we'll recycle existing image data the same source url exists in the database already
        /// </summary>
        public bool UseExisting { get; set; } = true;
    }

    public class FetchAndCreateImageDataResponse
    {
        public Guid Id { get; set; }

        public string MimeType { get; set; }

        public byte[] Data { get; set; }
    }

    public class FetchAndCreateImageData : ICommandHandler<FetchAndCreateImageDataRequest, FetchAndCreateImageDataResponse>
    {
        private readonly ScmmDbContext _db;
        private readonly SteamCommunityClient _communityClient;

        public FetchAndCreateImageData(ScmmDbContext db, SteamCommunityClient communityClient)
        {
            _db = db;
            _communityClient = communityClient;
        }

        public async Task<FetchAndCreateImageDataResponse> HandleAsync(FetchAndCreateImageDataRequest request)
        {
            // If we have already fetched this image source before, return the existing copy
            if (request.UseExisting)
            {
                var existingImageData = _db.ImageData.FirstOrDefault(x => x.Source == request.Url);
                if (existingImageData != null)
                {
                    return new FetchAndCreateImageDataResponse
                    {
                        Id = existingImageData.Id,
                        MimeType = existingImageData.MimeType,
                        Data = existingImageData.Data
                    };
                }
            }

            // Fetch the image from its source
            var imageResponse = await _communityClient.GetImage(new SteamBlobRequest(request.Url));
            if (imageResponse == null)
            {
                return null;
            }

            // Save the image to the database
            var imageData = new ImageData()
            {
                Source = request.Url,
                MimeType = imageResponse.Item2,
                Data = imageResponse.Item1
            };

            _db.ImageData.Add(imageData);
            _db.SaveChanges();

            return new FetchAndCreateImageDataResponse
            {
                Id = imageData.Id,
                MimeType = imageData.MimeType,
                Data = imageData.Data
            };
        }
    }
}
