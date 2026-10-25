# Итоговая сверка - 25.10.2026

Полная проверка всех 70 отдельных условий исходного PDF: **REQUIREMENTS.md**. Источник: Assignment.pdf, извлечённый текст и SHA256 рядом.

Подтверждены нужная Unity/HDRP, сцена, контроллер, трёхударное комбо, контактные VFX, Shader Graph/два признака/два пресета, mixed/baked/dynamic light, probes и Volume. Проверка всех scene renderers включает inactive; missing refs/shader/bake errors: 0. Все пять VFX-материалов имеют Transparent/ZWrite0/queue3000.

Итоговый Gameplay: **104.53 FPS**, Stress: **90.33 FPS** при PC/D3D11/1920×1080/VSync0. Runtime errors/warnings: 0; Gen0: 0; active objects 295→295. Все три удара дали попадания, вне дистанции 0. Возврат мишени: 7294 проверки, ошибка позиции/поворота 0.

Память проверена дополнительно: два snapshot, 180 с нагрузки, 235/235 попаданий, native Unity objects 3865→3865, GPU memory без изменения. Полная классификация дельты и границы вывода: Verification/MemoryAudit/REPORT.md. Общая allocation memory не объявляется абсолютно неизменной; данные и shutdown diagnostics не скрыты.

Комплект сдачи описан в SUBMISSION.md. Исходники опубликованы: https://github.com/mayten10000/DuskYard, ветка main. Отдельная сборка: https://drive.google.com/drive/folders/1NcFKTnDqreaLoHWBy0fCOUPg5MxGUcfM, доступ всем по ссылке на чтение. Приблизительный общий цикл работы - 23 часа, распределение по шести блокам приведено в README/TIMELOG и явно обозначено как оценка. Точный активный учёт задним числом не восстановлен.

Дополнительная проверка shutdown: крупный Manager-блок воспроизведён в отдельной освещённой HDRP-сцене без combat-кода. В её трассировке persistent-аллокаций присутствует NativeList<HDShadowRequest> из HDRP; такие отдельные предупреждения в основном player не наблюдались. Подробности и границы вывода - Verification/MemoryAudit/REPORT.md. Отсутствие всех утечек движка не заявляется.
