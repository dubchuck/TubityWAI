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

### ios build_ipa

```sh
[bundle exec] fastlane ios build_ipa
```

Build and sign the App Store IPA using manual cert/sigh

### ios upload_to_testflight

```sh
[bundle exec] fastlane ios upload_to_testflight
```

Upload the build to TestFlight

----


## Android

### android upload_to_play_store

```sh
[bundle exec] fastlane android upload_to_play_store
```

Upload Android App Bundle (AAB) to Google Play Console Internal Track

----

This README.md is auto-generated and will be re-generated every time [_fastlane_](https://fastlane.tools) is run.

More information about _fastlane_ can be found on [fastlane.tools](https://fastlane.tools).

The documentation of _fastlane_ can be found on [docs.fastlane.tools](https://docs.fastlane.tools).
