# AGENTS.md

Инструкции для ИИ-агентов и разработчиков, которые меняют этот репозиторий. Описание игры и история обновлений в `README.md`.

## Проект

- Hobbie: 2D стратегия с RPG-элементами (деревня, жители, ресурсы, босс) на процедурно сгенерированной карте.
- Движок: Godot 4.7.1 .NET (`Godot.NET.Sdk/4.7.1`), .NET 8. Рендерер Mobile, `viewport/hdr_2d` включён.
- Языки: C# и GDScript вперемешку. Генерация мира, игрок, сетка строительства и инвентарь на C#. UI, жители, ресурсы, режим строительства и навигация на GDScript.
- Рабочая ветка `Godot-develop`, PR открываются в неё. Ветка `master` старая, в неё не коммитить.

## Как запускается игра

1. Главная сцена `world_environment.tscn` содержит только меню `UI/MainMenu/menu.tscn`.
2. «Новый мир»: `UI/MainMenu/GenerationPanel.cs` берёт автозагрузку `GENERATOR` (`GeneratorV3`) и вызывает `Generate()`.
3. Генератор сохраняет мир в `user://SavedWorlds/` (константа `GenerationSettings.SAVED_WORLDS_DIR`):
   - `<имя>.tres`: полные данные (`GeneratorData`);
   - `__SIMPLE<имя>.tres`: краткие данные для списка миров (`SimpleGeneratorData`);
   - `<имя>.tscn`: сцена мира (`WorldScene`).
4. «Загрузить мир»: `UI/prefabs/world_list_item.gd` вызывает `Game.StartWorld(путь к .tscn, точка спавна)`. `GameManager` кладёт в корень дерева сцену мира (узел `Map`), `Player/Player.tscn` и HUD `Game/game_hud.tscn`, прячет меню. `Restart()` и `ToMenu()` убирают всё это и начинают заново.

Автозагрузки (`project.godot`): `BuildMode` (`BuildSystem/build_mode.tscn`), `MouseInfoPanel`, `GlobalNavigation` (`Navigation/navigation.tscn`), `GENERATOR` (`ProceduralGeneration/v3.1 BiggerSize/generator_v_3.1.tscn`), `Game` (`Game/GameManager.cs`).

## Игровой цикл

`Game/GameManager.cs` (автозагрузка `Game`, `GameManager.Instance`) хранит состояние партии: склад (`Stock`, из GDScript через `GetStock(kind)`), здания, жителей, босса, `SwordForged`, `Ended`. Цены и здания заданы словарями `Costs` и `Buildings` в начале файла.

1. Игрок добывает тайлы (`WorldScene.HarvestTile`), ресурс идёт на склад по `ResorseType` (`KindOf`, `YieldOf`).
2. Ратуша и кузница ставятся из меню строительства (B): `Grid.StartPlacement(id)`, ЛКМ ставит, ПКМ отменяет.
3. Вкладка «Деревня»: найм жителей (до `MaxWorkers`), выбор добываемого ресурса, ковка меча (урон героя ×3), призыв босса.
4. Победа: убит каменный гигант. Поражение: погиб герой или разрушена ратуша. `EndGame` ставит дерево на паузу, HUD показывает итог.

Здания наследуют `BuildSystem/scripts/building.gd` (`Building`: HP, `take_damage`, сигнал `destroyed`, `get_center()`). Урон любому объекту наносится через `Game.Damage(target, amount)`. Группы: `village` (герой, жители, здания — цели босса), `enemies` (цели атаки героя).

## Структура

| Папка | Что внутри |
|---|---|
| `ProceduralGeneration/v3/` | Актуальный код генератора: `GeneratorV3.cs`, `WorldScene.cs`, `GeneratorData.cs`, шаги `steps/*.cs` (ресурсы `GenerationStep`). |
| `ProceduralGeneration/v3.1 BiggerSize/` | Сцена генератора (автозагрузка), чанки `ChunkData.cs`, тайлсет `DualTileMapV3.1/V3.1.tres`. |
| `ProceduralGeneration/v1`, `V2`, `DEBUG`, `scripts/` | Старые генераторы. Оставлены только потому, что на них ссылается `Light/Debug/light_v1.2.tscn`. Не использовать в новом коде. |
| `Resourses/` | Префабы ресурсов (деревья, камень, железо, золото) и слой окружения `v2/EnviromentLayer.tscn`. |
| `Player/` | `Player.tscn`, `PlayerMainCharacter.cs`, спрайты. Старый `Player/scripts/player.gd` (`class_name Player`) не удалять: этот тип использует `BaseClasses/ScriptClasses/weapon_item.gd`. |
| `Game/` | `GameManager.cs` (состояние партии) и HUD `game_hud.tscn`. |
| `AI/` | `Village/worker.tscn` — житель, `BaseClasses/Enemy/stone_giant.tscn` — босс, `RL/` — RL-контроллеры, модели и сцена обучения жителей. Старое: `AI/training.tscn`, `AI/Prefabs/v2/`. |
| `BuildSystem/` | Сетка (`Grid.cs`), режим строительства, ратуша, кузница, склад. |
| `UI/` | Меню, инвентарь (`UI/Inventory/*.cs`, хотбар на GDScript), тема `UI/theme.tres`, шрифт Kurland. |
| `Light/` | Смена дня и ночи: `DayNight/day_night.tscn` вложена в `Player.tscn`. `DayNightCycle` (`DayNightCycle.instance`, `hour`, `day`, сигналы `hour_changed`, `night_started`, `day_started`, `new_day`; статическое `DayNightCycle.night` от 0 до 1) задаёт палитру по часам, облака и туман рисует шейдер `sky.gdshader`. Ночной фонарь `night_lamp.tscn` вешается на здания из `BuildSystem/buildings/` автоматически, на другие узлы через группу `night_lamp_host` или вручную. `Debug/` — старая отладочная сцена. |
| `Globals/` | `GenerationSettings.cs` (размер тайла 64, чанк 8 тайлов, путь сохранений), утилиты. |
| `BaseClasses/` | Базовые GDScript-классы сущностей, предметов и оружия. |
| `addons/` | `TileMapDual` (dual-grid тайлмапы), `AS2P`, `godot_rl_agents`. Сторонний код, правки только при необходимости. |
| `kenney_medieval-rts/`, `UI/UIAssets/` | Сторонние ассет-паки. Используется малая часть, остальное оставлено как запас. |

