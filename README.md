# Hobbie - мой игровой проект
[![Telegram](https://img.shields.io/badge/Telegram-2CA5E0?style=for-the-badge&logo=telegram&logoColor=white)](https://t.me/fklska)
![Unity](https://img.shields.io/badge/unity-%23000000.svg?style=for-the-badge&logo=unity&logoColor=white)
![Play Store](https://img.shields.io/badge/Google_Play-414141?style=for-the-badge&logo=google-play&logoColor=white)
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![Godot Engine](https://img.shields.io/badge/Godot-478CBF?style=for-the-badge&logo=GodotEngine&logoColor=white)

[YOUTUBE](https://www.youtube.com/@fklska3995/shorts) канал по игре

## Updates
### Update 0.0.6 RL Agents
Проведены первые успешные попытки обучения RL агента по исследованию карты и сбору ресурсов. Алгоритм - PPO, окружение ускорено в 4 раза. Планируется еще мультиагентное обучение, для вражеских мобов.


https://github.com/user-attachments/assets/894a089a-a91d-4b46-b8b0-1f0a3b295bd8


https://github.com/user-attachments/assets/f9e11a2a-92d0-46e3-9ec4-ccb425ac5d74



### Update 0.0.5 Optimization
Добавлена чанковая прогрузка карты, теперь вместо всей карты рендериться дистаниция в 3 чанка, также рендеринг карты разбивается на несколько кадров. Это значительно увеличило производительность, теперь на главной сцене больше фпс, а карта загружается намного быстрее из памяти. 

Однако генерация нового мира может стать узким местом, чтобы сгенерировать самую большую карту (384 на 384 тайла) уходит около минуты: 1 секунда на просчет данных и 59 секунд на сохранение ресурса Godot. На мальнький мир, например, на все провсе уходит 5 секунд. 

Пересмотрен подход к генерации ресурсов, было решено полностью отказаться от создания нодов в дереве сцены, в пользу тайловой карты. Из-за этого нужно пришлось отключить шейдеры для ресурсов, теперь недоступен `outline` шейдер и `покачивание` деревьев. В дальнейшем буду искать решения, для кастомизации отдельных тайлов на тайловой карте.

https://github.com/user-attachments/assets/3abaabfe-3e39-4c7f-b74a-febc8ff39fdc

### Update 0.0.4 New Procedural Generation and UI 15.07.2025
Полностью переписана процедурная генерация, теперь мир генерируется на основе нескольких шумовых карт - высотной, тепловой и карты влажности. Также изменена нормализация шума вместо `(value + 1) / 2` на `min-max normalization`. Добавлены новые биомы и текстуры, изменены настройки тайловой карты - теперь маштаб мира больше - размер 1 тайла увеличен с 16 до 64 пикселей. В дальнейшем генерация будет "тонко настраиваться". Добавлено UI меню генерации, сохранения и загрузки миров.
<div>
	<img width="200" height="200" alt="heightMap" src="https://github.com/user-attachments/assets/35a3a551-73c5-42bc-8d58-8302d6ab4388" />
	<img width="200" height="200" alt="heatMap" src="https://github.com/user-attachments/assets/641ceeee-2f58-4d7c-adea-3fdec2ee0988" />
	<img width="200" height="200" alt="moistureMap" src="https://github.com/user-attachments/assets/db31e0ce-f6fb-44f6-9870-7a327eafb495" />
	<img width="200" height="200" alt="biomeMap" src="https://github.com/user-attachments/assets/fc37a14e-778e-491c-b51b-99ee9416bdf2" />
</div>
  
### Light update 0.0.3
Добалены тени и эффект облаков, динамическое освещение. Освещение и тени реализованы через гдрадиентную карту и шумы, освещение меняется в зависимости от "времени". "Время" считается через нормализованную функцию синуса `(sin(delta) + 1) / 2`, где 1 - это ясный полдень, а 0 темнаяа ночь

https://github.com/user-attachments/assets/0914b3fc-8088-47cf-a815-b2ee11b8582e



Напиши [мне](https://t.me/fklska) если хочешь поучаствовать

[Шрифт](https://ggbot.itch.io/kurland-font)

[Предпологаемая музыка](https://www.youtube.com/watch?v=Lj7ifGu00kc)

## Концепция

Основная идея - слияния концепций [Age of empires](https://ru.wikipedia.org/wiki/Age_of_Empires_II:_The_Age_of_Kings), [The Wither 3](https://ru.wikipedia.org/wiki/%D0%92%D0%B5%D0%B4%D1%8C%D0%BC%D0%B0%D0%BA_3:_%D0%94%D0%B8%D0%BA%D0%B0%D1%8F_%D0%9E%D1%85%D0%BE%D1%82%D0%B0)
и [Heroes of Might and Magic III](https://ru.wikipedia.org/wiki/Heroes_of_Might_and_Magic_III) 

А также других игровых [проектов ...](https://ru.wikipedia.org/wiki/Terraria)

### Это я к тому, что многие концептуальные вещи взяты из этих работ. 

К примеру, из эпохи империй и героев стратегический, и строительный аспекты. Система строительства, улучшения зданий, изучение технологий и т. д.
Из ведьмака система боевки и прокачки персонажа. Из террарии дизайн и проработка боссов. Они должны выглядить ярко, запоминаться, иметь какую-то мифологическую основу, как например, глаз Ктулху. 

К сожалению, я не дизайнер, и всю графику беру из открытых источников.

---
Игра задумавыется в стиле стратегии-рпг. Есть какая-то стратегическая цель развить свою деревню / город / поселение и защитить ее от монстров:
<div id="header" align="center">
  <img src="https://img.itch.zone/aW1hZ2UvNDQ0MjA5LzIyMzgxOTQuZ2lm/original/XLeVa3.gif" width="400"/>
  <img src="https://img.itch.zone/aW1hZ2UvMTk4Mjk2OC8xMTY2MDgzMi5naWY=/original/8XELSd.gif" width="400"/>
  <img src="https://img.itch.zone/aW1hZ2UvNzMyODA0LzQxMzUxODIuZ2lm/original/tc%2FXFz.gif" width="400"/>
</div>

---

Есть РПГ аспекты: главный персонаж за которого играет игрок или несколько (планируется мультиплеер) прокачивает его, выбирает навыки, пути развития. А также жители (ИИ), который споособствует развитию и защите деревни / города / поселения:
<div id="header" align="center">
  <a href=https://free-game-assets.itch.io/free-yokai-pixel-sprite-sheets>
	<img src="https://img.craftpix.net/2023/07/Free-Yokai-Pixel-Art-Character-Sprites.gif" width="400"/>
  </a>
  <a href=https://craftpix.net/freebies/free-wizard-sprite-sheets-pixel-art/>
	<img src="https://img.craftpix.net/2023/01/Free-Wizard-Sprite-Sheets-Pixel-Art.gif" width="400"/>
  </a>
  <a href=https://free-game-assets.itch.io/free-3-character-sprite-pixel-art> 
	<img src="https://img.craftpix.net/2020/01/Free-3-Character-Sprite-Sheets-Pixel-Art.gif" width="400"/>
  </a>
</div>

Поведение ИИ основано на [NavMesh](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html) в частности на алгоритме [A*](https://ru.wikipedia.org/wiki/A*)


https://github.com/user-attachments/assets/365a39fe-342f-4798-b3c5-21504b11d2b0


---

## Генерация карты
Качество гифки пришлось сильно урезать

В игре присутствует процедурная генерация карты, основанная на [Шуме Перлина](https://ru.wikipedia.org/wiki/%D0%A8%D1%83%D0%BC%D0%9F%D0%B5%D1%80%D0%BB%D0%B8%D0%BD%D0%B0) , каждая целочисленная координата на карте имеет свою высоту (от 0f до 1f), все виды ресурсов распологаются на высоте до 0.35, чтобы оставались пустоты

### Генерация на Unity
<div id="header" align="center">
  <img src="https://i.imgur.com/SIV6W9p.gif" width="400"/>
  <img src="https://i.imgur.com/bxGGFRo.gif" width="400"/>
</div>

### Генерация на Godot
При переносе игры на другой движок изменились некоторые тайлы, а в общем и целом все также
<div id="header" align="center">
  <img src="https://imgur.com/iTpaBIo.gif" width="800"/>
  <img src="https://imgur.com/ckrUHsF.gif" width="800"/>
  <img src="https://imgur.com/esOvI57.gif" width="800"/>
</div>

## Внутриигровой интерфейс

Праобраз - интерфейс из [Age of empires](https://ru.wikipedia.org/wiki/Age_of_Empires_II:_The_Age_of_Kings) или той же [Dota 2](https://ru.wikipedia.org/wiki/Dota_2), основанный на селекторе существ. Есть 1 основная плашка ,которая показывает всю необходимую информацию выбранного существа или объекта.

UPD: Скорее всего буду отказываться от этого. Основная плашка будет показывать только запасы ресурсов в деревне, а информация по инвентарю и характеристике только про главного персонажа.
<div id="header" align="center">
  <img src="https://imgur.com/z4sdeRO.png" width="800"/>
  <img src="https://imgur.com/CRdyNYs.png" width="300"/>
  <img src="https://imgur.com/ijGd1yc.png" width="300"/>
  <img src="https://imgur.com/0mynifS.gif" width="800"/>
</div>

Неболшое изменение UI в сторону упрощения. Всю информацию по деревне и жителям, а также строительство и ветки развития можно будет посмотреть в одном меню.

https://github.com/user-attachments/assets/6cabadb0-bbd5-403d-8143-88bbe25a1529


## Шейдеры и эффекты
Шейдеры и VFX пока что находятся на стадии изучения. На данный момент реализован outline шейдер, который добавляет внешнее выделение выбранного цвета и ширины, используется при наведении и в селекторе.

Также получилось реализовать что то более менее сностное - эффект сплеша от удара мечом. 

https://github.com/user-attachments/assets/f18a7eae-57c0-4e68-aefc-e7bc1b6d5c1f
