# AGENTS.md

Инструкции для ИИ-агентов и разработчиков, которые меняют этот репозиторий. Описание игры и история обновлений в `README.md`.

## Проект

- Hobbie: 2D стратегия с RPG-элементами (деревня, жители, ресурсы, босс) на процедурно сгенерированной карте.
- Движок: Godot 4.7.1 .NET (`Godot.NET.Sdk/4.7.1`), .NET 8. Рендерер Mobile, `viewport/hdr_2d` включён.
- Языки: C# и GDScript вперемешку. Генерация мира, игрок, сетка строительства и инвентарь на C#. UI, жители, ресурсы, режим строительства и навигация на GDScript.
- Рабочая ветка `Godot-develop`, PR открываются в неё. Ветка `master` старая, в неё не коммитить.

## Как запускается игра

1. Главная сцена `world_environment.tscn` содержит только меню `UI/MainMenu/menu.tscn`.
2. «Новый мир»: `UI/MainMenu/generation_panel.gd` вызывает `GENERATOR.StartGeneration(полоса, имя, сид, размер)` и ждёт сигнал `GenerationFinished`. Шаги и сохранение `GeneratorV3.Generate` идут в `Task.Run`, этапы приходят сигналом `StageChanged` на экран загрузки.
3. Генератор сохраняет мир в `user://SavedWorlds/` (константа `GenerationSettings.SAVED_WORLDS_DIR`):
   - `<имя>.tres`: полные данные (`GeneratorData`);
   - `__SIMPLE<имя>.tres`: краткие данные для списка миров (`SimpleGeneratorData`);
   - `<имя>.tscn`: сцена мира (`WorldScene`).
4. «Мои миры» и «Продолжить»: `WorldStore.play()` (`UI/MainMenu/world_store.gd`) вызывает `Overlay.start_world()`, тот грузит сцену мира в потоке и вызывает `Game.ContinueRun` (есть сохранённая партия) или `Game.StartWorld(путь к .tscn, точка спавна)`. `GameManager` кладёт в корень дерева сцену мира (узел `Map`), `Player/Player.tscn` и HUD `Game/game_hud.tscn`, прячет меню. Пока на паузе грузятся чанки вокруг героя (`WorldScene.LoadedAround`, `LoadBudgetMs`), экран загрузки не закрывается. `Restart()` идёт через тот же `Overlay.start_world()`, `ToMenu()` убирает мир и возвращает меню.

Автозагрузки (`project.godot`): `BuildMode` (`BuildSystem/build_mode.tscn`), `MouseInfoPanel`, `GlobalNavigation` (`Navigation/navigation.tscn`), `GENERATOR` (`ProceduralGeneration/v3.1 BiggerSize/generator_v_3.1.tscn`), `Game` (`Game/GameManager.cs`), `Sound` (`Audio/SoundManager.cs`), `Overlay` (`UI/Overlay/overlay.tscn`: экран загрузки, пауза по Esc, настройки, окно подтверждения).

## Игровой цикл

`Game/GameManager.cs` (автозагрузка `Game`, `GameManager.Instance`) хранит состояние партии: склад (`Stock`, из GDScript через `GetStock(kind)`), здания, жителей, босса, `Ended`. Экономика вынесена в partial `Game/GameManager.Economy.cs`, её данные (цены, здания по уровням, оружие, инструменты, доспехи) в `Game/Economy.cs`.

1. Партия начинается с костра: `StartWorld` сам ставит центр поселения (`TownHall`, он же `MainBase`) рядом с точкой спавна.
2. Игрок добывает тайлы (`WorldScene.HarvestTile`), ресурс идёт на склад по `ResorseType` (`KindOf`, `YieldOf`) с учётом вместимости склада.
3. Меню строительства (B): вкладки «Постройки», «Деревня» (найм, работа жителей, ополчение через `ArmWorker`, `ArmNextWorker`), «Арсенал», «Рынок». `Grid.StartPlacement(id)`, ЛКМ ставит, ПКМ отменяет. Стены ставятся подряд, пока хватает ресурсов.
4. Победа: рассвет 31-го дня. Поражение: разрушен центр поселения (`MainBase`). Погибший герой возрождается у центра через 15 с. `EndGame` ставит дерево на паузу, HUD показывает итог.

