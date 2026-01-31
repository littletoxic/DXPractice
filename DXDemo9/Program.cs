// https://blog.csdn.net/DGAF2198588973/article/details/155643538

// 鸣谢原作者大大: Onerui(momo) (https://sketchfab.com/hswangrui)
// 模型项目地址: https://sketchfab.com/3d-models/blue-archivekasumizawa-miyu-108d81dfd5a44dab92e4dccf0cc51a02

// GLTF 是一种 3D 模型文件格式，用于引擎和应用程序高效传输和加载 3D 场景和模型
// 它的文件结构如下：
// textures           模型纹理文件夹
// license.txt        模型使用的许可证 (常用许可证是 CC-BY: 发布必须署名原作者，可商用)
// scene.bin          储存模型图元，顶点，骨骼动画等二进制数据
// scene.gltf         JSON 文件，定义 scene 的结构与元素，并储存 bin 和 textures 的链接

using System.Runtime.CompilerServices;
using Assimp;

namespace DXDemo9;

internal sealed class Program {

    private static uint _totalNodeNum;  // 总节点数

    // 递归展开计算模型骨骼节点，打印每个骨骼节点的名称，大部分情况下节点都是骨骼，节点名和骨骼名相同
    // 骨骼在模型中的呈现形式是骨骼树，在骨骼动画中，父节点会影响子节点，子节点拿到的偏移矩阵是相对于所属父节点的，所以要递归展开计算
    // Assimp 中，节点的变换矩阵会影响节点下的全部属性，包括网格、骨骼、子节点，骨骼名是唯一的
    private static void ModelNodeTraversal(ref Node node, string nodeBaseStr, uint tier) {
        Console.WriteLine($"{nodeBaseStr}┗━━ 节点名: {node.mName} (层级: {tier}, 子节点数: {node.mNumChildren})");

        // 当前节点打印完成，下一行打印子节点时，在前面添加空格，便于区分
        nodeBaseStr += "  ";

        // 遍历子节点，打印子节点的名称
        //for (int i = 0; i < node.mNumChildren; i++) {
        //    ModelNodeTraversal(ref node.Children[i], nodeBaseStr, tier + 1);
        //}

        foreach (ref var child in node.Children) {
            ModelNodeTraversal(ref child, nodeBaseStr, tier + 1);
        }

        _totalNodeNum++;  // 总节点数 +1
    }

