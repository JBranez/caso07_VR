Las Palmeras VR - Unity 6.5 local dependency cache

This folder is populated by Tools/Preparar_y_Abrir_Unity65.bat on the first run.
The launcher downloads official Unity UPM tarballs, validates their SHA-1 checksums, and keeps them here so later package resolution can use local file: references instead of the online registry catalogue.

Required tarballs:
- com.unity.inputsystem 1.20.1
- com.unity.xr.management 4.6.1
- com.unity.xr.legacyinputhelpers 3.0.1
- com.unity.xr.core-utils 2.6.0

The root manifest references them as file:../LocalPackages/<package>.tgz. In Unity's project manifest, this relative path is resolved from the Packages folder; LocalPackages is its sibling. XR Management's package.json declares XR Core Utilities and XR Legacy Input Helpers as dependencies; the versions above satisfy those ranges and Cardboard's direct requirements. Their remaining dependencies are built-in Unity modules. uGUI 2.5.0 is a Core package distributed with Editor 6000.5 and is therefore verified inside Unity rather than fetched as a standalone tarball; its required audio, IMGUI, physics, physics2d and UI modules are declared in the project manifest.

The tarballs are intentionally not bundled in this source archive because the packaging environment cannot reach Unity's package CDN. Internet access to download.packages.unity.com over HTTPS/443 is required for the first run. If the PC is offline, obtain the four exact .tgz files on another connected device and copy them into this folder; the launcher verifies each checksum before using it. Do not delete this folder after download.
