<div class="header" align="center">  
[<img alt="Space Station 14" width="880" height="300" src="https://raw.githubusercontent.com/space-wizards/asset-dump/de329a7898bb716b9d5ba9a0cd07f38e61f1ed05/github-logo.svg">](https://github.com/AtaraxiaSpaceFoundation/Europa-Station-14)
</div>

---

Backmen&Ataraxia - это форк [Space Wizards](https://github.com/space-wizards/space-station-14), ориентирующийся на идеи [СтароTG](https://github.com/tgstation/tgstation) и [Shiptest](https://github.com/shiptest-ss13/Shiptest) из Space Station 13, включая в это свои собственные идеи.

Space Station 14 - это ремейк SS13, который работает на собственном движке [Robust Toolbox](https://github.com/space-wizards/Robust-Toolbox), написанном на C#.

## Ссылки

<div class="header" align="center">  

[Discord](https://discord.gg/mk-europa) | [Steam](https://store.steampowered.com/app/1255460/Space_Station_14/) | [Standalone Download](https://spacestation14.com/about/nightlies/)

</div>

[<img src="https://i.imgur.com/xMzKtYK.png" alt="Discord" width="150" align="left">](https://discord.gg/ss-14-backmen-ru-1053200453829132298)
**Discord Server**<br>В космосе вас никто не услышит.

[<img src="https://imagizer.imageshack.com/img922/4959/8KTh9r.png" alt="Wiki" width="150" align="left">](https://wiki.backmen.ru)
**Wiki**<br>Что за блобы и с чем их едят?

## Сборка

Следуйте гайду от [Space Wizards](https://docs.spacestation14.com/en/general-development/setup/setting-up-a-development-environment.html) по настройке рабочей среды, но учитывайте, что репозитории отличаются друг от друга и некоторые вещи могут отличаться.
Ниже перечислены скрипты и методы облегчающие работу с билдом.

### Windows

> 1. Склонируйте данный репозиторий.
> 2. Запустите `git submodule update --init --recursive` в командной строке, чтобы скачать движок игры.
> 3. Запускайте `Scripts/bat/buildAllDebug.bat` после любых изменений в коде проекта.
> 4. Запустите `Scripts/bat/runQuickAll.bat`, чтобы запустить клиент и сервер.
> 5. Подключитесь к локальному серверу и играйте.

### Linux
1. Clone this repo:
```shell
git clone https://github.com/space-wizards/space-station-14.git
```
2. Go to the project folder and run `RUN_THIS.py` to initialize the submodules and load the engine:
```shell
cd space-station-14
python RUN_THIS.py
```
3. Compile the solution:  

Build the server using `dotnet build`.

> 1. Склонируйте данный репозиторий.
> 2. Запустите `git submodule update --init --recursive` в командной строке, чтобы скачать движок игры.
> 3. Запускайте `Scripts/sh/buildAllDebug.sh` после любых изменений в коде проекта.
> 4. Запустите `Scripts/sh/runQuickAll.sh`, чтобы запустить клиент и сервер.
> 5. Подключитесь к локальному серверу и играйте.

### MacOS

> Предположительно, также, как и на Линуксе, сами разберётесь.

---

## Лицензия

Весь код для репозитория контента лицензирован в соответствии с [MIT license](https://github.com/AtaraxiaSpaceFoundation/Europa-Station-14/blob/master/LICENSE-MIT.TXT) && [AGPLv3](https://github.com/AtaraxiaSpaceFoundation/Europa-Station-14/blob/master/LICENSE-AGPLv3.TXT). Взгляните на [наше лицензирование](https://github.com/AtaraxiaSpaceFoundation/Europa-Station-14/blob/master/LEGAL.md)

Большинство ресурсов лицензировано под [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/), если не указано иное. Лицензия и авторские права на ресурсах указаны в файле метаданных.
[Example](./Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).

Обратите внимание, что некоторые активы лицензированы под некоммерческой [CC-BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/) или аналогичной некоммерческой лицензией и должны быть удалены, если вы хотите использовать этот проект в коммерческих целях.
