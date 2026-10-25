# Комплект результата

## Что передавать

1. **DuskYard-Source.zip** - проект Unity: Assets, Packages, ProjectSettings, README, лицензии, исходный PDF и сверка требований. Library и Temp отсутствуют.
2. **DuskYard.bundle** - переносимый Git-репозиторий с историей. Восстановление: `git clone DuskYard.bundle DuskYard`. Source ZIP служит удобной копией текущего состояния, bundle содержит сам репозиторий.
3. **DuskYard-Windows.zip** - отдельный Windows build. Распаковать целиком, запускать PLAY.cmd или DuskYard.exe. SHOWCASE.cmd запускает автоматический показ на 30 секунд.
4. **DuskYard-Verification.zip** - дополнительные доказательства: снимки Memory Profiler до/после, инвентаризация, CSV памяти, журнал диагностики. Это приложение к результату, оно не требуется для запуска игры.
5. **SHA256.txt** - контрольные суммы архивов и Git bundle.

Исходники и история: [https://github.com/mayten10000/DuskYard](https://github.com/mayten10000/DuskYard), основная ветка `main`.

Windows build и дополнительные материалы: [Google Drive](https://drive.google.com/drive/folders/1NcFKTnDqreaLoHWBy0fCOUPg5MxGUcfM). Доступ - всем по ссылке, только просмотр и скачивание.

Локальная копия комплекта - **Deliverables/READY-TO-SEND**. Git bundle на диске разделён на `DuskYard.bundle.part1` и `.part2`; восстановление описано в `RESTORE-GIT.txt`. Старые соседние архивы не относятся к комплекту для отправки.

## С чего начать проверку

- Unity **6000.3.5f1**, HDRP **17.3.0**.
- Открыть сцену `Assets/DuskYard/Scenes/DuskYard.unity`.
- Прочитать **SHOWCASE.md**, затем **README.md** с замерами и ограничениями.
- Полная атомарная сверка исходного PDF - **Documentation/REQUIREMENTS.md**: 70 условий, ссылки на реализацию и доказательства.
- Источник - **Documentation/Assignment.pdf**, извлечённый текст и SHA256 рядом.
- Время работы - **TIMELOG.md**, приблизительное распределение общего цикла по блокам.

## Заготовка сопроводительного сообщения

Здравствуйте! Подготовил тестовый фрагмент Dusk Yard на Unity 6000.3.5f1 / HDRP. Передаю проект с Git-историей, отдельный Windows build и README с точным 30-секундным сценарием, конфигурацией ПК, замерами Baseline/Gameplay/Stress и ограничениями. Дополнительно приложены результаты проверки памяти и подробная сверка исходного ТЗ. Управление и порядок показа описаны в SHOWCASE.md.

Исходники: https://github.com/mayten10000/DuskYard

Сборка и проверки: https://drive.google.com/drive/folders/1NcFKTnDqreaLoHWBy0fCOUPg5MxGUcfM

Сообщение - заготовка, автоматически никому не отправлялось.