    private static void Main() {

        var modelFileName = "miyu/scene.gltf";  // 模型文件名

        // 导入模型使用的标志
        // ConvertToLeftHanded: Assimp 导入的模型是以 OpenGL 的右手坐标系为基础的，将模型转换成 DirectX 的左手坐标系
        // Triangulate: 模型设计师可能使用多边形对模型进行建模的，对于用多边形建模的模型，将它们都转换成基于三角形建模
        // FixInFacingNormals: 建模软件都是双面显示的，所以设计师不会在意顶点绕序方向，部分面会被剔除无法正常显示，需要翻转过来
        // LimitBoneWeights: 限制顶点的骨骼权重最多为 4 个，其余权重无需处理
        // GenBoundingBoxes: 对每个网格，都生成一个 AABB 体积盒
        // JoinIdenticalVertices: 将位置相同的顶点合并为一个顶点，从而减少模型的顶点数量，优化内存使用和提升渲染效率
        var modelImportFlag =
            (uint)(PostProcessSteps.MakeLeftHanded | PostProcessSteps.FlipUVs | PostProcessSteps.FlipWindingOrder |
            PostProcessSteps.Triangulate | PostProcessSteps.FixInfacingNormals | PostProcessSteps.LimitBoneWeights |
            PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.GenBoundingBoxes);

        // 读取模型数据，数据会存储在 Scene 对象

        ref var modelScene = ref ImportFileR(modelFileName, modelImportFlag);

        // 如果模型没有成功载入 (无法载入，载入未完成，载入后无根节点)
        if (Unsafe.IsNullRef(ref modelScene) || (modelScene.mFlags & AI_SCENE_FLAGS_INCOMPLETE) != 0 || Unsafe.IsNullRef(ref modelScene.RootNode)) {
            var errorMsg = GetErrorString();
            Console.WriteLine($"载入文件 {modelFileName} 失败！错误原因：{errorMsg}");
            return;
        }

        Console.WriteLine($"成功加载 {modelFileName} !\n");

        // ---------------------------------------------------------------------------------------------------------------

        Console.WriteLine("开始遍历节点!\n");

        // 基础偏移字符串，用于打印时区分父子节点
        string nodeBaseStr = "";

        // 从根节点开始递归打印
        ModelNodeTraversal(ref modelScene.RootNode, nodeBaseStr, 1);

        Console.WriteLine($"\n总节点数: {_totalNodeNum}\n");
        Console.WriteLine("------------------------------------------------------------------------\n");

        // ---------------------------------------------------------------------------------------------------------------

        // 在 3D 建模的世界里，材质 (Material) 就像是给模型穿上的一件"外衣"
        // 它不仅决定了模型的颜色和光泽，还能展现出材料的透明度、反射特性以及那些细微的表面纹理

        // 材质组
        var materialGroup = new List<string>();

        // 遍历模型中的所有材质
        foreach (ref var material in modelScene.Materials) {
            //for (int i = 0; i < modelScene.mNumMaterials; i++) {
            //    // Assimp 解析出来的模型材质
            //    ref var material = ref modelScene.Materials[i];

            // 获取材质名
            GetMaterialString(material, _AI_MATKEY_NAME_BASE, 0, 0, out var materialName);

            Console.WriteLine($"材质名: {materialName}");
            materialGroup.Add(materialName.ToString());

            // 纹理是材质的子集，一个材质可能有很多组不同类型的纹理
            // 获取材质中的纹理数，目前我们只会用到 EMISSIVE, DIFFUSE, NORMAL 这三种纹理，后面我们会逐一介绍这些纹理的功能与区别
            // 在 Assimp 中，有一些类型 (例如 DIFFUSE 和 BASE_COLOR) 其实指的是同一个纹理，不过会有一些功能上的区别
            var emissiveCount = GetMaterialTextureCount(material, TextureType.EMISSIVE);
            var diffuseCount = GetMaterialTextureCount(material, TextureType.EMISSIVE);
            var normalCount = GetMaterialTextureCount(material, TextureType.EMISSIVE);

            Console.WriteLine($"EMISSIVE 自发光纹理数: {emissiveCount}");
            Console.WriteLine($"DIFFUSE 漫反射纹理数: {diffuseCount}");
            Console.WriteLine($"NORMAL 法线纹理数: {normalCount}");


            // 获取材质对应的纹理，有时候一个材质甚至会有多个名字相同，但是类型不同的纹理贴图
            // Assimp 为了区分这些纹理，特意设置了一个叫 Channel 通道的东西，如果名字相同，不同类型的纹理贴图会占据不同通道
            // GetTexture 的第二个参数就是通道索引，大部分材质同类型下最多只有一个纹理，所以第二个参数直接指定 0 就行
            // 注意 GetTexture 的返回值表示状态，Return.Success 才算获取成功
            if (GetMaterialTexture(material, TextureType.EMISSIVE, 0, out var materialPath) == ReturnCode.SUCCESS) {
                Console.WriteLine($"EMISSIVE 自发光纹理文件名: {materialPath}");
            }
            if (GetMaterialTexture(material, TextureType.DIFFUSE, 0, out materialPath) == ReturnCode.SUCCESS) {
                Console.WriteLine($"DIFFUSE 漫反射纹理文件名: {materialPath}");
            }
            if (GetMaterialTexture(material, TextureType.NORMALS, 0, out materialPath) == ReturnCode.SUCCESS) {
                Console.WriteLine($"NORMAL 法线纹理文件名: {materialPath}");
            }

            Console.WriteLine();
        }

        Console.WriteLine($"\n总材质数: {modelScene.mNumMaterials}\n");
        Console.WriteLine("------------------------------------------------------------------------\n");

        // ---------------------------------------------------------------------------------------------------------------

        Console.WriteLine("开始遍历网格!\n");

        // Mesh 网格相当于模型的皮肤，它存储了模型要渲染的顶点信息。在骨骼模型中，Mesh 需要依赖骨骼节点才能正确渲染
        // 遍历模型的所有 Mesh 网格
        foreach (ref var mesh in modelScene.Meshes) {
            //for (int i = 0; i < modelScene.mNumMeshes; i++) {
            //    // 当前网格
            //    ref var mesh = ref modelScene.Meshes[i];

            Console.WriteLine($"网格名: {mesh.mName}");
            Console.WriteLine($"顶点数: {mesh.mNumVertices}");
            Console.WriteLine($"索引数: {mesh.mNumFaces * 3}");
            Console.WriteLine($"所用材质索引: {mesh.mMaterialIndex} (对应材质: {materialGroup[(int)mesh.mMaterialIndex]})");

            // 如果 Mesh 有被骨骼影响到，就输出相关骨骼。对骨骼模型而言非常重要
            // 一个网格可以被多个骨骼影响到，这是因为网格依赖骨骼，附着在骨骼上，通常是整块整块附着
            // 关节是骨骼之间的连接点，这些网格很多都会覆盖关节 (或占据关节部分位置)，网格上的顶点受不同骨骼的影响程度各不相同
            if (mesh.mNumBones > 0) {
                Console.WriteLine("受影响的骨骼:");

                // 遍历骨骼
                //for (uint j = 0; j < mesh.mNumBones; j++) {
                foreach (ref var bone in mesh.Bones) {
                    Console.WriteLine(bone.mName);
                }
            } else {  // 没有绑定骨骼，网格上的顶点坐标就表示相对于整个模型的绝对位置，即使是骨骼模型，也会有网格没被骨骼影响到
                Console.WriteLine("没有骨骼影响！");
            }

            Console.WriteLine();
        }

        Console.WriteLine($"总网格数: {modelScene.mNumMeshes}\n");

        // ---------------------------------------------------------------------------------------------------------------

        Console.WriteLine("开始计算包围盒!\n");

        // AABB 包围盒，下一章有大用
        float minBoundsX, minBoundsY, minBoundsZ;  // 最小坐标点
        float maxBoundsX, maxBoundsY, maxBoundsZ;  // 最大坐标点

        // 设置初始值，模型 AABB 包围盒，用于调整摄像机视野，防止模型在摄像机视野外飞出去
        ref var initialMesh = ref modelScene.Meshes[0];
        minBoundsX = initialMesh.mAABB.mMin.x;
        minBoundsY = initialMesh.mAABB.mMin.y;
        minBoundsZ = initialMesh.mAABB.mMin.z;
        maxBoundsX = initialMesh.mAABB.mMax.x;
        maxBoundsY = initialMesh.mAABB.mMax.y;
        maxBoundsZ = initialMesh.mAABB.mMax.z;

        // 逐网格遍历，计算整个模型的 AABB 包围盒，请注意导入模型时要指定 GenBoundingBoxes，否则 MAABB 成员会没有数据
        foreach (ref var mesh in modelScene.Meshes) {

            // 更新总包围盒
            minBoundsX = Math.Min(mesh.mAABB.mMin.x, minBoundsX);
            minBoundsY = Math.Min(mesh.mAABB.mMin.y, minBoundsY);
            minBoundsZ = Math.Min(mesh.mAABB.mMin.z, minBoundsZ);

            maxBoundsX = Math.Max(mesh.mAABB.mMax.x, maxBoundsX);
            maxBoundsY = Math.Max(mesh.mAABB.mMax.y, maxBoundsY);
            maxBoundsZ = Math.Max(mesh.mAABB.mMax.z, maxBoundsZ);
        }

        Console.WriteLine($"模型包围盒: min({minBoundsX},{minBoundsY},{minBoundsZ}) max({maxBoundsX},{maxBoundsY},{maxBoundsZ})");

        // 释放场景资源
        ReleaseImport(modelScene);
    }
}
