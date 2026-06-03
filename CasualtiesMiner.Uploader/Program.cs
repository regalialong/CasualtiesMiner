using CasualtiesMiner.Uploader.Data;
using CasualtiesMiner.Uploader.MediaWiki;
using CasualtiesMiner.Uploader.Wiki;

namespace CasualtiesMiner.Uploader;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintUsage();
            return 0;
        }

        var mode = args[0].ToLowerInvariant();
        var options = CliOptions.Parse(args);

        if (mode is not ("schemas" or "locales" or "bulk" or "all"))
        {
            Console.Error.WriteLine($"Unknown mode '{mode}'.");
            PrintUsage();

            return 1;
        }

        var rows = LoadRows(options);
        var locales = LoadLocales(options);
        Console.WriteLine($"Loaded {rows.Count} items from {options.DataPath}.");
        Console.WriteLine($"Loaded {locales.Locales.Count} locale(s) (default: {locales.DefaultCode}).");

        using var client = new MediaWikiClient(options.ApiUrl, options.RequestDelay);

        if (!options.DryRun)
        {
            if (string.IsNullOrEmpty(options.User) || string.IsNullOrEmpty(options.Password))
            {
                Console.Error.WriteLine(
                    "Credentials required for a live run. Pass --user/--password or set CU_WIKI_USER/CU_WIKI_PASSWORD, or use --dry-run.");
                return 1;
            }

            Console.WriteLine($"Logging in to {options.ApiUrl} as {options.User} ...");
            await client.LoginAsync(options.User!, options.Password!);
            Console.WriteLine("Login successful.");
        }
        else
        {
            Console.WriteLine("Dry run: no edits will be performed.");
        }

        if (mode is "schemas" or "all")
        {
            await UploadSchemasAsync(client, options);
        }

        if (mode is "locales" or "bulk" or "all")
        {
            await UploadLocalesAsync(client, locales, rows, options);
        }

        if (mode is "bulk" or "all")
        {
            await UploadItemModulesAsync(client, options);
            await UploadBulkAsync(client, rows, options);
        }

        Console.WriteLine("Done.");
        return 0;
    }

    private static async Task UploadSchemasAsync(MediaWikiClient client, CliOptions options)
    {
        Console.WriteLine("== Uploading Bucket schemas ==");

        foreach (var (bucket, schema) in BucketSchemas.All())
        {
            var title = "Bucket:" + bucket;
            var status = await client.EditAsync(title, schema, "Update Bucket schema", options.DryRun);
            Console.WriteLine($"  {title}: {status}");
        }
    }

    private static async Task UploadLocalesAsync(
        MediaWikiClient client,
        LocaleCatalog locales,
        IReadOnlyList<ItemRow> rows,
        CliOptions options)
    {
        Console.WriteLine("== Uploading locale modules ==");

        var router = await client.EditAsync(
            WikiContent.LocaleModuleTitle, WikiContent.LocaleModule, "Update locale resolver", options.DryRun);
        Console.WriteLine($"  {WikiContent.LocaleModuleTitle}: {router}");

        if (locales.Locales.Count == 0)
        {
            Console.WriteLine("  Warning: no locale files found; skipping Module:Locale/<lang>/*.");
            return;
        }

        var itemIds = rows.Select(r => r.ItemId).ToArray();

        foreach (var locale in locales.Locales)
        {
            var itemsTitle = LocaleWikiGenerator.ModuleTitle(locale.Code, "items");

            var itemsStatus = await client.EditAsync(
                itemsTitle,
                LocaleWikiGenerator.BuildItemsModule(locale, itemIds),
                $"Update {locale.Code} item strings",
                options.DryRun);
            Console.WriteLine($"  {itemsTitle}: {itemsStatus}");

            var uiTitle = LocaleWikiGenerator.ModuleTitle(locale.Code, "ui");

            var uiStatus = await client.EditAsync(
                uiTitle,
                LocaleWikiGenerator.BuildUiModule(locale),
                $"Update {locale.Code} UI strings",
                options.DryRun);
            Console.WriteLine($"  {uiTitle}: {uiStatus}");
        }
    }

    private static async Task UploadItemModulesAsync(MediaWikiClient client, CliOptions options)
    {
        Console.WriteLine("== Uploading item display modules ==");

        var bucketModule = await client.EditAsync(
            WikiContent.ItemBucketModuleTitle,
            WikiContent.ItemBucketModule,
            "Update ItemBucket reader",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.ItemBucketModuleTitle}: {bucketModule}");

        var itemTemplate = await client.EditAsync(
            WikiContent.ItemTemplateTitle,
            WikiContent.ItemTemplate,
            "Update Item template (Bucket-backed infobox)",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.ItemTemplateTitle}: {itemTemplate}");
    }

    private static async Task UploadBulkAsync(MediaWikiClient client, IReadOnlyList<ItemRow> rows, CliOptions options)
    {
        Console.WriteLine("== Uploading bulk Bucket data ==");

        var router = await client.EditAsync(
            WikiContent.RouterModuleTitle, WikiContent.RouterModule, "Update item data router", options.DryRun);
        Console.WriteLine($"  {WikiContent.RouterModuleTitle}: {router}");

        var data = await client.EditAsync(
            WikiContent.DataModuleTitle, WikiGenerator.BuildDataModule(rows), "Regenerate item data", options.DryRun);
        Console.WriteLine($"  {WikiContent.DataModuleTitle}: {data}");

        var trigger = await client.EditAsync(
            options.TriggerPage, WikiContent.TriggerPage, "Refresh Bucket item data", options.DryRun);
        Console.WriteLine($"  {options.TriggerPage}: {trigger}");
    }

    private static IReadOnlyList<ItemRow> LoadRows(CliOptions options)
    {
        var items = DataJson.LoadItems(options.DataPath);

        return items
            .Where(item => !string.IsNullOrWhiteSpace(item.fullName))
            .Select(ItemRowMapper.Map)
            .OrderBy(row => row.ItemId, StringComparer.Ordinal)
            .ToList();
    }

    private static LocaleCatalog LoadLocales(CliOptions options)
    {
        var catalog = LocaleCatalog.Load(options.LocaleDir, options.LocalePath, options.DefaultLocale);

        if (catalog.Locales.Count == 0)
            Console.WriteLine("Warning: no locale files found; infoboxes will fall back to item ids.");

        return catalog;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            CasualtiesMiner.Uploader - uploads dumped item data to a Bucket-enabled MediaWiki.

            Usage:
              uploader <mode> [options]

            Modes:
              schemas    Upload Bucket table definitions (Bucket:* pages).
              locales    Upload Module:Locale and Module:Locale/<lang>/* from game JSON files.
              bulk       Upload locales, ItemBucket, Module:Item/data, and refresh Bucket.
              all        schemas, then bulk (locales + modules + Bucket data).

            Options:
              --api <url>              api.php endpoint (default: casualtiesunknown.miraheze.org)
              --user / --password      Bot credentials (or CU_WIKI_USER / CU_WIKI_PASSWORD env vars).
              --data <path>            Path to data.json (default: data.json).
              --locale-dir <path>      Directory with game locale JSON files (EN.json, RU.json, ...).
              --locale <path>          Single locale file if --locale-dir is not set (default: EN.json).
              --default-locale <code>  Fallback language code (default: EN).
              --trigger-page <title>   Bulk trigger page (default: Project:Items data).
              --request-delay-ms <n> Pause after each API call (default: 750).
              --dry-run                Preview changes without logging in or editing.
            """);
    }

    private sealed class CliOptions
    {
        public string ApiUrl { get; private init; } = "https://casualtiesunknown.miraheze.org/w/api.php";
        public string? User { get; private init; }
        public string? Password { get; private init; }
        public string DataPath { get; private init; } = "data.json";
        public string? LocaleDir { get; private init; }
        public string LocalePath { get; private init; } = "EN.json";
        public string DefaultLocale { get; private init; } = LocaleCatalog.DefaultLanguageCode;
        public string TriggerPage { get; private init; } = "Project:Items data";
        public TimeSpan RequestDelay { get; private init; } = TimeSpan.FromMilliseconds(750);
        public bool DryRun { get; private init; }

        public static CliOptions Parse(string[] args)
        {
            string? Get(string name)
            {
                var index = Array.IndexOf(args, name);
                return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
            }

            var delayRaw = Get("--request-delay-ms");

            return new CliOptions
            {
                ApiUrl = Get("--api") ?? "https://casualtiesunknown.miraheze.org/w/api.php",
                User = Get("--user") ?? Environment.GetEnvironmentVariable("CU_WIKI_USER"),
                Password = Get("--password") ?? Environment.GetEnvironmentVariable("CU_WIKI_PASSWORD"),
                DataPath = Get("--data") ?? "data.json",
                LocaleDir = Get("--locale-dir"),
                LocalePath = Get("--locale") ?? "EN.json",
                DefaultLocale = Get("--default-locale") ?? LocaleCatalog.DefaultLanguageCode,
                TriggerPage = Get("--trigger-page") ?? "Project:Items data",
                RequestDelay = int.TryParse(delayRaw, out var delayMs) ? TimeSpan.FromMilliseconds(delayMs) : TimeSpan.FromMilliseconds(750),
                DryRun = args.Contains("--dry-run")
            };
        }
    }
}
