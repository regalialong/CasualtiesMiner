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

        var itemRows = LoadItemRows(options);
        var liquidRows = LoadLiquidRows(options);
        var moodleRows = LoadMoodleRows(options);
        var locales = await LoadLocalesAsync(options);

        Console.WriteLine($"Loaded {itemRows.Count} items from {options.DataPath}.");
        Console.WriteLine($"Loaded {liquidRows.Count} liquids from {options.DataPath}.");
        Console.WriteLine($"Loaded {moodleRows.Count} moodles from {options.DataPath}.");
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
            await UploadLocalesAsync(client, locales, itemRows, liquidRows, options);
        }

        if (mode is "bulk" or "all")
        {
            await UploadItemModulesAsync(client, options);
            await UploadBulkAsync(client, itemRows, liquidRows, moodleRows, options);
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
        IReadOnlyList<ItemRow> itemRows,
        IReadOnlyList<LiquidRow> liquidRows,
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

        var itemIds = itemRows.Select(r => r.ItemId).ToArray();
        var liquidItems = liquidRows.Select(r => r.LiquidId).ToArray();

        foreach (var locale in locales.Locales)
        {
            var itemsTitle = LocaleWikiGenerator.ModuleTitle(locale.Code, "items");

            var itemsStatus = await client.EditAsync(
                itemsTitle,
                LocaleWikiGenerator.BuildObjectsLocaleModule(locale, itemIds, "main"),
                $"Update {locale.Code} item strings",
                options.DryRun);
            Console.WriteLine($"  {itemsTitle}: {itemsStatus}");

            var liquidsTitle = LocaleWikiGenerator.ModuleTitle(locale.Code, "liquids");

            var liquidsStatus = await client.EditAsync(
                liquidsTitle,
                LocaleWikiGenerator.BuildObjectsLocaleModule(locale, liquidItems, "other"),
                $"Update {locale.Code} item strings",
                options.DryRun);
            Console.WriteLine($"  {liquidsTitle}: {liquidsStatus}");

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

        var bucketItemModule = await client.EditAsync(
            WikiContent.ItemBucketModuleTitle,
            WikiContent.ItemBucketModule,
            "Update ItemBucket reader",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.ItemBucketModuleTitle}: {bucketItemModule}");

        var bucketLiquidModule = await client.EditAsync(
            WikiContent.LiquidBucketModuleTitle,
            WikiContent.LiquidBucketModule,
            "Update LiquidBucket reader",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.LiquidBucketModuleTitle}: {bucketLiquidModule}");
    }

    private static async Task UploadBulkAsync(
        MediaWikiClient client,
        IReadOnlyList<ItemRow> itemRows,
        IReadOnlyList<LiquidRow> liquidRows,
        IReadOnlyList<MoodleRow> moodleRows,
        CliOptions options)
    {
        Console.WriteLine("== Uploading bulk Bucket data ==");

        Console.WriteLine("== Items ==");
        var router = await client.EditAsync(
            WikiContent.RouterItemModuleTitle, WikiContent.RouterItemModule, "Update item data router", options.DryRun);
        Console.WriteLine($"  {WikiContent.RouterItemModuleTitle}: {router}");

        var data = await client.EditAsync(
            WikiContent.ItemDataModuleTitle, WikiGenerator.BuildItemDataModule(itemRows), "Regenerate item data", options.DryRun);
        Console.WriteLine($"  {WikiContent.ItemDataModuleTitle}: {data}");

        Console.WriteLine("== Liquids ==");
        router = await client.EditAsync(
            WikiContent.RouterLiquidModuleTitle, WikiContent.RouterLiquidModule, "Update liquid data router", options.DryRun);
        Console.WriteLine($"  {WikiContent.RouterLiquidModuleTitle}: {router}");

        data = await client.EditAsync(
            WikiContent.LiquidDataModuleTitle, WikiGenerator.BuildLiquidDataModule(liquidRows), "Regenerate liquid data", options.DryRun);
        Console.WriteLine($"  {WikiContent.LiquidDataModuleTitle}: {data}");

        var itemTrigger = await client.EditAsync(
            WikiContent.TriggerItemPageTitle,
            WikiContent.TriggerItemPage,
            "Refresh Bucket item data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.TriggerItemPageTitle}: {itemTrigger}");

        var liquidTrigger = await client.EditAsync(
            WikiContent.TriggerLiquidPageTitle,
            WikiContent.TriggerLiquidPage,
            "Refresh Bucket liquid data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.TriggerLiquidPageTitle}: {liquidTrigger}");

        Console.WriteLine("== Moodles ==");
        var moodleData = await client.EditAsync(
            WikiContent.MoodleDataModuleTitle,
            WikiGenerator.BuildMoodleDataModule(moodleRows),
            "Regenerate moodle data",
            options.DryRun);
        Console.WriteLine($"  {WikiContent.MoodleDataModuleTitle}: {moodleData}");
    }

    private static IReadOnlyList<ItemRow> LoadItemRows(CliOptions options)
    {
        var items = DataJson.LoadItems(options.DataPath);

        return items
            .Where(item => !string.IsNullOrWhiteSpace(item.fullName))
            .Select(ItemRowMapper.Map)
            .OrderBy(row => row.ItemId, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<LiquidRow> LoadLiquidRows(CliOptions options)
    {
        var liquids = DataJson.LoadLiquids(options.DataPath);

        return liquids
            .Where(item => !string.IsNullOrWhiteSpace(item.localeName))
            .Select(LiquidRowMapper.Map)
            .OrderBy(row => row.LiquidId, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<MoodleRow> LoadMoodleRows(CliOptions options)
    {
        var moodles = DataJson.LoadMoodles(options.DataPath);

        return moodles
            .Where(m => !string.IsNullOrWhiteSpace(m.localeId))
            .Select(MoodleRowMapper.Map)
            .OrderBy(row => row.LocaleId, StringComparer.Ordinal)
            .ToList();
    }

    private static async Task<LocaleCatalog> LoadLocalesAsync(CliOptions options)
    {
        var catalog = await LocaleCatalog.LoadAsync(
            options.LocaleDir,
            options.LocalePath,
            options.DefaultLocale,
            options.LocaleTag);

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
              bulk       Upload locales, ItemBucket, Module:Item/data, Module:Liquid/data, Module:Moodle/data, and refresh Bucket.
              all        schemas, then bulk (locales + modules + Bucket data).

            Options:
              --api <url>              api.php endpoint (default: casualtiesunknown.miraheze.org)
              --user / --password      Bot credentials (or CU_WIKI_USER / CU_WIKI_PASSWORD env vars).
              --data <path>            Path to data.json (default: data.json).
              --locale-dir <path>      Folder with game locale JSON (only files with a "main" object).
              --locale <path>          Single locale file (e.g. ru-RU.json); if omitted, only EN is fetched remotely.
              --locale-tag <git-ref>   Git ref for orsoniks/scavgame-locale (default: v6.1; "6.1" → v6.1).
              GitHub overrides local only for the same file name (ru-RU.json).
              --default-locale <code>  Locale id = JSON stem (default: EN → EN.json).
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
        public string LocaleTag { get; private init; } = LocaleCatalog.DefaultRemoteTag;
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
                LocaleTag = Get("--locale-tag") ?? LocaleCatalog.DefaultRemoteTag,
                RequestDelay = int.TryParse(delayRaw, out var delayMs) ? TimeSpan.FromMilliseconds(delayMs) : TimeSpan.FromMilliseconds(750),
                DryRun = args.Contains("--dry-run")
            };
        }
    }
}
