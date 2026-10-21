#!/usr/bin/env bash
# Compile-check the Unity runtime scripts without opening the Editor.
# Requires: dotnet SDK 8+, Unity 6.4 installation (for managed DLLs).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY_APP="${UNITY_APP:-/Volumes/data/unity-engine/6000.4.2f1/Unity.app}"
MANAGED="$UNITY_APP/Contents/Resources/Scripting/Managed/UnityEngine"
UI="$(find "$UNITY_APP/Contents/Resources/PackageManager/ProjectTemplates" -name UnityEngine.UI.dll 2>/dev/null | head -n 1 || true)"

if [[ ! -d "$MANAGED" ]]; then
  echo "Unity managed assemblies not found: $MANAGED" >&2
  echo "Set UNITY_APP to your Unity 6.4 app bundle." >&2
  exit 2
fi

OUT="$ROOT/.cache/unity-runtime-check"
mkdir -p "$OUT"

python3 - "$ROOT" "$MANAGED" "$UI" "$OUT" <<'PY'
import os, sys
root, managed, ui, out = sys.argv[1:5]
dlls = [os.path.join(managed, f) for f in sorted(os.listdir(managed))
        if f.startswith('UnityEngine') and f.endswith('.dll') and not f.startswith('UnityEditor')]
if ui and os.path.exists(ui):
    dlls.append(ui)
refs = '\n'.join(
    '    <Reference Include="%s"><HintPath>%s</HintPath></Reference>' %
    (os.path.splitext(os.path.basename(d))[0], d) for d in dlls)
scripts = os.path.join(root, 'unity/AshenDao/Assets/Scripts')
project = f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <Nullable>disable</Nullable>
    <NoWarn>CS0649;CS0618;CS0414</NoWarn>
    <AssemblyName>AshenDao.CompileCheck</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="{scripts}/**/*.cs" />
{refs}
  </ItemGroup>
</Project>'''
os.makedirs(out, exist_ok=True)
open(os.path.join(out, 'runtime.csproj'), 'w').write(project)
print('Generated csproj with', len(dlls), 'Unity references')
PY

dotnet build "$OUT/runtime.csproj" -v quiet
