using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

class GameManger : MonoBehaviour
{
	EntityManager em;
	public GameObject prefab;

	private void Start()
	{
		//em = World.Active.GetOrCreateManager<EntityManager>();
// 		ScriptBehaviourManager SBM = World.Active.GetOrCreateManager<LocalPlayerSystem>();
// 		SBM.Update();
	}

	private void Update()
	{
		if (Input.GetKeyUp(KeyCode.X))
		{
			var e = em.Instantiate(prefab);
			em.SetComponentData<Position>(e, new Position() { Value = new float3(UnityEngine.Random.Range(0, 20), 1, UnityEngine.Random.Range(0, 20)) });
			em.SetComponentData<LocalPlayerData>(e, new LocalPlayerData() { speed = UnityEngine.Random.Range(1,50) });
		}
	}
}

