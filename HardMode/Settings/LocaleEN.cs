using Colossal;

using HardMode.Domain;

using System.Collections.Generic;

namespace HardMode.Settings
{
	public class LocaleEN : IDictionarySource
	{
		private readonly HardModeSettings m_Setting;
		public LocaleEN(HardModeSettings setting)
		{
			m_Setting = setting;
		}

		public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
		{
			return new Dictionary<string, string>
			{
				{ m_Setting.GetSettingsLocaleID(), "Hard Mode Continued" },

				{ m_Setting.GetOptionGroupLocaleID(HardModeSettings.GAMEPLAY_GROUP), "Gameplay Options" },

				{ m_Setting.GetOptionLabelLocaleID(nameof(HardModeSettings.EconomyDifficulty)), "Economy Difficulty" },
				{ m_Setting.GetOptionDescLocaleID(nameof(HardModeSettings.EconomyDifficulty)), "Changes income, expenses, milestone rewards, subsidies, and building demolition costs.\nEasy: highest income bonus, 85% expenses, low demolition costs.\nMedium: medium income bonus, 90% expenses, medium demolition costs.\nHard: small income bonus, full expenses, high demolition costs.\nGood Luck: no regular milestone cash, no income bonus, 110% expenses, full demolition costs." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(HardModeSettings.BulldozeCostsMoney)), "Bulldozing Building Costs Money" },
				{ m_Setting.GetOptionDescLocaleID(nameof(HardModeSettings.BulldozeCostsMoney)), $"Shows demolition costs in the bulldozer tooltip and subtracts that amount immediately when a building is demolished. Costs are based on evicted residents, land value, and building size. Roads, paths, rails, pipes, cables, and other networks are not charged." },

				{ m_Setting.GetEnumValueLocaleID(EconomyDifficulty.Easy), "Easy (Gentler)" },
				{ m_Setting.GetEnumValueLocaleID(EconomyDifficulty.Medium), "Medium (Balanced)" },
				{ m_Setting.GetEnumValueLocaleID(EconomyDifficulty.Hard), "Hard (Strict)" },
				{ m_Setting.GetEnumValueLocaleID(EconomyDifficulty.GoodLuck), "Good Luck (Brutal)" },

				{ "EconomyPanel.BUDGET_SUB_ITEM[ExportedGoods]", "Exported Goods" },
				{ "EconomyPanel.BUDGET_SUB_ITEM[ImportedGoods]", "Imported Goods" },
				{ "HardMode.BULLDOZECOST_LABEL", "Demolition Cost" },
			};
		}

		public void Unload()
		{

		}
	}
}
