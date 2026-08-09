import subprocess
import time

exe_path = r"C:\Users\admin\source\gemmi-engine\Gemmi.App\bin\Debug\net10.0-windows\Gemmi.App.exe"

print(f"Launching {exe_path}...")
proc = subprocess.Popen([exe_path], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)

time.sleep(2)
poll = proc.poll()
if poll is None:
    print("SUCCESS: Gemmi.App process is running cleanly and window is visible!")
else:
    stdout, stderr = proc.communicate()
    print(f"FAILED: Exit code {poll}")
    print("STDOUT:", stdout)
    print("STDERR:", stderr)
