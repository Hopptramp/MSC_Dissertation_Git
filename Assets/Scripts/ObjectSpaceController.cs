using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

namespace ObjectSpace
{
    public struct ShaderStruct
    {
        public Shader cachedShader;
        public Shader calculationShader;
    }

    public enum RenderState
    {
        None,
        Render,
        CollectTexture,
    }

    public class ObjectSpaceController : MonoBehaviour
    {
        public static ObjectSpaceController instance { get; private set; }
        private List<ObjectSpaceInstance> OSinstances;

        private delegate void DebugCallback(string _message);
        DebugCallback debugCallBack = new DebugCallback(DebugMethod);

        private delegate void GetTexture(IntPtr _tex);
        GetTexture getTexture = new GetTexture(GetNativeTexture);

        [SerializeField] Light directionalLight;
        

        #region Monobehaviour & initialise
        private void Awake()
        {
            if (instance)
                Destroy(this);
            else
                instance = this;

            OSinstances = new List<ObjectSpaceInstance>();
            RegisterDebugCallback(debugCallBack);
            PassTextureBack(getTexture);
        }

        void Update()
        {
            for(int i = 0; i < OSinstances.Count; ++i)
            {
                if (OSinstances[i].renderState == RenderState.CollectTexture)
                {
                    RequestTexture(OSinstances[i].id);
                    OSinstances[i].renderState = RenderState.None;
                    OSinstances[i].mRenderer.material.mainTexture = OSinstances[i].renderedOutput;
                }
            }


            if (Input.GetKeyDown(KeyCode.A))
            {
                if (OSinstances[0].renderState == RenderState.None)
                {
                    CollectRenderingVariables(OSinstances[0]);
                    OSinstances[0].renderState = RenderState.Render;
                    DebugRunVisibility();
                }
            }
        }

        /// <summary>
        /// Called after Camera finishes rendering (After Update also)
        /// </summary>
        public void OnPostRender()
        {
            for (int i = 0; i < OSinstances.Count; ++i)
            {
                if (OSinstances[i].renderState == RenderState.Render)
                {
                    //StartCoroutine(CallPluginAtEndOfFrames(0));
                    GL.IssuePluginEvent(GetRenderEventFunc(), i);
                    OSinstances[i].renderState = RenderState.CollectTexture;
                }
            }
        }

        public void AddToList(ObjectSpaceInstance _instance)
        {
            OSinstances.Add(_instance);
            _instance.id = (uint)(OSinstances.Count - 1);
            //CreateDataTexture((int)_instance.id);
            SendMeshToNative(_instance, _instance.id);
        }

        private void OnApplicationQuit()
        {
            ShutDown();
        }

        #endregion


        [DllImport("NativePluginVS2015")]
        private static extern void SetTextureFromUnity(System.IntPtr texture, int id);

        [DllImport("NativePluginVS2015")]
        private static extern IntPtr GetRenderEventFunc();

        [DllImport("NativePluginVS2015")]
        private static extern void InitialiseObjectSpaceInstance(IntPtr vertexBuffer, IntPtr indexBuffer, uint indexCount, int vertexCount, 
            IntPtr sourceIndices, IntPtr sourceVertices, IntPtr sourceNormals, IntPtr sourceUVs, uint id);

        [DllImport("NativePluginVS2015")]
        private static extern void RegisterDebugCallback(DebugCallback callback);

        [DllImport("NativePluginVS2015")]
        private static extern void PassTextureBack(GetTexture _tex);

        [DllImport("NativePluginVS2015")]
        private static extern void RequestTexture(uint _id);

        [DllImport("NativePluginVS2015")]
        private static extern void ShutDown();

        [DllImport("NativePluginVS2015")]
        private static extern void DebugRunVisibility();

        [DllImport("NativePluginVS2015")]
        private static extern void SetRenderingVariables(IntPtr worldViewProjectionMat, IntPtr worldMat, IntPtr viewDirection,
            IntPtr lightDirection, IntPtr lightPos, IntPtr lightColour, IntPtr specColour, IntPtr ambientColour, IntPtr tintColour, 
            float shininess, uint id);

        void CollectRenderingVariables(ObjectSpaceInstance _objectInstance)
        {
            // initialise the matrices for passing through
            bool d3d = SystemInfo.graphicsDeviceVersion.IndexOf("Direct3D") > -1;
            Matrix4x4 M = _objectInstance.transform.localToWorldMatrix;
            Matrix4x4 V = Camera.main.worldToCameraMatrix;
            Matrix4x4 P = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, false);
            
            Matrix4x4 MVP = P * V * M;

