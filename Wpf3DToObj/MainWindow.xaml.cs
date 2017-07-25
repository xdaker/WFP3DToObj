using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab3d;
using Ab3d.Assimp;
using Ab3d.DirectX;
using Ab3d.Visuals;
using Assimp;
using ObjSaveXDaker;

namespace Wpf3DToObj
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Ab3d.Visuals.LightingRigVisual3D _defaultLights;
        private string Path;
        public MainWindow()
        {
            string assimp32Folder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assimp32");
            string assimp64Folder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assimp64");
            AssimpWpfImporter.LoadAssimpNativeLibrary(assimp32Folder, assimp64Folder);
            InitializeComponent();
            if (_defaultLights == null)
            {
                _defaultLights = new LightingRigVisual3D();
                MainViewport.Children.Add(_defaultLights);
            }
            Path = AppDomain.CurrentDomain.BaseDirectory;
            
            LoadFile(Path + @"./obj/original_box.obj");
            //LoadFile(Path + @"./obj/ss.obj");
            Export.Click += ExportCilck;
            MyExport.Click += MyExportCilck;
            
        }

        private void MyExportCilck(object sender, RoutedEventArgs e)
        {
            if (wpf3DModel != null)
            {
                //SaveObj saveObj = new SaveObj();
                //saveObj.Save(@"./obj", "ss.obj", wpf3DModel);
                SaveObj saveObj = new SaveObj();
                saveObj.AddViewport3D(MainViewport);
                saveObj.Save(@"./obj" ,"ss.obj");
            }
        }

        private Model3DGroup wpf3DModel;
        private void ExportCilck(object sender, RoutedEventArgs e)
        {
            //if (wpf3DModel != null)
            //{
            //    AssimpWpfExporter assimp = new AssimpWpfExporter();
            //    assimp.AddViewport3D(MainViewport);
            //    AssimpWpfImporter importer = new AssimpWpfImporter();
            //    foreach (var chi in wpf3DModel.Children)
            //    {
            //        if (chi is GeometryModel3D)
            //        {
            //            importer.DefaultMaterial =
            //                (chi as GeometryModel3D).Material;
            //        }
            //    }
            //    assimp.Export("./obj/aa.obj",
            //        AssimpWpfExporter.AssimpExportType.Obj);
            //}
            //else
            //{
            //    Console.WriteLine("null");
            //}

        }

        private void LoadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            if (!System.IO.File.Exists(fileName))
                return;

            if (fileName.ToLower().EndsWith(".obj"))
                LoadObjFile(fileName);
        }
        private void LoadObjFile(string fileName)
        {
            Model3D wpf3DModel = null;
            Exception lastException = null;

            // UH: The following code is much much easier with async and await from .net 4.5
            // But here we want to use only .net 4.0 

            // Read obj file and convert to wpf 3d objects in background thread
            var readObjFileTask = new Task(() =>
            {
                lastException = null;

                try
                {
                    var readerObj = new Ab3d.ReaderObj();
                    wpf3DModel = readerObj.ReadModel3D(fileName);
                    this.wpf3DModel = wpf3DModel as Model3DGroup;
                    // We need to freeze the model because it was created on another thread
                    // After the model is forzen, it cannot be changed any more.
                    // If you want to change the model than the ReadModel3D method must be called on UI thread 
                    // or you must clone the read object in the UI thread.
                    // Another option is to use ReadFile method to read obj file in background thread and then use 
                    // ObjFileToWpfModel3DConverter to convert the read obj file into WPF objects on UI thread - see Ab3d.PowerToys ReaderObj sames for more info
                    wpf3DModel.Freeze();

                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            });

            // After reading the obj file and converting it to WPF 3D objects we need to show the objects or errors - this needs to be done on UI thread
            var showObjectTask = readObjFileTask.ContinueWith(_ =>
            {
                if (lastException != null)
                {
                    MessageBox.Show("Error reading file\r\n" + lastException.Message);

                }
                else if (wpf3DModel != null)
                {
                    ModelVisual3D model = new ModelVisual3D();
                    model.Content = wpf3DModel;
                    MainViewport.Children.Add(model);
                    Console.WriteLine($"model Count={ MainViewport.Children.Count}");

                }

                Mouse.OverrideCursor = null;
            },
            TaskScheduler.FromCurrentSynchronizationContext()); // Run on UI thread


            Mouse.OverrideCursor = Cursors.Wait;

            // Start tasks
            readObjFileTask.Start();
            //readObjFileTask.Start(TaskScheduler.FromCurrentSynchronizationContext()); // This read the model on UI thread
        }

        private void MyExport_Copy_OnClick(object sender, RoutedEventArgs e)
        {
            MainViewport.Children.Clear();
            MainViewport.Children.Add(_defaultLights);
            LoadFile(Path + @".\Sofa\Sofa1\sofa.obj");
        }

        private void MyExport_Copy1_OnClick(object sender, RoutedEventArgs e)
        {
            MainViewport.Children.Clear();
            MainViewport.Children.Add(_defaultLights);
            LoadFile(Path + @"./obj/ss.obj");
        }
    }
}
