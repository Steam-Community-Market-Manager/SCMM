using AutoMapper;
using CommandQuery;
using Microsoft.Extensions.Configuration;
using SCMM.Steam.Client;
using SCMM.Steam.Shared.Community.Requests.Blob;
using SCMM.Web.Server.Data;
using SCMM.Web.Server.Data.Models.Steam;
using SCMM.Web.Server.Extensions;
using System;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Commands.FetchAndCreateImageData
{
    public class FetchAndCreateImageDataRequest : ICommand<FetchAndCreateImageDataResponse>
    {
        public string Url { get; set; }
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
        private readonly SteamConfiguration _cfg;
        private readonly SteamCommunityClient _communityClient;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IMapper _mapper;

        public FetchAndCreateImageData(ScmmDbContext db, IConfiguration cfg, SteamCommunityClient communityClient, IQueryProcessor queryProcessor, IMapper mapper)
        {
            _db = db;
            _cfg = cfg?.GetSteamConfiguration();
            _communityClient = communityClient;
            _queryProcessor = queryProcessor;
            _mapper = mapper;
        }

        public async Task<FetchAndCreateImageDataResponse> HandleAsync(FetchAndCreateImageDataRequest request)
        {
            var imageResponse = await _communityClient.GetImage(new SteamBlobRequest(request.Url));
            if (imageResponse == null)
            {
                return null;
            }

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
