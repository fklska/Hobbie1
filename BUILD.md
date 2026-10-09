# Сборка

В `export_presets.cfg` три пресета: `Windows Desktop`, `Android`, `iOS`. Пароли и сертификаты подписи редактор пишет в `.godot/export_credentials.cfg`, он в репозиторий не попадает. Результаты сборки кладутся в `build/` (папка в .gitignore).

## Что нужно

- Godot 4.7.1 .NET (mono) и шаблоны экспорта той же версии (`Editor → Manage Export Templates → Download and Install`).
- .NET SDK 9 или новее. Он собирает и `net8.0` (Windows, iOS, редактор), и `net9.0` (Android). Если нужна только Windows, хватит .NET SDK 8.

## Windows

В редакторе: `Project → Export → Windows Desktop → Export Project`.

Из консоли:

```
godot --headless --import
godot --headless --export-release "Windows Desktop" build/windows/Hobbie.exe
```

Результат в `build/windows/`:

- `Hobbie.exe`, `Hobbie.pck` — движок и ресурсы игры;
- `data_Godot1_windows_x86_64/` — C#-сборка `Godot1.dll`, рантайм .NET и ONNX Runtime. Без этой папки игра не запустится.

## Android

Важно: сенсорного управления в игре пока нет. Герой ходит только с клавиатуры (WASD, стрелки) или геймпада, касание экрана работает как щелчок мыши (меню нажимаются). На телефоне без геймпада или клавиатуры играть нельзя.

### Один раз на компьютере

1. OpenJDK 17.
2. Android SDK. Проще через Android Studio (`SDK Manager`): Platform-Tools 35+, Build-Tools 35.0.1, Platform 35 (Android 15), Command-line Tools. Или командой:
   ```
   sdkmanager --sdk_root=<путь к SDK> "platform-tools" "build-tools;35.0.1" "platforms;android-35" "cmdline-tools;latest" "cmake;3.10.2.4988404" "ndk;28.1.13356709"
   ```
3. .NET SDK 9 или новее.
4. В Godot: `Editor → Editor Settings → Export → Android`:
   - `Java SDK Path` — папка JDK 17;
   - `Android SDK Path` — папка SDK, в ней должен быть `platform-tools/adb` (на Windows обычно `%LOCALAPPDATA%\Android\Sdk`);
   - `Debug Keystore` Godot заполняет сам. Если поле пустое, создай ключ и укажи его с пользователем `androiddebugkey` и паролем `android`:
     ```
     keytool -genkey -v -keystore debug.keystore -storepass android -alias androiddebugkey -keypass android -keyalg RSA -validity 10000 -dname "CN=Android Debug,O=Android,C=US"
     ```

### APK для проверки на телефоне

На телефоне включи «Для разработчиков → Отладка по USB» и подключи кабель. В редакторе появится кнопка Android справа вверху (Remote Deploy): она собирает debug-APK и сразу ставит его на телефон.

Отдельным файлом: `Project → Export → Android → Export Project`, галка `Export With Debug` включена. Из консоли:

```
godot --headless --import
godot --headless --export-debug "Android" build/android/Hobbie.apk
adb install -r build/android/Hobbie.apk
```

Если установка падает с «Could not install to device», удали с телефона прежнюю версию игры: она подписана другим ключом.

### Релиз в Google Play

1. Создай release-ключ и храни его вместе с паролем вне репозитория. Без него обновление в Google Play не выпустить.
   ```
   keytool -v -genkey -keystore hobbie.keystore -alias hobbie -keyalg RSA -validity 10000
   ```
   Пароль ключа и хранилища должен совпадать, лучше только латиница и цифры.
2. В пресете `Android`: `Keystore → Release` (путь к файлу), `Release User` (`hobbie`), `Release Password`. Вместо пресета можно задать переменные `GODOT_ANDROID_KEYSTORE_RELEASE_PATH`, `GODOT_ANDROID_KEYSTORE_RELEASE_USER`, `GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD`.
3. Google Play принимает только AAB, а AAB собирается через Gradle: `Project → Install Android Build Template`, в пресете включи `Gradle Build → Use Gradle Build` и выбери `Export Format → Export AAB`. Шаблон ставится в папку `android/`. Первая сборка Gradle качает зависимости из интернета.
4. Перед каждой загрузкой увеличивай `Version → Code` (1, 2, 3…), `Version → Name` — версия, которую видит игрок.
5. Сборка: `Project → Export → Android`, галку `Export With Debug` сними. Из консоли:
   ```
   godot --headless --export-release "Android" build/android/Hobbie.aab
   ```

Идентификатор приложения `package/unique_name` = `com.fklska.hobbie`. После первой публикации в Google Play его менять нельзя, поэтому если нужен другой, поменяй до неё.

### Что в проекте настроено под Android

- C# под Android с Godot 4.5 требует `net9.0`, иначе экспорт останавливается с ошибкой «The export template only supports 'net9.0'». В `Godot1.csproj` `TargetFramework` переключается на `net9.0` только при `GodotTargetPlatform=android`, остальное собирается под `net8.0`.
- Архитектура только `arm64-v8a`: это все современные телефоны. Для эмулятора на ПК включи в пресете `x86_64`.
- Сжатие текстур ETC2/ASTC для телефонов уже включено в настройках проекта (`import_etc2_astc`).
- ONNX Runtime: см. раздел ниже.

