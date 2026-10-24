# Аудит памяти — 25.10.2026

Это отдельный standalone-прогон, не источник FPS/GC-таблицы. Метод: прогрев 15 с, покой 2 с, GC и inventory, snapshot; 180 с повторных ударов, покой 2 с, GC и inventory, snapshot. Два snapshot успешно прочитаны штатным PackedMemorySnapshot reader Unity 6000.3.5f1. Runtime errors: 0. Попадания: 235/235, маска трёх шагов комбо 7, пик контактных частиц 96.

| Показатель | До | После | Дельта |
|---|---:|---:|---:|
| Native objects | 3865 | 3865 | 0 |
| Native object bytes | 539848761 | 539852441 | 3680 |
| Native allocations | 83708 | 84062 | 354 |
| Native allocation bytes | 142949768 | 144305814 | 1356046 |

Число объектов каждого нативного типа не изменилось. Дельта object bytes: ParticleSystem +3840 байт и TrailRenderer −160 байт. В runtime inventory: загруженные GameObject 346→346, GPU memory 590786149→590786149 байт. Inventory включает inactive/engine objects, поэтому не равен счётчику active GameObject в FPS-замере.

Основные дельты native allocations по root:

| Root | Дельта байт | Дельта allocation count |
|---|---:|---:|
| UnsafeUtility / Malloc(Temp) Pool | 1310720 | 5 |
| System / ExecutableAndDlls | 41486 | 339 |
| Objects | 3680 | 0 |
| UnsafeUtility / Malloc(Persistent) | 160 | 10 |

Основная дельта классифицирована как увеличение временного пула Unity, без появления новых Unity objects. Runtime/JIT, снимки и инвентаризация имеют собственные расходы; маленькая Persistent-дельта не приписывается без доказательств конкретному игровому коду. Во время аудита нет создания игровых объектов/материалов в цикле атаки. Исходный CSV показывает временной ряд, а snapshot-comparison.json — полную разбивку, не только выбранные строки.

При завершении player Unity также печатает MemoryLeaks-диагностику около 0.6 МБ. Она сохранена в журнале и не скрыта. Этот ограниченный тест подтверждает отсутствие накопления игровых/native Unity objects и ограниченный particle count; он не доказывает отсутствие любой возможной утечки движка/драйвера. В FPS-прогоне отдельно проверяются Gen0 и frame allocation без влияния snapshots.

Raw before.snap/after.snap находятся в Deliverables/MemoryAudit и входят в отдельный DuskYard-Verification.zip. Для повторного анализа распаковать его в Deliverables и вызвать DuskYardMemoryAnalysis.Run. Никакой внешний runtime-плагин для захвата/анализа не добавлялся.

Последние 45 с временного ряда: диапазон 141885082–141900346 байт, изменение между первой/последней выборкой -8632 байт. Кривая выходит на плато после расширения пулов; график: memory-trend.png.

График воспроизводится Documentation/Plot-Memory.ps1 стандартной Windows Charting library.
