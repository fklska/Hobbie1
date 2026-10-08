# Сборка

## Что нужно

- Godot 4.7.1 .NET (mono) и шаблоны экспорта той же версии (`Editor → Manage Export Templates`).
- .NET SDK 8.

## Windows

В редакторе: `Project → Export → Windows Desktop → Export Project`.

Из консоли:

```
godot --headless --import
godot --headless --export-release "Windows Desktop" build/windows/Hobbie.exe
```

Результат в `build/windows/` (папка в .gitignore):

- `Hobbie.exe`, `Hobbie.pck` — движок и ресурсы игры;
- `data_Godot1_windows_x86_64/` — C#-сборка `Godot1.dll`, рантайм .NET и ONNX Runtime. Без этой папки игра не запустится.

Пресет хранится в `export_presets.cfg`. Пароли и сертификаты подписи редактор пишет в `.godot/export_credentials.cfg`, он в репозиторий не попадает.

## Что не попадает в билд

`exclude_filter` в пресете убирает: `*.tmp`, `*.old`, `*.psd`, `Thumbs.db`, папки `DEBUG`/`Debug`, `Debugging/`, старые генераторы (`v1`, `V2`, `scripts/generator.gd`). Ни один из этих файлов не используется главной сценой и автозагрузками.

Если сцена из этих папок понадобится в игре, убери её шаблон из фильтра, иначе в билде она не загрузится.

## ONNX Runtime

Пакет `Microsoft.ML.OnnxRuntime` подключён в `Godot1.csproj`. При экспорте Godot запускает `dotnet publish -c ExportRelease -r win-x64 --self-contained true`, и NuGet кладёт в результат `Microsoft.ML.OnnxRuntime.dll` и нативные `onnxruntime.dll` (~13 МБ), `onnxruntime_providers_shared.dll`. Godot копирует их в `data_Godot1_windows_x86_64/`. Отдельно ничего ставить не нужно.

Сами модели `.onnx` не ресурсы Godot, поэтому в пресете стоит `include_filter="*.onnx"`: без него модель не попадёт в `.pck` и `ONNXModel` не найдёт файл.

Модель должна быть одним файлом. Модели из RLStudy (PyTorch 2.13) сохранены с весами в отдельном `<имя>.onnx.data`. `ONNXInference.cs` передаёт модель в ONNX Runtime как массив байт, и тогда рантайм ищет `.data` в рабочей папке процесса через ОС, а не рядом с моделью и не внутри `.pck`. В экспортированной игре такая модель не загрузится, в редакторе загрузится только из корня проекта. Перед тем как класть модель в проект, собери её в один файл:

```
python -c "import onnx,sys; onnx.save_model(onnx.load(sys.argv[1]), sys.argv[2])" model_v9.onnx model_v9_full.onnx
```

Путь в `onnx_model_path` указывай через `res://`.
