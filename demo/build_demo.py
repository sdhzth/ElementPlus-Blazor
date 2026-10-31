import os, subprocess, sys

DOTNET = r"C:\Program Files\dotnet\dotnet.exe"
DEMO = r"E:\电子病历\Element-Blazor-master\demo\Element.Blazor.Demo"
PROJ = r"E:\电子病历\Element-Blazor-master"

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

args = [DOTNET, "build", DEMO, "-c", "Debug", "--nologo"]
log = open(os.path.join(PROJ, "demo", "build_demo.log"), "wb")
proc = subprocess.run(args, cwd=PROJ, env=env, stdout=log, stderr=subprocess.STDOUT)
print("EXIT", proc.returncode, flush=True)
