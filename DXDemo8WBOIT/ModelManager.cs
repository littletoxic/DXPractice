using System.Numerics;
using DXDemo8WBOIT.Models;
using Windows.Win32.Graphics.Direct3D12;

namespace DXDemo8WBOIT;

internal sealed class ModelManager {

    private readonly Dictionary<string, TextureMapInfo> _textureSRVMap = [];
    private readonly List<Model> _opaqueGroup = [];
    private readonly List<Model> _transparentGroup = [];
    private readonly List<Model> _translucenceGroup = [];

    public IReadOnlyDictionary<string, TextureMapInfo> TextureSRVMap => _textureSRVMap;

    public ModelManager() {
        _textureSRVMap["dirt"] = new() { TextureFilePath = "resource/dirt.png" };
        _textureSRVMap["grass_top"] = new() { TextureFilePath = "resource/grass_top.png" };
        _textureSRVMap["grass_side"] = new() { TextureFilePath = "resource/grass_side.png" };
        _textureSRVMap["log_oak"] = new() { TextureFilePath = "resource/log_oak.png" };
        _textureSRVMap["log_oak_top"] = new() { TextureFilePath = "resource/log_oak_top.png" };
        _textureSRVMap["furnace_front_off"] = new() { TextureFilePath = "resource/furnace_front_off.png" };
        _textureSRVMap["furnace_side"] = new() { TextureFilePath = "resource/furnace_side.png" };
        _textureSRVMap["furnace_top"] = new() { TextureFilePath = "resource/furnace_top.png" };
        _textureSRVMap["crafting_table_front"] = new() { TextureFilePath = "resource/crafting_table_front.png" };
        _textureSRVMap["crafting_table_side"] = new() { TextureFilePath = "resource/crafting_table_side.png" };
        _textureSRVMap["crafting_table_top"] = new() { TextureFilePath = "resource/crafting_table_top.png" };
        _textureSRVMap["planks_oak"] = new() { TextureFilePath = "resource/planks_oak.png" };

        _textureSRVMap["glass_light_blue"] = new() { TextureFilePath = "resource/glass_light_blue.png" };
        _textureSRVMap["door_wood_lower"] = new() { TextureFilePath = "resource/door_wood_lower.png" };
        _textureSRVMap["door_wood_upper"] = new() { TextureFilePath = "resource/door_wood_upper.png" };
        _textureSRVMap["bed_feet_end"] = new() { TextureFilePath = "resource/bed_feet_end.png" };
        _textureSRVMap["bed_feet_side"] = new() { TextureFilePath = "resource/bed_feet_side.png" };
        _textureSRVMap["bed_feet_top"] = new() { TextureFilePath = "resource/bed_feet_top.png" };
        _textureSRVMap["bed_head_end"] = new() { TextureFilePath = "resource/bed_head_end.png" };
        _textureSRVMap["bed_head_side"] = new() { TextureFilePath = "resource/bed_head_side.png" };
        _textureSRVMap["bed_head_top"] = new() { TextureFilePath = "resource/bed_head_top.png" };
    }

