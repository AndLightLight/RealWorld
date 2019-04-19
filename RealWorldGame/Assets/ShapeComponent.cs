using UnityEngine;
using UnityEditor;
using Unity.Entities;
using TrueSync;
using TrueSync.Physics3D;
using System.Collections.Generic;
using System;


public struct ShapeData : IComponentData
{
    // public values so we can access them fast  without calling properties.
    public TSMatrix inertia;
    public FP mass;

    public TSBBox boundingBox;
    public TSVector geomCen;

    /// <summary>
    /// Gets called when the shape changes one of the parameters.
    /// </summary>
    //public event ShapeUpdatedHandler ShapeUpdated;

    /// <summary>
    /// Creates a new instance of a shape.
    /// </summary>
    //     public ShapeData()
    //     {
    //     }

    public TSVector halfSize;

    /// <summary>
    /// Returns the inertia of the untransformed shape.
    /// </summary>
    public TSMatrix Inertia { get { return inertia; }  set { inertia = value; } }


    /// <summary>
    /// Gets the mass of the shape. This is the volume. (density = 1)
    /// </summary>
    public FP Mass { get { return mass; }  set { mass = value; } }

    /// <summary>
    /// Informs all listener that the shape changed.
    /// </summary>
    public void RaiseShapeUpdated()
    {
        //if (ShapeUpdated != null) ShapeUpdated();
    }

    /// <summary>
    /// The untransformed axis aligned bounding box of the shape.
    /// </summary>
    public TSBBox BoundingBox { get { return boundingBox; } }

    /// <summary>
    /// Allows to set a user defined value to the shape.
    /// </summary>
    //public object Tag { get; set; }

    private struct ClipTriangle
    {
        public TSVector n1;
        public TSVector n2;
        public TSVector n3;
        public int generation;
    };

    /// <summary>
    /// Hull making.
    /// </summary>
    /// <remarks>Based/Completely from http://www.xbdev.net/physics/MinkowskiDifference/index.php
    /// I don't (100%) see why this should always work.
    /// </remarks>
    /// <param name="triangleList"></param>
    /// <param name="generationThreshold"></param>
    public void MakeHull(ref List<TSVector> triangleList, int generationThreshold)
    {
        FP distanceThreshold = FP.Zero;

        if (generationThreshold < 0) generationThreshold = 4;

        Stack<ClipTriangle> activeTriList = new Stack<ClipTriangle>();

        TSVector[] v = new TSVector[] // 6 Array
        {
            new TSVector( -1,  0,  0 ),
            new TSVector(  1,  0,  0 ),

            new TSVector(  0, -1,  0 ),
            new TSVector(  0,  1,  0 ),

            new TSVector(  0,  0, -1 ),
            new TSVector(  0,  0,  1 ),
        };

        int[,] kTriangleVerts = new int[8, 3] // 8 x 3 Array
        {
            { 5, 1, 3 },
            { 4, 3, 1 },
            { 3, 4, 0 },
            { 0, 5, 3 },

            { 5, 2, 1 },
            { 4, 1, 2 },
            { 2, 0, 4 },
            { 0, 2, 5 }
        };

        for (int i = 0; i < 8; i++)
        {
            ClipTriangle tri = new ClipTriangle();
            tri.n1 = v[kTriangleVerts[i, 0]];
            tri.n2 = v[kTriangleVerts[i, 1]];
            tri.n3 = v[kTriangleVerts[i, 2]];
            tri.generation = 0;
            activeTriList.Push(tri);
        }

        // surfaceTriList
        while (activeTriList.Count > 0)
        {
            ClipTriangle tri = activeTriList.Pop();

            TSVector p1; SupportMapping(ref tri.n1, out p1);
            TSVector p2; SupportMapping(ref tri.n2, out p2);
            TSVector p3; SupportMapping(ref tri.n3, out p3);

            FP d1 = (p2 - p1).sqrMagnitude;
            FP d2 = (p3 - p2).sqrMagnitude;
            FP d3 = (p1 - p3).sqrMagnitude;

            if (TSMath.Max(TSMath.Max(d1, d2), d3) > distanceThreshold && tri.generation < generationThreshold)
            {
                ClipTriangle tri1 = new ClipTriangle();
                ClipTriangle tri2 = new ClipTriangle();
                ClipTriangle tri3 = new ClipTriangle();
                ClipTriangle tri4 = new ClipTriangle();

                tri1.generation = tri.generation + 1;
                tri2.generation = tri.generation + 1;
                tri3.generation = tri.generation + 1;
                tri4.generation = tri.generation + 1;

                tri1.n1 = tri.n1;
                tri2.n2 = tri.n2;
                tri3.n3 = tri.n3;

                TSVector n = FP.Half * (tri.n1 + tri.n2);
                n.Normalize();

                tri1.n2 = n;
                tri2.n1 = n;
                tri4.n3 = n;

                n = FP.Half * (tri.n2 + tri.n3);
                n.Normalize();

                tri2.n3 = n;
                tri3.n2 = n;
                tri4.n1 = n;

                n = FP.Half * (tri.n3 + tri.n1);
                n.Normalize();

                tri1.n3 = n;
                tri3.n1 = n;
                tri4.n2 = n;

                activeTriList.Push(tri1);
                activeTriList.Push(tri2);
                activeTriList.Push(tri3);
                activeTriList.Push(tri4);
            }
            else
            {
                if (((p3 - p1) % (p2 - p1)).sqrMagnitude > TSMath.Epsilon)
                {
                    triangleList.Add(p1);
                    triangleList.Add(p2);
                    triangleList.Add(p3);
                }
            }
        }
    }


