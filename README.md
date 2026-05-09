# Vampire Crawlers Mod

一个基于 BepInEx 6 IL2CPP 的《Vampire Crawlers》功能增强 Mod。当前项目主要提供手牌整理、自动打出限制、卡牌碎裂剩余次数提示等战斗界面改进。

## 功能

- 手牌整理按钮
  - 战斗界面显示“整理手牌”按钮。
  - 左键点击后整理当前手牌。
  - 按住右键可拖动按钮位置，位置会保存到配置文件。
- 手牌自动整理
  - 抽牌后可自动整理手牌。
  - 可配置整理延迟。
  - 可配置出牌后自动整理抑制时间，降低出牌效果抽牌时误点的风险。
- 手牌排序规则
  - 支持按费用升序或降序。
  - 支持万能牌放在最左或最右。
  - 同费用时按卡牌名排序。
  - “本次免费打出”卡牌按卡面/连击使用的原本费用排序，不按实际 0 消耗排序。
- 自动打出限制
  - 可限制自动打出只打万能牌。
  - 可跳过碎裂剩余次数为 1 的牌。
  - 自动打出万能牌时支持优先级：临时牌、销毁牌、无裂纹牌、法力牌、攻击牌、其他牌。
- 碎裂剩余次数提示
  - 在卡牌说明中显示还剩几次会碎裂。
  - 提示颜色可配置。

## 环境要求

- Windows
- Steam 版《Vampire Crawlers》
- BepInEx 6 IL2CPP
- .NET SDK 6 或更高版本

当前项目默认游戏目录通过仓库根目录的 `game/` 符号链接指向：

```text
C:\Programs\Steam\steamapps\common\Vampire Crawlers
```

项目引用的 BepInEx 和游戏程序集路径来自：

```text
game\BepInEx\core
game\BepInEx\interop
```

## 安装

1. 先给游戏安装 BepInEx 6 IL2CPP。
2. 启动一次游戏，让 BepInEx 生成 `BepInEx\interop` 目录。
3. 构建本项目：

```powershell
dotnet build .\src\VampireCrawlersMod\VampireCrawlersMod.csproj -c Debug --no-restore
```

构建成功后，项目会自动复制插件到：

```text
game\BepInEx\plugins\VampireCrawlersMod.dll
```

如果复制失败，通常是游戏仍在运行并锁定了 DLL。关闭游戏后重新构建。

## 配置

首次启动游戏后，BepInEx 会生成配置文件：

```text
game\BepInEx\config\ttat.vampirecrawlers.mod.cfg
```

常用配置项：

```ini
[HandSortButton]
ReferenceX = 236
ReferenceY = 835

[HandSort]
AutoSortAfterDraw = true
AutoSortDelaySeconds = 1
AutoSortSuppressAfterPlaySeconds = 1.5
SortCostAscending = true
WildCardsOnLeft = false

[AutoPlay]
OnlyPlayWildCards = true
SkipOneBreakRemainingCards = true

[CardBreakCountdown]
TextColor = #00ff66
```

说明：

- `SortCostAscending`：`true` 为费用从小到大，`false` 为从大到小。
- `WildCardsOnLeft`：`true` 为万能牌放最左，`false` 为放最右。
- `OnlyPlayWildCards`：自动打出只允许打出万能牌。
- `SkipOneBreakRemainingCards`：自动打出跳过碎裂剩余次数为 1 的牌。
- `TextColor`：碎裂剩余次数提示颜色，支持 `#RRGGBB` 或 `#RRGGBBAA`。

## 项目结构

```text
src/VampireCrawlersMod/        Mod 源码
docs/开发记录.md               开发记录和实现注意事项
GameSourceCode/                反编译源码，用于查证游戏逻辑
DummyDll/                      Il2CppDumper 生成的引用程序集
Mod本体/                       打包或发布相关文件
game/                          指向本机游戏目录的符号链接
```

核心源码：

- `Plugin.cs`：BepInEx 插件入口。
- `HandSortButtonController.cs`：整理按钮、手牌排序、抽牌后自动整理。
- `AutoPlayFilter.cs`：自动打出过滤和优先级。
- `CardBreakCountdownDisplay.cs`：碎裂剩余次数显示。
- `CardBreakCountdownPatches.cs`：Harmony patch 入口。
- `CardRules.cs`：卡牌规则判断工具。

## 开发说明

- 游戏为 IL2CPP，不能完全依赖普通 C# 类型判断。判断运行时实际子类型时优先使用 `TryCast<T>()`。
- 游戏启用了新版 Input System，Mod 中不要调用 `UnityEngine.Input`。
- 需要挂到 Unity 对象上的 `MonoBehaviour` 类型要先通过 `ClassInjector.RegisterTypeInIl2Cpp<T>()` 注册。
- UI 属性写入可能触发 Canvas 重建，应避免每帧重复写相同值。
- 日志建议使用英文或 ASCII，避免 BepInEx 控制台编码导致中文乱码。
- 更多细节见 [docs/开发记录.md](docs/开发记录.md)。

## 构建

```powershell
dotnet build .\src\VampireCrawlersMod\VampireCrawlersMod.csproj -c Debug --no-restore
```

`VampireCrawlersMod.csproj` 中配置了 `CopyPluginToGame` 目标，构建后会自动部署 DLL 到游戏插件目录。

## 免责声明

本项目是非官方 Mod，与《Vampire Crawlers》开发者、发行商和 Steam 无关联。使用前建议备份存档和配置文件。
