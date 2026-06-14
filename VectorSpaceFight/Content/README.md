# MonoGame content

Source assets and `Content.mgcb` live here. Runtime loads compiled `.xnb` files from the `Content/` folder next to the game executable.

## Prebuilt content (Linux / macOS / linux-arm64 / CI)

Compiled DesktopGL assets are checked in under `Prebuilt/DesktopGL/`. Non-Windows builds set `UsePrebuiltContent=true` automatically so MGCB (and Wine on Linux) are not required.

After changing `Content.mgcb` or `Shaders/CRT.fx`, rebuild prebuilt content on Windows:

```powershell
./scripts/build-content.ps1
```

Then commit the updated files under `Prebuilt/DesktopGL/`.

## Windows development

By default, Windows builds compile content from `Content.mgcb` during `dotnet build`. To test the prebuilt path locally:

```powershell
dotnet build -p:UsePrebuiltContent=true
```