## iOS

Собрать можно только на Mac: C# под iOS компилируется через NativeAOT, а для этого нужен Xcode. Про сенсорное управление верно то же, что для Android.

### Один раз

1. Mac с последним Xcode (запусти его один раз и прими лицензию), Godot 4.7.1 .NET для macOS, шаблоны экспорта 4.7.1, .NET SDK 8 или новее.
2. Аккаунт Apple Developer. Для проверки на своём iPhone хватит бесплатного Apple ID, для App Store и TestFlight нужен платный аккаунт.
3. В пресете `iOS`: `Application → App Store Team ID` (10 символов из developer.apple.com → Membership, или из Xcode → Settings → Accounts), `Bundle Identifier` = `com.fklska.hobbie` (поменяй, если занят).

### Сборка

`Project → Export → iOS → Export Project`, путь `build/ios/Hobbie.ipa`. Из консоли:

```
godot --headless --import
godot --headless --export-release "iOS" build/ios/Hobbie.ipa
```

Godot собирает C# через NativeAOT (`ios-arm64`), создаёт проект Xcode `build/ios/Hobbie.xcodeproj` и, если `Export Project Only` выключено, сразу собирает `.ipa`.

- На свой iPhone: открой `build/ios/Hobbie.xcodeproj` в Xcode, выбери телефон и нажми Run. На телефоне разреши разработчика в «Настройки → Основные → VPN и управление устройством».
- В App Store и TestFlight: в Xcode `Product → Archive`, затем `Distribute App → App Store Connect`.

### Ограничение: модели ONNX на iOS не работают

Пакет ONNX Runtime для iOS — статическая библиотека, её нужно вшивать в сборку NativeAOT. Это не настроено и не проверено, поэтому на iOS `NPCController` не грузит модели и жители работают на эвристике, как мобы и босс. `Godot1.csproj` убирает файлы ONNX Runtime из сборки iOS, чтобы они не попали в проект Xcode.

## Перед релизом

- Иконка: сейчас `icon.svg` — стандартная иконка Godot. Её берут все платформы, если в пресете не задана своя.
- Название: в настройках проекта `application/config/name` = `Godot1`. На Android игрок видит `package/name` из пресета (`Hobbie`). Если переименовать проект, на ПК сменится папка `user://` и старые сохранения (миры, партии, настройки) пропадут из игры, поэтому делай это до релиза.

## Что не попадает в билд

`exclude_filter` в пресетах убирает: `*.tmp`, `*.old`, `*.psd`, `Thumbs.db`, папки `DEBUG`/`Debug`, `Debugging/`, старые генераторы (`v1`, `V2`, `scripts/generator.gd`). Ни один из этих файлов не используется главной сценой и автозагрузками.

Если сцена из этих папок понадобится в игре, убери её шаблон из фильтра, иначе в билде она не загрузится.

## ONNX Runtime

Пакет `Microsoft.ML.OnnxRuntime` подключён в `Godot1.csproj`. При экспорте Godot запускает `dotnet publish -c ExportRelease -r <платформа> --self-contained true` и забирает всё из результата.

- Windows: NuGet кладёт `Microsoft.ML.OnnxRuntime.dll` и нативные `onnxruntime.dll` (~13 МБ), `onnxruntime_providers_shared.dll`. Godot копирует их в `data_Godot1_windows_x86_64/`. Отдельно ничего ставить не нужно.
- Android: в пакете NuGet библиотека для Android лежит только внутри `onnxruntime.aar`, который Godot не распаковывает. Цель `OnnxRuntimeMobile` в `Godot1.csproj` достаёт из него `libonnxruntime.so` нужной архитектуры, Godot кладёт её в `lib/arm64-v8a/` внутри APK. Та же цель убирает из сборки `.aar` и Windows-библиотеки (иначе +42 МБ мёртвого груза). Библиотека выровнена под страницы 16 КБ, как требует Google Play.
- iOS: та же цель убирает файлы ONNX Runtime, модели не используются (см. «Ограничение» в разделе iOS).

Сами модели `.onnx` не ресурсы Godot, поэтому в пресетах Windows и Android стоит `include_filter="*.onnx"`: без него модель не попадёт в `.pck` и `ONNXModel` не найдёт файл.

Модель должна быть одним файлом. Модели из RLStudy (PyTorch 2.13) сохранены с весами в отдельном `<имя>.onnx.data`. `ONNXInference.cs` передаёт модель в ONNX Runtime как массив байт, и тогда рантайм ищет `.data` в рабочей папке процесса через ОС, а не рядом с моделью и не внутри `.pck`. В экспортированной игре такая модель не загрузится, в редакторе загрузится только из корня проекта. Перед тем как класть модель в проект, собери её в один файл:

```
python -c "import onnx,sys; onnx.save_model(onnx.load(sys.argv[1]), sys.argv[2])" model_v9.onnx model_v9_full.onnx
```

Путь в `onnx_model_path` указывай через `res://`.
