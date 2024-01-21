# Hobbie - мой игровой проект
[![Telegram](https://img.shields.io/badge/Telegram-2CA5E0?style=for-the-badge&logo=telegram&logoColor=white)](https://t.me/fklska)
![Unity](https://img.shields.io/badge/unity-%23000000.svg?style=for-the-badge&logo=unity&logoColor=white)
![Play Store](https://img.shields.io/badge/Google_Play-414141?style=for-the-badge&logo=google-play&logoColor=white)
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![Godot Engine](https://img.shields.io/badge/Godot-478CBF?style=for-the-badge&logo=GodotEngine&logoColor=white)

### Внимание, автор не учился на программиста в ВУЗе 
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
  <img src="https://imgur.com/oitsuVW.png" width="400"/>
  <img src="https://imgur.com/R9GQl0K.png" width="400"/>
  <img src="https://imgur.com/Qeb2Xuh.png" width="1000"/>
  <img src="https://imgur.com/7zeSmi3.gif" width="800"/>
</div>
