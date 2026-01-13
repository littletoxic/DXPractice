// WBOIT (Weighted Blended Order-Independent Transparency) 合成 Pass 着色器
// 参考算法实现: https://github.com/nvpro-samples/vk_order_independent_transparency/blob/main/shaders/oitWeighted.frag.glsl
//
// 合成 Pass：将累积的透明层结果与不透明场景混合
// 使用全屏三角形技巧，无需顶点缓冲，通过 SV_VertexID 生成顶点

struct VSOutput
{
    float4 position : SV_Position;
    float2 texcoord : TEXCOORD0;
};

// 全屏三角形顶点着色器
// 3 个顶点覆盖整个屏幕，比全屏四边形（6 顶点）更高效
VSOutput VSMain(uint vertexID : SV_VertexID)
{
    VSOutput output;

    // 根据顶点 ID 生成 UV 坐标：(0,0), (2,0), (0,2)
    output.texcoord = float2((vertexID << 1) & 2, vertexID & 2);

    // 将 UV [0,2] 映射到 NDC [-1,1]，Y 轴翻转
    output.position = float4(output.texcoord * float2(2, -2) + float2(-1, 1), 0, 1);

    return output;
}

// 累积纹理和透明度纹理
Texture2D<float4> texAccum : register(t0, space0);   // 加权颜色累积 (RGBA16F)
Texture2D<float> texReveal : register(t1, space0);   // 透明度乘积 (R16F)

float4 PSMain(VSOutput input) : SV_Target
{
    // 使用 Load 直接按像素坐标读取，比 Sample 更精确（无插值）
    int3 coord = int3(input.position.xy, 0);
    float4 accum = texAccum.Load(coord);
    float reveal = texReveal.Load(coord);

    // 如果没有透明片段贡献，跳过此像素
    if (accum.a < 1e-5)
    {
        discard;
    }

    // 计算平均颜色：累积的加权颜色 / 累积的权重
    float3 avgColor = accum.rgb / accum.a;

    // 输出 reveal 作为 alpha
    // 混合状态: Src × (1-SrcAlpha) + Dst × SrcAlpha
    // 即: avgColor × (1-reveal) + Background × reveal
    // reveal 高 = 更多背景可见，reveal 低 = 更多透明层颜色可见
    return float4(avgColor, reveal);
}
