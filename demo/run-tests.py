import os, subprocess, sys

DOTNET = r"C:\Program Files\dotnet\dotnet.exe"
PROJ = r"E:\电子病历\Element-Blazor-master\test\Element.ComponentTests"

env = os.environ.copy()
env.update({
    "APPDATA": r"C:\Users\Administrator\AppData\Roaming",
    "LOCALAPPDATA": r"C:\Users\Administrator\AppData\Local",
    "ProgramData": r"C:\ProgramData",
    "SystemRoot": r"C:\Windows",
    "SystemDrive": "C:",
    "USERPROFILE": r"C:\Users\Administrator",
    "ALLUSERSPROFILE": r"C:\ProgramData",
    "PUBLIC": r"C:\Users\Public",
    "ProgramFiles": r"C:\Program Files",
    "ProgramFiles(x86)": r"C:\Program Files (x86)",
    "CommonProgramFiles": r"C:\Program Files\Common Files",
    "CommonProgramFiles(x86)": r"C:\Program Files (x86)\Common Files",
    "TEMP": r"C:\Users\Administrator\AppData\Local\Temp",
    "TMP": r"C:\Users\Administrator\AppData\Local\Temp",
    "NUGET_PACKAGES": r"C:\Users\Administrator\.nuget\packages",
})

args = [DOTNET, "test", "--nologo"] + sys.argv[1:]
r = subprocess.run(args, cwd=PROJ, env=env, capture_output=True, text=True,
                   encoding="utf-8", errors="replace")
out = (r.stdout or "") + "\n" + (r.stderr or "")
print("EXIT", r.returncode)
lines = [l for l in out.splitlines() if l.strip()]
for l in lines[-25:]:
    print(l)
failed = [l for l in lines if "Failed " in l or "失败" in l and "总计" not in l]
if failed:
    print("---- FAILED LINES ----")
    for l in failed[:30]:
        print(l)