Опечатки в именах (`Resourses`, `Enviroment`, `GDScrpt`, `CoatsLineStep`) исторические. Не переименовывать без необходимости: пути зашиты в сцены и код.

## Правила при изменениях

- Не трогать `.godot/`: это кэш редактора, он в `.gitignore`.
- Коммитить `.uid` рядом со скриптами и шейдерами и `.import` рядом с картинками. Godot ссылается на файлы по `uid://`, без них ссылки ломаются.
- Переносить и переименовывать файлы лучше в редакторе Godot: он обновит ссылки. Если переносишь вручную, поправь `path=` во всех `ext_resource` и сохрани прежний `uid`.
- Пути, зашитые строками, ищутся только поиском по тексту. Например, `ProceduralGeneration/v3/GenUtils.cs` грузит префабы `res://Resourses/Prefabs/*.tscn` по строкам, а `GameManager.cs` грузит `res://Player/Player.tscn`, `res://AI/Village/worker.tscn` и сцены зданий.
- Перед удалением файла проверь, что на него нет ссылок: по `uid://` из его `.uid`/`.import`, по `res://` пути, по `class_name` (GDScript) и по имени класса (C#). Все `.cs` компилируются в одну сборку, поэтому неиспользуемый C#-класс, на который ссылается другой `.cs`, удалять нельзя.
- Сохранения пишутся только в `user://`. В экспортированной игре `res://` доступен только для чтения.
- Новые C#-классы, которые вешаются на узлы, делать `partial` и наследовать от типа узла, как в существующем коде.
- Исходники графики (`.psd`, `.pxo`) хранятся рядом с картинками. В билд они не попадают.

## Проверка

Godot в CI нет. Минимум перед PR:

```
dotnet build Godot1.csproj
```

Если есть Godot 4.7.1 .NET:

```
godot --headless --import
godot --headless --path . --quit
```

Сборка и экспорт описаны в `BUILD.md`, пресет в `export_presets.cfg`.

## Input Map

Действия заданы в `project.godot`: `ui_left/right/up/down` (WASD и стрелки), `LeftMouseButton`, `RightMouseButton`, `action` (E), `attack` (F), `inventory` (Tab), `HotBar` (1–4), `menu` (B), `zoom+`/`zoom-` (Z/X), `ESC`, `DEBUG` (Alt+9), `test` (L). В коде используй эти имена, новые добавляй туда же.

Слои физики: 1 World, 2 Player, 3 Enemy, 4 Resourses, 5 Buildings.

## RL

NPC не используют `NavigationAgent2D`: каждого ведёт `AIController2D` из аддона `godot_rl_agents`, движение прямое, без навмеша.

- `AI/RL/npc_controller.gd` (`NPCController`): база. Вне обучения (`heuristic == "human"`) сам раз в `decision_interval` кадров берёт действие из ONNX-модели (`onnx_model_path`), если она есть, иначе из `heuristic_action()`. `deterministic = false` сэмплирует действие из softmax, как при обучении.
- `AI/RL/worker_controller.gd`: житель по контракту Collector из RLStudy, то есть модель `model_v9` подходит без переобучения. Наблюдение 23 числа: позиция в зоне 3840×3840 вокруг ратуши / 3840, флаг «стою на ресурсе», 5 лучей в конусе 145° на 1000 px по `[стена, ресурс, касание, дистанция]`. Действие `rotate` из 5: вперёд, назад, поворот +, поворот −, стоп/сбор. Сбор: 2.94 с на клетке. Несёт добычу в ратушу житель по скрипту, сеть только ищет и собирает.
- `AI/RL/models/collector_v9.onnx`: `model_v9`, сохранённый одним файлом (ONNX-модели только одним файлом, см. `BUILD.md`).
- `AI/RL/boss_controller.gd`: босс. Наблюдение 12 чисел, действия `move` (непрерывное 2) и `attack` (дискретное 3: ничего, удар, лазер). Модели пока нет, работает эвристика.
- Обучение жителей: `AI/RL/worker_training.tscn` (узел `Sync`, 8 жителей у ратуши на сохранённом мире `world_name`, пустое имя значит первый найденный; добытые тайлы восстанавливаются каждые `restore_interval` с). Сначала запусти сервер обучения (`gdrl`), потом сцену. Без сервера сцена идёт на эвристике.
- Обучение идёт из Python (godot-rl + Stable-Baselines3) вне этого репозитория. Чтобы поменять модель, положи `.onnx` в `AI/RL/models/` и укажи путь в `onnx_model_path` контроллера в сцене NPC.
