namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Hand-authored wiki pages (Lua router, locale resolver, template, trigger page).
/// Generated pages: <c>Module:Item/data</c>, <c>Module:Locale/&lt;lang&gt;/*</c>.
/// </summary>
public static class WikiContent
{
    public const string RouterModuleTitle = "Module:ItemData";
    public const string ItemBucketModuleTitle = "Module:ItemBucket";
    public const string LocaleModuleTitle = "Module:Locale";
    public const string DataModuleTitle = "Module:Item/data";
    public const string TriggerPageTitle = "Data:Items";
    public const string ItemTemplateTitle = "Template:Item";

    /// <summary>
    /// Resolves localized item names, descriptions and UI labels at render time.
    /// </summary>
    public const string LocaleModule =
        """
        -- Module:Locale
        -- Runtime i18n: item strings live in Module:Locale/<LANG>/items and UI labels in .../ui.
        -- Language is taken from the optional |lang= parameter, otherwise the wiki content language.

        local p = {}
        local DEFAULT = "EN"

        local function normalizeLang(code)
            if not code or code == "" then return DEFAULT end
            return mw.language.new(code):getCode():upper()
        end

        local function tryLoadData(moduleTitle)
            local ok, data = pcall(mw.loadData, moduleTitle)
            if ok and type(data) == "table" then return data end
            return nil
        end

        local function loadTable(lang, suffix)
            lang = normalizeLang(lang)
            return tryLoadData("Module:Locale/" .. lang .. "/" .. suffix)
                or tryLoadData("Module:Locale/" .. DEFAULT .. "/" .. suffix)
                or {}
        end

        function p.resolveLang(frame)
            local args = frame:getParent().args
            if args.lang and tostring(args.lang) ~= "" then
                return normalizeLang(args.lang)
            end
            return normalizeLang(mw.language.getContentLanguage():getCode())
        end

        function p.getItem(itemId, lang)
            if not itemId or itemId == "" then return nil end
            return loadTable(lang, "items")[itemId]
        end

        function p.ui(key, lang)
            local ui = loadTable(lang, "ui")
            return ui[key] or key
        end

        function p.categoryName(category, lang)
            return p.ui("cat_" .. (category or "custom"), lang)
        end

        function p.itemName(frame)
            local itemId = frame.args[1] or frame.args.item_id
            local lang = p.resolveLang(frame)
            local item = p.getItem(itemId, lang)
            return item and item.name or itemId
        end

        function p.itemDescription(frame)
            local itemId = frame.args[1] or frame.args.item_id
            local lang = p.resolveLang(frame)
            local item = p.getItem(itemId, lang)
            return item and item.description or ""
        end

        function p.uiLabel(frame)
            local key = frame.args[1] or frame.args.key
            return p.ui(key, p.resolveLang(frame))
        end

        return p
        """;

