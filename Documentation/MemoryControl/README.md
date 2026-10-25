# Контроль shutdown-диагностики Unity

Контрольные сцены собираются в отдельном проекте Unity 6000.3.5f1 и не содержат скриптов Dusk Yard. Их задача - проверить shutdown-строку `MemoryLeaks` без боевой системы и VFX. Результаты находятся в `../Verification/MemoryAudit/engine-control.json` и REPORT.md.

## Подготовка

1. Создать отдельный проект той же версии Unity.
2. Скопировать приложенный `manifest.json` в Packages, `ControlExit.cs` в Assets, `ControlBuild.cs` в Assets/Editor.
3. Выполнить Unity в batchmode с `-executeMethod ControlBuild.Run -quit`. Сборка будет в Build/Control.exe.
4. Запустить `Control.exe -force-d3d11 -logFile <полный путь к журналу>`. Таймер завершает player примерно через 15 секунд после старта. Development build, D3D11, 1280x720, VSync 0, ограничение 60 FPS.

## Варианты

- Пустая сцена со встроенным рендером: только модули из приложенного manifest.
- Input System: добавить `com.unity.inputsystem` 1.19.0 и включить новый Input System в Player Settings. Компонент пишет число найденных устройств.
- 300 статичных кубов: build-метод `ControlBuild.RunBuiltinPopulated`, аргумент редактора `-populated-control`. Это обычные Unity primitives с коллайдерами.
- ProfilerRecorder: ту же сборку запустить с `-record-control`. Создаётся один recorder `GC Allocated In Frame`, который освобождается в OnDestroy.
- HDRP: использовать версии пакетов из основного проекта, скопировать его GraphicsSettings/QualitySettings, `DuskHDRP.asset` с meta и папку HDRPDefaultResources. Важно Linear Color Space. Выполнить `ControlBuild.Run` без аргумента заполнения сцены. В этой сцене нет персонажа, мишени, combat-скриптов, материалов боя и VFX.

Для первого HDRP-контроля по ошибке использовалась Gamma. Он дал ошибки шейдеров и исключён из сравнения; это не прогон основной сцены. Конфигурации и результаты успешных контролей перечислены в JSON.

Совпадение отдельных shutdown labels не доказывает отсутствие всех утечек. Для проверки накопления объектов используются снимки и временной ряд памяти основной сцены. Строка shutdown и runtime-ошибки рассматриваются отдельно.

Дополнительные runtime-флаги: `-controller-control` двигает стандартный CharacterController; `-particles-control` создаёт три ParticleSystem с отключёнными renderer и maxParticles 256; `-long-control` задаёт 60 секунд после Start. Заключительный чистый контроль: HDRP Linear, build `ControlBuild.Run -populated-control`, player `-long-control -native-leak-detection EnabledWithStackTrace -diag-job-temp-memory-leak-validation`. В этом варианте включены только статичные Lit-кубы и directional light, без дополнительных runtime-флагов контроллера/частиц/recorder.