Выживание ведёт `Game/Survival.cs` (узел `Survival`, `Game.Survival`): его создаёт `StartWorld`, убирает `Cleanup`. День берётся из `DayNightCycle` героя (`day_length_minutes = 6`). Каждую ночь (`night_started`) приходит волна мобов из словаря `Survival.Mobs`, бюджет растёт с днём; каждую 5-ю ночь вместе с волной приходит каменный гигант. Мобы роняют монеты (`Stock["coins"]`) и артефакты (`Survival.Artifacts`), их подбирает герой (`Game/Loot/loot_pickup.tscn`). Вооружённые жители (`AI/Village/villager_combat.gd`) по ночам охраняют главное здание, безоружные убегают к нему от врагов. HUD выживания: `Game/survival_hud.tscn`.

Партия сохраняется в `user://Runs/<имя мира>.json` на каждом рассвете, при выходе в меню и при закрытии окна, удаляется при победе или поражении. Код сохранения в `Game/GameManager.Survival.cs` (`SaveRun`, `HasRun`, `ContinueRun`), здания и экономика сохраняются через `SaveEconomy`/`LoadEconomy`. На время `ContinueRun` поднят флаг `Game.Restoring`, костёр тогда не ставится.

Здания наследуют `BuildSystem/scripts/building.gd` (`Building`: HP, уровень, `take_damage`, `heal`, сигнал `destroyed`, `get_center()`). Урон любому объекту наносится через `Game.Damage(target, amount)`. Группы: `village` (герой, жители, здания — цели мобов), `enemies` (мобы и гигант — цели героя, жителей и башен).

## Экономика

- Ресурсы: `wood`, `stone`, `iron`, `gold`, `food`, `coins` (`Economy.Resources`, названия в `Economy.ResourceTitles`). Всё, кроме монет, ограничено `Game.Capacity(kind)`: центр плюс склады. Добавлять только через `Game.AddResource(kind, n)` или `Game.AddCoins(n)`, тратить через `TrySpend(cost)`.
- Здания: `Economy.Buildings[id]` (сцена, размер в клетках, лимит по уровню центра, уровни с ценой, HP и эффектами). Уровень общий для всех зданий одного типа (`Game.GetLevel(id)`), улучшение стоит цену уровня × число зданий. Уровень центра (`CoreLevel`, 1–4: костёр, деревянная ратуша, каменная ратуша, замок) открывает новые здания и уровни, переход требует зданий из `Requires`.
- Сцены зданий лежат в `BuildSystem/buildings/`, у каждой `building_id`, `level_textures` (картинка на уровень, для меню и призрака при постройке) и `level_parts` (`BuildingParts`: земля и предметы здания по слоям, см. «Порядок отрисовки»). При `fit_texture = true` картинка масштабируется по ширине footprint и стоит на его нижнем краю. Стены на слое физики 6 (Walls): герой и жители проходят сквозь них.
- Утро (`day_started` у `DayNightCycle`): фермы дают еду, каждый житель съедает 1, рынок собирает налог. Без еды `Hungry` и жители работают вдвое медленнее.
- Кузница: инструменты (`ToolTier`, множитель `ToolSpeed` для добычи героя и `WorkSpeed` для жителей), доспех героя (`ArmorTier`, здоровье), оружие ближнего боя. Мастерская: оружие дальнего боя. Выкованное оружие лежит в арсенале (`Armory`, `TakeWeapon`, `ReturnWeapon`), статы для GDScript через `Game.GetWeaponStats(id)`. Урон героя `HeroDamage` берётся от `HeroWeapon`, здоровье `HeroMaxHealth` от доспеха, оба учитывают артефакты.
- Сохранение экономики: `SaveEconomy()` / `LoadEconomy(data)` (здания с клетками, уровнями и HP, тиры, арсенал; склад сохраняется отдельно).

## Структура

