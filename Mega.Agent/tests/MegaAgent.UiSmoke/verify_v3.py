"""第三轮验收：输入框文本顶左对齐 + 斜杠技能项完整显示（名称左、说明右）。

安全性：只通过 MegaAgent.exe 的 PID 定位窗口（不再按标题匹配，避免撞上其他含
"MegaAgent" 字样的窗口）；只用 UIA Invoke/Value 模式，不发送键盘/鼠标事件。
"""
import csv
import ctypes
import io
import subprocess
import sys
import time

sys.path.insert(0, r"E:\dotnet_ai_sample\Mega.Agent\tests\MegaAgent.UiSmoke")

from PIL import ImageGrab
from pywinauto import Desktop

user32 = ctypes.windll.user32


class RECT(ctypes.Structure):
    _fields_ = [("left", ctypes.c_long), ("top", ctypes.c_long),
                ("right", ctypes.c_long), ("bottom", ctypes.c_long)]


def megaagent_pids():
    out = subprocess.run(["tasklist", "/FI", "IMAGENAME eq MegaAgent.exe", "/FO", "CSV"],
                         capture_output=True).stdout.decode("gbk", errors="replace")
    pids = set()
    for row in csv.reader(io.StringIO(out)):
        if len(row) >= 2 and row[0].lower() == "megaagent.exe":
            pids.add(int(row[1]))
    return pids


def hwnd_of(pid):
    res = []
    proc = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)

    def cb(h, lp):
        wpid = ctypes.c_ulong()
        user32.GetWindowThreadProcessId(h, ctypes.byref(wpid))
        if wpid.value == pid and user32.GetWindowTextLengthW(h) > 0:
            res.append(h)
        return True

    user32.EnumWindows(proc(cb), None)
    return res


def shot(hwnd, name):
    r = RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(r))
    ImageGrab.grab(bbox=(r.left, r.top, r.right, r.bottom)).save(
        rf"E:\dotnet_ai_sample\Mega.Agent\shots\{name}.png")
    print("shot:", name)


def main():
    pids = megaagent_pids()
    print("MegaAgent PIDs:", pids)
    if not pids:
        print("NO PROCESS")
        return
    hs = [h for p in pids for h in hwnd_of(p)]
    if not hs:
        print("NO WINDOW")
        return
    hwnd = hs[0]
    user32.SetForegroundWindow(hwnd)
    time.sleep(1.0)

    win = Desktop(backend="uia").window(handle=hwnd)

    # 1) 输入框文本顶左对齐：比较输入框 top 与文本 top 的间距
    box = win.descendants(control_type="Edit")[0]
    br = box.rectangle()
    print(f"输入框 rect: ({br.left},{br.top})-({br.right},{br.bottom}) h={br.height()}")

    box.set_edit_text("字体对齐测试\n第二行")
    time.sleep(0.8)
    docs = win.descendants(control_type="Document")
    texts = [t for t in win.descendants(control_type="Text")
             if "字体对齐测试" in t.window_text()]
    if texts:
        tr = texts[0].rectangle()
        print(f"文本 rect top={tr.top}  → 距输入框顶部 {tr.top - br.top}px"
              f"（越小越靠顶部对齐），左边距 {tr.left - br.left}px")
    shot(hwnd, "30-input-topleft")

    # 2) 斜杠技能项：每项应完整显示且带上说明
    box.set_edit_text("/")
    time.sleep(1.0)
    shot(hwnd, "31-slash-items")
    names = ["翻译助手", "代码审查", "中英润色"]
    descs = ["中英互译", "从正确性", "把口语化"]
    got_names, got_descs = [], []
    for t in win.descendants(control_type="Text"):
        w = t.window_text()
        if w in names:
            got_names.append((w, t.rectangle().height()))
        if any(w.startswith(d) for d in descs):
            got_descs.append((w, t.rectangle().height()))
    print("技能名 TextBlock:", got_names)
    print("技能说明 TextBlock:", got_descs)
    box.set_edit_text("")
    time.sleep(0.5)


if __name__ == "__main__":
    main()