    /// <summary>
    /// Reads a row from Bucket + locale strings, formats fields (weight, decay, rec, wear slots),
    /// and expands <c>Template:Item Infobox</c>. Keep in sync with the live wiki module.
    /// </summary>
    public const string ItemBucketModule =
        """
        -- Module:ItemBucket
        -- Usage on a page: {{Item|shotgun}}  or  {{#invoke:ItemBucket|infobox|shotgun}}

        local Locale = require("Module:Locale")
        local getArgs = require("Module:Arguments").getArgs
        local bit32 = require( 'bit32' )
        local yesNo = require("Module:Yesno")

        local p = {}
        local lang = mw.getContentLanguage()

        local DETAIL_COLUMNS = {
            "item_id", "page", "weight", "value", "tags", "qualities", "slot_rotation",
            "usable", "usable_on_limb", "usable_with_lmb", "auto_attack", "only_hold_in_hands",
            "combineable", "destroy_at_zero_condition", "scale_weight_with_condition",
            "ignore_depression", "decay_minutes", "decay_info", "rec",
            "wearable", "wearable_can_be_held", "wear_slot_id", "desired_wear_limb",
            "wearable_armor", "wearable_isolation", "wear_hit_dur_loss_mult",
            "jump_height_mult_change", "wearable_visual_offset",
        }

        local function merge(into, from)
            if not from then return end
            for k, v in pairs(from) do into[k] = v end
        end

        local function firstRow(result)
            return result and result[1] or nil
        end

        function p.fetch(itemId)
            local index = firstRow(bucket("item")
                .select("item_id", "category", "subtype", "weight", "value", "tags", "usable", "wearable", "combineable", "obtainable")
                .where("item_id", itemId)
                .run())

            if not index then return nil end

            local row = {}
            merge(row, index)

            local category = row.category or "custom"
            local detail = firstRow(bucket("item_" .. category)
                .select(unpack(DETAIL_COLUMNS))
                .where("item_id", itemId)
                .run())
            merge(row, detail)

            if row.subtype == "liquid" then
                merge(row, firstRow(bucket("item_liquid")
                    .select("capacity", "auto_fill", "default_contents")
                    .where("item_id", itemId)
                    .run()))
            elseif row.subtype == "battery" then
                merge(row, firstRow(bucket("item_battery")
                    .select("max_charge")
                    .where("item_id", itemId)
                    .run()))
            end

            return row
        end

        local function paramValue(v)
            if v == nil then return "" end
            if type(v) == "boolean" then
                return yesNo(v) and "true" or "false"
            end
            if type(v) == "table" then
                return table.concat(v, ", ")
            end
            return tostring(v)
        end

        local function escapeTemplate(v)
            return paramValue(v):gsub("|", "{{!}}")
        end

        local function round(num)
            return math.floor(num + 0.5)
        end

        -- Source - https://stackoverflow.com/a/67917761
        -- Posted by George Williams, modified by community. See post 'Timeline' for change history
        -- Retrieved 2026-05-25, License - CC BY-SA 4.0

        local function round_to_digit(num, dp)
            --[[
            round a number to so-many decimal of places, which can be negative,
            e.g. -1 places rounds to 10's,

            examples
                173.2562 rounded to 0 dps is 173.0
                173.2562 rounded to 2 dps is 173.26
                173.2562 rounded to -1 dps is 170.0
            ]]--
            local mult = 10^(dp or 0)
            return math.floor(num * mult + 0.5)/mult
        end

        local function format_decay(minutes)
            local durationStr = lang:formatDuration(minutes * 60)
            return tostring(mw.html.create("span")
                :wikitext("<b>[[File:Icon_decay.png|16px|class=pixelated]]&nbsp;" .. durationStr .. "</b>"))
        end

        -- =================================================

        function p.infobox(frame)
            local args = getArgs(frame)
            local itemId = args.item_id or args[1] or args["1"]
            itemId = itemId and mw.text.trim(tostring(itemId)) or ""
            if itemId == "" then
                return "[[Category:Errors]]<strong>ItemBucket:</strong> missing item id."
            end

            local row = p.fetch(itemId)
            if not row then
                return "[[Category:Errors]]<strong>ItemBucket:</strong> no Bucket row for '" .. itemId .. "'."
            end

            local lang = Locale.resolveLang(frame)
            local localeItem = Locale.getItem(itemId, lang)

            local resArgs = {
                item_id = itemId,
                display_name = localeItem and localeItem.name or itemId,
                description = localeItem and localeItem.description or "",
            }
            local yesTemplate = tostring(frame:expandTemplate{ title = "yes" })

            for key, value in pairs(row) do
                if key == "decay_minutes" or key == "rot_speed" then
                    -- - decayMinutes = minutes it takes for the item to decay. can sometimes be 0 and rotSpeed be non zero - see rotSpeed.
                    -- - rotSpeed = 1.666f / decayMinutes (ie you could say it duplicates decayMinutes - in most cases).
                    -- decayMinutes can be calculated back via "(100 / rotSpeed) / 60" (+round to get rid of float err).
                    -- correlates with decayMinutes UNLESS decayMinutes is 0, then this value is used. if both are 0, then they are 0
                    -- rotSpeed can also be NEGATIVE, which means that the item doesn't decay - instead it regenerates.

                    local decayMinutes = row.decay_minutes
                    local rotSpeed = row.rot_speed

                    if decayMinutes and decayMinutes ~= 0 then
                        resArgs.decay_duration = format_decay(decayMinutes)
                    elseif rotSpeed and rotSpeed ~= 0 then
                        decayMinutes = round_to_digit((100 / rotSpeed) / 60, 1)

                        if decayMinutes >= 0 then
                            resArgs.decay_duration = format_decay(decayMinutes)
                        else
                            resArgs.regenerate_duration = format_decay(-decayMinutes)
                        end
                    end
                elseif key == "decay_info" then
                    -- decayInfo = flag
                    -- public enum DecayType : byte
                    -- {
                    --     NoDecayWithoutContainerItem = 1, - doesn't decay when doesn't have items inside
                    --     NoDecayWhenNotWorn = 2, doesn't decay when not worn
                    --     NoDecayWhenStill = 4, doesn't decay when standing still
                    --     BatteryDecay = 0x10 = uses charge instead of using hp for decay (stuff like flashlight, gravbag)
                    -- }

                    local decayInfo = value

                    if bit32.btest(decayInfo, 1) then resArgs.no_decay_when_empty_as_container = yesTemplate end
                    if bit32.btest(decayInfo, 2) then resArgs.no_decay_when_not_worn = yesTemplate end
                    if bit32.btest(decayInfo, 4) then resArgs.no_decay_when_standing_still = yesTemplate end
                    if bit32.btest(decayInfo, 8) then resArgs.battery_charge_as_decay = yesTemplate end
                elseif key == "wear_slot_id" or key == "desired_wear_limb" then
                    local wearSlotId = row.wear_slot_id
                    local desiredWearLimb = row.desired_wear_limb

                    local res = ""
                    if wearSlotId or desiredWearLimb then
                        local wearSlotIdTooltip = "Technical field: wearSlotId"
                        local desiredWearLimbTooltip = "Technical field: desiredWearLimb"

                        if wearSlotId and desiredWearLimb then
                            res = "{{Tooltip|" .. desiredWearLimb .. "|" .. desiredWearLimbTooltip .. "}} {{Tooltip|" .. wearSlotId .. "|" .. wearSlotIdTooltip .. "}}"
                        elseif wearSlotId then
                            res = "{{Tooltip|" .. wearSlotId .. "|" .. wearSlotIdTooltip .. "}}"
                        else
                            res = "{{Tooltip|" .. desiredWearLimb .. "|" .. desiredWearLimbTooltip .. "}}"
                        end
                    end

                    resArgs.wearable_onn = frame:preprocess("<code>" .. res .. "</code>")
                elseif key == "weight" then
                    local weight = row.weight
                    local scaleWeightWithCondition = yesNo(row.scale_weight_with_condition)

                    local res = ""
                    if weight then
                        res = "{{ui icon|weight|" .. weight .. "}}"

                        if scaleWeightWithCondition then
                            res = res .. " " .. '<br><span style="color: gray; filter: contrast(25%);">{{ui icon|weight|0.1}} <sub style="vertical-align: middle;">(minimal)</sub></span>'
                        end
                    end
                    resArgs.weight = frame:preprocess(res)
                elseif key == "rec" then
                    local rec = value

                    if rec == 0 then
                        resArgs.rec_min = "<span style='color: #fc007c;'>None required</span>"
                    else
                        local container = mw.html.create("div")
                            :addClass("rec-grid")

                        for i = 1, 20 do
                            local cell = container:tag("div")

                            if i <= rec then
                                cell:addClass("f")
                            end
                        end

                        local wrapper = mw.html.create("div")
                            :wikitext("<span style='color: #fc007c;'>'''INT "..rec.." required'''</span>\n")
                            :node(container)

                        resArgs.rec_min = tostring(wrapper)
                    end
                else
                    resArgs[key] = paramValue(value)
                end
            end

            return frame:expandTemplate{ title = "Item Infobox", args = resArgs }
        end

        -- Renders N infoboxes. For debugging purposes.
        function p.n_infoboxes(frame)
            local n = tonumber(frame.args.n) or 1
            local from = tonumber(frame.args.from) or 1

            local parent = mw.html.create("div")
                :css("display", "flex")
                :css("flex-direction", "row")
                :css("flex-wrap", "wrap")

            local queryRes = bucket("item")
                .select("item_id")
                .run()

            mw.logObject(queryRes)

            local total = #queryRes

            local count = 0
            for _, obj in ipairs(queryRes) do
                count = count + 1
                if count >= from then
                    local itemId = obj.item_id
                    frame.args[1] = itemId
                    parent:node(p.infobox(frame))
                end

                if count == (from + n) then break end
            end

            return "Displaying " .. from .. " to " .. count .. " of " .. total, parent
        end

        return p
        """;

