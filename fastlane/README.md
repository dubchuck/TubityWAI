fastlane documentation
----

# Installation

Make sure you have the latest version of the Xcode command line tools installed:

```sh
xcode-select --install
```

For _fastlane_ installation instructions, see [Installing _fastlane_](https://docs.fastlane.tools/#installing-fastlane)

# Available Actions

## iOS

### ios create_app

```sh
[bundle exec] fastlane ios create_app
```

Create App Store Connect Record

### ios create_iaps

```sh
[bundle exec] fastlane ios create_iaps
```

Create Remove Ads In-App Purchase

Create every In-App Purchase in IAP_PRODUCTS on App Store Connect

### ios create_remove_ads_iap

```sh
[bundle exec] fastlane ios create_remove_ads_iap
```



### ios create_achievements

```sh
[bundle exec] fastlane ios create_achievements
```

Create Game Center Achievements on App Store Connect

### ios build_ipa

```sh
[bundle exec] fastlane ios build_ipa
```

Build and sign the App Store IPA using manual cert/sigh

### ios upload_to_testflight

```sh
[bundle exec] fastlane ios upload_to_testflight
```

Upload the build to TestFlight AND push local metadata & screenshots

### ios upload_metadata

```sh
[bundle exec] fastlane ios upload_metadata
```

Upload only local metadata and screenshots to App Store Connect

### ios upload_build_only

```sh
[bundle exec] fastlane ios upload_build_only
```

Upload only the IPA build to TestFlight

### ios build_tvos_ipa

```sh
[bundle exec] fastlane ios build_tvos_ipa
```

Build and sign the tvOS IPA using gym

### ios upload_tvos_to_testflight

```sh
[bundle exec] fastlane ios upload_tvos_to_testflight
```

Upload tvOS build to TestFlight

----


## Android

### android upload_to_play_store

```sh
[bundle exec] fastlane android upload_to_play_store
```

Upload Android App Bundle (AAB) to Google Play Console Internal Track

----


## Mac

### mac upload_mac_to_app_store

```sh
[bundle exec] fastlane mac upload_mac_to_app_store
```

Upload macOS .pkg installer package to App Store Connect / TestFlight

----

This README.md is auto-generated and will be re-generated every time [_fastlane_](https://fastlane.tools) is run.

More information about _fastlane_ can be found on [fastlane.tools](https://fastlane.tools).

The documentation of _fastlane_ can be found on [docs.fastlane.tools](https://docs.fastlane.tools).
