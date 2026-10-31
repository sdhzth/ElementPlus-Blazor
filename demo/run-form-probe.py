import os, subprocess, time, urllib.request, sys

DOTNET = r"C:\Program Files\dotnet\dotnet.exe"
NODE = r"C:\Users\Administrator\.workbuddy\binaries\node\versions\22.22.2-3\node.exe"
DEMO = r"E:\电子病历\Element-Blazor-master\demo\Element.Blazor.Demo"
DEMODIR = r"E:\电子病历\Element-Blazor-master\demo"
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

args = [DOTNET, "run", "--no-build", "--urls", "http://127.0.0.1:5199"]
extra = [a for a in sys.argv[1:] if a != "build"]
if "build" in sys.argv[1:]:
    args = [DOTNET, "run", "--urls", "http://127.0.0.1:5199"]
probe = extra[0] if extra else "probe-form-input.cjs"

log = open(os.path.join(DEMODIR, "demo5199b.log"), "wb")
proc = subprocess.Popen(args, cwd=DEMO, env=env, stdout=log, stderr=subprocess.STDOUT)
print("started pid", proc.pid, "args", args, flush=True)

up = False
for i in range(60):
    try:
        with urllib.request.urlopen("http://127.0.0.1:5199/form", timeout=3) as r:
            if r.status == 200:
                up = True
                break
    except Exception:
        pass
    if proc.poll() is not None:
        print("SERVER_EXITED code", proc.returncode, flush=True)
        break
    time.sleep(2)
print("SERVER_UP" if up else "SERVER_TIMEOUT", flush=True)

if up:
    env2 = os.environ.copy()
    env2["NODE_PATH"] = r"C:\Users\Administrator\.workbuddy\binaries\node\workspace\node_modules"
    r = subprocess.run([NODE, probe], cwd=DEMODIR, env=env2,
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    print("---- PROBE STDOUT ----", flush=True)
    print(r.stdout, flush=True)
    if r.stderr.strip():
        print("---- PROBE STDERR ----", flush=True)
        print(r.stderr[-3000:], flush=True)
print("done", flush=True)
