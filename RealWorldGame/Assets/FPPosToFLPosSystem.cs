using UnityEngine;
using UnityEditor;
using Unity.Entities;
using TrueSync;
using TrueSync.Physics3D;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;

class FPPosToFLPosSystem : JobComponentSystem
{
    struct Group
    {
        public ComponentDataArray<RigidBodyData> rbdList;
        public ComponentDataArray<Position> pList;
        public ComponentDataArray<Rotation> rList;
        public EntityArray entityList;
        public readonly int Length;
    }

    [Inject]
    Group m_RigidBodys;


    struct Handle : IJobParallelFor
    {
        public void Execute(int index)
        {
            
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        FP timestep = Time.deltaTime;
        
        for (int i = 0;i < m_RigidBodys.rbdList.Length; ++ i)
        {
            TSVector pos = m_RigidBodys.rbdList[i].position;
            TSMatrix rot = m_RigidBodys.rbdList[i].orientation;
            m_RigidBodys.pList[i] = new Position
            {
                Value = new float3(pos.x.AsFloat(),pos.y.AsFloat(),pos.z.AsFloat())
            };
            m_RigidBodys.rList[i] = new Rotation
            {
                Value = new quaternion(new float3x3(rot.M11.AsFloat(), rot.M12.AsFloat(), rot.M13.AsFloat(), rot.M21.AsFloat(), rot.M22.AsFloat(), rot.M23.AsFloat(), rot.M31.AsFloat(), rot.M32.AsFloat(), rot.M33.AsFloat()))
            };
        }
        
        return inputDeps;
    }
}