            // allocate handles for native plugin handover
            GCHandle MVPhandle = GCHandle.Alloc(MVP, GCHandleType.Pinned);
            GCHandle worldHandle = GCHandle.Alloc(M, GCHandleType.Pinned);
            GCHandle viewHandle = GCHandle.Alloc(Camera.main.transform.forward, GCHandleType.Pinned);
            GCHandle lightDirHandle = GCHandle.Alloc(directionalLight.transform.forward, GCHandleType.Pinned);
            GCHandle lightPosHandle = GCHandle.Alloc(directionalLight.transform.position, GCHandleType.Pinned);
            GCHandle lightColourHandle = GCHandle.Alloc(directionalLight.color, GCHandleType.Pinned);
            GCHandle specColourHandle = GCHandle.Alloc(_objectInstance.specularColour, GCHandleType.Pinned);
            GCHandle ambientColourHandle = GCHandle.Alloc(RenderSettings.ambientLight, GCHandleType.Pinned);
            GCHandle tintColourHandle = GCHandle.Alloc(_objectInstance.tintColour, GCHandleType.Pinned);
            //GCHandle shininessHandle = GCHandle.Alloc(_objectInstance.shininess, GCHandleType.Pinned);

            // Send data to native plugin
            SetRenderingVariables(MVPhandle.AddrOfPinnedObject(), worldHandle.AddrOfPinnedObject(), viewHandle.AddrOfPinnedObject(), 
                lightDirHandle.AddrOfPinnedObject(), lightPosHandle.AddrOfPinnedObject(), lightColourHandle.AddrOfPinnedObject(),
                specColourHandle.AddrOfPinnedObject(), ambientColourHandle.AddrOfPinnedObject(), tintColourHandle.AddrOfPinnedObject(),
                _objectInstance.shininess, _objectInstance.id);
            
            // free handles
            MVPhandle.Free();
            worldHandle.Free();
            viewHandle.Free();
            lightDirHandle.Free();
            lightPosHandle.Free();
            lightColourHandle.Free();
            specColourHandle.Free();
            ambientColourHandle.Free();
            tintColourHandle.Free();
            //shininessHandle.Free();
        }

        /// <summary>
        /// Send the mesh data to the plugin (vertex buffer / index buffer)
        /// </summary>
        void SendMeshToNative(ObjectSpaceInstance _objectInstance, uint _id)
        {
            Mesh mesh = _objectInstance.mesh;
            mesh.MarkDynamic();

            print(mesh.uv[0]);
            GCHandle vertices = GCHandle.Alloc(mesh.vertices, GCHandleType.Pinned);
            GCHandle normals = GCHandle.Alloc(mesh.normals, GCHandleType.Pinned);
            GCHandle uv = GCHandle.Alloc(mesh.uv, GCHandleType.Pinned);
            GCHandle indices = GCHandle.Alloc(mesh.GetIndices(0), GCHandleType.Pinned);


            InitialiseObjectSpaceInstance(mesh.GetNativeVertexBufferPtr(0), mesh.GetNativeIndexBufferPtr(), mesh.GetIndexCount(0), mesh.vertexCount, 
                indices.AddrOfPinnedObject(), vertices.AddrOfPinnedObject(), normals.AddrOfPinnedObject(), uv.AddrOfPinnedObject(), _id);

            vertices.Free();
            normals.Free();
            uv.Free();
            indices.Free();
        }

        /// <summary> 
        /// Create texture and send to plugin
        /// </summary>
        void CreateDataTexture(int _id)
        {
            OSinstances[_id].renderedOutput = new Texture2D(512, 512, TextureFormat.RGBA32, false, false);
            SetTextureFromUnity(OSinstances[_id].renderedOutput.GetNativeTexturePtr(), _id);
        }

        /// <summary>
        ///Debug log messages from the unmanaged code
        /// </summary>
        /// <param name="message"></param>
        private static void DebugMethod(string message)
        {
            Debug.Log("NativePlugin: " + message);
        }

        private static void GetNativeTexture(IntPtr _tex)
        {
            instance.OSinstances[0].renderedOutput = Texture2D.CreateExternalTexture(512, 512, TextureFormat.RGBA32,
                false, false, _tex);

        }






        #region shader switch prototype
        // geometry shaders
        [SerializeField] Shader cachedShader;
        [SerializeField] Shader calculationShader;

        public ShaderStruct GetShaders()
        {
            ShaderStruct shaderStruct = new ShaderStruct();
            shaderStruct.cachedShader = cachedShader;
            shaderStruct.calculationShader = calculationShader;

            return shaderStruct;
        }

        #endregion

    }

}