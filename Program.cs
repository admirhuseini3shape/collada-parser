using ColladaParser.Collada;
using ColladaParser.Collada.Model;
using ColladaParser.Shaders;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Drawing;

namespace ColladaParser
{
    public class Program : GameWindow
    {
        private static NativeWindowSettings _nativeWindowSettings = new NativeWindowSettings
        {
            Size = new Vector2i(1280, 720),
            Title = "ColladaParser",
            Flags = ContextFlags.Debug | ContextFlags.ForwardCompatible,

        };

        private DefaultShader defaultShader;
        private ColladaModel model;

        private float cameraDistance = 20.0f;
        private float cameraRotation = 0.0f;

        private int FPS;
        private double lastFPSUpdate;
        private string modelName;
        private bool useBlend;

        private Multisampling multisampling;

        public Program(string modelName) : base(GameWindowSettings.Default, _nativeWindowSettings)
        {
            this.modelName = modelName;
            this.lastFPSUpdate = 0;
        }

        protected override void OnLoad()
        {
            VSync = VSyncMode.On;

            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Cw);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            GL.ClearColor(Color.FromArgb(255, 24, 24, 24));
            var width = Size.X;
            var height = Size.Y;
            multisampling = new Multisampling(width, height, 8);
            defaultShader = new DefaultShader();

            model = ColladaLoader.Load(modelName);
            model.CreateVBOs();
            model.LoadTextures();
            model.Bind(defaultShader.ShaderProgram,
                defaultShader.Texture,
                defaultShader.HaveTexture,
                defaultShader.Ambient,
                defaultShader.Diffuse,
                defaultShader.Specular,
                defaultShader.Shininess);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.IsRepeat)
                return;
            
            if (KeyboardState.IsKeyDown(Keys.Escape))
                Close();

            if (KeyboardState.IsKeyDown(Keys.F))
                WindowState = WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;

            if (KeyboardState.IsKeyDown(Keys.B))
            {
                useBlend = !useBlend;

                if (useBlend)
                {
                    GL.Disable(EnableCap.DepthTest);
                    GL.Enable(EnableCap.Blend);
                }
                else
                {
                    GL.Enable(EnableCap.DepthTest);
                    GL.Disable(EnableCap.Blend);
                }
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (KeyboardState.IsKeyDown(Keys.W))
            {
                cameraDistance -= 0.5f;
            }
            if (KeyboardState.IsKeyDown(Keys.S))
            {
                cameraDistance += 0.5f;
            }

            lastFPSUpdate += e.Time;
            if (lastFPSUpdate > 1)
            {
                Title = $"Collada Parser (Vsync: {VSync}) - FPS: {FPS}";
                FPS = 0;
                lastFPSUpdate %= 1;
            }

            cameraRotation += (float)e.Time;
            var camX = (float)Math.Sin(cameraRotation) * cameraDistance;
            var camZ = (float)Math.Cos(cameraRotation) * cameraDistance;

            Matrix.SetViewMatrix(defaultShader.ViewMatrix, new Vector3(camX, cameraDistance * 0.5f, camZ), new Vector3(0, 0, 0));
        }

        protected override void OnResize(ResizeEventArgs resizeEventArgs)
        {
            var width = Size.X;
            var height = Size.Y;
            var aspectRatio = (float)width / (float)height;
            Matrix.SetProjectionMatrix(defaultShader.ProjectionMatrix, (float)Math.PI / 4, aspectRatio);

            GL.Viewport(0, 0, width, height);
            multisampling.RefreshBuffers(width, height);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            FPS++;
            multisampling.Bind();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            model.Render();

            multisampling.Draw();

            SwapBuffers();
        }

        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Model not specified!");
                Environment.Exit(-1);
            }

            using (var program = new Program(args[0]))
            {
                program.Run();
            }
        }
    }
}