| Папка | Что внутри |
|---|---|
| `ProceduralGeneration/v3/` | Актуальный код генератора: `GeneratorV3.cs`, `WorldScene.cs`, `GeneratorData.cs`, шаги `steps/*.cs` (ресурсы `GenerationStep`). |
| `ProceduralGeneration/v3.1 BiggerSize/` | Сцена генератора (автозагрузка), чанки `ChunkData.cs`, тайлсет `DualTileMapV3.1/V3.1.tres`. |
| `ProceduralGeneration/v1`, `V2`, `DEBUG`, `scripts/` | Старые генераторы. Оставлены только потому, что на них ссылается `Light/Debug/light_v1.2.tscn`. Не использовать в новом коде. |
| `Resourses/` | Префабы ресурсов (деревья, камень, железо, золото) и слой окружения `v2/EnviromentLayer.tscn`. |
| `Player/` | `Player.tscn`, `PlayerMainCharacter.cs`. Старый `Player/scripts/player.gd` (`class_name Player`) не удалять: этот тип использует `BaseClasses/ScriptClasses/weapon_item.gd`. |
| `Game/` | `GameManager.cs` (состояние партии), `GameManager.Economy.cs` и `Economy.cs` (экономика), `GameManager.Survival.cs` и `Survival.cs` (выживание, сохранение партии), HUD `game_hud.tscn`. |
| `AI/` | `Village/worker.tscn` — житель, `Enemies/` — мобы волн (`mob.gd`, гоблин, волк, скелет-лучник, орк) и стрела, `BaseClasses/Enemy/stone_giant.tscn` — босс, `RL/` — RL-контроллеры, модели и сцены обучения. Старое: `AI/training.tscn`, `AI/Prefabs/v2/`. |
| `BuildSystem/` | Сетка (`Grid.cs`), меню строительства (`UI/BuildMenu.cs`), сцены зданий `buildings/`, скрипты `scripts/` (`building.gd`, `tower.gd`). Спрайты зданий в `Art/buildings/`. |
| `UI/` | Меню (`MainMenu/`), пауза, настройки и загрузка (`Overlay/`, настройки в `user://settings.cfg` через `GameSettings`), инвентарь (`UI/Inventory/*.cs`, хотбар на GDScript), тема `UI/theme.tres`, шрифт Kurland. Тема собирается из скина: `Skin/source/make_skin.py` рисует `Skin/*.png` и иконки `Icons/*.png`, `godot --headless --path . -s res://UI/Skin/source/build_theme.gd` пересобирает `theme.tres`. В сценах используй варианты темы (`PrimaryButton`, `HudPanel`, `HeaderLabel` и др.) вместо своих стилей. Окна в группе `closable_ui` закрываются по Esc раньше паузы. |
| `Light/` | Смена дня и ночи: `DayNight/day_night.tscn` вложена в `Player.tscn`. `DayNightCycle` (`DayNightCycle.instance`, `hour`, `day`, сигналы `hour_changed`, `night_started`, `day_started`, `new_day`; статическое `DayNightCycle.night` от 0 до 1) задаёт палитру по часам, облака и туман рисует шейдер `sky.gdshader`. Ночной фонарь `night_lamp.tscn` вешается на здания из `BuildSystem/buildings/` автоматически, на другие узлы через группу `night_lamp_host` или вручную. `Debug/` — старая отладочная сцена. |
| `Art/` | Своя графика игры: герой (`hero/`), мобы и гигант (`mobs/`), здания по уровням (`buildings/<id>_<уровень>.png`), ресурсы (`resources/`), оружие, стрела, монеты, артефакт (`items/`). `directional_sprite.gd` (`DirectionalSprite`) выбирает анимацию по направлению. Всё рисует генератор `tools/sprites/`. |
| `Audio/` | Звук: автозагрузка `SoundManager.cs`, фоновая мелодия и джинглы в `music/`, звуки в `sfx/`, их генератор `tools/synth.py`, источники и лицензии в `CREDITS.md`. |
| `Globals/` | `GenerationSettings.cs` (размер тайла 64, чанк 8 тайлов, путь сохранений), утилиты. |
| `BaseClasses/` | Базовые GDScript-классы сущностей, предметов и оружия. |
| `addons/` | `TileMapDual` (dual-grid тайлмапы), `AS2P`, `godot_rl_agents`. Сторонний код, правки только при необходимости. |
| `kenney_medieval-rts/`, `UI/UIAssets/` | Сторонние ассет-паки. Используется малая часть, остальное оставлено как запас. Новую графику брать из `Art/`. |
| `tools/` | Инструменты вне игры (`.gdignore`): `tools/sprites/` — генератор спрайтов на Python. |
| `docs/` | Гифки и видео для `README.md` (`.gdignore`): `docs/updates/<версия>.gif` и `.mp4`. |

Опечатки в именах (`Resourses`, `Enviroment`, `GDScrpt`, `CoatsLineStep`) исторические. Не переименовывать без необходимости: пути зашиты в сцены и код.

## Правила при изменениях

