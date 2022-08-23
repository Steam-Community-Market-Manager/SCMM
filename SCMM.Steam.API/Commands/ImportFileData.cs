using CommandQuery;
using Microsoft.EntityFrameworkCore;
using SCMM.Shared.Data.Store;
using SCMM.Steam.Client;
using SCMM.Steam.Data.Models.Community.Requests.Blob;
using SCMM.Steam.Data.Store;

namespace SCMM.Steam.API.Commands
{
    public class ImportFileDataRequest : ICommand<ImportFileDataResponse>
    {
        public string Url { get; set; }

        public DateTimeOffset? ExpiresOn { get; set; } = null;

        /// <summary>
        /// If true, we'll recycle existing file data with the same source url if it exists in the database already
        /// </summary>
        public bool UseExisting { get; set; } = true;

        /// <summary>
        /// If true, we'll persist to database
        /// </summary>
        public bool Persist { get; set; } = true;
    }

    public class ImportFileDataResponse
    {
        public FileData File { get; set; }
    }

    public class ImportFileData : ICommandHandler<ImportFileDataRequest, ImportFileDataResponse>
    {
        private readonly SteamDbContext _db;
        private readonly SteamCommunityWebClient _communityClient;

        public ImportFileData(SteamDbContext db, SteamCommunityWebClient communityClient)
        {
            _db = db;
            _communityClient = communityClient;
        }

        public async Task<ImportFileDataResponse> HandleAsync(ImportFileDataRequest request, CancellationToken cancellationToken)
        {
            // If we have already fetched this file source before, return the existing copy
            if (request.UseExisting)
            {
                var existingFileData = await _db.FileData.FirstOrDefaultAsync(x => x.Source == request.Url);
                if (existingFileData != null)
                {
                    return new ImportFileDataResponse
                    {
                        File = existingFileData
                    };
                }
            }

            // Fetch the file from its source
            var response = await _communityClient.GetBinary(new SteamBlobRequest(request.Url));
            if (response == null)
            {
                return null;
            }

            var fileData = new FileData()
            {
                Source = request.Url,
                Name = response.Name,
                MimeType = response.MimeType,
                Data = response.Data,
                ExpiresOn = request.ExpiresOn
            };

            // Save the new file data to the database
            if (request.Persist)
            {
                _db.FileData.Add(fileData);
            }

            return new ImportFileDataResponse
            {
                File = fileData
            };
        }
    }
}
