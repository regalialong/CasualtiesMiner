namespace CasualtiesMiner.Uploader.Wiki;

/// <summary>
/// Wiki-only labels for game expression paths (<c>body.*</c>) shown in moodle cause columns.
/// </summary>
public static class WikiUiLabels
{
    public static readonly IReadOnlyDictionary<string, string> BodyFields =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["body.curAdrenaline"] = "Adrenaline",
            ["body.badSleepAmount"] = "Bad sleep",
            ["body.totalBleedSpeed"] = "Bleeding",
            ["body.heartRate"] = "Heart rate",
            ["body.brainHealth"] = "Brain damage",
            ["body.clawHealth"] = "Claw damage",
            ["body.temperature"] = "Temperature",
            ["body.consciousness"] = "Consciousness",
            ["body.hearingLoss"] = "Hearing loss",
            ["body.dirtyness"] = "Dirtiness",
            ["body.overdoseIndex"] = "Drug overdose",
            ["body.overEncumberance"] = "Encumbrance",
            ["body.caffeinated"] = "Caffeine",
            ["body.stamina"] = "Stamina",
            ["body.fibrillationProgress"] = "Fibrillation",
            ["body.focusedLevel"] = "Focus",
            ["body.hemothorax"] = "Hemothorax",
            ["body.immunity"] = "Immunity",
            ["body.hunger"] = "Hunger",
            ["body.bloodPressure"] = "Blood pressure",
            ["body.respiratoryRate"] = "Respiratory rate",
            ["body.horrifiedLevel"] = "Horror",
            ["body.internalBleeding"] = "Internal bleeding",
            ["body.radiationSickness"] = "Radiation sickness",
            ["body.clawRegrowTime"] = "Claw regrowth",
            ["body.lastStandTime"] = "Last stand",
            ["body.thirst"] = "Hydration",
            ["body.weightOffset"] = "Weight",
            ["body.bloodOxygen"] = "Blood oxygen",
            ["body.averagePain"] = "Pain",
            ["body.brainGrowSickness"] = "Brain regrowth sickness",
            ["body.septicShock"] = "Sepsis",
            ["body.painShock"] = "Pain shock",
            ["body.sicknessAmount"] = "Sickness",
            ["body.strokeAmount"] = "Stroke",
            ["body.energy"] = "Energy",
            ["body.traumaAmount"] = "Trauma",
            ["body.venomCurrent"] = "Venom",
            ["body.wetness"] = "Wetness",
            ["body.harmer.timeWasStill"] = "Time stood still",
            ["body.limbs[0].boneHealTimer"] = "Bone heal (neck)",
            ["body.limbs[1].boneHealTimer"] = "Bone heal (ribs)",
            ["body.limbs[0].dislocationTimer"] = "Dislocation (jaw)",
            ["body.limbs[1].dislocationTimer"] = "Dislocation (spine)",
            ["body.limbs.max.infectionAmount"] = "Max limb infection",
            ["body.limbs.max.boneHealTimer"] = "Max bone heal time",
            ["body.limbs.max.dislocationTimer"] = "Max dislocation time",
            ["body.limbs.any.dismembered"] = "Any limb dismembered",
            ["body.limbs.any.showInfection"] = "Any limb infection visible",
            ["body.mindWipe"] = "Mind wipe",
            ["body.mindWipe.active"] = "Mind wipe active",
        };

    public static readonly IReadOnlyDictionary<string, string> Misc =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["is_chipped"] = "Requires chip: ",
            ["caused_by"] = "Caused by ",
            ["critical"] = "Critical",
            ["intensity_label"] = "Intensity",
            ["recipe.any_item_with"] = "Any item with ",
            ["recipe.any_liquid_with"] = "Any liquid with",
            ["recipe.any_item"] = "Any item",
            ["recipe.ingredients"] = "Ingredients",
            ["recipe.count"] = "(x%d)",
            ["recipe.countLiquid"] = "(%dmL)",
            ["recipe.condition_at_least"] = "At least %d%% condition",
            ["recipe.liquid_condition_at_least"] = "At least %s mL",
            ["recipe.liquid_quality"] = "Total (%s) %s quality",
            ["recipe.quality"] = "(%s) %s quality",
            ["recipe.info"] = "Info",
            ["recipe.condition"] = "Condition: %d",
            ["recipe.amount"] = "Amount: %d",
            ["recipe.intRequired"] = "INT needed: %d",
        };
}
