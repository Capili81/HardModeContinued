using Colossal.Mathematics;

using Game.City;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;

using HardMode.Domain;

using HarmonyLib;

using System;

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace HardMode.Patches
{
	[HarmonyPatch(typeof(TerrainSystem), nameof(TerrainSystem.ApplyBrush))]
	internal static class TerrainSystem_ApplyBrush
	{
		private static Texture2D s_HeightmapBefore;
		private static bool s_SkipCharge;

		public static bool Prefix(TerrainSystem __instance, TerraformingType type, Bounds2 area, Brush brush, Texture texture)
		{
			s_SkipCharge = true;
			CleanupHeightmapBefore();

			if (Mod.Settings?.TerraformingCostsMoney != true)
			{
				return true;
			}

			if (!TryGetPlayerMoney(__instance, false, out _, out var playerMoney, out _))
			{
				Mod.Log.Warn("Terraforming cost skipped because city money could not be found.");
				return true;
			}

			if (playerMoney.money <= 0)
			{
				Mod.Log.Info("Terraforming blocked because the city has no money.");
				return false;
			}

			try
			{
				s_HeightmapBefore = ReadHeightmapArea(__instance, area);
				s_SkipCharge = false;
			}
			catch (Exception ex)
			{
				Mod.Log.Warn(ex, "Terraforming cost skipped because the terrain heightmap could not be read before applying the brush.");
				CleanupHeightmapBefore();
			}

			return true;
		}

		public static void Postfix(TerrainSystem __instance, TerraformingType type, Bounds2 area, Brush brush, Texture texture)
		{
			if (s_SkipCharge || s_HeightmapBefore == null || Mod.Settings?.TerraformingCostsMoney != true)
			{
				CleanupHeightmapBefore();
				return;
			}

			Texture2D heightmapAfter = null;

			try
			{
				heightmapAfter = ReadHeightmapArea(__instance, area);
				var heightDifference = GetHeightDifferenceInMeters(__instance, s_HeightmapBefore, heightmapAfter);
				var cost = GetTerraformingCost(heightDifference);

				if (cost <= 0)
				{
					return;
				}

				if (!TryGetPlayerMoney(__instance, true, out var city, out var playerMoney, out var lookup))
				{
					Mod.Log.Warn($"Terraforming cost {cost} could not be applied because city money could not be found.");
					return;
				}

				var previousMoney = playerMoney.money;
				playerMoney.Subtract(cost);
				lookup[city] = playerMoney;

				Mod.Log.Info($"Applied terraforming cost: {cost}. Height difference {heightDifference:0.##}. Money {previousMoney} -> {playerMoney.money}");
			}
			catch (Exception ex)
			{
				Mod.Log.Warn(ex, "Terraforming cost could not be applied.");
			}
			finally
			{
				if (heightmapAfter != null)
				{
					Texture2D.Destroy(heightmapAfter);
				}

				CleanupHeightmapBefore();
			}
		}

		private static Texture2D ReadHeightmapArea(TerrainSystem terrainSystem, Bounds2 area)
		{
			area.min -= terrainSystem.playableOffset;
			area.max -= terrainSystem.playableOffset;
			area.min /= terrainSystem.playableArea;
			area.max /= terrainSystem.playableArea;

			var pixelArea = new int4(
				(int)math.max(math.floor(area.min.x * terrainSystem.heightmap.width) - 1f, 0f),
				(int)math.max(math.floor(area.min.y * terrainSystem.heightmap.height) - 1f, 0f),
				(int)math.min(math.ceil(area.max.x * terrainSystem.heightmap.width) + 1f, terrainSystem.heightmap.width - 1),
				(int)math.min(math.ceil(area.max.y * terrainSystem.heightmap.height) + 1f, terrainSystem.heightmap.height - 1));

			pixelArea.zw -= pixelArea.xy;

			var width = math.max(pixelArea.z, 1);
			var height = math.max(pixelArea.w, 1);
			var snapshot = new Texture2D(width, height, terrainSystem.heightmap.graphicsFormat, TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate);
			var previousActiveTexture = RenderTexture.active;

			try
			{
				RenderTexture.active = terrainSystem.heightmap as RenderTexture;
				snapshot.ReadPixels(new Rect(pixelArea.x, pixelArea.y, width, height), 0, 0);
				snapshot.Apply();
				return snapshot;
			}
			catch
			{
				Texture2D.Destroy(snapshot);
				throw;
			}
			finally
			{
				RenderTexture.active = previousActiveTexture;
			}
		}

		private static float GetHeightDifferenceInMeters(TerrainSystem terrainSystem, Texture2D before, Texture2D after)
		{
			var heightsBefore = before.GetPixelData<short>(0);
			var heightsAfter = after.GetPixelData<short>(0);
			var count = math.min(heightsBefore.Length, heightsAfter.Length);
			var difference = 0L;

			for (var i = 0; i < count; i++)
			{
				difference += math.abs(heightsAfter[i] - heightsBefore[i]);
			}

			var heightScaler = short.MaxValue / terrainSystem.heightScaleOffset.x;
			return difference / heightScaler;
		}

		private static int GetTerraformingCost(float heightDifference)
		{
			var multiplier = Mod.Settings?.EconomyDifficulty switch
			{
				EconomyDifficulty.Easy => 1f,
				EconomyDifficulty.Medium => 2f,
				EconomyDifficulty.Hard => 3f,
				EconomyDifficulty.GoodLuck => 4f,
				_ => 2f
			};

			return Mathf.RoundToInt(heightDifference * multiplier / 100f) * 100;
		}

		private static bool TryGetPlayerMoney(TerrainSystem terrainSystem, bool readWrite, out Entity city, out PlayerMoney playerMoney, out ComponentLookup<PlayerMoney> lookup)
		{
			var query = terrainSystem.EntityManager.CreateEntityQuery(readWrite ? ComponentType.ReadWrite<Game.City.City>() : ComponentType.ReadOnly<Game.City.City>());
			var entities = query.ToEntityArray(Allocator.Temp);

			try
			{
				if (entities.Length == 0)
				{
					city = Entity.Null;
					playerMoney = default;
					lookup = default;
					return false;
				}

				city = entities[0];
				lookup = terrainSystem.CheckedStateRef.GetComponentLookup<PlayerMoney>(!readWrite);

				if (!lookup.HasComponent(city))
				{
					playerMoney = default;
					return false;
				}

				playerMoney = lookup[city];
				return true;
			}
			finally
			{
				entities.Dispose();
			}
		}

		private static void CleanupHeightmapBefore()
		{
			if (s_HeightmapBefore != null)
			{
				Texture2D.Destroy(s_HeightmapBefore);
				s_HeightmapBefore = null;
			}
		}
	}
}
