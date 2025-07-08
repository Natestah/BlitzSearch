// See https://aka.ms/new-console-template for more information


using Blitz;
using Discord;
using Discord.Webhook;

using var webclient = new HttpClient();
await using var s = await webclient.GetStreamAsync(ChangeLog.LatestGitHubChangeLogURL);
using var sr = new StreamReader(s);
var versions = ChangeLog.ParseChangeMarkDown(sr);
var latest = versions.FirstOrDefault();
var hook = System.Environment.GetEnvironmentVariable("DISCORD_WEBHOOK");
using var client = new DiscordWebhookClient(hook);
if (latest == null) return;
var embed = new EmbedBuilder
{
    Title = $"Blitz Search Update v{latest.Revision}",
    Description = latest.Changes,
    ThumbnailUrl = "https://blitzsearch.s3.us-east-2.amazonaws.com/blitzDownload.png",
    Footer = new EmbedFooterBuilder()
    {
        Text = "(Use update button in Blitz Search!)"
    },
};
await client.SendMessageAsync(text: "", embeds: [embed.Build()]);
