"""第四轮验收：模型下拉可选 deepseek-flash / deepseek-v4-pro；头像菜单不再一闪而过。

安全性：窗口按 MegaAgent.exe 的 PID 定位；点击用 PostMessage 投递到目标窗口
（不移动真实光标、不影响其它程序）。
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
WM_LBUTTONDOWN, WM_LBUTTONUP = 0x0201, 0x0202


class POINT(ctypes.Structure):
    _fields_ = [("x", ctypes.c_long), ("y", ctypes.c_long)]


class RECT(ctypes.Structure):
    _fields_ = [("left", ctypes.c_long), ("top", ctypes.c_long),
                ("right", ctypes.c_long), ("bottom", ctypes.c_long)]


def megaagent_pids():
    out = subprocess.run(["tasklist", "/FI", "IMAGENAME eq MegaAgent.exe", "/FO", "CSV"],
                         capture_output=True).stdout.decode("gbk", errors="replace")
    return {int(r[1]) for r in csv.reader(io.StringIO(out))
            if len(r) >= 2 and r[0].lower() == "megaagent.exe"}


def hwnds_of(pids):
    res = []
    proc = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)

    def cb(h, lp):
        pid = ctypes.c_ulong()
        user32.GetWindowThreadProcessId(h, ctypes.byref(pid))
        if pid.value in pids and user32.GetWindowTextLengthW(h) > 0:
            res.append(h)
        return True

    user32.EnumWindows(proc(cb), None)
    return res


def post_click(hwnd, sx, sy):
    """把屏幕坐标的点击合成投递到目标窗口，不移动真实光标。"""
    pt = POINT(sx, sy)
    user32.ScreenToClient(hwnd, ctypes.byref(pt))
    lparam = (pt.y << 16) | (pt.x & 0xFFFF)
    user32.PostMessageW(hwnd, WM_LBUTTONDOWN, 1, lparam)
    time.sleep(0.08)
    user32.PostMessageW(hwnd, WM_LBUTTONUP, 0, lparam)


def shot(hwnd, name):
    r = RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(r))
    ImageGrab.grab(bbox=(r.left, r.top, r.right, r.bottom)).save(
        rf"E:\dotnet_ai_sample\Mega.Agent\shots\{name}.png")
    print("shot:", name)


def texts_of(win):
    return [t.window_text() for t in win.descendants(control_type="Text")]


def main():
    pids = megaagent_pids()
    if not pids:
        print("NO PROCESS")
        return
    hs = hwnds_of(pids)
    if not hs:
        print("NO WINDOW")
        return
    hwnd = hs[0]
    user32.SetForegroundWindow(hwnd)
    time.sleep(1.0)
    win = Desktop(backend="uia").window(handle=hwnd)

    # 1) 模型下拉选项
    combos = win.descendants(control_type="ComboBox")
    print("ComboBox 数量:", len(combos), [c.window_text() for c in combos])
    model_combo = combos[0]
    print("当前选中模型:", model_combo.window_text())
    try:
        model_combo.expand()
        time.sleep(0.8)
        items = [i.window_text() for i in win.descendants(control_type="ListItem")]
        print("下拉选项:", items)
        shot(hwnd, "40-model-list")
        model_combo.collapse()
        time.sleep(0.4)
    except Exception as ex:
        print("expand 失败:", ex)

    # 2) 头像菜单：合成点击后应持续显示
    wr = win.rectangle()
    post_click(hwnd, wr.right - 45, wr.top + 79)   # 顶栏右上角头像中心
    time.sleep(1.8)
    shot(hwnd, "41-avatar-menu-stable")
    t = texts_of(win)
    print("1.8s 后菜单是否仍在:", [x for x in t if "模型设置" in x or "技能选择" in x])

    time.sleep(2.5)
    t2 = texts_of(win)
    print("4.3s 后菜单是否仍在:", [x for x in t2 if "模型设置" in x or "技能选择" in x])
    shot(hwnd, "42-avatar-menu-after4s")


if __name__ == "__main__":
    main()