    public void CreateBlock() {
        // 两层泥土地基，y 是高度
        for (int x = 0; x < 10; x++) {
            for (int z = -4; z < 10; z++) {
                for (int y = -2; y < 0; y++) {
                    var dirt = new Dirt {
                        ModelMatrix = Matrix4x4.CreateTranslation(x, y, z)
                    };
                    _opaqueGroup.Add(dirt);
                }
            }
        }

        // 一层草方块地基
        for (int x = 0; x < 10; x++) {
            for (int z = -4; z < 10; z++) {
                var grass = new Grass {
                    ModelMatrix = Matrix4x4.CreateTranslation(x, 0, z)
                };
                _opaqueGroup.Add(grass);
            }
        }

        // 4x4 木板房基
        for (int x = 3; x < 7; x++) {
            for (int z = 3; z < 7; z++) {
                var plank = new PlanksOak() {
                    ModelMatrix = Matrix4x4.CreateTranslation(x, 2, z)
                };
                _opaqueGroup.Add(plank);
            }
        }

        // 8 柱原木 

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(3, y, 2)
            };
            _opaqueGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(2, y, 3)
            };
            _opaqueGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(6, y, 2)
            };
            _opaqueGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(7, y, 3)
            };
            _opaqueGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(7, y, 6)
            };
            _opaqueGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(6, y, 7)
            };
            _opaqueGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(2, y, 6)
            };
            _opaqueGroup.Add(logOak);
        }

        for (int y = 1; y < 7; y++) {
            var logOak = new LogOak {
                ModelMatrix = Matrix4x4.CreateTranslation(3, y, 7)
            };
            _opaqueGroup.Add(logOak);
        }

        // 其他木板与门前台阶
        {
            var plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(4, 2, 2)
            };
            _opaqueGroup.Add(plank);

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(5, 2, 2)
            };
            _opaqueGroup.Add(plank);

            for (int y = 5; y < 7; y++) {
                for (int x = 4; x < 6; x++) {
                    plank = new PlanksOak {
                        ModelMatrix = Matrix4x4.CreateTranslation(x, y, 2)
                    };
                    _opaqueGroup.Add(plank);
                }
            }

            for (int y = 2; y < 4; y++) {
                for (int z = 4; z < 6; z++) {
                    plank = new PlanksOak {
                        ModelMatrix = Matrix4x4.CreateTranslation(2, y, z)
                    };
                    _opaqueGroup.Add(plank);
                }
            }

            for (int y = 2; y < 4; y++) {
                for (int x = 4; x < 6; x++) {
                    plank = new PlanksOak {
                        ModelMatrix = Matrix4x4.CreateTranslation(x, y, 7)
                    };
                    _opaqueGroup.Add(plank);
                }
            }

            for (int y = 2; y < 4; y++) {
                for (int z = 4; z < 6; z++) {
                    plank = new PlanksOak {
                        ModelMatrix = Matrix4x4.CreateTranslation(7, y, z)
                    };
                    _opaqueGroup.Add(plank);
                }
            }

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(2, 6, 4)
            };
            _opaqueGroup.Add(plank);

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(2, 6, 5)
            };
            _opaqueGroup.Add(plank);

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(4, 6, 7)
            };
            _opaqueGroup.Add(plank);

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(5, 6, 7)
            };
            _opaqueGroup.Add(plank);

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(7, 6, 4)
            };
            _opaqueGroup.Add(plank);

            plank = new PlanksOak {
                ModelMatrix = Matrix4x4.CreateTranslation(7, 6, 5)
            };
            _opaqueGroup.Add(plank);

            var stair = new PlanksOakSoildStair {
                ModelMatrix = Matrix4x4.CreateTranslation(4, 2, 1)
            };
            _opaqueGroup.Add(stair);

            stair = new PlanksOakSoildStair {
                ModelMatrix = Matrix4x4.CreateTranslation(5, 2, 1)
            };
            _opaqueGroup.Add(stair);

            stair = new PlanksOakSoildStair {
                ModelMatrix = Matrix4x4.CreateTranslation(4, 1, 0)
            };
            _opaqueGroup.Add(stair);

            stair = new PlanksOakSoildStair {
                ModelMatrix = Matrix4x4.CreateTranslation(5, 1, 0)
            };
            _opaqueGroup.Add(stair);
        }

        // 4x4 木板房顶
        for (int x = 3; x < 7; x++) {
            for (int z = 3; z < 7; z++) {
                var plank = new PlanksOak {
                    ModelMatrix = Matrix4x4.CreateTranslation(x, 6, z)
                };
                _opaqueGroup.Add(plank);
            }
        }

        // 屋顶

        // 第一层
        for (int x = 3; x < 7; x++) {
            var stair = new PlanksOakSoildStair {
                ModelMatrix = Matrix4x4.CreateTranslation(x, 6, 1)
            };
            _opaqueGroup.Add(stair);
        }

        for (int x = 3; x < 7; x++) {
            // 旋转橡木台阶用的模型矩阵
            // 这里本来是可以不用 XMMatrixTranslation(-0.5, -0.5, -0.5) 平移到模型中心的
            // 因为作者本人 (我) 的设计失误，把模型坐标系原点建立在模型左下角了 (见上文的 VertexArray)
            // 导致还要先把原点平移到模型中心，旋转完再还原，增大计算量，这个是完全可以规避的
            // 读者可以自行修改 VertexArray，使方块以自身中心为原点建系，这样就可以直接 XMMatrixRotationY() 进行旋转了
            var transform = Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f);
            transform *= Matrix4x4.CreateRotationY(MathF.PI);                                      // 平移中心后，再旋转，否则会出错 (旋转角度是弧度)
            transform *= Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);         // 旋转完再还原
            transform *= Matrix4x4.CreateTranslation(x, 6, 8);                  // 再平移到对应的坐标

            var stair = new PlanksOakSoildStair {
                ModelMatrix = transform
            };
            _opaqueGroup.Add(stair);
        }

        for (int z = 3; z < 7; z++) {
            var transform = Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f);
            transform *= Matrix4x4.CreateRotationY(MathF.PI / 2.0f);            // 旋转 90°
            transform *= Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);
            transform *= Matrix4x4.CreateTranslation(1, 6, z);

            var stair = new PlanksOakSoildStair {
                ModelMatrix = transform
            };
            _opaqueGroup.Add(stair);
        }

        for (int z = 3; z < 7; z++) {
            var transform = Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f);
            transform *= Matrix4x4.CreateRotationY(MathF.PI + MathF.PI / 2.0f); // 旋转 270°
            transform *= Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);
            transform *= Matrix4x4.CreateTranslation(8, 6, z);

            var stair = new PlanksOakSoildStair {
                ModelMatrix = transform
            };
            _opaqueGroup.Add(stair);
        }

        // 第二层
        for (int x = 3; x < 7; x++) {
            var stair = new PlanksOakSoildStair {
                ModelMatrix = Matrix4x4.CreateTranslation(x, 7, 2)
            };
            _opaqueGroup.Add(stair);
        }

        for (int x = 3; x < 7; x++) {
            var transform = Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f);
            transform *= Matrix4x4.CreateRotationY(MathF.PI);
            transform *= Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);
            transform *= Matrix4x4.CreateTranslation(x, 7, 7);

            var stair = new PlanksOakSoildStair {
                ModelMatrix = transform
            };
            _opaqueGroup.Add(stair);
        }

        for (int z = 3; z < 7; z++) {
            var transform = Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f);
            transform *= Matrix4x4.CreateRotationY(MathF.PI / 2.0f);
            transform *= Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);
            transform *= Matrix4x4.CreateTranslation(2, 7, z);

            var stair = new PlanksOakSoildStair {
                ModelMatrix = transform
            };
            _opaqueGroup.Add(stair);
        }

        for (int z = 3; z < 7; z++) {
            var transform = Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f);
            transform *= Matrix4x4.CreateRotationY(MathF.PI + MathF.PI / 2.0f);
            transform *= Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);
            transform *= Matrix4x4.CreateTranslation(7, 7, z);

            var stair = new PlanksOakSoildStair {
                ModelMatrix = transform
            };
            _opaqueGroup.Add(stair);
        }

        // 补上屋顶空位
        for (int x = 3; x < 7; x++) {
            for (int z = 3; z < 7; z++) {
                var plank = new PlanksOak {
                    ModelMatrix = Matrix4x4.CreateTranslation(x, 7, z)
                };
                _opaqueGroup.Add(plank);
            }
        }


        // 工作台和熔炉
        var craftTable = new CraftingTable {
            ModelMatrix = Matrix4x4.CreateTranslation(3, 3, 6)
        };
        _opaqueGroup.Add(craftTable);

        var furnace = new Furnace {
            ModelMatrix = Matrix4x4.CreateTranslation(4, 3, 6)
        };
        _opaqueGroup.Add(furnace);

        furnace = new Furnace {
            ModelMatrix = Matrix4x4.CreateTranslation(5, 3, 6)
        };
        _opaqueGroup.Add(furnace);

        // 门
        var door = new Door {
            ModelMatrix = Matrix4x4.CreateTranslation(4, 3, 2)
        };
        _transparentGroup.Add(door);

        var mirrorDoor = new MirrorDoor {
            ModelMatrix = Matrix4x4.CreateTranslation(5, 3, 2)
        };
        _transparentGroup.Add(mirrorDoor);

        // 床
        var bed = new Bed {
            ModelMatrix = Matrix4x4.CreateTranslation(6, 3, 5)
        };
        _transparentGroup.Add(bed);

        // 玻璃
        for (int y = 4; y < 6; y++) {
            for (int z = 4; z < 6; z++) {
                var glass = new GlassLightBlue {
                    ModelMatrix = Matrix4x4.CreateTranslation(2, y, z)
                };
                _translucenceGroup.Add(glass);
            }
            for (int z = 4; z < 6; z++) {
                var glass = new GlassLightBlue {
                    ModelMatrix = Matrix4x4.CreateTranslation(7, y, z)
                };
                _translucenceGroup.Add(glass);
            }
        }

        for (int x = 4; x < 6; x++) {
            for (int y = 4; y < 6; y++) {
                var glass = new GlassLightBlue {
                    ModelMatrix = Matrix4x4.CreateTranslation(x, y, 7)
                };
                _translucenceGroup.Add(glass);
            }
        }

    }

    public void CreateModelResource(ID3D12Device4 d3d12Device) {
        var globalTextureGPUHandleMap = _textureSRVMap.ToDictionary(kv => kv.Key, kv => kv.Value.GPUHandle);
        foreach (var model in _opaqueGroup) {
            model.CreateResourceAndDescriptor(d3d12Device);
            model.BuildTextureGPUHandleMap(globalTextureGPUHandleMap);
        }

        foreach (var model in _transparentGroup) {
            model.CreateResourceAndDescriptor(d3d12Device);
            model.BuildTextureGPUHandleMap(globalTextureGPUHandleMap);
        }

        foreach (var model in _translucenceGroup) {
            model.CreateResourceAndDescriptor(d3d12Device);
            model.BuildTextureGPUHandleMap(globalTextureGPUHandleMap);
        }
    }

    public void RenderOpaqueModel(ID3D12GraphicsCommandList commandList) {
        foreach (var model in _opaqueGroup) {
            model.DrawModel(commandList);
        }
    }

    public void RenderTransparentModel(ID3D12GraphicsCommandList commandList) {
        foreach (var model in _transparentGroup) {
            model.DrawModel(commandList);
        }
    }

    public void RenderTranslucenceModel(ID3D12GraphicsCommandList commandList) {
        foreach (var model in _translucenceGroup) {
            model.DrawModel(commandList);
        }
    }
}
