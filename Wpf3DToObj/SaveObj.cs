using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using Ab3d;
using ObjSaveXDaker;

namespace Wpf3DToObj
{
    class MyClass
    {
        public string Name;
        public string MaterialName;
        public Model3DHelper.MergedMesh3D Mesh3D;
    }
   public class SaveObj
    {
     List<MyClass> listMyClasses = new List<MyClass>();
       public SaveObj()
       {
       }

       public void Save(string path,string name)
       {
           string str = $"#File produced by Open Asset Import Class (359923820@qq.com)\r\nmtllib {name}.mtl\r\n";
           using (
               FileStream fs =
                   new FileStream(path + "\\" + name,
                       FileMode.Create))
           {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.ASCII))
                {
                    sw.WriteLine(str);
                    int vt = 0, v = 0, vn = 0;
                    //面
                    foreach (var cls in listMyClasses)
                    {
                        SaveGeometry(path, name, cls, sw, vt, v, vn);
                        if (cls.Mesh3D.Textures != null)
                        {
                            vt += cls.Mesh3D.Textures.Values.Count;
                        }
                        vn += cls.Mesh3D.Normals.Values.Count;
                        v += cls.Mesh3D.Vertices.Values.Count;
                    }
                }
           }
       }

       public void AddGeometryModel3D(GeometryModel3D model3D)
       {
           ListAdd(model3D);
       }

       public void AddModel3DGroup(Model3DGroup model3DGroup)
       {
           foreach (var chi in model3DGroup.Children)
           {
               if (chi is GeometryModel3D)
               {
                    ListAdd(chi as GeometryModel3D);
                }
           }
       }

       public void AddModelVisual3D(ModelVisual3D modelVisual3D)
       {
           if (modelVisual3D.Content is Model3DGroup)
           {
               AddModel3DGroup(modelVisual3D.Content as Model3DGroup);
           }
       }

       public void AddViewport3D(Viewport3D viewport3)
       {
           foreach (var chi in viewport3.Children)
           {
               if (chi is ModelVisual3D)
               {
                   AddModelVisual3D(chi as ModelVisual3D);
               }
           }
       }

       private void ListAdd(GeometryModel3D model3D)
       {
            MeshGeometry3D meshGeometry3 =
                model3D.Geometry as MeshGeometry3D;
            var mergeMesh3D = Model3DHelper.MergeMesh3D(meshGeometry3, true, true);
            listMyClasses.Add(new MyClass()
            {
                Name = model3D.GetName(),
                Mesh3D = mergeMesh3D,
                MaterialName = model3D.Material.GetName(),
            });
       }

       private void SaveGeometry(string path, string name,MyClass resources, StreamWriter sw, int vt = 0, int v = 0, int vn = 0)
       {
           try
           {
               string str = string.Empty;
               sw.WriteLine(
                   $"# {resources.Mesh3D.Vertices.Values.Count} vertex positions");
               foreach (var point in resources.Mesh3D.Vertices.Values)
               {
                   str =
                       $"v {Round(point.X, 3)} {Round(point.Y, 3)} {Round(point.Z, 3)}";
                   sw.WriteLine(str);
               }
               if (resources.Mesh3D.Textures != null)
               {

                   sw.WriteLine(
                       $"# {resources.Mesh3D.Textures.Values.Count} vertex textures");
                   foreach (
                       var texture in resources.Mesh3D.Textures.Values)
                   {
                       str =
                           $"vt {Round(texture.X, 4)} {Round(texture.Y, 4)} 0";
                       sw.WriteLine(str);
                   }
               }

               sw.WriteLine(
                   $"# {resources.Mesh3D.Normals.Values.Count} vertex Normal");
               foreach (var normal in resources.Mesh3D.Normals.Values)
               {
                   str =
                       $"vn {Round(normal.X, 4)} {Round(normal.Y, 4)} {Round(normal.Z, 4)}";
                   sw.WriteLine(str);
               }
               sw.WriteLine($"g {resources.Name}");
               sw.WriteLine($"usemtl {resources.MaterialName}");
               for (int i = 0;
                   i < resources.Mesh3D.Vertices.Indices.Count;
                   i += 3)
               {
                   if (resources.Mesh3D.Textures != null)
                   {
                       sw.WriteLine(
                           $"f {resources.Mesh3D.Vertices.Indices[i] + 1 + v}/{resources.Mesh3D.Textures.Indices[i] + 1 + vt}/{resources.Mesh3D.Normals.Indices[i] + 1 + vn} " +
                           $" {resources.Mesh3D.Vertices.Indices[i + 1] + 1 + v}/{resources.Mesh3D.Textures.Indices[i + 1] + 1 + vt}/{resources.Mesh3D.Normals.Indices[i + 1] + 1 + vn} " +
                           $" {resources.Mesh3D.Vertices.Indices[i + 2] + 1 + v}/{resources.Mesh3D.Textures.Indices[i + 2] + 1 + vt}/{resources.Mesh3D.Normals.Indices[i + 2] + 1 + vn}");

                   }
                   else
                   {
                       sw.WriteLine(
                           $"f {resources.Mesh3D.Vertices.Indices[i] + 1 + v}//{resources.Mesh3D.Normals.Indices[i] + 1 + vn} " +
                           $" {resources.Mesh3D.Vertices.Indices[i + 1] + 1 + v}//{resources.Mesh3D.Normals.Indices[i + 1] + 1 + vn} " +
                           $" {resources.Mesh3D.Vertices.Indices[i + 2] + 1 + v}//{resources.Mesh3D.Normals.Indices[i + 2] + 1 + vn}");
                   }
               }
           }
           catch (Exception e)
           {
               throw new Exception(e.ToString());
           }
       }

       ///<summary>
        /// 实现数据的四舍五入
        ///</summary>
        ///<param name="v">要进行处理的数据</param>
        ///<param name="x">保留的小数位数</param>
        ///<returns>四舍五入后的结果</returns>
        private double Round(double v, int x)
        {
            bool isNegative = false;
            //如果是负数
            if (v < 0)
            {
                isNegative = true;
                v = -v;
            }

            int value = 1;
            for (int i = 1; i <= x; i++)
            {
                value = value * 10;
            }
            double Int = Math.Round(v * value + 0.5, 0);
            v = Int / value;

            if (isNegative)
            {
                v = -v;
            }

            return v;
        }
    }
}
