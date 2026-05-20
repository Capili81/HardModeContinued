using Colossal.Entities;

using Game.Buildings;
using Game.Events;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;

using HardMode.Domain;
using HardMode.Systems;

using Unity.Entities;

namespace HardMode.Utility
{
	public static class DemolitionUtility
	{
		public const int DemolitionTimerValue = DemolitionTimerSystem.UPDATE_INTERVAL * 16;

		private static readonly DemolitionTimerSystem _demolitionTimerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<DemolitionTimerSystem>();

		public static void TriggerControlledDemolition(EntityManager entityManager, Entity entity, EntityCommandBuffer commandBuffer)
		{
			if (Mod.Settings?.BulldozeCausesDemolition == false)
			{
				return;
			}

			// Create new entity.
			var componentData = entityManager.GetComponentData<ObjectData>(entityManager.GetComponentData<PrefabRef>(entity).m_Prefab);
			var newEntity = entityManager.CreateEntity(componentData.m_Archetype);
			var collapseComponentData = entityManager.GetComponentData<EventData>(_demolitionTimerSystem.BuildingCollapseEntity);

			// Set prefab data and transform.
			entityManager.SetComponentData(newEntity, entityManager.GetComponentData<PrefabRef>(entity));
			entityManager.SetComponentData(newEntity, entityManager.GetComponentData<Transform>(entity));
			entityManager.SetComponentData(newEntity, entityManager.GetComponentData<Building>(entity));
			entityManager.SetComponentData(newEntity, entityManager.GetComponentData<Color>(entity));
			entityManager.SetComponentData(newEntity, entityManager.GetComponentData<Lot>(entity));
			entityManager.RemoveComponent<PlaceableObjectData>(newEntity);
			entityManager.AddComponentData(newEntity, default(ControlledDemolition));

			if (entityManager.TryGetBuffer<InstalledUpgrade>(entity, true, out var buffer))
			{
				for (var i = 0; i < buffer.Length; i++)
				{
					entityManager.AddComponentData(buffer[i].m_Upgrade, default(ControlledDemolition));
				}
			}

			if (entityManager.TryGetBuffer<MeshColor>(entity, true, out var colors))
			{
				var newColors = entityManager.AddBuffer<MeshColor>(newEntity);
				
				for (var i = 0; i < colors.Length; i++)
				{
					newColors.Add(colors[i]);
				}
			}

			var collapseEntity = commandBuffer.CreateEntity(collapseComponentData.m_Archetype);
			commandBuffer.SetComponent(collapseEntity, new PrefabRef(_demolitionTimerSystem.BuildingCollapseEntity));
			commandBuffer.SetBuffer<TargetElement>(collapseEntity).Add(new TargetElement(newEntity));
		}
	}
}
