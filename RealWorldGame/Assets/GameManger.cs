using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;


public struct TBool
{
    private readonly byte _value;
    public TBool(bool value) { _value = (byte)(value ? 1 : 0); }
    public static implicit operator TBool(bool value) { return new TBool(value); }
    public static implicit operator bool(TBool value) { return value._value != 0; }
}

class GameManger : MonoBehaviour
{
	EntityManager em;
	public GameObject prefab;

	private void Start()
	{
		em = World.Active.GetOrCreateManager<EntityManager>();
// 		ScriptBehaviourManager SBM = World.Active.GetOrCreateManager<LocalPlayerSystem>();
// 		SBM.Update();
	}

	private void Update()
	{
		if (Input.GetKeyUp(KeyCode.X))
		{
            for (int i = 0;i < 50; ++ i)
            {
                var e = em.Instantiate(prefab);
                em.SetComponentData<Position>(e, new Position() { Value = new float3(UnityEngine.Random.Range(0, 20), 1, UnityEngine.Random.Range(0, 20)) });
                em.SetComponentData<LocalPlayerData>(e, new LocalPlayerData() { speed = UnityEngine.Random.Range(1, 50) });
            }

		}
        if (Input.GetKeyUp(KeyCode.A))
        {
            var dfdf = em.GetAllEntities();
            for (int i = 0;i < dfdf.Length && i < 3; ++ i)
            {
                em.DestroyEntity(dfdf[i]);
            }
//             em.DestroyEntity(em.CreateComponentGroup(ComponentType.Create<Position>(), ComponentType.Create<LocalPlayerData>(),
//                 ComponentType.Create<LocalToWorld>(), ComponentType.Create<MeshInstanceRenderer>(),
//                 ComponentType.Create<VisibleLocalToWorld>()));
        }
	}
}

