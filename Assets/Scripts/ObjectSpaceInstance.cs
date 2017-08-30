using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using System.Collections;
using System;



namespace ObjectSpace
{
    public class ObjectSpaceInstance : MonoBehaviour
    {
        // Index texture map
        Texture indexMap;
        private int framesSinceShade = 0;
        public uint id = 0;
        public Mesh mesh;
        public Texture2D renderedOutput;
        public MeshRenderer mRenderer;
        public RenderState renderState = RenderState.None;

        public Color specularColour;
        public Color tintColour;
        public float shininess;

        // geometry shaders
        [SerializeField] Shader cachedShader;
        [SerializeField] Shader calculationShader;
        [SerializeField] RenderTexture rendTex;
        [SerializeField] Material lightingCalculationMat;
        [SerializeField] Texture2D cachedTex;
        Texture temp;

        CommandBuffer commandBuffer;
        

        [SerializeField] Camera tempCam;

        [SerializeField] Text debugText;

        Vector3[] shadedData;

        ComputeBuffer vertexBuffer;
        ComputeBuffer shadedBuffer;

        RenderBuffer defaultColorBuffer;
        RenderBuffer defaultDepthBuffer;


        #region MonoBehaviour
        // Use this for initialization
        void Awake()
        {
            mRenderer = GetComponent<MeshRenderer>();
            mesh = GetComponent<MeshFilter>().mesh;
            lightingCalculationMat = new Material(calculationShader);
            lightingCalculationMat.SetTexture("_MainTex", mRenderer.material.mainTexture);

            ObjectSpaceController.instance.AddToList(this);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
               // RenderStoredData();
            }
        }

        private void OnDisable()
        {
            if(vertexBuffer != null)
            {
                vertexBuffer.Dispose();
                shadedBuffer.Dispose();
                vertexBuffer = null;
                shadedBuffer = null;
            }
        }

#endregion

        #region Native Plugin Interaction





        #endregion

        #region Unity Shader Switch prototype
        /// <summary>
        /// Initialise and assign buffers
        /// </summary>
        void InitialiseBuffers()
        {

            defaultColorBuffer = Graphics.activeColorBuffer;
            defaultDepthBuffer = Graphics.activeDepthBuffer;
            int count = mesh.vertexCount;

            //vertexBuffer = new ComputeBuffer(count, 36 /*Calculated above*/, ComputeBufferType.Default);

            //CustomAppdata[] vertexData = new CustomAppdata[mesh.vertexCount];
            //Vector3[] vertices = mesh.vertices;
            //Vector3[] normals = mesh.normals;
            //Vector2[] UVs = mesh.uv;

            //for(int i = 0; i < count; ++i)
            //{
            //    vertexData[i].vertex = vertices[i];
            //    vertexData[i].normal = normals[i];
            //    vertexData[i].uv = UVs[i];

            //    shadedData[i] = Vector4.zero;
            //}

            //vertexBuffer.SetData(vertexData);

            //shadedBuffer = new ComputeBuffer(count, sizeof(float) * 3, ComputeBufferType.Default);
            //shadedData = new Vector3[mesh.vertexCount];
            //shadedBuffer.SetData(shadedData);

            if (!rendTex)
            {
                rendTex = new RenderTexture(mRenderer.material.mainTexture.width, mRenderer.material.mainTexture.height, 0);
                rendTex.anisoLevel = 0;

                rendTex.name = "Render Texture";
                rendTex.enableRandomWrite = true;
            }

           // tempCam.SetTargetBuffers(rendTex.colorBuffer, rendTex.depthBuffer);

            commandBuffer = new CommandBuffer();
            commandBuffer.name = "Calculate Shade Buffer";

            Camera.main.AddCommandBuffer(CameraEvent.AfterForwardOpaque, commandBuffer);

            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

            //commandBuffer.SetGlobalBuffer("pixelBuffer", shadedBuffer);
            commandBuffer.SetGlobalTexture("outTex", rendTex);
            lightingCalculationMat.SetTexture("outTex", rendTex);
            commandBuffer.SetRenderTarget(rendTex);
            commandBuffer.DrawMesh(mesh, matrix, lightingCalculationMat);
            
        }

        void SwitchShaders()
        {
            mRenderer.material.shader = mRenderer.material.shader == cachedShader ? calculationShader : cachedShader;
        }

        public void OnRenderObject()
        {
            framesSinceShade = 0;            
        }

#endregion
    }
}
