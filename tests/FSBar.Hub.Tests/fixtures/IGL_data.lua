-- Addon Custom Data (synthetic fixture for FSBar.Hub.Tests; real file
-- is many hundreds of lines long, shape faithfully modelled here).
return {
	["Menu"] = {
		-- Comments around the target line must be preserved verbatim.
		indexedRepeatEvents = {},
		onetimeEvents = {
			skirmish_firstgame = true,
		},
		simpleAiList = true,
		simplifiedSkirmishSetup = true,
		steamLinkComplete = false,
	},
	["Analytics Handler"] = {
		-- No simpleAiList key here; installer must not rewrite this section.
		analyticsSent = true,
	},
}
