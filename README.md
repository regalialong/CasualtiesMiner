# CasualtiesMiner

A suite of Data Mining tool for [Casualties: Unknown](https://store.steampowered.com/app/4576490/Casualties_Unknown/).

Used for the Wiki project where automation of data dumping is needed

## CasualtiesMiner.Dumper.Cli

The data dumper, analyze the Assembly-CSharp's IL code to give us the game's data, the current limitation is Delegate (as seen in OnUse, LimbUse etc etc), I don't wanna write a complex parser, so I just dump C# code lmao

### Usage

Windows
```
.\CasualtiesMiner.Dumper.Cli.exe path\to\Assembly-CSharp.dll
```

macOS / Linux
```
./CasualtiesMiner.Dumper.Cli path/to/Assembly-CSharp.dll
```

## CasualtiesMiner.Uploader

Uploads the dumped item data to a [Bucket](https://meta.weirdgloop.org/w/Extension:Bucket)-enabled
MediaWiki (the wiki at `casualtiesunknown.miraheze.org` already has Bucket + Scribunto installed).

### Data model on the wiki

A reference ("справочник") table plus per-category and per-subtype detail tables:

- `Bucket:Item` — index of every item with `item_id`, a link to `Item:<id>`, `category`, and stats.
- `Bucket:Item_<category>` — one bucket per game category (`medical`, `drug`, `food`, `water`,
  `tool`, `utility`, `container`, `trash`, `custom`, `unobtainable`) holding the full item fields.
- `Bucket:Item_liquid` / `Bucket:Item_battery` — extra fields for liquid containers and batteries.

`Module:ItemData` routes language-neutral rows into Bucket. **Localized names and descriptions are not
stored in Bucket** — they live in `Module:Locale/<LANG>/items` and are resolved at render time.

### Localization (i18n)

Game locale files (`Assets/Lang/EN.json`, community translations, etc.) are uploaded as Scribunto modules:

- `Module:Locale` — resolves the active language and looks up strings.
- `Module:Locale/EN/items`, `Module:Locale/RU/items`, … — `{ bandage = { name = "...", description = "..." } }`.
- `Module:Locale/EN/ui`, … — infobox labels and category names.

Item pages can use any title (e.g. `Pump-action shotgun`). The infobox is driven by the item id
(`{{Item|shotgun}}`), not the page name. Field labels and descriptions follow the wiki content
language, or an explicit `|lang=RU` template parameter.

Upload all languages from a directory:

```
CasualtiesMiner.Uploader locales --locale-dir "E:\AssetRipper\...\Assets\Lang"
```

Or a single file: `--locale EN.json`. Fallback language: `--default-locale EN`.

### Upload modes

Bucket can only be written through `bucket.put` calls executed while a page is parsed, so the uploader
is a bot that edits pages which trigger those calls.

- **`schemas`** — upload `Bucket:*` table definitions (run once or after schema changes).
- **`locales`** — upload `Module:Locale` and per-language item/UI modules.
- **`bulk`** — upload locales, `Template:Item`, `Module:ItemBucket`, `Module:Item/data`, and refresh Bucket via the trigger page.
- **`all`** — `schemas`, then `bulk`.

The uploader does **not** create item article pages. Add `{{Item|item_id}}` to pages yourself.

### Item pages on the wiki

After `bulk` has populated Bucket, any page can render the full infobox with one line:

```wikitext
{{Item|shotgun}}
```

Stats come from Bucket; name and description from `Module:Locale`. Your existing `Template:Item Infobox` styling is unchanged.

### Usage

```
# Preview without editing (no login needed):
CasualtiesMiner.Uploader bulk --dry-run --data data.json --locale-dir path/to/Lang

# Live run (bot credentials via Special:BotPasswords):
CasualtiesMiner.Uploader all --user "Bot@uploader" --password "<botpassword>" \
    --data data.json --locale-dir path/to/Lang
```

Locale JSON files come from the game (`<game>/CU_Data/Lang/`) or the
[community locale repository](https://github.com/Orsoniks/scavgame-locale).
Credentials may also be provided via the `CU_WIKI_USER` / `CU_WIKI_PASSWORD` environment variables.

### Notes / prerequisites

- Create a bot account at `Special:BotPasswords` with the *Edit existing/new pages* grants.
- The `bulk` trigger page defaults to `Project:Items data`; override with `--trigger-page` if your wiki
  prefers another namespace.
- The dumper currently emits a placeholder for `tags` (the `//TEMP` line in `Dumper.cs`), so the `tags`
  column will be empty until that field stores the raw tag string; the uploader handles this gracefully.
