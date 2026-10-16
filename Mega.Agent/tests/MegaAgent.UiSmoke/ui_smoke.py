"""MegaAgent UI 冒烟验证脚本（pywinauto + Pillow）。

用法::
    <venv-python> ui_smoke.py <step>

依赖: pip install pywinauto pillow
说明: 通过 UIA 驱动真实窗口，验证折叠侧栏、头像菜单、斜杠技能、Markdown 渲染与发送链路。
"""
import ctypes
import sys
import time

from PIL import ImageGrab
from pywinauto import Desktop

SHOT_DIR = r"E:\dotnet_ai_sample\Mega.Agent\shots"

user32 = ctypes.windll.user32
EnumProc = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)


def find_hwnds(substr: str):
    res = []

    def cb(h, lp):
        n = user32.GetWindowTextLengthW(h)
        if n:
            buf = ctypes.create_unicode_buffer(n + 1)
            user32.GetWindowTextW(h, buf, n + 1)
            if substr in buf.value:
                res.append(h)
        return True

    user32.EnumWindows(EnumProc(cb), None)
    return res


def main_window(focus: bool = True):
    hs = find_hwnds("MegaAgent")
    if not hs:
        return None
    if focus:
        user32.ShowWindow(hs[0], 9)
        user32.SetForegroundWindow(hs[0])
        time.sleep(0.8)
    return Desktop(backend="uia").window(handle=hs[0])


def shot(name: str):
    ImageGrab.grab().save(rf"{SHOT_DIR}\{name}.png")
    print("shot:", name)


def rect_of(win):
    r = win.rectangle()
    print(f"window rect: {r.width()}x{r.height()}")
    return r


def buttons(win):
    return {b.window_text(): b for b in win.descendants(control_type="Button")}


def wait_stream_done(win, timeout=120):
    for i in range(timeout // 3):
        time.sleep(3)
        texts = [t.window_text() for t in win.descendants(control_type="Text")]
        if i > 0 and not any("生成中" in t for t in texts):
            print(f"stream finished ~{(i + 1) * 3}s")
            return True
    print("stream timeout")
    return False
