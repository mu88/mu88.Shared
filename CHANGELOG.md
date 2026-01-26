

<a name="5.0.0"></a>
## [5.0.0](https://www.github.com/mu88/mu88.Shared/releases/tag/5.0.0) (2026-01-26)

### ✨ Features

* remove tracing for the moment as it is not needed ([1e581fa](https://www.github.com/mu88/mu88.Shared/commit/1e581fa15ebc474d2458d6ff27048414795c4d44))

### 🐛 Bug Fixes

* dispose `HttpClient` in health check to avoid socket exceptions ([df4a395](https://www.github.com/mu88/mu88.Shared/commit/df4a39595ee906dab786dba5acce377316dd9de2))

### Breaking Changes

* remove tracing for the moment as it is not needed ([1e581fa](https://www.github.com/mu88/mu88.Shared/commit/1e581fa15ebc474d2458d6ff27048414795c4d44))

<a name="4.3.0"></a>
## [4.3.0](https://www.github.com/mu88/mu88.Shared/releases/tag/4.3.0) (2025-12-19)

### ✨ Features

* add PrecomputeContainerRepository target to compute container repository automatically ([e87f60a](https://www.github.com/mu88/mu88.Shared/commit/e87f60afaf5b6d3dcdb12b9696281652ab82a846))
* publish fully qualified image via MSBuild ([6972a64](https://www.github.com/mu88/mu88.Shared/commit/6972a644af81d6975e85adb1896a2a5af15acb54))

### ♻️ Refactors

* split precomputation logic into separate targets ([6bafd97](https://www.github.com/mu88/mu88.Shared/commit/6bafd97f5ef916b1a4cbefd65d1cd89d91ab8137))

### ✅ Tests

* do not explicitly couple tests to fixed ContainerRepository ([a315cf8](https://www.github.com/mu88/mu88.Shared/commit/a315cf8d22579e9308a3555d274558f7872443d5))

<a name="4.2.0"></a>
## [4.2.0](https://www.github.com/mu88/mu88.Shared/releases/tag/4.2.0) (2025-12-17)

### ✨ Features

* add multiple improvements ([d3d232f](https://www.github.com/mu88/mu88.Shared/commit/d3d232f6641749fd9e79d19e48c97d2dbba29d12))
* emit MSBuild ItemGroups GeneratedImages and GeneratedContainers ([6e89fa1](https://www.github.com/mu88/mu88.Shared/commit/6e89fa1c12f6f20d2da12e658841f11b623f3e94))

### 🐛 Bug Fixes

* only calculate ContainerImageTags when not already set ([da51dda](https://www.github.com/mu88/mu88.Shared/commit/da51ddad9b2db3c57a8d4bbbbf7190c48d974b93))

### ✅ Tests

* add tests for error handling of container publishing ([adb8710](https://www.github.com/mu88/mu88.Shared/commit/adb8710ca61068f123a4b42ffe51785984e373c7))
* fix broken test ([73f599e](https://www.github.com/mu88/mu88.Shared/commit/73f599e9256bd1fed4cc238e0fa7f6caf17eece7))
* improve tests ([4c8f1fb](https://www.github.com/mu88/mu88.Shared/commit/4c8f1fbd5a13f435204d60c598c66ab0ff850f55))
* refactor tests so that they are easier to maintain ([08441cc](https://www.github.com/mu88/mu88.Shared/commit/08441ccda6d4433240e3b3abc7989b9760b0cbba))

<a name="4.1.0"></a>
## [4.1.0](https://www.github.com/mu88/mu88.Shared/releases/tag/v4.1.0) (2025-12-12)

### ✨ Features

* add several improvements ([5cebe48](https://www.github.com/mu88/mu88.Shared/commit/5cebe481adbf31dec2d0c3e6e614e0b67b993da8))

### 🔧 Chores

* update dev container to .NET 10 ([3d57d79](https://www.github.com/mu88/mu88.Shared/commit/3d57d798e9bee0d0046ce7b9b945b09c8c206a77))

<a name="4.0.0"></a>
## [4.0.0](https://www.github.com/mu88/mu88.Shared/releases/tag/v4.0.0) (2025-12-03)

### ✨ Features

* upgrade to .NET 10 ([ff6e4d3](https://www.github.com/mu88/mu88.Shared/commit/ff6e4d3b28ea5644200fd4feed8c3ca28a4fdebe))

### 🔧 Chores

* **deps:** update all dependencies ([f3dc862](https://www.github.com/mu88/mu88.Shared/commit/f3dc862146c876df9225a9ad4013b8e1d8c695b6))
* **deps:** update all dependencies ([e02426e](https://www.github.com/mu88/mu88.Shared/commit/e02426e504b4fbcebc43a9fe843eee257b39b4e8))
* **deps:** update all dependencies ([93c858b](https://www.github.com/mu88/mu88.Shared/commit/93c858b8c3de25a3f0227780203102406c5508ed))
* **deps:** update all dependencies ([dc14a14](https://www.github.com/mu88/mu88.Shared/commit/dc14a1450996040695bb357e40d34ecdcc73af07))

### Breaking Changes

* upgrade to .NET 10 ([ff6e4d3](https://www.github.com/mu88/mu88.Shared/commit/ff6e4d3b28ea5644200fd4feed8c3ca28a4fdebe))

<a name="3.1.0"></a>
## [3.1.0](https://www.github.com/mu88/mu88.Shared/releases/tag/v3.1.0) (2025-10-13)

### Features

* set MSBuild property to always restore via NuGet lock file ([134b4b6](https://www.github.com/mu88/mu88.Shared/commit/134b4b6a2057dc9cb1857c38c45d7336c158d843))

<a name="3.0.0"></a>
## [3.0.0](https://www.github.com/mu88/mu88.Shared/releases/tag/v3.0.0) (2025-10-12)

### Features

* enable NuGet lock file via shared MSBuild properties ([4252c94](https://www.github.com/mu88/mu88.Shared/commit/4252c94c6f798e31d8e231e95b3d9e742e501cf3))
* share common MSBuild settings via NuGet package ([944dcc9](https://www.github.com/mu88/mu88.Shared/commit/944dcc9e88480a000dba04c3e3b10ac79a317a37))

### Breaking Changes

* enable NuGet lock file via shared MSBuild properties ([4252c94](https://www.github.com/mu88/mu88.Shared/commit/4252c94c6f798e31d8e231e95b3d9e742e501cf3))

<a name="2.0.0"></a>
## [2.0.0](https://www.github.com/mu88/mu88.Shared/releases/tag/v2.0.0) (2025-03-30)

### Features

* add reusable workflow ([da1dfb8](https://www.github.com/mu88/mu88.Shared/commit/da1dfb8490422e6ccd665ae3e27f211d0e7fe9fc))
* remove multi-platform Docker image publishing as the issue has been resolved in the SDK ([10e56e9](https://www.github.com/mu88/mu88.Shared/commit/10e56e96cd8878fb32c4f86ff462f03926ff7727))

### Breaking Changes

* remove multi-platform Docker image publishing as the issue has been resolved in the SDK ([10e56e9](https://www.github.com/mu88/mu88.Shared/commit/10e56e96cd8878fb32c4f86ff462f03926ff7727))

<a name="1.1.1"></a>
## [1.1.1](https://www.github.com/mu88/mu88.Shared/releases/tag/v1.1.1) (2024-12-19)

<a name="1.1.0"></a>
## [1.1.0](https://www.github.com/mu88/mu88.Shared/releases/tag/v1.1.0) (2024-12-19)

### Features

* provide minimalistic health check tool ([56e7c14](https://www.github.com/mu88/mu88.Shared/commit/56e7c14ba58a85c8b7eb1d951b28cd21dcb741f3))

<a name="1.0.0"></a>
## [1.0.0](https://www.github.com/mu88/mu88.Shared/releases/tag/v1.0.0) (2024-12-06)

### Features

* **deps:** upgrade to .NET 9 ([9db1c25](https://www.github.com/mu88/mu88.Shared/commit/9db1c252c0f78682c498d1a785639bd4feacbddc))

### Breaking Changes

* **deps:** upgrade to .NET 9 ([9db1c25](https://www.github.com/mu88/mu88.Shared/commit/9db1c252c0f78682c498d1a785639bd4feacbddc))

<a name="0.5.1"></a>
## [0.5.1](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.5.1) (2024-10-05)

<a name="0.5.0"></a>
## [0.5.0](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.5.0) (2024-08-07)

### Features

* remove warning ([b957a17](https://www.github.com/mu88/mu88.Shared/commit/b957a17d0ba17b10ffaf86300d322680ec886346))

<a name="0.4.0"></a>
## [0.4.0](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.4.0) (2024-08-05)

### Features

* log warning if OpenTelemetry endpoint is not set ([2c4d63a](https://www.github.com/mu88/mu88.Shared/commit/2c4d63ae6cc28c2e8074f55124db9d942a2f1b54))

### Bug Fixes

* map .NET RIDs to Golang architecture items on full strings ([f435745](https://www.github.com/mu88/mu88.Shared/commit/f4357453a67f1af6e6deac4f3cbf7fee090432a1))

<a name="0.3.0"></a>
## [0.3.0](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.3.0) (2024-08-03)

### Features

* map .NET RIDs to Golang architecture items ([fa69789](https://www.github.com/mu88/mu88.Shared/commit/fa697890bb583dba771880bf9876e0e429bdf3f2))

<a name="0.2.2"></a>
## [0.2.2](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.2.2) (2024-08-03)

### Bug Fixes

* call other target directly to avoid build problem ([f1ace63](https://www.github.com/mu88/mu88.Shared/commit/f1ace6370ae7c80eb124c4159860ac76c9dcb3b5))

<a name="0.2.1"></a>
## [0.2.1](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.2.1) (2024-08-02)

### Bug Fixes

* move targets to project root ([d2d81e9](https://www.github.com/mu88/mu88.Shared/commit/d2d81e9ca64aa327038eb3df0fbcb2c113f1ccbe))

<a name="0.2.0"></a>
## [0.2.0](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.2.0) (2024-08-02)

### Features

* add MSBuild targets for multi-arch Docker images ([3a70f6c](https://www.github.com/mu88/mu88.Shared/commit/3a70f6c2d9cbdd3fabfdc915c319cc037a358c04))

<a name="0.1.2"></a>
## [0.1.2](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.1.2) (2024-08-02)

### Bug Fixes

* temporarily remove MSBuild target as it breaks downstream ([45de444](https://www.github.com/mu88/mu88.Shared/commit/45de444ead8fef6ff8d1eab2018075465470abe6))

<a name="0.1.1"></a>
## [0.1.1](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.1.1) (2024-08-02)

### Bug Fixes

* ignore NU5104 as it's on purpose to use the prerelease packages from OpenTelemetry ([3767529](https://www.github.com/mu88/mu88.Shared/commit/376752929a4077de8dbffad07c17510b120f2dc7))

<a name="0.1.0"></a>
## [0.1.0](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.1.0) (2024-08-02)

### Features

* add process and EF core instrumentation ([d1231ce](https://www.github.com/mu88/mu88.Shared/commit/d1231ceeb680984f0810e0f7915887d4c53d5507))

### Bug Fixes

* don't reference SDK type as it reimports the related targets ([8277743](https://www.github.com/mu88/mu88.Shared/commit/8277743238e6699107ad100a845819dcf7a7a07e))

<a name="0.0.8"></a>
## [0.0.8](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.0.8) (2024-08-02)

### Bug Fixes

* don't enforce using OTLP exporter ([471f916](https://www.github.com/mu88/mu88.Shared/commit/471f9168c7cd14bc80c2c0cda82e85192a2e4aaa))

<a name="0.0.7"></a>
## [0.0.7](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.0.7) (2024-08-02)

<a name="0.0.6"></a>
## [0.0.6](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.0.6) (2024-08-02)

<a name="0.0.5"></a>
## [0.0.5](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.0.5) (2024-08-02)

<a name="0.0.4"></a>
## [0.0.4](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.0.4) (2024-08-02)

<a name="0.0.3"></a>
## [0.0.3](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.0.3) (2024-08-02)

<a name="0.0.2"></a>
## [0.0.2](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.0.2) (2024-08-02)

<a name="0.0.1"></a>
## [0.0.1](https://www.github.com/mu88/mu88.Shared/releases/tag/v0.0.1) (2024-08-02)

### Features

* initialize repo ([6168801](https://www.github.com/mu88/mu88.Shared/commit/616880167c2d3c277985362c8ddc1d6126f32e92))

