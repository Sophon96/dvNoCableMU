# Changelog

## [1.1.0](https://github.com/Sophon96/dvNoCableMU/compare/v1.0.1...v1.1.0) (2023-12-04)


### Features

* add ability to sync DM3 gearbox and cylinder cocks, add settings to enable non-standard MU ([b79cb05](https://github.com/Sophon96/dvNoCableMU/commit/b79cb0586f52feec112ef54c887d748c29419001))
* make window actually draggable ([398b846](https://github.com/Sophon96/dvNoCableMU/commit/398b846f36c240e6fc9bfd37169d6beee02a7376))

## [1.0.1](https://github.com/Sophon96/dvNoCableMU/compare/v1.0.0...v1.0.1) (2023-09-10)


### Bug Fixes

* correct url for repository.json ([b14e854](https://github.com/Sophon96/dvNoCableMU/commit/b14e854bacb641dbb6b2aacde50c87f180799810))
* Decoupling paired locomotives causes them to still be synced ([1887b07](https://github.com/Sophon96/dvNoCableMU/commit/1887b07b4e8fbb0d4375cb73013461b95fa6e6ee))
* Locomotive stuck in broken/limbo state if cleared via Comms. Radio while paired ([703929d](https://github.com/Sophon96/dvNoCableMU/commit/703929d1e4342f56caaa56ef8b935bb237910a08))
* Null references when loading the plugin ([1e51f43](https://github.com/Sophon96/dvNoCableMU/commit/1e51f43d7aba325008d5ddd6c8843248f0cced68))

## [1.0.0](https://github.com/Sophon96/dvNoCableMU/compare/v1.0.0...v1.0.0) (2023-08-21)


### Features

* add informative status message to UI ([c9a89ff](https://github.com/Sophon96/dvNoCableMU/commit/c9a89ffbbba1cf247dd3a102acf128203dced069))
* Add repository.json ([0809a32](https://github.com/Sophon96/dvNoCableMU/commit/0809a32733e2bc83270b8c9b58466f4259868382))
* basic implementation for bepinex ([59e5e11](https://github.com/Sophon96/dvNoCableMU/commit/59e5e1188cb8d681a5e7f4c02083b42ecb9342fb))
* implement better way of determining direction of locomotive and limit locomotives to same train ([f72fca7](https://github.com/Sophon96/dvNoCableMU/commit/f72fca732bc5a61371955cd0489f660a9a904cb1))
* Made UI worse ([eeb0ac9](https://github.com/Sophon96/dvNoCableMU/commit/eeb0ac960900347c1705d02ce97fa2b0a0f31201))
* switch to event-based syncing of locomotive controls ([99ce897](https://github.com/Sophon96/dvNoCableMU/commit/99ce897addac73226fab2a5b33c223648b34f533))
* Update project name ([2f1310c](https://github.com/Sophon96/dvNoCableMU/commit/2f1310cc55266bdd7d1483705fcb094faafc732d))
* Update to UMM ([88ff6b9](https://github.com/Sophon96/dvNoCableMU/commit/88ff6b95df4e5e63e45309311c78ee73a342a433))


### Bug Fixes

* defer adding and removing locomotives to coroutine to avoid flickering (also fix status messages not updating) ([1f3cba8](https://github.com/Sophon96/dvNoCableMU/commit/1f3cba8cd96a14c0d0585065b0f695932ad1ba38))
* memory leak from event handlers not being removed ([cfaa109](https://github.com/Sophon96/dvNoCableMU/commit/cfaa10924dba5a47dce4e9871f4cfd8d6aa0ce42))
* omit unneeded top level folder in archive ([a04103a](https://github.com/Sophon96/dvNoCableMU/commit/a04103a835b3c81a92a325b8312a633776c79957))
* remove stupid comments ([85df312](https://github.com/Sophon96/dvNoCableMU/commit/85df312043a6dbdd71cec231c76d1b654cd33138))


### Miscellaneous Chores

* version 1.0.0 ([7fe49ef](https://github.com/Sophon96/dvNoCableMU/commit/7fe49efd7bec9da99fa2e266a00ab501481bb974))