- Не трогать `.godot/`: это кэш редактора, он в `.gitignore`.
- Коммитить `.uid` рядом со скриптами и шейдерами и `.import` рядом с картинками. Godot ссылается на файлы по `uid://`, без них ссылки ломаются.
- Переносить и переименовывать файлы лучше в редакторе Godot: он обновит ссылки. Если переносишь вручную, поправь `path=` во всех `ext_resource` и сохрани прежний `uid`.
- Пути, зашитые строками, ищутся только поиском по тексту. Например, `ProceduralGeneration/v3/GenUtils.cs` грузит префабы `res://Resourses/Prefabs/*.tscn` по строкам, а `GameManager.cs` грузит `res://Player/Player.tscn`, `res://AI/Village/worker.tscn`, а пути сцен зданий заданы в `Game/Economy.cs`.
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

## Звук

Шины в `default_bus_layout.tres`: `Master` (с лимитером), `Music`, `SFX`. Автозагрузка `Sound` (`SoundManager.Instance` из C#) сама крутит фоновую мелодию, играет джингл в конце партии, шаги героя и щелчок любой кнопки.

- Звук в точке мира: `Sound.PlayAt("hit", global_position)`, без позиции: `Sound.Play("forge")`. Повтор не чаще раза в N секунд: `PlayEvery(id, pos, N)`. Звук добычи по типу ресурса: `HarvestSound(type)`.
- Громкостью управляют настройки игры через `AudioServer` по именам шин. Шины не переименовывать.
- Новый звук: функция в `Audio/tools/synth.py` и словарь `SFX` там же, строка в `Sfx` в `SoundManager.cs` (громкость в дБ и разброс высоты), строка в `Audio/CREDITS.md`. Чужие файлы только с лицензией, разрешающей коммерческий релиз.
- В геймплейный код звук добавляется одной строкой рядом с действием.

## Графика

Спрайты героя, мобов, зданий и предметов рисует генератор `tools/sprites/` (Python 3, numpy, Pillow): объёмные модели из примитивов, общая палитра `palette.py`, контур и тени в одном стиле. Как перегенерировать, в `tools/sprites/README.md`. Чужих ассетов в `Art/` нет.

- Персонажи смотрят в 8 сторон. Анимация называется `<действие>_<сторона>`, стороны `e se s sw w nw n ne` (по часовой от востока, как `Vector2.angle()`). Листы: строка на сторону, кадр на столбец; `SpriteFrames` лежит рядом (`<имя>_frames.tres`).
- `DirectionalSprite.play_dir(действие, направление, restart)` выбирает сторону и сохраняет кадр при повороте. Мобы делают то же в `mob.gd` (`_resolve`).
- Масштаб 1:1, клетка 64 px. Ноги персонажа в точке привязки кадра: герой, гоблин и скелет 64×64 (ноги на y = 52), волк 64×64 (y = 46), орк 96×96 (y = 78), гигант 128×128 (y = 110). Смерть и удар гиганта рисуются в увеличенном кадре с тем же центром.
- Здания шириной `footprint.x × 64`, низ спрайта на южном краю клетки.

## Порядок отрисовки

Объекты мира сортируются по Y в одном списке: `WorldScene` в `_Ready` включает `y_sort_enabled` у себя, у `EnviromentLayer` (деревья и камни) и у `Enviroment` (здания, жители, мобы, лут, герой). Герой тоже кладётся в `Enviroment`.

- `z_index`: 0 земля (`DualMap`), 1 земля под зданиями и лут, 2 всё, что стоит (`WorldScene.ObjectsZ`), выше эффекты и полоски HP. Сортировка по Y работает только внутри одного `z_index`.
- Точка сортировки = ноги. У героя корень узла стоит в ногах. У жителей и мобов корень выше ног, поэтому у корня включён `y_sort_enabled`, а `AnimatedSprite2D` сдвинут в ноги (`position`) и обратно рисуется через `offset`. Подписи и лучи у таких сцен на `z_index = 1`, иначе их закроют деревья.
- Деревья и камни: точка сортировки задана в тайлсете `Resourses/v2/TileSetResV2.tres` (`y_sort_origin` у тайла, основание ствола).
- Здания: генератор (`tools/sprites`) режет каждый уровень на землю `<id>_<ур>_ground.png` (рисуется на `z_index` 1 под всеми) и отдельные предметы в атласе `<id>_<ур>_parts.png`; `<id>_<ур>_parts.tres` хранит, где стоит каждый предмет и строку его переднего края. `building.gd` создаёт по `Sprite2D` на предмет, поэтому герой может стоять за палаткой и перед костром одного здания.
- Маска героя: `shaders/hero_reveal.gdshaderinc`, глобальный uniform `hero_position` (объявлен в `project.godot`, его каждый кадр ставит `PlayerMainCharacter`). Объект, который стоит перед героем (его `MODEL_MATRIX` origin ниже ног героя), становится полупрозрачным в овале вокруг героя. Включено у деревьев и камней (`EnviromentLayer.gdshader`) и у зданий (`reveal_hero` в `shaders/outline.gdshader`, его ставит `building.gd`). Жители и мобы маску не получают и не вызывают.

## Input Map

Действия заданы в `project.godot`: `ui_left/right/up/down` (WASD и стрелки), `LeftMouseButton`, `RightMouseButton`, `action` (E), `attack` (F), `inventory` (Tab), `HotBar` (1–4), `menu` (B), `zoom+`/`zoom-` (Z/X), `ESC`, `DEBUG` (Alt+9), `test` (L). В коде используй эти имена, новые добавляй туда же.

Слои физики: 1 World, 2 Player, 3 Enemy, 4 Resourses, 5 Buildings, 6 Walls.

## RL

NPC не используют `NavigationAgent2D`: каждого ведёт `AIController2D` из аддона `godot_rl_agents`, движение прямое, без навмеша.

- `AI/RL/npc_controller.gd` (`NPCController`): база. Вне обучения (`heuristic == "human"`) сам раз в `decision_interval` кадров берёт действие из ONNX-модели (`onnx_model_path`), если она есть, иначе из `heuristic_action()`. `deterministic = false` сэмплирует действие из softmax, как при обучении.
- `AI/RL/worker_controller.gd`: житель по контракту Collector из RLStudy, то есть модель `model_v9` подходит без переобучения. Наблюдение 23 числа: позиция в зоне 3840×3840 вокруг ратуши / 3840, флаг «стою на ресурсе», 5 лучей в конусе 145° на 1000 px по `[стена, ресурс, касание, дистанция]`. Действие `rotate` из 5: вперёд, назад, поворот +, поворот −, стоп/сбор. Сбор: 2.94 с на клетке. Несёт добычу в ратушу житель по скрипту, сеть только ищет и собирает.
- `AI/RL/models/collector_v9.onnx`: `model_v9`, сохранённый одним файлом (ONNX-модели только одним файлом, см. `BUILD.md`).
- `AI/RL/boss_controller.gd`: босс. Наблюдение 12 чисел, действия `move` (непрерывное 2) и `attack` (дискретное 3: ничего, удар, лазер). Модели пока нет, работает эвристика.
- `AI/RL/mob_controller.gd` (`MobController`): общий для всех мобов волн. Наблюдение 17 чисел: цель (x, y, зазор до края, здание ли, в досягаемости), главное здание (x, y), ближайший живой защитник (x, y), ближайший союзник (x, y), доля HP, атака готова, дальний бой, дальность атаки / 1000, скорость / 300, ночь. Векторы относительные, делённые на 1000 и обрезанные до длины 1. Действия `move` (непрерывное 2) и `attack` (дискретное 2). Награда: +урон/10, −полученный урон/20, −2 за смерть, плюс за сближение с целью. Моделей пока нет, работает эвристика: ближний бой идёт к цели, лучник держит дистанцию.
- Обучение мобов: `AI/RL/mob_training.tscn` (узел `Sync`, `mobs_per_scene` мобов каждого типа из `mob_scenes` против `defender_count` вооружённых жителей у ратуши, ратуша не разрушается). Погибший моб возвращается на кольцо вокруг ратуши, эпизод длится `episode_frames` кадров.
- Обучение жителей: `AI/RL/worker_training.tscn` (узел `Sync`, 8 жителей у ратуши на сохранённом мире `world_name`, пустое имя значит первый найденный; добытые тайлы восстанавливаются каждые `restore_interval` с). Сначала запусти сервер обучения (`gdrl`), потом сцену. Без сервера сцена идёт на эвристике.
- Обучение идёт из Python (godot-rl + Stable-Baselines3) вне этого репозитория. Чтобы поменять модель, положи `.onnx` в `AI/RL/models/` и укажи путь в `onnx_model_path` контроллера в сцене NPC.
