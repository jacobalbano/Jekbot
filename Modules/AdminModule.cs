using Discord;
using Discord.Interactions;
using Jekbot.Models;
using Jekbot.Utility;
using NodaTime;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Jekbot.Modules
{
    [RequireOwner]
    [RequireContext(ContextType.Guild)]
    [Group("admin", "Bot administration functions")]
    public class AdminModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("enable-feature", "Enable a feature")]
        public async Task EnableFeature(FeatureId feature)
        {
            await DeferAsync();
            var builder = new EmbedBuilder()
                .WithDescription($"{feature} is now enabled");

            var instance = Context.GetInstance();
            instance.SetFeatureEnabled(feature, true);
            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("disable-feature", "Disable a feature")]
        public async Task DisableFeature(FeatureId feature)
        {
            await DeferAsync();
            var builder = new EmbedBuilder()
                .WithDescription($"{feature} is now disabled");

            var instance = Context.GetInstance();
            instance.SetFeatureEnabled(feature, false);
            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("show-features", "Show feature")]
        public async Task ShowFeatureStatus()
        {
            await DeferAsync();
            var instance = Context.GetInstance();
            var sb = new StringBuilder();
            foreach (var e in Enum.GetValues<FeatureId>())
                sb.AppendLine($"{e}: **{(instance.IsFeatureEnabled(e) ? "en" : "dis")}abled**");

            await FollowupAsync(embed: new EmbedBuilder()
                .WithDescription(sb.ToString())
                .Build());
        }

        [SlashCommand("run-diagnostics", "Run various diagnostics to see how the bot can be expected to perform", runMode: RunMode.Async)]
        public async Task RunDiagnostics()
        {
            await DeferAsync();
            var builder = new EmbedBuilder();

            var proc = Process.GetCurrentProcess();
            var nativeMem = proc.PrivateMemorySize64;
            var gcMem = GC.GetTotalMemory(forceFullCollection: false);
            static string format(long l) => $"{BytesFormatter.ToSize(l, BytesFormatter.SizeUnits.MB)}mb";

            var gitHash = Assembly
                .GetEntryAssembly()?
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attr => attr.Key == "GitHash")?.Value;

            builder.AddField("Native memory usage", format(nativeMem), inline: true);
            builder.AddField("GC memory usage", format(gcMem), inline: true);
            builder.AddField("Total memory usage", format(nativeMem + gcMem));

            if (gitHash != null)
                builder.AddField("Commit", gitHash);

            builder.WithFooter($"Bot uptime: {Duration.FromTimeSpan(DateTime.UtcNow - proc.StartTime.ToUniversalTime())}");

            await FollowupAsync(embed: builder.Build());
        }

    }
}
