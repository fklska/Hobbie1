# Обучение RL-агентов

Как обучить модели для NPC и подключить их к игре. Устройство контроллеров описано в `AGENTS.md` (раздел «RL»), упаковка моделей в сборку в `BUILD.md` (раздел «ONNX Runtime»).

## Сколько моделей нужно

| Кто | Контроллер | Модель | Сцена обучения |
|---|---|---|---|
| Жители | `AI/RL/worker_controller.gd` | `AI/RL/models/collector_v9.onnx`, уже в игре | `AI/RL/worker_training.tscn` |
| Мобы волн: гоблин, волк, скелет-лучник, орк | `AI/RL/mob_controller.gd` | одна общая на всех четверых, пока нет | `AI/RL/mob_training.tscn` |
| Каменный гигант | `AI/RL/boss_controller.gd` | нет | нет |

Для мобов волн нужна одна модель. У всех четырёх один `MobController`: одни и те же 17 чисел наблюдения и одни и те же действия. Чем моб отличается (дальний бой, дальность атаки, скорость), сеть видит в наблюдении. Сцена `mob_training.tscn` гоняет всех четверых сразу, и Stable-Baselines3 учит их одной общей политикой.

Без модели каждый NPC работает на эвристике (`heuristic_action()`), поэтому игра полностью играбельна и без новых моделей. На iOS модели не грузятся вообще (см. `BUILD.md`).

Гигант обучать пока нельзя: нет сцены обучения, а его действие `attack` (дискретное из 3) вместе с непрерывным `move` Stable-Baselines3 через godot-rl не принимает, он умеет смешивать с непрерывными только дискретные действия из 2. Чтобы учить гиганта, `attack` придётся разбить на два действия по 2 (удар, лазер) и сделать сцену по образцу `mob_training.tscn`.

## Подготовка (один раз)

1. Python 3.10 или новее. Окружение для обучения, например в папке `RLStudy/python-side`:

   ```
   python -m venv .venv
   .venv\Scripts\activate
   pip install godot-rl onnxscript
   ```

   На Linux и macOS вторая строка `source .venv/bin/activate`.

   `godot-rl` ставит Stable-Baselines3, PyTorch, `onnx`, `onnxruntime` и TensorBoard. `onnxscript` нужен новому экспорту ONNX в PyTorch: без него обучение доходит до конца и падает на экспорте, а `.zip` модели тоже не сохраняется.
2. Скрипт обучения `stable_baselines3_example.py`. Он уже лежит в `RLStudy/python-side` (им учили `model_v9`), оригинал в репозитории godot_rl_agents: `examples/stable_baselines3_example.py`.
3. Хотя бы один сохранённый мир: запусти игру и создай его через «Новый мир». Сцены обучения берут мир из `user://SavedWorlds/`: с именем из поля `world_name` или первый найденный, если поле пустое. Лучше маленький, он быстрее грузится.
4. Собери C#: `dotnet build Godot1.csproj` или кнопка Build в редакторе.

## Обучение мобов

1. Запусти сервер обучения в папке со скриптом:

   ```
   python stable_baselines3_example.py --experiment_name=mobs_v1 --timesteps=2000000 --save_model_path=mobs_v1.zip --onnx_export_path=mobs_v1.onnx --save_checkpoint_frequency=200000
   ```

   Дождись строки `waiting for remote GODOT connection on port 11008`.
2. Запусти сцену `AI/RL/mob_training.tscn`, пока сервер ждёт. Два способа:
   - В редакторе: открой сцену и нажми F6 (Run Current Scene). Видно, что происходит, но окно тормозит обучение.
   - Без окна, быстрее. Путь к консольному exe Godot и папке проекта свои:

     ```
     Godot_v4.7.1-stable_mono_win64_console.exe --path C:\путь\к\Hobbie1 --scene res://AI/RL/mob_training.tscn --headless --speedup=8
     ```

     `--speedup` ускоряет игровое время. 8 стоит в сцене по умолчанию, можно попробовать 16, если процессор успевает.

   Если сцена запустилась раньше сервера, она не подключится и пойдёт на эвристике. Тогда закрой её и запусти снова.
3. Следи за обучением: `tensorboard --logdir logs/sb3`, график `rollout/ep_rew_mean` должен расти. В облачной проверке 20 тысяч шагов прошли примерно за 1,5 минуты без окна, то есть 2 миллиона шагов займут несколько часов. Это ориентир, на твоём ПК скорость будет своя.
4. Остановить можно в любой момент: Ctrl+C в консоли Python. Скрипт всё равно сохранит `mobs_v1.zip` и выгрузит `mobs_v1.onnx`, Godot закроется сам. Продолжить обучение с сохранённого места: добавь `--resume_model_path=mobs_v1.zip` и новое `--experiment_name`.

Что настраивается в `mob_training.tscn` (корневой узел): `mobs_per_scene` (по умолчанию 4 моба каждого типа, всего 16 агентов), `defender_count` и `defender_weapons` (вооружённые жители у ратуши, они не учатся и возрождаются), `episode_frames` (длина эпизода), `night`. На узле `Sync`: `action_repeat = 8` (как часто агент выбирает действие) и `speed_up`.

## Подключение модели мобов к игре

1. Собери модель в один файл и положи в проект (почему так, в `BUILD.md`):

   ```
   python -c "import onnx,sys; onnx.save_model(onnx.load(sys.argv[1]), sys.argv[2])" mobs_v1.onnx <путь к Hobbie1>/AI/RL/models/mobs_v1.onnx
   ```

2. В каждой из четырёх сцен `AI/Enemies/goblin.tscn`, `wolf.tscn`, `skeleton_archer.tscn`, `orc.tscn` выбери узел `AIController2D` и задай:
   - `Onnx Model Path` = `res://AI/RL/models/mobs_v1.onnx`;
   - `Decision Interval` = 8, как `action_repeat` при обучении.
3. Проверь: запусти `mob_training.tscn` без сервера (F6). Сцена не подключится к Python, и мобы пойдут на модели, как в игре. Или дождись ночи в обычной партии.

Модель, выгруженная Stable-Baselines3, отдаёт для моба 3 числа: `move` x, y и `attack` (больше 0 значит бить). `NPCController` сам узнаёт такую модель по размеру выхода, отдельно ничего настраивать не нужно.

## Обучение жителей

Так же, как мобов, только сцена `AI/RL/worker_training.tscn` и свои имена файлов (`--experiment_name=workers_v10` и так далее). Восемь жителей собирают ресурс текущей работы деревни (`Game.WorkerJob`), добытые клетки возвращаются каждые `restore_interval` секунд. Готовую модель собери в один файл и укажи в `onnx_model_path` узла `AIController2D` в `AI/Village/worker.tscn`. Сейчас там `decision_interval = 1` под `collector_v9.onnx`; для модели из `worker_training.tscn` поставь 8, как `action_repeat` в этой сцене. Переучивать жителей не обязательно: `collector_v9.onnx` уже работает.

Наблюдение и действия жителя совпадают с Collector из RLStudy, поэтому модели оттуда подходят как есть. Если поменять `get_obs()` или `get_action_space()` у контроллера, старые модели перестанут подходить и обучать придётся заново.
