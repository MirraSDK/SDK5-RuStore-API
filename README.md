### About RuStore API for MirraSDK5

RuStore providers for MirraSDK5: payments, reviews, player sign-in, and optional in-app updates and remote config.

Built on the RuStore Unity SDK from RuStore's package registry: Pay 11.2.0, Review 10.5.2, Core 10.5.1. Version 2.0.0 replaces the bundled 10.0.x packages, which can no longer download their Android libraries.

A setup check stops an Android build of the RuStore configuration while something is missing and lists what to fix, so a game cannot ship with payments silently turned off.

### Features

1. Payments on RuStore Pay 11.
2. Purchases restored after a wiped save.
3. In-app reviews.
4. In-app updates without game code.
5. Remote config as MirraSDK Flags.
6. Player sign-in through RuStore.
7. Setup check before every build.
8. One-step migration from 1.x.

### Requirements

1. Unity 2022.3.38 or newer.
2. MirraSDK5 5.2.5 or newer.
3. External Dependency Manager for Unity.
4. Android Minimum API Level 24 or higher.
5. Application Entry Point set to Activity.
6. A game registered in RuStore Console.

### Limitations

1. Payments do not work with the GameActivity entry point.
2. Subscriptions, promotions and coupons are not exposed.
3. Push notifications are not included.
4. The player profile is empty: RuStore reports only whether the player is signed in.
5. `ShareGame` is not implemented.

### How to install

1. Open `MirraSDK5 > Open Toolkit`, go to the package list, select `RuStore API for MirraSDK5` and press its install button. The Toolkit adds the RuStore registry, the RuStore packages and this package.
2. Optional modules have their own buttons on the same card: `Install RuStore In-app Updates` and `Install RuStore Remote Config`.
3. To move a project from 1.x, press the update button on the same card, then run `MirraSDK5 > RuStore > Fix setup`.

Manual install: in `Project Settings > Package Manager` add a scoped registry named `RuStore Nexus` with the URL `https://nexus-external.rustore.ru/repository/npm-unity-rustore-exposed/` and the scope `ru.rustore`. Install `ru.rustore.pay` 11.2.0 and `ru.rustore.review` 10.5.2 from it, then add this package by git URL:

```
https://github.com/MirraSDK/SDK5-RuStore-API.git
```

### How to use

Select the Android platform and choose `RuStore` as the build configuration in the Toolkit. Run `MirraSDK5 > RuStore > Fix setup`. In the settings it opens (`Window > RuStoreSDK > Settings > PayClient`) enter `Console Application Id` and `Deeplink Scheme`. In the Toolkit list the product ids in `RuStorePayments > ProductsJson`, for example `{ "Values": ["coins100", "noads"] }`. Then build: the setup check reports anything still missing.

Game code stays plain MirraSDK. Call `RestorePurchases` on every launch and grant each pending product, so a purchase interrupted by a crash or lost with a wiped save reaches the player:

```csharp
MirraSDK.Payments.RestorePurchases((restoreData) => {
    foreach (string productTag in restoreData.PendingProducts) {
        restoreData.RestoreProduct(productTag, () => GiveProduct(productTag));
    }
});
```

Optional modules are set in the Toolkit: `RuStorePlatformInteractions > UpdateMode` (`Disabled`, `Flexible`, `Immediate`, `Silent`) and `RuStoreFlags > AppId`.

Detailed setup, behaviour and troubleshooting - see [RuStore](https://mirrasdk.github.io/Documentation/en/unity/mirrasdk/rustore).
