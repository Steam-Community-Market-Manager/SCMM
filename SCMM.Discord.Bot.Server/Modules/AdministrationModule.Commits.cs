using Discord;
using Discord.Commands;
using SCMM.Discord.Client.Commands;
using SCMM.Shared.Data.Models.Extensions;
using SCMM.Steam.Data.Models.Community.Requests.Blob;
using System.Web;

namespace SCMM.Discord.Bot.Server.Modules
{
    public partial class AdministrationModule
    {
        // commits.facepunch.com
        // commits.facepunch.com/387716
        [Command("decode-redacted-commits")]
        public async Task<RuntimeResult> DecodeRedactedCommitsAsync()
        {
            using var client = new HttpClient();
            var commitsPage = "commits.facepunch.com";
            var webArchiveSnapshotResponse = await client.GetAsync(
                $"https://web.archive.org/cdx/search/cdx?url={Uri.EscapeDataString(commitsPage)}"
            );

            var dictionary = new Dictionary<char, List<char>>();

            webArchiveSnapshotResponse?.EnsureSuccessStatusCode();

            var webArchiveSnapshotResponseText = await webArchiveSnapshotResponse?.Content?.ReadAsStringAsync();
            if (string.IsNullOrEmpty(webArchiveSnapshotResponseText))
            {
                return CommandResult.Fail("No web archive snapshots found");
            }

            var message = await Context.Message.ReplyAsync("Parsing snapshots...");
            var webArchiveSnapshots = webArchiveSnapshotResponseText.Split('\n');
            foreach (var webArchiveSnapshot in webArchiveSnapshots)
            {
                // "com,example)/ 20020120142510 http://example.com:80/ text/html 200 HT2DYGA5UKZCPBSFVCV3JOBXGW2G5UUA 1792"
                var snapshot = webArchiveSnapshot.Split(' ', StringSplitOptions.TrimEntries).Skip(1).FirstOrDefault();
                await message.ModifyAsync(
                    x => x.Content = $"Parsing snapshot {snapshot} ({Array.IndexOf(webArchiveSnapshots, webArchiveSnapshot) + 1}/{webArchiveSnapshots.Length})..."
                );

                try
                {
                    await ParseCommitsPageSnapshot($"http://web.archive.org/web/{snapshot}/{Uri.EscapeDataString(commitsPage)}", dictionary);
                    await Context.Message.ReplyAsync(
                        String.Join("\n", dictionary.Select(x => $"{x.Key} = {String.Join(",", x.Value.OrderBy(o => o))}").OrderBy(o => o))
                    );
                }
                catch (Exception ex)
                {
                    await Context.Message.ReplyAsync(
                        $"Error parsing snapshot {snapshot}, skipping. {ex.Message}"
                    );
                    continue;
                }
            }

            await message.ModifyAsync(
                x => x.Content = $"Parsed {webArchiveSnapshots.Length}/{webArchiveSnapshots.Length} snapshots"
            );

            return CommandResult.Success();
        }

        private async Task ParseCommitsPageSnapshot(string url, Dictionary<char, List<char>> dictionary)
        {
            var oldCommitsPageHtml = await _communityClient.GetHtml(new SteamBlobRequest(url));
            if (oldCommitsPageHtml == null)
            {
                throw new Exception($"Unable to load snapshot {url}");
            }

            var redactedCharacters = new[] { '▄', '▅', '▆', '▇', '█', '▉', '▊', '▋', '▌', '▍' };

            var oldCommits = oldCommitsPageHtml.Descendants("div").Where(x => x?.Attribute("class")?.Value?.StartsWith("commit ") == true).ToList();
            foreach (var oldCommit in oldCommits)
            {
                var commitId = oldCommit.Attribute("like-id")?.Value;
                var oldCommitMessage = oldCommit.Descendants("div").Where(x => x?.Attribute("class")?.Value == "commits-message").FirstOrDefault()?.Value;
                oldCommitMessage = HttpUtility.HtmlDecode(oldCommitMessage);
                if (!String.IsNullOrEmpty(commitId) && !String.IsNullOrEmpty(oldCommitMessage))
                {
                    var oldCommitIsPublic = true;
                    if (redactedCharacters.Any(x => oldCommitMessage.Contains(x)))
                    {
                        oldCommitIsPublic = false;
                    }

                    try
                    {
                        var newCommitPageHtml = await _communityClient.GetHtml(new SteamBlobRequest($"https://commits.facepunch.com/{commitId}"));
                        if (newCommitPageHtml == null)
                        {
                            throw new Exception($"Unable to load commit {commitId}");
                        }

                        var newCommitMessage = newCommitPageHtml.Descendants("div").Where(x => x?.Attribute("class")?.Value == "commits-message").FirstOrDefault()?.Value;
                        newCommitMessage = HttpUtility.HtmlDecode(newCommitMessage);
                        if (!String.IsNullOrEmpty(commitId) && !String.IsNullOrEmpty(oldCommitMessage))
                        {
                            var newCommitIsPublic = true;
                            if (redactedCharacters.Any(x => newCommitMessage.Contains(x)))
                            {
                                newCommitIsPublic = false;
                            }

                            if (oldCommitIsPublic && !newCommitIsPublic)
                            {
                                BuildDecodeDictionary(newCommitMessage, oldCommitMessage, dictionary);
                                /*
                                await Context.Message.ReplyAsync(
                                    $"**Found commit {commitId} was redacted.**\n\n**Old:**\n{oldCommitMessage}\n\n**New:**\n{newCommitMessage}"
                                );
                                */
                            }
                            else if (!oldCommitIsPublic && newCommitIsPublic)
                            {
                                BuildDecodeDictionary(oldCommitMessage, newCommitMessage, dictionary);
                                /*
                                await Context.Message.ReplyAsync(
                                    $"**Found commit {commitId} was unredacted.**\n\n**Old:**\n{oldCommitMessage}\n\n**New:**\n{newCommitMessage}"
                                );
                                */
                            }
                        }
                    }
                    finally
                    {
                        // can't find commit
                    }
                }
            }
        }

        private void BuildDecodeDictionary(string encoded, string decoded, Dictionary<char, List<char>> dictionary)
        {
            for (int i = 0; i < Math.Min(encoded.Length, decoded.Length); i++)
            {
                if (!dictionary.ContainsKey(encoded[i]))
                {
                    dictionary[encoded[i]] = new List<char>();
                }
                if (!dictionary[encoded[i]].Contains(decoded[i]))
                {
                    dictionary[encoded[i]].Add(decoded[i]);
                }
            }
        }
    }
}
