using Game.City;
using Game.Simulation;
using Game.Tools;

using Unity.Entities;

namespace HardMode.Systems
{
	public partial class BulldozeDemolitionSystem : SystemBase
	{
		private ToolSystem m_ToolSystem;
		private BulldozeToolSystem m_BulldozeTool;
		private CitySystem m_CitySystem;
		private BulldozeCostSystem m_BulldozeCostSystem;
		private bool m_WasApplying;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
			m_BulldozeTool = World.GetOrCreateSystemManaged<BulldozeToolSystem>();
			m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
			m_BulldozeCostSystem = World.GetOrCreateSystemManaged<BulldozeCostSystem>();
		}

		protected override void OnUpdate()
		{
			var isBulldozing = m_ToolSystem.activeTool == m_BulldozeTool;
			var isApplying = isBulldozing && m_ToolSystem.activeTool.applyMode == ApplyMode.Apply;

			if (isApplying && !m_WasApplying)
			{
				ApplyCurrentBulldozeCost();
			}

			m_WasApplying = isApplying;
		}

		private void ApplyCurrentBulldozeCost()
		{
			var cost = m_BulldozeCostSystem.TotalCost;
			if (cost <= 0)
			{
				Mod.Log.Info($"Bulldoze apply started, but current demolition cost is {cost}");
				return;
			}

			var city = m_CitySystem.City;
			var playerMoney = EntityManager.GetComponentData<PlayerMoney>(city);
			var previousMoney = playerMoney.money;

			playerMoney.Subtract(cost);
			EntityManager.SetComponentData(city, playerMoney);

			Mod.Log.Info($"Applied bulldoze demolition cost from tooltip total: {cost}. Money {previousMoney} -> {playerMoney.money}");
		}
	}
}
