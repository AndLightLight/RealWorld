using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

[UpdateBefore(typeof(FixedUpdate))]
class LocalPlayerSystem : JobComponentSystem
{
	struct Group
	{
		public ComponentDataArray<LocalPlayerData> lpdList;
		public ComponentDataArray<Position> pList;
		public EntityArray entityList;
		public readonly int Length;
	}

	[Inject]
	Group filter;

	struct Handle : IJobParallelFor
	{
		public float dl;
		public ComponentDataArray<Position> pList;
		public ComponentDataArray<LocalPlayerData> lpdList;

		public void Execute(int index)
		{
			var p = pList[index];
			if (lpdList[index].currentNeedMove > 0)
			{
				lpdList[index] = new LocalPlayerData
				{
					currentNeedMove = lpdList[index].currentNeedMove - 1,
					speed = lpdList[index].speed
				};
				var newV = pList[index].Value;
				newV.x += lpdList[index].speed*dl;
				pList[index] = new Position
				{
					Value = newV
				};
			}
		}
	}

	override protected JobHandle OnUpdate(JobHandle inputDeps)
	{
        if (Input.GetKeyUp(KeyCode.Space))
		{
			for (int i = 0;i < filter.Length; ++i)
			{
				filter.lpdList[i] = new LocalPlayerData()
				{
					currentNeedMove = 3,
					speed = filter.lpdList[i].speed
				};
			}
			
		}

		var h = new Handle()
		{
			pList = filter.pList,
			lpdList = filter.lpdList,
			dl = Time.deltaTime
		};
		inputDeps.Complete();
		return h.Schedule(filter.Length, filter.Length / 6, inputDeps);

	}
}

