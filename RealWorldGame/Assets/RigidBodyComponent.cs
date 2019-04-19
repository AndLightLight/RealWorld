using UnityEngine;
using UnityEditor;
using Unity.Entities;
using TrueSync;
using TrueSync.Physics3D;
using Unity.Mathematics;

[System.Serializable]
public struct RigidBodyData : IComponentData
{
    public enum DampingType { None = 0x00, Angular = 0x01, Linear = 0x02 }
    public TSMatrix inertia;
    public TSMatrix invInertia;

    public TSMatrix invInertiaWorld;
    public TSMatrix orientation;
    public TSMatrix invOrientation;
    public TSVector position;
    public TSVector linearVelocity;
    public TSVector angularVelocity;

    public FP staticFriction;
    public FP restitution;

     public TSBBox boundingBox;
 
     public FP inactiveTime;

    public TBool isActive;
    public TBool isStatic;
    public TBool isKinematic;
    public TBool affectedByGravity;
    public TBool isColliderOnly;

    public FP inverseMass;

    public TSVector force, torque;

    private int hashCode;

    public int internalIndex;

    public int marker;

    public TBool disabled;

    public TSVector _freezePosition;

    public TSMatrix _freezeRotation;
    public TSQuaternion _freezeRotationQuat;

    // Previous state of gravity before switch Kinematic to true
    public TBool prevKinematicGravity;

    public FP linearDrag;

    public FP angularDrag;

    public TBool isParticle;

    public ShapeData shape;

    private DampingType damping;



    public DampingType Damping { get { return damping; } set { damping = value; } }


    public RigidBodyData()
    {
        inertia = TSMatrix.InternalIdentity;
        invInertia = TSMatrix.InternalIdentity;

        invInertiaWorld = TSMatrix.InternalIdentity;
        orientation = TSMatrix.InternalIdentity;
        invOrientation = TSMatrix.InternalIdentity;
    position = TSVector.zero;
        linearVelocity = TSVector.zero;
        angularVelocity = TSVector.zero;

    staticFriction = FP.Zero;
        restitution = FP.Zero;

    boundingBox = new TSBBox();

    inactiveTime = FP.Zero;

    isActive = true;
    isStatic = false;
        isKinematic = false;
        affectedByGravity = true;
        isColliderOnly = false;

    inverseMass = FP.Zero;

    force = TSVector.zero;
    torque = TSVector.zero;

    hashCode = 0;

        internalIndex = 0;

        marker = 0;

        disabled = false;

    _freezePosition = TSVector.zero;

    _freezeRotation = TSMatrix.InternalIdentity;
    _freezeRotationQuat = TSQuaternion.identity;

    // Previous state of gravity before switch Kinematic to true
    prevKinematicGravity = false;

    linearDrag = FP.Zero;

    angularDrag = FP.Zero;

    isParticle = false;

    shape = new ShapeData();

    damping = DampingType.Angular | DampingType.Linear;
}

    /// <summary>
    /// The shape the body is using.
    /// </summary>
    public ShapeData Shape
    {
        get { return shape; }
        set
        {
            // deregister update event
            //if (shape != null) shape.ShapeUpdated -= updatedHandler;

            // register new event
            shape = value;
            //shape.ShapeUpdated += new ShapeUpdatedHandler(ShapeUpdated);
        }
    }

    /// <summary>
    /// If set to false the velocity is set to zero,
    /// the body gets immediately freezed.
    /// </summary>
    public TBool IsActive
    {
        get
        {
            return isActive;
        }
        set
        {
            if (!isActive && value)
            {
                // if inactive and should be active
                inactiveTime = FP.Zero;
            }
            else if (isActive && !value)
            {
                // if active and should be inactive
                inactiveTime = FP.PositiveInfinity;
                this.angularVelocity.MakeZero();
                this.linearVelocity.MakeZero();
            }

            isActive = value;
        }
    }

    public TBool IsParticle
    {
        get { return isParticle; }
        set
        {
            if (isParticle && !value)
            {
//                 updatedHandler = new ShapeUpdatedHandler(ShapeUpdated);
//                 this.Shape.ShapeUpdated += updatedHandler;
//                 SetMassProperties();
                isParticle = false;
            }
            else if (!isParticle && value)
            {
                this.inertia = TSMatrix.Zero;
                this.invInertia = this.invInertiaWorld = TSMatrix.Zero;
                this.invOrientation = this.orientation = TSMatrix.Identity;
                inverseMass = FP.One;

                //this.Shape.ShapeUpdated -= updatedHandler;

                isParticle = true;
            }

            //Update();
        }
    }
}

public class RigidBodyComponent : ComponentDataWrapper<RigidBodyData> { }