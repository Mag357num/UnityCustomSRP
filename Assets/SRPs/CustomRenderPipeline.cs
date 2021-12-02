using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer {
    ScriptableRenderContext context;
    Camera camera;
    CommandBuffer buffer;
    bool batchingFlag = true;
    bool instancingFlag = true;

    public CameraRenderer(bool dynamicBatching, bool instancing)
    {
        batchingFlag = dynamicBatching;
        instancingFlag = instancing;
    }

    void Setup()
    {
        context.SetupCameraProperties(camera); // 更新相机矩阵
        buffer.ClearRenderTarget(true, true, Color.clear); // 清除渲染目标、Z、stencil
        buffer.BeginSample(camera.name);  // 用于做profiler

        if (!camera.TryGetCullingParameters(out var cullingParameters)) {return;} // 获取剔除信息
		CullingResults cullRes = context.Cull(ref cullingParameters);

        var sortingSettings = new SortingSettings(camera) {criteria = SortingCriteria.CommonOpaque};
        var drawSettings = new DrawingSettings(new ShaderTagId("SRPDefaultUnlit"), sortingSettings); // shader pass与渲染顺序
        drawSettings.enableDynamicBatching = batchingFlag; // 是否启用动态批处理
        drawSettings.enableInstancing = instancingFlag; // 是否启用gpu实例化
        var desiredRenderQueueRange = RenderQueueRange.opaque; // opaque渲染队列
        int layerMask = -1; // 渲染layer
		var filterSettings = new FilteringSettings(desiredRenderQueueRange, layerMask);
 
        ExecuteBuffer(); // 复制CommandBuffer渲染队列中的渲染命令到context
		context.DrawRenderers(cullRes, ref drawSettings, ref filterSettings); // 绘制Unlit不透明物体
        context.DrawSkybox(camera); // 添加绘制天空盒的指令到渲染队列
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        filterSettings.renderQueueRange = RenderQueueRange.transparent; // transparent渲染队列
		context.DrawRenderers(cullRes, ref drawSettings, ref filterSettings);  // 绘制Unlit透明物体

        var drawSettingsForwardBase = new DrawingSettings(new ShaderTagId("ForwardBase"), new SortingSettings(camera)); // ForwardBase 是不会绘制unlit的
		filterSettings.renderQueueRange = RenderQueueRange.all;
		context.DrawRenderers(cullRes, ref drawSettingsForwardBase, ref filterSettings); // 做不区分渲染顺序的普通前向渲染
    }

    void Submit() 
    {
        buffer.EndSample(camera.name); // 用于做profiler
        ExecuteBuffer(); // 复制CommandBuffer渲染队列中的渲染命令到context
        context.Submit(); // 实际执行context渲染队列中的渲染命令
    }

    void ExecuteBuffer() 
    {
        // context将command buffer中的渲染命令复制到context中而非立即执行
        context.ExecuteCommandBuffer(buffer); 
        buffer.Clear();  // 释放渲染队列及资源，也是排队而非立即执行
    }

    public void Render(ScriptableRenderContext context, Camera camera) 
    {
        this.context = context;
        this.camera = camera;
        buffer = new CommandBuffer {name = "Render Camera"};

        Setup();
        Submit();
    }
}

public class CustomRenderPipeline : RenderPipeline {
    public CameraRenderer cameraRenderer;

    public CustomRenderPipeline(bool dynamicBatching, bool instancing)
    {
        cameraRenderer = new CameraRenderer(dynamicBatching, instancing);
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(var camera in cameras)
        {
            cameraRenderer.Render(context, camera); // camera来做实际渲染
        }
    }
}