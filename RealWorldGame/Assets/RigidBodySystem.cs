using UnityEngine;
using UnityEditor;
using Unity.Entities;
using TrueSync;
using TrueSync.Physics3D;
using Unity.Jobs;


class RigidBodySystem : JobComponentSystem
{
    struct Group
    {
        public ComponentDataArray<RigidBodyData> rbdList;
        public EntityArray entityList;
        public readonly int Length;
    }

    [Inject]
    Group m_RigidBodys;

    private TSVector gravity = new TSVector(0, -981 * FP.EN2, 0);

    struct Handle : IJobParallelFor
    {


        public void Execute(int index)
        {
            
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        FP timestep = Time.deltaTime;
//         for (int i = 0;i < m_RigidBodys.rbdList.Length; ++i)
//         {
//             TSVector temp;
//             var body = m_RigidBodys.rbdList[i];
//             if (!body.isStatic && body.IsActive)
//             {
//                 //hll 根据外力和体重计算线性速度
//                 TSVector.Multiply(ref body.force, body.inverseMass * timestep, out temp);
//                 TSVector.Add(ref temp, ref body.linearVelocity, out body.linearVelocity);
// 
//                 //hll 根据扭矩计算角速度
//                 if (!(body.isParticle))
//                 {
//                     TSVector.Multiply(ref body.torque, timestep, out temp);
//                     TSVector.Transform(ref temp, ref body.invInertiaWorld, out temp);
//                     TSVector.Add(ref temp, ref body.angularVelocity, out body.angularVelocity);
//                 }
//                 //hll 根据重力计算线性速度
//                 if (body.affectedByGravity)
//                 {
//                     TSVector.Multiply(ref gravity, timestep, out temp);
//                     TSVector.Add(ref body.linearVelocity, ref temp, out body.linearVelocity);
//                 }
//             }
// 
//             body.force.MakeZero();
//             body.torque.MakeZero();
// 
//             TSVector.Multiply(ref body.linearVelocity, timestep, out temp);
//             TSVector.Add(ref temp, ref body.position, out body.position);
// 
//             if (!(body.isParticle))
//             {
// 
//                 //exponential map
//                 TSVector axis;
//                 FP angle = body.angularVelocity.magnitude;
// 
//                 if (angle < FP.EN3)
//                 {
//                     // use Taylor's expansions of sync function
//                     // axis = body.angularVelocity * (FP.Half * timestep - (timestep * timestep * timestep) * (0.020833333333f) * angle * angle);
//                     TSVector.Multiply(ref body.angularVelocity, (FP.Half * timestep - (timestep * timestep * timestep) * (2082 * FP.EN6) * angle * angle), out axis);
//                 }
//                 else
//                 {
//                     // sync(fAngle) = sin(c*fAngle)/t
//                     TSVector.Multiply(ref body.angularVelocity, (FP.Sin(FP.Half * angle * timestep) / angle), out axis);
//                 }
// 
//                 TSQuaternion dorn = new TSQuaternion(axis.x, axis.y, axis.z, FP.Cos(angle * timestep * FP.Half));
//                 TSQuaternion ornA; TSQuaternion.CreateFromMatrix(ref body.orientation, out ornA);
// 
//                 TSQuaternion.Multiply(ref dorn, ref ornA, out dorn);
// 
//                 dorn.Normalize(); TSMatrix.CreateFromQuaternion(ref dorn, out body.orientation);
//             }
// 
//             body.linearVelocity *= 1 / (1 + timestep * body.linearDrag);
//             body.angularVelocity *= 1 / (1 + timestep * body.angularDrag);
// 
//             /*if ((body.Damping & RigidBody.DampingType.Linear) != 0)
//                 TSVector.Multiply(ref body.linearVelocity, currentLinearDampFactor, out body.linearVelocity);
// 
//             if ((body.Damping & RigidBody.DampingType.Angular) != 0)
//                 TSVector.Multiply(ref body.angularVelocity, currentAngularDampFactor, out body.angularVelocity);*/
// 
//             Update(body);
// 
// 
//         }


        

        return inputDeps;
    }

    public void Update(RigidBodyData body)
    {
//         if (body.isParticle)
//         {
//             body.inertia = TSMatrix.Zero;
//             body.invInertia = body.invInertiaWorld = TSMatrix.Zero;
//             body.invOrientation = body.orientation = TSMatrix.Identity;
//             body.boundingBox = body.shape.boundingBox;
//             TSVector.Add(ref body.boundingBox.min, ref body.position, out body.boundingBox.min);
//             TSVector.Add(ref body.boundingBox.max, ref body.position, out body.boundingBox.max);
// 
//             body.angularVelocity.MakeZero();
//         }
//         else
//         {
//             // Given: Orientation, Inertia
//             TSMatrix.Transpose(ref body.orientation, out body.invOrientation);
//             body.Shape.GetBoundingBox(ref body.orientation, out body.boundingBox);
//             TSVector.Add(ref body.boundingBox.min, ref body.position, out body.boundingBox.min);
//             TSVector.Add(ref body.boundingBox.max, ref body.position, out body.boundingBox.max);
// 
// 
//             if (!body.isStatic)
//             {
//                 TSMatrix.Multiply(ref body.invOrientation, ref body.invInertia, out body.invInertiaWorld);
//                 TSMatrix.Multiply(ref body.invInertiaWorld, ref body.orientation, out body.invInertiaWorld);
//             }
//         }
    }
}