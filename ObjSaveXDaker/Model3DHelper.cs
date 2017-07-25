using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ObjSaveXDaker
{
    class Model3DHelper
    {
        public class Triangles<TValue> where TValue : struct
        {
            public List<TValue> Values;
            public List<int> Indices;
        }

        public class MergedMesh3D
        {
            public Triangles<Point3D> Vertices;
            public Triangles<Vector3D> Normals;
            public Triangles<System.Windows.Point> Textures;
        }

        private class ValueAndListIndex<TValue>
        {
            public TValue Value;
            public int ListIndex;
        }

        private class ValueEqualityComparer<TValue> : EqualityComparer<TValue>
            where TValue : struct
        {
            public override bool Equals(TValue aValue, TValue bValue)
            {
                return aValue.Equals(bValue);
            }

            public override int GetHashCode(TValue value)
            {
                return value.GetHashCode();
            }
        }

        private static Triangles<T> MergeTriangles<T>(Triangles<T> srcTriangles)
            where T : struct
        {
            var values = srcTriangles.Values;
            var indices = srcTriangles.Indices.ToList();

            var valueAndListIndices =
                new List<ValueAndListIndex<T>>(indices.Count);
            valueAndListIndices.AddRange(
                indices.Select(
                    (valueIndex, listIndex) => new ValueAndListIndex<T>
                    {
                        Value = values[valueIndex],
                        ListIndex = listIndex
                    }));

            var group = valueAndListIndices.GroupBy(pi => pi.Value,
                pi => pi.ListIndex,
                new ValueEqualityComparer<T>());
            int newIndex = 0;
            List<T> newValues = new List<T>();
            foreach (var gv in group)
            {
                newValues.Add(gv.Key);
                foreach (var oldIndex in gv)
                {
                    indices[oldIndex] = newIndex;
                }
                newIndex++;
            }

            return new Triangles<T>() {Values = newValues, Indices = indices};
        }

        /// <summary>
        /// 合并重复的顶点、纹理坐标、法线
        /// </summary>
        /// <param name="srcMesh"></param>
        /// <param name="bTexture"></param>
        /// <param name="bNormal"></param>
        /// <returns></returns>
       public  static MergedMesh3D MergeMesh3D(MeshGeometry3D srcMesh,
            bool bTexture = false, bool bNormal = false)
        {
            MergedMesh3D mesh = new MergedMesh3D
            {
                Vertices = MergeTriangles(new Triangles<Point3D>()
                {
                    Values = srcMesh.Positions.ToList(),
                    Indices = srcMesh.TriangleIndices.ToList()
                })
            };

            if (bTexture)
            {
                if (srcMesh.TextureCoordinates.Count > 0)
                {
                    mesh.Textures = MergeTriangles(new Triangles<System.Windows.Point>()
                    {
                        Values = srcMesh.TextureCoordinates.ToList(),
                        Indices = srcMesh.TriangleIndices.ToList()
                    });
                }

            }

            if (bNormal)
            {
                mesh.Normals = MergeTriangles(new Triangles<Vector3D>()
                {
                    Values = srcMesh.Normals.ToList(),
                    Indices = srcMesh.TriangleIndices.ToList()
                });
            }

            return mesh;
        }

        /// <summary>
        /// 从Model3D获取GeometryModel3D的列表，从左到右遍历Model3DGroup的树形节点
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static List<GeometryModel3D> GetListFromModel(Model3D model)
        {
            Stack<Model3D> modelStack = new Stack<Model3D>(new[] { model });
            List<GeometryModel3D> gmList = new List<GeometryModel3D>();

            while (modelStack.Count > 0)
            {
                var m = modelStack.Pop();
                var mg = m as Model3DGroup;
                if (mg != null)
                {
                    foreach (var child in mg.Children)
                    {
                        modelStack.Push(child);
                    }
                    continue;
                }

                var gm = m as GeometryModel3D;
                if (gm != null)
                {
                    gmList.Add(gm);
                }
            }

            return gmList;
        }

        /// <summary>
        /// 由精简的MergedMesh3D构建可以用于显示的MeshGeometry3D，
        /// 目前仅是根据三角形索引展开所有顶点、纹理、法线，
        /// 顶点数目等于三角形索引数目，后期可进一步优化，合并部分顶点
        /// </summary>
        /// <param name="srcMesh"></param>
        /// <returns></returns>
        public static MeshGeometry3D BuildMesh3D(MergedMesh3D srcMesh)
        {
            var mesh = new MeshGeometry3D
            {
                Positions = new Point3DCollection(
                    srcMesh.Vertices.Indices.Select(
                        i => srcMesh.Vertices.Values[i]))
            };

            if (srcMesh.Textures != null)
            {
                mesh.TextureCoordinates =
                    new PointCollection(
                        srcMesh.Textures.Indices.Select(
                            i => srcMesh.Textures.Values[i]));
            }

            if (srcMesh.Normals != null)
            {
                mesh.Normals =
                    new Vector3DCollection(
                        srcMesh.Normals.Indices.Select(
                            i => srcMesh.Normals.Values[i]));
            }

            var ti = new List<int>(mesh.Positions.Count);
            for (int i = 0; i < mesh.Positions.Count; i++)
            {
               ti.Add(i);
            }
            mesh.TriangleIndices = new Int32Collection(ti);

            return mesh;
        }
    }
}