    /// <summary>
    /// Uses the supportMapping to calculate the bounding box. Should be overidden
    /// to make this faster.
    /// </summary>
    /// <param name="orientation">The orientation of the shape.</param>
    /// <param name="box">The resulting axis aligned bounding box.</param>
    public void GetBoundingBox(ref TSMatrix orientation, out TSBBox box)
    {
        // I don't think that this can be done faster.
        // 6 is the minimum number of SupportMap calls.

        TSVector vec = TSVector.zero;

        vec.Set(orientation.M11, orientation.M21, orientation.M31);
        SupportMapping(ref vec, out vec);
        box.max.x = orientation.M11 * vec.x + orientation.M21 * vec.y + orientation.M31 * vec.z;

        vec.Set(orientation.M12, orientation.M22, orientation.M32);
        SupportMapping(ref vec, out vec);
        box.max.y = orientation.M12 * vec.x + orientation.M22 * vec.y + orientation.M32 * vec.z;

        vec.Set(orientation.M13, orientation.M23, orientation.M33);
        SupportMapping(ref vec, out vec);
        box.max.z = orientation.M13 * vec.x + orientation.M23 * vec.y + orientation.M33 * vec.z;

        vec.Set(-orientation.M11, -orientation.M21, -orientation.M31);
        SupportMapping(ref vec, out vec);
        box.min.x = orientation.M11 * vec.x + orientation.M21 * vec.y + orientation.M31 * vec.z;

        vec.Set(-orientation.M12, -orientation.M22, -orientation.M32);
        SupportMapping(ref vec, out vec);
        box.min.y = orientation.M12 * vec.x + orientation.M22 * vec.y + orientation.M32 * vec.z;

        vec.Set(-orientation.M13, -orientation.M23, -orientation.M33);
        SupportMapping(ref vec, out vec);
        box.min.z = orientation.M13 * vec.x + orientation.M23 * vec.y + orientation.M33 * vec.z;
    }

    /// <summary>
    /// This method uses the <see cref="ISupportMappable"/> implementation
    /// to calculate the local bounding box, the mass, geometric center and 
    /// the inertia of the shape. In custom shapes this method should be overidden
    /// to compute this values faster.
    /// </summary>
    public void UpdateShape()
    {
        GetBoundingBox(ref TSMatrix.InternalIdentity, out boundingBox);

        CalculateMassInertia();
        RaiseShapeUpdated();
    }