    public const string RouterModule =
        """
        -- Module:ItemData
        -- Routes language-neutral item rows into Bucket tables.
        -- Localized names/descriptions are resolved at render time via Module:Locale.

        local Locale = require("Module:Locale")

        local p = {}

        local NUMBER_FIELDS = {
            weight = true, value = true, slot_rotation = true, decay_minutes = true,
            decay_info = true, rec = true, wearable_armor = true, wearable_isolation = true,
            wear_hit_dur_loss_mult = true, jump_height_mult_change = true,
            capacity = true, max_charge = true,
        }

        local BOOLEAN_FIELDS = {
            obtainable = true, usable = true, usable_on_limb = true, usable_with_lmb = true,
            auto_attack = true, only_hold_in_hands = true, combineable = true,
            destroy_at_zero_condition = true, scale_weight_with_condition = true,
            ignore_depression = true, wearable = true, wearable_can_be_held = true, auto_fill = true,
        }

        local LIST_FIELDS = { tags = true, qualities = true, default_contents = true }

        local function toBoolean(v)
            if v == true then return true end
            if v == false or v == nil then return false end
            v = tostring(v):lower()
            return v == "true" or v == "1" or v == "yes"
        end

        local function toList(v)
            if type(v) == "table" then return v end
            local t = {}
            if v == nil then return t end
            for piece in tostring(v):gmatch("[^,]+") do
                local trimmed = mw.text.trim(piece)
                if trimmed ~= "" then t[#t + 1] = trimmed end
            end
            return t
        end

        local function coerce(args)
            local r = {}
            for k, v in pairs(args) do
                if NUMBER_FIELDS[k] then
                    r[k] = tonumber(v) or 0
                elseif BOOLEAN_FIELDS[k] then
                    r[k] = toBoolean(v)
                elseif LIST_FIELDS[k] then
                    r[k] = toList(v)
                elseif v ~= nil then
                    r[k] = tostring(v)
                end
            end
            if r.item_id and not r.page then
                r.page = "Item:" .. r.item_id
            end
            return r
        end

        local function putRow(r)
            bucket("item").put(r)
            bucket("item_" .. (r.category or "custom")).put(r)
            if r.subtype == "liquid" then bucket("item_liquid").put(r) end
            if r.subtype == "battery" then bucket("item_battery").put(r) end
        end

        function p.putAll(frame)
            local data = mw.loadData("Module:Item/data")
            for _, row in ipairs(data) do
                if row.item_id and not row.page then row.page = "Item:" .. row.item_id end
                putRow(row)
            end
            return string.format("Stored %d items into Bucket.", #data)
        end

        function p.fromTemplate(frame)
            local parent = frame:getParent()
            local r = coerce(parent.args)
            r.subtype = r.subtype or "base"
            if r.obtainable == nil then
                r.obtainable = (r.category ~= "unobtainable")
            end
            putRow(r)
            return p.renderInfobox(r, parent)
        end

        function p.renderInfobox(r, frame)
            local lang = Locale.resolveLang(frame)
            local item = Locale.getItem(r.item_id, lang)
            local title = item and item.name or r.item_id

            local parts = {}
            local function add(labelKey, value)
                if value ~= nil and value ~= "" then
                    local label = Locale.ui(labelKey, lang)
                    parts[#parts + 1] = string.format("|-\n! %s\n| %s", label, tostring(value))
                end
            end

            add("lbl_internal_id", r.item_id)
            add("lbl_category", Locale.categoryName(r.category, lang))
            add("lbl_weight", r.weight)
            add("lbl_value", r.value)
            if type(r.tags) == "table" and #r.tags > 0 then
                add("lbl_tags", table.concat(r.tags, ", "))
            end

            return string.format(
                '{| class="wikitable infobox" style="float:right"\n|+ %s\n%s\n|}',
                title, table.concat(parts, "\n"))
        end

        return p
        """;

    public const string TriggerPage =
        """
        This page stores all item data into [[Extension:Bucket|Bucket]] in a single batch.
        It is generated automatically; do not edit by hand.

        {{#invoke:ItemData|putAll}}
        """;

    public const string ItemTemplate =
        """
        {{#invoke:ItemBucket|infobox|1={{{1|}}}|item_id={{{item_id|{{{fullName|}}}}}}|lang={{{lang|}}}}}<noinclude>
        Renders [[Template:Item Infobox]] using stats from [[Extension:Bucket|Bucket]] and text from [[Module:Locale]].

        '''Usage:''' <code><nowiki>{{Item|shotgun}}</nowiki></code> or <code><nowiki>{{Item|item_id=shotgun}}</nowiki></code>

        Run the uploader bulk pass first so Bucket is populated.
        </noinclude>
        """;
}
