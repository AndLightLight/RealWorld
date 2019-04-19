using System;
using System.Linq;
using UnityEngine;

namespace Unity.Entities
{
// 	static class AutomaticWorldBootstrap
// 	{
// 		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
// 		static void Initialize()
// 		{
// 			DefaultWorldInitialization.Initialize("Default World", false);
// 		}
// 	}

	static class DefaultWorldInitialization
	{
		static void DomainUnloadShutdown()
		{
			World.DisposeAllWorlds();
			ScriptBehaviourUpdateOrder.UpdatePlayerLoop();
		}

		static void GetBehaviourManagerAndLogException(World world, Type type)
		{
			try
			{
				world.GetOrCreateManager(type);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		public static void SetupInjectionHooks()

		{

			// Reflection to get internal hook classes. Doesn't seem to be a proper way to do this.

			var gameObjectArrayInjectionHookType =

				typeof(Unity.Entities.GameObjectEntity).Assembly.GetType("Unity.Entities.GameObjectArrayInjectionHook");

			var transformAccessArrayInjectionHookType =

				typeof(Unity.Entities.GameObjectEntity).Assembly.GetType(

					"Unity.Entities.TransformAccessArrayInjectionHook");

			var componentArrayInjectionHookType =

				typeof(Unity.Entities.GameObjectEntity).Assembly.GetType("Unity.Entities.ComponentArrayInjectionHook");

			InjectionHookSupport.RegisterHook(

				(InjectionHook)Activator.CreateInstance(gameObjectArrayInjectionHookType));

			InjectionHookSupport.RegisterHook(

				(InjectionHook)Activator.CreateInstance(transformAccessArrayInjectionHookType));

			InjectionHookSupport.RegisterHook(

				(InjectionHook)Activator.CreateInstance(componentArrayInjectionHookType));

		}

		public static void Initialize(string worldName, bool editorWorld)
		{
			var world = new World(worldName);
			World.Active = world;

			// Register hybrid injection hooks
			// 			InjectionHookSupport.RegisterHook(new GameObjectArrayInjectionHook());
			// 			InjectionHookSupport.RegisterHook(new TransformAccessArrayInjectionHook());
			// 			InjectionHookSupport.RegisterHook(new ComponentArrayInjectionHook());
			SetupInjectionHooks();

			PlayerLoopManager.RegisterDomainUnload(DomainUnloadShutdown, 10000);

			foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
			{
				var allTypes = ass.GetTypes();

				// Create all ComponentSystem
				var systemTypes = allTypes.Where(t =>
					t.IsSubclassOf(typeof(ComponentSystemBase)) &&
					!t.IsAbstract &&
					!t.ContainsGenericParameters &&
					t.GetCustomAttributes(typeof(DisableAutoCreationAttribute), true).Length == 0);
				foreach (var type in systemTypes)
				{
					if (editorWorld && type.GetCustomAttributes(typeof(ExecuteInEditMode), true).Length == 0)
						continue;

					GetBehaviourManagerAndLogException(world, type);
				}
			}

			ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
		}
	}
}