    /// <summary>
    /// Calculates the inertia of the shape relative to the center of mass.
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="centerOfMass"></param>
    /// <param name="inertia">Returns the inertia relative to the center of mass, not to the origin</param>
    /// <returns></returns>
    #region  public static FP CalculateMassInertia(ShapeData shape, out JVector centerOfMass, out JMatrix inertia)
    public static FP CalculateMassInertia(ShapeData shape, out TSVector centerOfMass,
        out TSMatrix inertia)
    {
        FP mass = FP.Zero;
        centerOfMass = TSVector.zero; inertia = TSMatrix.Zero;

        if (shape is Multishape) throw new ArgumentException("Can't calculate inertia of multishapes.", "shape");

        // build a triangle hull around the shape
        List<TSVector> hullTriangles = new List<TSVector>();
        shape.MakeHull(ref hullTriangles, 3);

        // create inertia of tetrahedron with vertices at
        // (0,0,0) (1,0,0) (0,1,0) (0,0,1)
        FP a = FP.One / (60 * FP.One), b = FP.One / (120 * FP.One);
        TSMatrix C = new TSMatrix(a, b, b, b, a, b, b, b, a);

        for (int i = 0; i < hullTriangles.Count; i += 3)
        {
            TSVector column0 = hullTriangles[i + 0];
            TSVector column1 = hullTriangles[i + 1];
            TSVector column2 = hullTriangles[i + 2];

            TSMatrix A = new TSMatrix(column0.x, column1.x, column2.x,
                column0.y, column1.y, column2.y,
                column0.z, column1.z, column2.z);

            FP detA = A.Determinant();

            // now transform this canonical tetrahedron to the target tetrahedron
            // inertia by a linear transformation A
            TSMatrix tetrahedronInertia = TSMatrix.Multiply(A * C * TSMatrix.Transpose(A), detA);

            TSVector tetrahedronCOM = (FP.One / (4 * FP.One)) * (hullTriangles[i + 0] + hullTriangles[i + 1] + hullTriangles[i + 2]);
            FP tetrahedronMass = (FP.One / (6 * FP.One)) * detA;

            inertia += tetrahedronInertia;
            centerOfMass += tetrahedronMass * tetrahedronCOM;
            mass += tetrahedronMass;
        }

        inertia = TSMatrix.Multiply(TSMatrix.Identity, inertia.Trace()) - inertia;
        centerOfMass = centerOfMass * (FP.One / mass);

        FP x = centerOfMass.x;
        FP y = centerOfMass.y;
        FP z = centerOfMass.z;

        // now translate the inertia by the center of mass
        TSMatrix t = new TSMatrix(
            -mass * (y * y + z * z), mass * x * y, mass * x * z,
            mass * y * x, -mass * (z * z + x * x), mass * y * z,
            mass * z * x, mass * z * y, -mass * (x * x + y * y));

        TSMatrix.Add(ref inertia, ref t, out inertia);

        return mass;
    }
    #endregion

    /// <summary>
    /// Numerically calculates the inertia, mass and geometric center of the shape.
    /// This gets a good value for "normal" shapes. The algorithm isn't very accurate
    /// for very flat shapes. 
    /// </summary>
    public void CalculateMassInertia()
    {
        this.mass = ShapeData.CalculateMassInertia(this, out geomCen, out inertia);
    }

    /// <summary>
    /// SupportMapping. Finds the point in the shape furthest away from the given direction.
    /// Imagine a plane with a normal in the search direction. Now move the plane along the normal
    /// until the plane does not intersect the shape. The last intersection point is the result.
    /// </summary>
    /// <param name="direction">The direction.</param>
    /// <param name="result">The result.</param>
    public void SupportMapping(ref TSVector direction, out TSVector result)
    {
        result.x = FP.Sign(direction.x) * halfSize.x;
        result.y = FP.Sign(direction.y) * halfSize.y;
        result.z = FP.Sign(direction.z) * halfSize.z;
    }

    /// <summary>
    /// The center of the SupportMap.
    /// </summary>
    /// <param name="geomCenter">The center of the SupportMap.</param>
    public void SupportCenter(out TSVector geomCenter)
    {
        geomCenter = this.geomCen;
    }
}

public class ShapeComponent : ComponentDataWrapper<ShapeData> { }