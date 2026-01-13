// WBOIT (Weighted Blended Order-Independent Transparency) 累积 Pass 着色器
// 参考算法实现: https://github.com/nvpro-samples/vk_order_independent_transparency/blob/main/shaders/oitWeighted.frag.glsl
//
// 这是一种近似的顺序无关透明度算法，通过为每个片段分配基于深度、透明度和颜色的权重，
// 然后累积加权的预乘颜色和透明度乘积来实现无需排序的透明渲染。

struct VSInput
{
    float4 position : POSITION;
    float2 texcoordUV : TEXCOORD;
    float4 Matrix_Row0 : MATRIX0;
    float4 Matrix_Row1 : MATRIX1;
    float4 Matrix_Row2 : MATRIX2;
    float4 Matrix_Row3 : MATRIX3;
};

struct VSOutput
{
    float4 position : SV_Position;
    float2 texcoordUV : TEXCOORD;
    float clipW : CLIPW;  // 裁剪空间 W 分量，即视图空间 Z
};

cbuffer GlobalData : register(b0, space0)
{
    row_major float4x4 MVP; // View * Projection 矩阵
}

Texture2D m_texture : register(t0, space0);
SamplerState m_sampler : register(s0, space0);

VSOutput VSMain(VSInput input)
{
    float4x4 ModelMatrix;
    ModelMatrix[0] = input.Matrix_Row0;
    ModelMatrix[1] = input.Matrix_Row1;
    ModelMatrix[2] = input.Matrix_Row2;
    ModelMatrix[3] = input.Matrix_Row3;

    VSOutput output;

    output.position = mul(input.position, ModelMatrix);
    output.position = mul(output.position, MVP);

    // 保存裁剪空间的 W 分量（即视图空间 Z），避免在 PS 中做除法
    output.clipW = output.position.w;

    output.texcoordUV = input.texcoordUV;

    return output;
}

// MRT 输出结构
struct PSOutput
{
    float4 accumulation : SV_Target0;  // 加权预乘颜色累积 (RGBA16F)
    float reveal : SV_Target1;         // 透明度乘积 (R16F)
};

PSOutput PSMain(VSOutput input)
{
    PSOutput output;

    float4 color = m_texture.Sample(m_sampler, input.texcoordUV);

    // 跳过几乎完全透明的像素
    if (color.a < 0.01)
    {
        discard;
    }

    // === WBOIT 权重函数 ===
    // 直接使用从 VS 传递的 clipW（视图空间 Z），无需除法
    float viewZ = input.clipW;

    // 深度权重：基于视图空间 Z 坐标，较近的物体获得更高权重
    // 场景使用的范围约 0.01 到 50，乘以 10 调整到 0.1 到 500
    float depthZ = abs(viewZ) * 10.0;
    float distWeight = clamp(0.03 / (1e-5 + pow(depthZ / 200.0, 4.0)), 1e-2, 3e3);

    // Alpha 权重：基于颜色和透明度（使用原始值，非预乘）
    float alphaWeight = min(1.0, max(max(color.r, color.g), max(color.b, color.a)) * 40.0 + 0.01);
    alphaWeight *= alphaWeight;

    float weight = alphaWeight * distWeight;

    // 预乘 alpha (在计算权重之后)
    float3 premultipliedColor = color.rgb * color.a;

    // 输出加权颜色 (混合状态: ONE + ONE，加法累积)
    output.accumulation = float4(premultipliedColor * weight, color.a * weight);

    // 输出 alpha (混合状态: ZERO + INV_SRC_COLOR，计算 (1-a) 的乘积)
    output.reveal = color.a;

    return output;
}
