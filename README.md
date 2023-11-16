# HexPainter
---
用于快速在场景中布置六边形地块的一个工具，操作方式类似PS笔刷

![565c8e536a6510926d66a58ca0812c7c.gif](https://pic.matrix64.xyz:7001/images/565c8e536a6510926d66a58ca0812c7c.gif)

[![6e07cd0f57cf8cc7df998d3147b4d5e1.md.png](https://pic.matrix64.xyz:7001/images/6e07cd0f57cf8cc7df998d3147b4d5e1.md.png)](https://pic.matrix64.xyz:7001/image/B7A)


## Features
---
- 独立的工作窗口
- prefab交互直接拖拽即可
- 自动精确定位六边形坐标
- 自动按组合并同类prefab
- 支持 `替换` 和 `重创建` 两种工作模式
- prefab的增删切换都可以用键鼠快捷键完成，美术友好

其中， `替换` 模式是指unity2022.2更新的PrefabSystem功能，详情见 [What’s new for Prefabs in 2022.2? | Unity Blog](https://blog.unity.com/engine-platform/prefabs-whats-new-2022-2)， `重创建` 模式就是单纯的删除原本物体并创建新物体

脚本默认处于 `替换` 模式，好处是替换后的prefab的GUID不会发生变化，脚本引用等不会丢失



## Installation
---
找到 `manifest.json` 文件添加下面内容：
```
{
  "dependencies": {
    "xyz.matrix64works.hexpainter":"https://github.com/Matrix64/HexPainter.git",
    ...
  },
}
```



## First Usage
---
1. 找到 `ArtTools/Hex Painter` 菜单打开工作窗口
2. 在Unity中新增一个 `HexTile` 层
3. 拖拽目标Prefab到HexPainter窗口下方的提示框内,并选择其中一个prefab激活
4. 设置好间距和旋转角度后，点击 `Start Drawing`即可


## Controls
---
1. 点击鼠标左键在当前位置创建一个新的hex对象
2. 按住Shift键并点击鼠标左键删除绘制的hex对象
3. 按住Control键并点击鼠标左键使用鼠标点击位置已有的hex对象作为当前笔刷对象



## Troubleshooting
---
- 当没有prefab激活时，不会进入绘画模式
- 每次绘制时会自动生成 `prefab名`+ `_HexGroup` 的一个空物体作为容器，并且后续的操作都需要基于这个后缀名，如果这个后缀名被删除则整个组内物体都将不再被这个脚本识别，反过来说，如果实现制作了一部分prefab，想要用这个脚本继续进行编辑，只需要将原本的物体放在一个 `任意名称`+`_HexGroup` 的空物体下即可
- 这个脚本设计的初衷只可以操作 `Prefab`，如果不是Prefab则不支持
- 每个Prefab需要有 `碰撞` 才可以正确被删除操作等识别，没有 `碰撞` 时会在绘制中产生大量重复物体