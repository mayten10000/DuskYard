# Проверка требований — 24.10.2026

| Требование | Реализация / доказательство |
|---|---|
| Unity 6000.3.5f, HDRP | ProjectVersion.txt: 6000.3.5f1; manifest: HDRP 17.3.0; standalone build успешен |
| Компактный двор 20–40 м | 28×28 м; камень, дерево, ворота, жаровни |
| Средневековье, low-fantasy, реализм | Сеттинг и PBR-материалы выполнены; персонаж детализирован процедурно, анатомия и анимация упрощены; часть реквизита модульная, художественный реализм закрыт частично |
| Контроллер от третьего лица | WASD, мышь, камера с коллизией, ЛКМ/Space |
| Удар / отклик / восстановление | Активная фаза клинка, физический контакт, четыре состояния мишени, возврат материала |
| VFX | Trail, короткая вспышка, частицы, реакция материала. Без попадания вне дистанции; результат автопроверки — в benchmark.json |
| Shader Graph / два признака / два пресета | HDR emission + world-space; warm/cold; клавиша F4 |
| Mixed + static + dynamic lighting | Shadowmask, bake 3 lightmap, baked fill/жаровни, realtime rim/contact |
| Light Probes и Reflection Probe | 75 probes; baked HDRP reflection; проба SH на движущемся контроллере |
| Global Volume | Exposure, ACES, ColorAdjustments, слабый Bloom, HDRISky |
| 1080p / VSync off / FPS | PC/D3D11; Gameplay 118.88 FPS; Stress 92.21 FPS; Verification/Run-Art/benchmark.json |
| Stress и GC | 120 с, frame CSV, GC recorder, memory samples и object count; глубокий Memory/GPU capture не приложен |
| Console | Build: 0 errors, 1 Cloud symbols warning; runtime: 0 errors / 0 warnings |
| README | Управление, 30-секундный сценарий, архитектура, метод замера, ресурсы, ограничения |
| Учёт времени по блокам | Не вёлся с начала работы; достоверно восстановить нельзя |
| Git без Library/Temp | Локальный репозиторий и переносимый bundle; внешнего remote пока нет |
| Отдельный build / облачная ссылка | Локальный Windows build; облачная публикация отложена пользователем; локальные архивы обновлены |

Полное соответствие всем пунктам не заявляется: остаются художественная доводка персонажа/реквизита, публикация материалов и отсутствующий исходный учёт времени. Фактические измерения и ограничения важнее формальной отметки «всё готово».
